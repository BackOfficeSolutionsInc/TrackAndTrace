using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.ElastiCache.Model;
using Mandrill;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class MandrillAccessor
	{
		public static void ProcessWebhooks(IEnumerable<WebHookEvent> events)
		{
			using(var s = HibernateSession.GetCurrentSession())
			{
				using(var tx=s.BeginTransaction())
				{
					foreach (var e in events){
						ProcessWebhook(s, e);
					}

					tx.Commit();
					s.Flush(); 
				}
			}
		}


		public static void UpdateJoinOrganization(ISession s,IHubContext hub, WebHookEvent e, EmailModel email)
		{
			var tu = s.QueryOver<TempUserModel>()
				.WhereRestrictionOn(x => x.Email).IsInsensitiveLike(email.ToAddress)
				.List().FirstOrDefault();
			if (tu != null){
				tu.EmailStatus = e.Event;

				var lu = s.QueryOver<UserLookup>().Where(x=>x.UserId==tu.UserOrganizationId).List().FirstOrDefault();
				switch (e.Event)
				{
					case WebHookEventType.Send:			tu.EmailStatusUnseen = false;	break;
					case WebHookEventType.Hard_bounce:  tu.EmailStatusUnseen = true;	break;
					case WebHookEventType.Soft_bounce:  ; break;
					case WebHookEventType.Open:			; break;
					case WebHookEventType.Click:		; break;
					case WebHookEventType.Spam:			tu.EmailStatusUnseen = true;	break;
					case WebHookEventType.Unsub:		tu.EmailStatusUnseen = true;	break;
					case WebHookEventType.Reject:		tu.EmailStatusUnseen = true;	break;
					case WebHookEventType.Deferral:		; break;
					case WebHookEventType.Inbound:		; break;
					default: break;
				}
				if (lu != null){
					lu.EmailStatus = e.Event;
					s.Update(lu);
				}
				s.Update(tu);
				var message = MessageAccessor.GenerateManageMembersMessage(tu, e.Event);
				if (tu.OrganizationId.HasValue && message != null && tu.LastSentByUserId>0)
				{
					var username = s.Get<UserOrganizationModel>(tu.LastSentByUserId).GetUsername();
					hub.Clients.User(username).showAlert(ResultObject.CreateError(message.Message,message));
				}

			}
		}

		public static void ProcessWebhook(ISession s, WebHookEvent e)
		{
			var hook =new EmailWebhookModel(){
				Clicks = e.Msg.Clicks.NotNull(x=>x.Count),
				Opens = e.Msg.Opens.NotNull(x => x.Count),
				MandrillId = e.Msg.Id,
				TimeStamp = e.TimeStamp,
				EventType = e.Event
			};

			s.Save(hook);

			var hub = GlobalHost.ConnectionManager.GetHubContext<MessageHub>();
			var found = s.QueryOver<EmailModel>().Where(x => x.MandrillId == hook.MandrillId).List().FirstOrDefault();
			if (found != null){
				switch(found.EmailType){
					case EmailType.JoinOrganization: UpdateJoinOrganization(s, hub,e, found); break;
					default: break;
				}	
			}
		
			switch(e.Event){
				case WebHookEventType.Send:break;
				case WebHookEventType.Hard_bounce:break;
				case WebHookEventType.Soft_bounce:break;
				case WebHookEventType.Open:break;
				case WebHookEventType.Click:break;
				case WebHookEventType.Spam:break;
				case WebHookEventType.Unsub:break;
				case WebHookEventType.Reject:break;
				case WebHookEventType.Deferral:break;
				case WebHookEventType.Inbound:break;
				default:break;
			}

		}
	}
}