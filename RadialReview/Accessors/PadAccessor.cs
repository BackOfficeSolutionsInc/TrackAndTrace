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
using System.Web.Mvc;
using RadialReview.Exceptions;
using RadialReview.Models.Payments;
using RadialReview.Models.FirePad;
using RadialReview.Utilities;
using RadialReview.Controllers;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using RadialReview.Models;
using RadialReview.Crosscutting.Schedulers;
using Hangfire;
using RadialReview.Hangfire;

namespace RadialReview.Accessors
{ 
    public enum PadType
    {
        firepad = 0,
        etherpad = 1
    }
    public class PadAccessor :BaseAccessor
    {
       
        public static async Task<string> CreatePad(string text=null,PadType padType=PadType.firepad )
		{
            string padid = Guid.NewGuid().ToString();
            try
            {
                
                switch (padType)
                {
                    case PadType.firepad:
                        padid = "-" + padid;
                        Scheduler.Enqueue(() => Firepad(padid, text));
                        break;
                    case PadType.etherpad:
                        Scheduler.Enqueue(() => CreateEtherpad(padid, text));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("" + padType);
                }
               
            }
            catch (Exception e){
				log.Error("Error PadAccessor.CreatePad",e);
            }
            return padid;
		}
        [AutomaticRetry(Attempts = 0)]
        [Queue(HangfireQueues.Immediate.ETHERPAD)]
        public static async Task CreateEtherpad(string padid,string text) {
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
                
            }

        }
        [AutomaticRetry(Attempts = 0)]
        [Queue(HangfireQueues.Immediate.FIREPAD)]
        public static async Task Firepad(string padid, string text)
        {

            IFirebaseClient FirePadClient = new FireSharp.FirebaseClient(Config.GetFirePadConfig());
            if (FirePadClient != null)
            {
                var data = new FirePadData
                {
                    initialText = text ?? ""

                };
               
                    SetResponse FPResponse = await FirePadClient.SetTaskAsync(padid, data);
                if (FPResponse == null)
                {
                    throw new Exception("Firepad Client returns null");
                }
                    FirePadData FPresult = FPResponse.ResultAs<FirePadData>();

            }
            else
            {
                throw new PermissionsException("Error connecting to firebase");
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
							var pad = await CreatePad();
							return await GetReadonlyPad(pad);
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
            HtmlString result=null;
            
            if (padid.Substring(0, 1) == "-")
            {
                result = await GetHtmlFirepad(padid);
            } else { 
                result = await GetHtml(padid);
            }
            return Tuple.Create(padid, result);
		}
		private static async Task<Tuple<string, string>> _GetText(string padid) {
            string result;
            if (padid.Substring(0, 1) == "-")
            {
                result = await GetTextFirepad(padid);
            }
            else
            {
                result = await GetText(padid);
            }
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



        public static async Task<HtmlString> GetHtmlFirepad(string padId)
        {
            FirePadData firePadData = null;
            try {
                firePadData = await GetFirepadNote(padId);
            } catch (Exception e){
                    log.Error("Error PadAccessor.GetHtmlFirepad " , e);
            }
            return new HtmlString(firePadData.html); 
        }

        //public static async Task<FirePadData> GetFirepadNote(string padId)
        //{
        //    FirePadData firepadData=null;
        //    string response;
        //    string note = "";
        //    using (var client = new WebClient())
        //    {
        //        client.BaseAddress = Config.FirePadUrl();
        //        client.Headers.Add("Accept", "application /json");
        //        response = client.DownloadString("/" + padId + "/note.json");
        //    }
        //    if (response != "null")
        //    {
        //        var items = JsonConvert.DeserializeObject<FirePadData>(response);
        //        firepadData = items;

        //    }
        //    else
        //    {
        //        using (var client = new WebClient())
        //        {
        //            client.BaseAddress = Config.FirePadUrl();
        //            client.Headers.Add("Accept", "application /json");
        //            response = client.DownloadString("/" + padId + ".json");
        //        }
        //        var items = JsonConvert.DeserializeObject<FirePadData>(response);
        //        firepadData = items;
        //        firepadData.html = firepadData.initialText;
        //        firepadData.text = firepadData.initialText;
        //    }
        //    return firepadData;
        //}

        public static async Task<FirePadData> GetFirepadNote(string padId)
        {
            FirePadData firepadData = new FirePadData();
            string response;
            string note = "";
            using (var client = new WebClient())
            {
                client.BaseAddress = Config.FirePadUrl();
                client.Headers.Add("Accept", "application /json");
                response = client.DownloadString("/" + padId + "/history.json");
            }
            if (response != "null")
            {

              Dictionary<string, Dictionary<string, object>> items = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,object>>>(response);
                //firepadData.setText(items);

                firepadData.setHtml(items);
               
            }
            else
            {
                using (var client = new WebClient())
                {
                    client.BaseAddress = Config.FirePadUrl();
                    client.Headers.Add("Accept", "application /json");
                    response = client.DownloadString("/" + padId + ".json");
                }
                var items = JsonConvert.DeserializeObject<FirePadData>(response);
                firepadData = items;
                firepadData.html = firepadData.initialText;
                firepadData.text = firepadData.initialText;
            }
            return firepadData;
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
        public static async Task<String> GetTextFirepad(string padId)
        {
            FirePadData firePadData = null;
            try
            {
                firePadData = await GetFirepadNote(padId);
            }
            catch (Exception e)
            {
                log.Error("Error PadAccessor.GetHtmlFirepad", e);
            }
            return firePadData.text;
        }



        public static async Task<string> GetNotesURL(string padId, bool showControls, string name , bool readOnly=false)
        {
            string url;

            UrlHelper Url = new UrlHelper();

            if (padId.Substring(0, 1) != "-")
            {
                if (readOnly)
                    url = await GetReadonlyPad(padId);
                else
                    url = Config.NotesUrl("p/" + padId + "?showControls=" + (showControls ? "true" : "false") + "&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + Url.Encode(name));
            }
            else 
            {
                if (readOnly)
                    url = "~/FirePad/Index?id=" + padId + "&readOnly=" + readOnly;
                else
                    url = "~/FirePad/Index/" + padId;
            }
           
            return url;
        }
    }
    
}