using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using RadialReview.Exceptions;
using RadialReview.Models.Payments;
using RadialReview.Utilities;
using Hangfire;
using RadialReview.Hangfire;
using RadialReview.Crosscutting.Schedulers;

namespace RadialReview.Accessors
{
	public class PadAccessor :BaseAccessor
	{
		private static IEnumerable<string> WholeChunks(string str, int chunkSize) {
			for (int i = 0; i < str.Length; i += chunkSize)
				if (str.Length - i >= chunkSize)
					yield return str.Substring(i, chunkSize);
				else
					yield return str.Substring(i);
		}


		[AutomaticRetry(Attempts = 0)]
		[Queue(HangfireQueues.Immediate.ETHERPAD)]
		public static async Task<string> HangfireCreatePad(string padId, string text = null) {
			try {
				var client = new HttpClient();
				//if (!String.IsNullOrWhiteSpace(text))
				//	urlText = "&text=" + WebUtility.UrlEncode(text);

				{
					//Create pad
					var baseUrl = Config.NotesUrl() + "api/1/createPad?apikey=" + Config.NoteApiKey() + "&padID=" + padId ;
					HttpResponseMessage response = await client.GetAsync(baseUrl);
					HttpContent responseContent = response.Content;
					using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
						var result = await reader.ReadToEndAsync();
						int code = Json.Decode(result).code;
						string message = Json.Decode(result).message;
						if (code != 0) {
							throw new PermissionsException("Error " + code + ": " + message);
						}
					}
				}

				if (!String.IsNullOrWhiteSpace(text)) {
					var chunkSize = 100;
					var subtexts = WholeChunks(text, chunkSize);//Enumerable.Range(0, text.Length / chunkSize).Select(i => text.Substring(i * chunkSize, chunkSize)).ToList();
					//Append text to pad
					foreach (var t in subtexts) {
						var urlText = "&text=" + WebUtility.UrlEncode(t);
						var baseUrl = Config.NotesUrl() + "api/1.2.13/appendText?apikey=" + Config.NoteApiKey() + "&padID=" + padId + urlText;
						HttpResponseMessage response = await client.GetAsync(baseUrl);
						HttpContent responseContent = response.Content;
						using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
							var result = await reader.ReadToEndAsync();
							int code = Json.Decode(result).code;
							string message = Json.Decode(result).message;
							if (code != 0) {
								throw new PermissionsException("Error " + code + ": " + message);
							}
						}
					}
				}

				return padId;
			} catch (Exception e) {
				log.Error("Error PadAccessor.CreatePad", e);
				return "err";
			}
		}


		public static async Task<bool> CreatePad(string padid, string text=null){
			Scheduler.Enqueue(() => HangfireCreatePad(padid, text));
			return true;
		}

		public static async Task<string> GetReadonlyPad(string padid) {
			try {
				var client = new HttpClient();

				var baseUrl = Config.NotesUrl() + "api/1/getReadOnlyID?apikey=" + Config.NoteApiKey() + "&padID=" + padid ;
				HttpResponseMessage response = await client.GetAsync(baseUrl);
				HttpContent responseContent = response.Content;
				using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
					var result = await reader.ReadToEndAsync();
					int code = Json.Decode(result).code;
					string message = Json.Decode(result).message;
					if (code != 0) {
						if (message == "padID does not exist") {
							var pad = await CreatePad(padid);
							return await GetReadonlyPad(padid);
						}

						throw new PermissionsException("Error " + code + ": " + message);
					}
					return (string)(Json.Decode(result).data.readOnlyID);
				}
			} catch (Exception e) {
				log.Error("Error PadAccessor.GetReadOnlyID", e);
				return "r.0a198a5362822f17b4690e5e66a6fba3"; //readonly pad for https://notes.traction.tools/p/undefined-1657717875444
			}
		}

		public static async Task<Dictionary<string, HtmlString>> GetHtmls(List<string> padIds) {
			var results = await Task.WhenAll(padIds.Distinct().Select(x => _GetHtml(x)));
			return results.ToDictionary(x => x.Item1, x => x.Item2);
		}
		public static async Task<Dictionary<string, string>> GetTexts(List<string> padIds) {
			var results = await Task.WhenAll(padIds.Distinct().Select(x => _GetText(x)));
			return results.ToDictionary(x => x.Item1, x => x.Item2);
		}

		private static async Task<Tuple<string, HtmlString>> _GetHtml(string padid) {
			var result = await GetHtml(padid);
			return Tuple.Create(padid, result);
		}
		private static async Task<Tuple<string, string>> _GetText(string padid) {
			var result = await GetText(padid);
			return Tuple.Create(padid, result);
		}

		public static async Task<HtmlString> GetHtml(string padid){
			try{
				var client = new HttpClient();
				var baseUrl = Config.NotesUrl() + "api/1/getHTML?apikey=" + Config.NoteApiKey() + "&padID=" + padid;
				HttpResponseMessage response = await client.GetAsync(baseUrl);
				HttpContent responseContent = response.Content;
				using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())){
					var result = await reader.ReadToEndAsync();
					int code = Json.Decode(result).code;
					string message = Json.Decode(result).message;
					if (code != 0){
						if (message == "padID does not exist"){
							return new HtmlString("");
						}
						throw new PermissionsException("Error " + code + ": " + message);
					}

					var html = (string) (Json.Decode(result).data.html);
					html = html.Substring("<!DOCTYPE HTML><html><body>".Length, html.Length - ("</body></html>".Length + "<!DOCTYPE HTML><html><body>".Length));
					return new HtmlString(html);
				}
			}catch (Exception e){
				log.Error("Error PadAccessor.GetHtml", e);
				return new HtmlString("");
			}
		}


		public static async Task<String> GetText(string padid)
		{
			try{
				var client = new HttpClient();
				var baseUrl = Config.NotesUrl() + "api/1/getText?apikey=" + Config.NoteApiKey() + "&padID=" + padid;
				HttpResponseMessage response = await client.GetAsync(baseUrl);
				HttpContent responseContent = response.Content;
				using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())){
					var result = await reader.ReadToEndAsync();
					int code = Json.Decode(result).code;
					string message = Json.Decode(result).message;
					if (code != 0){
						if (message == "padID does not exist"){
							return "";
						}
						throw new PermissionsException("Error " + code + ": " + message);
					}
					return (string) (Json.Decode(result).data.text);
				}
			}
			catch (Exception e){
				log.Error("Error PadAccessor.GetText", e);
				return "";
			}
		}
	}
}