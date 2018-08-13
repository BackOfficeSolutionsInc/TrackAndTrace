using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities {
	public class CookieReader {

		public static HttpWebRequest CreateRequest(string url, bool refreshCookies = true) {
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.CookieContainer = request.CookieContainer ?? new CookieContainer();
			foreach (var c in ReadCookies(url, refreshCookies)) {
				request.CookieContainer.Add(c);
			}
			return request;
		}


		public static string ExecuteGetRequest(string url, Action<HttpWebRequest> requestManipulations = null,bool refreshCookies=true) {
			Log.Info(url);
			var request = CreateRequest(url,refreshCookies);
			request.AutomaticDecompression = DecompressionMethods.GZip;
			requestManipulations?.Invoke(request);
			//var diag = JsonConvert.SerializeObject(request, Formatting.Indented);
			using (var response = (HttpWebResponse)request.GetResponse()) {
				if (response.Cookies != null) {
					foreach (Cookie c in response.Cookies) {
						Cache[ReformatHostName(url)].Add(c);
					}
				}
				var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
				return responseString;
			}
		}


		private static Dictionary<string, List<Cookie>> Cache = new Dictionary<string, List<Cookie>>();

		private static IEnumerable<Cookie> TryLookup(string hostName) {
			var originalHostname = hostName;

			if (Cache.ContainsKey(hostName))
				return Cache[hostName];

			var dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Default\Cookies";
			if (!System.IO.File.Exists(dbPath))
				throw new System.IO.FileNotFoundException("Cant find cookie store", dbPath); // race condition, but i'll risk it

			var builder = new List<Cookie>();
			var connectionString = "Data Source=" + dbPath + ";pooling=true";
			foreach (var hn in new[] { "." + hostName, hostName }) {
				using (var conn = new System.Data.SQLite.SQLiteConnection(connectionString))
				using (var cmd = conn.CreateCommand()) {

					var prm = cmd.CreateParameter();
					prm.ParameterName = "hostName";
					prm.Value = hn;
					cmd.Parameters.Add(prm);

					//var prm2 = cmd.CreateParameter();
					//prm2.ParameterName = "utc";
					//prm2.Value = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds*10000; /*donno why they use 10ths of ms but... */
					//cmd.Parameters.Add(prm2);

					cmd.CommandText = "SELECT name,encrypted_value,path,expires_utc FROM cookies WHERE host_key = @hostName";
					//cmd.CommandText = "SELECT name,encrypted_value FROM cookies WHERE host_key = @hostName and (has_expires=0 or expires_utc > @utc)";
					//select * from cookies where host_key like "%aws%" and (has_expires=0 or expires_utc > 15314593380000);
					try {
						conn.Open();
						using (var reader = cmd.ExecuteReader()) {
							while (reader.Read()) {
								var encryptedData = (byte[])reader[1];
								var decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
								var name = reader.GetString(0);
								var path = reader.GetString(2);
								var value = Encoding.ASCII.GetString(decodedData); // Looks like ASCII
								var expires = (reader.GetInt64(3)/ 10000).ToDateTime();

							//	if (/*!builder.Any(x => x.Name == name) &&*/ name != "noflush_LogsConsole_streamsSortPrefs")
									builder.Add(new Cookie(name, value) {
										Domain = hn,
										Path = path,
										//Expires = expires
									});
							}
						}
					} finally {
						conn.Close();
					}
				}
			}
			if (hostName.Count(f => f == '.') > 1) {
				var loc = hostName.IndexOf('.', 1);
				var shorterHostName = hostName.Substring(loc);
				foreach (var cookie in TryLookup(shorterHostName))
					if (!builder.Any(x => x.Name == cookie.Name))
						builder.Add(cookie);
			}

			Cache[originalHostname] = builder;

			return builder;
		}

		private static string ReformatHostName(string hostName) {

			hostName = hostName.ToLower();

			if (hostName.StartsWith("https://"))
				hostName = hostName.Substring(8);
			if (hostName.StartsWith("http://"))
				hostName = hostName.Substring(7);


			var slash = hostName.IndexOf("/");
			if (slash >= 0)
				hostName = hostName.Substring(0, slash);
			return hostName;
		}

		public static IEnumerable<Cookie> ReadCookies(string hostName, bool refresh = false) {
			if (hostName == null)
				throw new ArgumentNullException("hostName");

			var formatted = ReformatHostName(hostName);

			if (refresh && Cache.ContainsKey(formatted)) {
				Cache.Remove(formatted);
			}

			var found = TryLookup(formatted);

			return found.GroupBy(x => Tuple.Create(x.Name, x.Domain))
						.Select(y => y.OrderByDescending(x => x.Expires).First())
						.ToList();

		}



		public static IEnumerable<Tuple<string, string>> Hosts() {
			var dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Default\Cookies";
			if (!System.IO.File.Exists(dbPath))
				throw new System.IO.FileNotFoundException("Cant find cookie store", dbPath); // race condition, but i'll risk it
			var connectionString = "Data Source=" + dbPath + ";pooling=false";
			using (var conn = new System.Data.SQLite.SQLiteConnection(connectionString))
			using (var cmd = conn.CreateCommand()) {

				//cmd.CommandText = "SELECT name,encrypted_value FROM cookies WHERE host_key = @hostName";
				cmd.CommandText = "SELECT name,host_key FROM cookies";
				try {
					conn.Open();
					using (var reader = cmd.ExecuteReader()) {
						while (reader.Read()) {
							var plainText = (string)reader[1];
							yield return Tuple.Create(reader.GetString(0), plainText);
						}
					}
				} finally {
					conn.Close();
				}
			}
		}
	}
}
