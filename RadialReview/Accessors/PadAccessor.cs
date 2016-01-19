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

namespace RadialReview.Accessors
{
	public class PadAccessor :BaseAccessor
	{

		public static async Task<bool> CreatePad(string padid, string text=null)
		{
			try{
				var client = new HttpClient();

				var urlText = "";

				if (!String.IsNullOrWhiteSpace(text))
					urlText = "&text=" + WebUtility.UrlEncode(text);


				var baseUrl = Config.NotesUrl() + "api/1/createPad?apikey=" + Config.NoteApiKey() + "&padID=" + padid + urlText;
				HttpResponseMessage response = await client.GetAsync(baseUrl);
				HttpContent responseContent = response.Content;
				using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())){
					var result = await reader.ReadToEndAsync();
					int code = Json.Decode(result).code;
					string message = Json.Decode(result).message;
					if (code != 0){
						throw new PermissionsException("Error " + code + ": " + message);
					}
					return true;
				}

			}
			catch (Exception e){
				log.Error("Error PadAccessor.CreatePad",e);
				return false;
			}
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
			}
			catch (Exception e){
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
					/*html = html.Substring("<!DOCTYPE HTML><html><body>".Length, html.Length - ("</body></html>".Length + "<!DOCTYPE HTML><html><body>".Length));
				return new HtmlString(html);*/
				}
			}
			catch (Exception e){
				log.Error("Error PadAccessor.GetText", e);
				return "";
			}
		}
	}
}