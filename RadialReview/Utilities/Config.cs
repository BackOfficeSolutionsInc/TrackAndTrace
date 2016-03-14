using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using RadialReview.Models.Enums;

namespace RadialReview.Utilities
{
	public class Config
	{

		public static string BaseUrl(OrganizationModel organization)
		{
            if (HttpContext.Current != null)
            {
                try{
                    var strPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
                    return HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
                }catch (Exception){
                    //Skip
                }
            }
            switch (GetEnv())
				{
					case Env.local_sqlite:
						return "http://localhost:2200/";
					case Env.local_mysql:
						return "http://localhost:2200/";
					case Env.production:
						if (organization==null)
							return "https://review.radialreview.com/";

						switch (organization.Settings.Branding)
						{
							case BrandingType.RadialReview:
								return "https://review.radialreview.com/";
							case BrandingType.RoundTable:
								return "https://radialroundtable.com/";
							default:
								throw new ArgumentOutOfRangeException();
						}
					default:
						throw new ArgumentOutOfRangeException();
				}
		}

		public static string ProductName(OrganizationModel organization = null)
		{
			var org = organization.NotNull(x => x);
			if (org != null)
			{
				switch (org.Settings.Branding)
				{
					case BrandingType.RadialReview:
						return GetAppSetting("ProductName_Review", "Traction® Tools");
					case BrandingType.RoundTable:
                        return GetAppSetting("ProductName_Roundtable", "Traction® Tools");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			try
			{
				if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview"))
					return GetAppSetting("ProductName_Review", "Traction Tools");
				else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable"))
					return GetAppSetting("ProductName_Roundtable", "Traction Tools");
			}
			catch (Exception)
			{
				//Fall back...
			}
			return GetAppSetting("ProductName_Review", "Radial Review");
		}
		public static string ReviewName(OrganizationModel organization = null)
		{
			if (organization != null)
			{
				switch (organization.Settings.Branding)
				{
					case BrandingType.RadialReview:
						return GetAppSetting("ReviewName_Review", "Review");
					case BrandingType.RoundTable:
						return GetAppSetting("ReviewName_Roundtable", "Round Table");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			try
			{
				if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview"))
					return GetAppSetting("ReviewName_Review", "Review");
				else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable")) 
					return GetAppSetting("ReviewName_Roundtable", "Round Table");
			}
			catch (Exception)
			{
				//Fall back...
			}
			return GetAppSetting("ReviewName_Review", "Review");
		}

		public static bool IsLocal()
		{
			switch (GetEnv())
			{
                case Env.local_test_sqlite: return true;
                case Env.local_sqlite:      return true;
				case Env.local_mysql:	    return true;
                case Env.production:        return false;
				default:throw new ArgumentOutOfRangeException();
			}
		}

		public static string GetAppSetting(string key,string deflt=null)
		{
			var config = System.Configuration.ConfigurationManager.AppSettings;
			return config[key] ?? deflt;
		}

		public static Env GetEnv()
		{
			Env result;
			if (Enum.TryParse(GetAppSetting("Env").ToLower(), out result))
			{
				return result;
			}
			throw new Exception("Invalid Environment");
		}

		public static string GetSecret()
		{
			return GetAppSetting("sha_secret");
		}

		public static bool OptimizationEnabled()
		{
			switch (GetEnv())
			{
				case Env.local_sqlite: return GetAppSetting("OptimizeBundles", "False").ToBoolean();
				case Env.local_mysql: return GetAppSetting("OptimizeBundles", "False").ToBoolean();
				case Env.production: return true;
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public static string GetTrelloKey()
		{
			return GetAppSetting("TrelloKey");
		}

		public static class Basecamp
		{
			/*public static string GetUrl()
			{
				return GetAppSetting("BasecampUrl");
			}*/

			public static string GetUserAgent()
			{
				switch (GetEnv())
				{
					case Env.local_mysql: goto case Env.local_sqlite;
					case Env.local_sqlite: return GetAppSetting("BasecampTestApp");
					case Env.production: return GetAppSetting("BasecampApp");
					default: throw new ArgumentOutOfRangeException();
				}
			}

			public static BCXAPI.Service GetService(OrganizationModel organization)
			{
				string key, secret, app;
				var redirect = BaseUrl(organization) + "Callback/Basecamp";
				switch (GetEnv())
				{
					case Env.local_mysql:
						goto case Env.local_sqlite;
					case Env.local_sqlite:
						{
							key = GetAppSetting("BasecampTestKey");
							secret = GetAppSetting("BasecampTestSecret");
							break;
						}
					case Env.production:
						{
							key = GetAppSetting("BasecampKey");
							secret = GetAppSetting("BasecampSecret");
							break;
						}
					default:
						throw new ArgumentOutOfRangeException();
				}
				return new BCXAPI.Service(key, secret, redirect, GetUserAgent());
			}
		}

		public static bool SendEmails()
		{
			switch (GetEnv())
			{
				case Env.local_mysql:	return GetAppSetting("SendEmail_Debug","false").ToBooleanJS();
                case Env.local_sqlite:  return GetAppSetting("SendEmail_Debug", "false").ToBooleanJS();
				case Env.production:	return true;
				default:				throw new ArgumentOutOfRangeException();
			}
		}

        public static string PaymentSpring_PublicKey(bool forceUseTest = false)
        {
            if (forceUseTest)
                return GetAppSetting("PaymentSpring_PublicKey_Test");

            switch (GetEnv()) {
                case Env.local_test_sqlite: return GetAppSetting("PaymentSpring_PublicKey_Test");
                case Env.local_mysql: return GetAppSetting("PaymentSpring_PublicKey_Test");
                case Env.local_sqlite: return GetAppSetting("PaymentSpring_PublicKey_Test");
                case Env.production: return GetAppSetting("PaymentSpring_PublicKey");
                default: throw new ArgumentOutOfRangeException();
            }
        }
        [Obsolete("Be careful with private keys")]
        public static string PaymentSpring_PrivateKey(bool forceUseTest=false)
        {
            if (forceUseTest)
                return GetAppSetting("PaymentSpring_PrivateKey_Test");

            switch (GetEnv()) {
                case Env.local_test_sqlite: return GetAppSetting("PaymentSpring_PrivateKey_Test");
                case Env.local_mysql: return GetAppSetting("PaymentSpring_PrivateKey_Test");
                case Env.local_sqlite: return GetAppSetting("PaymentSpring_PrivateKey_Test");
                case Env.production: return GetAppSetting("PaymentSpring_PrivateKey");
                default: throw new ArgumentOutOfRangeException();
            }
        }

		public static string VideoConferenceUrl(string resource=null)
		{
			var server =  GetAppSetting("VideoConferenceServer").TrimEnd('/');
			if (resource != null){
				server = server + "/" + resource.TrimStart('/');
			}
			return server;
			/*
			switch (GetEnv())
			{
				case Env.local_mysql:   return server;
				case Env.local_sqlite:	return GetAppSetting("VideoConferenceServer");
				case Env.production:	return GetAppSetting("VideoConferenceServer");
				default: throw new ArgumentOutOfRangeException();
			}*/
		}

		public static string GetMandrillGoogleAnalyticsDomain()
		{
			return GetAppSetting("Mandrill_GoogleAnalyticsDomain", null);
		}

		/*internal static string PaymentEmail()
		{
			throw new NotImplementedException();
		} */
        public static string GetAccessLogDir()
        {
            switch (GetEnv())
            {
                case Env.local_mysql: return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+@"\IISExpress\Logs\RadialReview\";
                case Env.local_sqlite: return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\IISExpress\Logs\RadialReview\";
                case Env.production: return @"C:\inetpub\logs\LogFiles\W3SVC1\";
                default: throw new ArgumentOutOfRangeException();
            }
        }

		public static string NotesUrl()
		{
			return GetAppSetting("NotesServer", "https://notes.traction.tools");
		}

		internal static string NoteApiKey()
		{
			return GetAppSetting("NotesServer_ApiKey");
		}

		public class RedisConfig
		{
			public string Server { get; set; }
			public int Port { get; set; }
			public string Password { get; set; }
			public string ChannelName { get; set; }
		}

		public static RedisConfig Redis(string channel)
		{
			string server;
			switch(GetEnv()){
				case Env.local_mysql:  server = "127.0.0.1";break;
				case Env.local_sqlite: server = "127.0.0.1";break;
				case Env.production:   server = GetAppSetting("RedisSignalR_Server", null);break;
				default: throw new ArgumentOutOfRangeException();
			}

			return new RedisConfig(){
				Server = server,
				ChannelName = channel,
				Password = GetAppSetting("RedisSignalR_Password", null),
				Port = GetAppSetting("RedisSignalR_Port", "6379").ToInt()
			};
			/*
			switch (GetEnv())
			{
				case Env.local_mysql: return GetAppSetting("RedisSignalR-server",null);
				case Env.local_sqlite: return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\IISExpress\Logs\RadialReview\";
				case Env.production: return @"C:\inetpub\logs\LogFiles\W3SVC1\";
				default: throw new ArgumentOutOfRangeException();
			}*/
		}
	}
}