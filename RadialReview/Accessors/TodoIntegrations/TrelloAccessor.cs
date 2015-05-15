using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using TrelloNet;
using WebGrease.Css.Extensions;

namespace RadialReview.Accessors.TodoIntegrations
{
	public class TrelloAccessor
	{
		
		public static String AuthUrl(UserOrganizationModel caller,long recurrence,long userId)
		{
			new PermissionsAccessor().Permitted(caller,x=>x.EditL10Recurrence(recurrence).ViewUserOrganization(userId,false));

			return  new Trello(Config.GetTrelloKey()).GetAuthorizationUrl("Radial", Scope.ReadWrite,Expiration.Never).ToString()+
				"&return_url=" + Config.BaseUrl(caller.Organization) + "CallBack/Trello?recurrence=" + recurrence + "%26user=" + userId;
		}

		public class TrelloList
		{
			public String Name { get; set; }
			public string ListId { get; set; }
		}

		public static List<TrelloList> GetLists(UserOrganizationModel caller, string tokenId)
		{
			var trello = new Trello(Config.GetTrelloKey());
			trello.Authorize(tokenId);
			//var me = trello.Members.Me();
			//var boards = trello.Boards.ForMember(me);

			//https://trello.com/1/members/my/boards?key=dd6d2def5f8eface4a31fb7faefff3b9&token=428cf27d88aee85582283f2be29ad7c5d259a1ff0b6aaa962130ffdacbb30ac2
			var boardsJson = new StreamReader(WebRequest.Create("https://trello.com/1/members/my/boards?key="+Config.GetTrelloKey()+"&token="+tokenId).GetResponse().GetResponseStream()).ReadToEnd();

			var boards = JArray.Parse(boardsJson);
			var output = new List<TrelloList>();
			foreach (var b in boards){
				try{
					var listJson = new StreamReader(WebRequest.Create("https://trello.com/1/boards/" + b["id"] + "/lists?key=" + Config.GetTrelloKey() + "&token=" + tokenId).GetResponse().GetResponseStream()).ReadToEnd();
					var lists = JArray.Parse(listJson);

					foreach (var l in lists){
						try{
							output.Add(new TrelloList(){
								ListId = (string)l["id"],
								Name = (string)b["name"] + ": " + (string)l["name"]
							});
						}
						catch (Exception)
						{
							var failed = true;
							
						}
					}
				}catch (Exception){
					var failed = true;
				}
			}
			
			return output;
		}

		

		public static void AttachToTrello(UserOrganizationModel caller,string token, long recurrenceId, long userId,string listId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId).ViewUserOrganization(userId,false);

					var trello=new Trello(Config.GetTrelloKey());
					trello.Authorize(token);
					var accountName =trello.Members.Me().Username;
					var creds = new TrelloTodoCreds(){
						AccountName = accountName,
						AssociatedWith = ForModel.Create<L10Recurrence>(recurrenceId),
						ForRGM = s.Load<ResponsibilityGroupModel>(userId),
						ForRGMId = userId,
						Token = token,
						ListId = listId,
						CreatedBy = caller.Id
					};

					s.Save(creds);
					tx.Commit();
					s.Flush();
				}
			}
		}

	}
}