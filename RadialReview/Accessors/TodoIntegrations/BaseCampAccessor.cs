using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Amazon.IdentityManagement.Model;
using Newtonsoft.Json.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using TrelloNet;

namespace RadialReview.Accessors.TodoIntegrations
{
	public class BaseCampAccessor
	{

		public static String AuthUrl(UserOrganizationModel caller, long recurrence, long userId)
		{
			new PermissionsAccessor().Permitted(caller, x => x.EditL10Recurrence(recurrence).ViewUserOrganization(userId, false));
			return Config.Basecamp.GetService(caller.Organization).GetRequestAuthorizationURL() + "&state=" + userId + "_" + recurrence;
		}
		public class BasecampList
		{
			public String Name { get; set; }
			public string ListId { get; set; }
		}

		//public class BasecampInfo
		//{
		//	public long ApiId { get; set; }
		//	public string Identity { get; set; }
		//	public string Expires { get; set; }
		//	public string ApiUrl { get; set; }
		//	public string AccessToken { get; set; }
		//	public string RefreshToken { get; set; }
		//}

		public static BasecampTodoCreds Authorize(UserOrganizationModel caller, string tokenId,long recurrenceId, long userId)
		{
			var bc = Config.Basecamp.GetService(caller.Organization);
			var accessToken = bc.GetAccessToken(tokenId);

			var url = "https://launchpad.37signals.com/authorization.json";
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.UserAgent = Config.Basecamp.GetUserAgent();
			request.Headers.Add("Authorization", "Bearer " + accessToken["access_token"]);

			var output = new BasecampTodoCreds()
			{
				Token = accessToken["access_token"],
				RefreshToken = accessToken["refresh_token"],
			};

			var json = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

			var o=JObject.Parse(json);
			var expires = (string)o["expires_at"];
			var apiUrl = (string)o["accounts"][0]["href"];
			var identity = (string)o["identity"]["first_name"] + " " + (string)o["identity"]["last_name"];

			output.ApiUrl = apiUrl+"/";
			output.AccountName = identity;
			output.Expires = expires;
			output.ApiId = (long)o["identity"]["id"];
			output.CreatedBy = caller.Id;
			output.UID = Guid.NewGuid().ToString();

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId).ViewUserOrganization(userId, false);

					output.ForRGM = s.Load<ResponsibilityGroupModel>(userId);
					output.ForRGMId = userId;
					output.AssociatedWith = ForModel.Create<L10Recurrence>(recurrenceId);
					s.Save(output);
					tx.Commit();
					s.Flush();
				}
			}

			return output;
		}

		public static List<BasecampList> GetLists(UserOrganizationModel caller, long apiId)
		{
			BasecampTodoCreds authorized;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					authorized= s.QueryOver<BasecampTodoCreds>().Where(x => x.DeleteTime == null && x.ApiId == apiId && x.ApiUrl!=null).OrderBy(x=>x.CreateTime).Desc.Take(1).SingleOrDefault();
					if (authorized==null)
						throw new PermissionsException("Credentials do not exist");
				}
			}

			var url = authorized.ApiUrl + "todolists.json";
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.UserAgent = Config.Basecamp.GetUserAgent();
			request.Headers.Add("Authorization", "Bearer " + authorized.Token);

			var listsJson = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

			var lists = JArray.Parse(listsJson);
			var output = new List<BasecampList>();
			foreach (var b in lists)
			{
				try{
					output.Add(new BasecampList(){
						ListId = (string)b["id"]+"~"+(string)b["bucket"]["id"],
						Name = (string)b["name"] + ": " + (string)b["bucket"]["name"]
					});
				}catch (Exception){
					//var failed = true;
				}
			}

			return output;
		}


		public static void AttachToBasecamp(UserOrganizationModel caller, string uid, long recurrenceId, long userId, string listId_bucketId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId).ViewUserOrganization(userId, false);
					var authorized = s.QueryOver<BasecampTodoCreds>().Where(x => x.DeleteTime == null && x.UID == uid && x.ListId==null).SingleOrDefault();
					if (authorized == null)
						throw new PermissionsException("Credentials do not exist");
				

					var url = authorized .ApiUrl+ "people/me.json";
					var request = (HttpWebRequest)WebRequest.Create(url);
					request.UserAgent = Config.Basecamp.GetUserAgent();
					request.Headers.Add("Authorization", "Bearer " + authorized.Token);

					var me = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
					
					var assigneeId =(string) JObject.Parse(me)["id"];
					var name = (string)JObject.Parse(me)["name"];

					var ids = listId_bucketId.Split('~');


					//authorized.AssociatedWith = ForModel.Create<L10Recurrence>(recurrenceId);
					//authorized.ForRGM = s.Load<ResponsibilityGroupModel>(userId);
					//authorized.ForRGMId = userId;
					authorized.ListId = ids[0];
					authorized.BasecampAssigneeId = assigneeId;
					authorized.ProjectId = ids[1];
					authorized.AccountName = name;

					s.Update(authorized);
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}