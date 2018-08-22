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
using RadialReview.Models.FirePad;
using RadialReview.Utilities;

//install FireSharp.Serialization.JsonNet 1.1.0
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace RadialReview.Accessors
{
	public class PadAccessor :BaseAccessor
	{
        
		public static async Task<bool> CreatePad(string padid, string text=null,bool firePad=true)
		{
			try{
                //start firepad creation
                if (firePad)
                {
                    var FirePadClient = new FireSharp.FirebaseClient(Config.getFirePadConfig());
                    if (FirePadClient != null)
                    {
                        var data = new FirePadData
                        {
                            padID = padid,
                            text = text ?? "",
                            firepadRef = " "
                        };
                        SetResponse FPResponse = await FirePadClient.SetTaskAsync("FirePad/" + padid, data);
                        FirePadData FPresult = FPResponse.ResultAs<FirePadData>();
                    }
                    else
                    {
                        throw new PermissionsException("Error connecting to firebase");
                    }
                    return true;
                    //end firepad creation
                }
                else
                {
                    var client = new HttpClient();
                    var urlText = "";
                    if (!String.IsNullOrWhiteSpace(text))
                        urlText = "&text=" + WebUtility.UrlEncode(text);

                    var baseUrl = Config.NotesUrl() + "api/1/createPad?apikey=" + Config.NoteApiKey() + "&padID=" + padid + urlText;
                    HttpResponseMessage response = await client.GetAsync(baseUrl);
                    HttpContent responseContent = response.Content;
                    using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                    {
                        var result = await reader.ReadToEndAsync();
                        int code = Json.Decode(result).code;
                        string message = Json.Decode(result).message;
                        if (code != 0)
                        {
                            throw new PermissionsException("Error " + code + ": " + message);
                        }
                        return true;
                    }

                }
                
            }
            catch (Exception e){
				log.Error("Error PadAccessor.CreatePad",e);
				return false;
			}
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

		public static async Task<HtmlString> GetHtml(string padid)
		{
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

        public static string GetFirePadRef(string padId)
        {
            string firePadRef = null;
            var client = new WebClient();
            client.BaseAddress = Config.FirePadUrl();
            client.Headers.Add("Accept","application /json");
            var response = client.DownloadString ("/FirePad/" + padId + ".json");
            if (response != "null")
            {
                var items = JsonConvert.DeserializeObject<FirePadData>(response);
                firePadRef = items.firepadRef;
            }
            return firePadRef;
        }
        public static FirePadData GetFirePadData(string padId)
        {
            FirePadData firePadData = null;
            var client = new WebClient();
            client.BaseAddress = Config.FirePadUrl();
            client.Headers.Add("Accept", "application /json");
            var response = client.DownloadString("/FirePad/" + padId + ".json");
            if (response != "null")
            {
                var items = JsonConvert.DeserializeObject<FirePadData>(response);
                firePadData = items;
            }
            return firePadData;
        }
    }
    
}