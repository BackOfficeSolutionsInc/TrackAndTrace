using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.IdentityManagement.Model;
using Mandrill;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class MessageAccessor
	{
		public interface IMessage
		{
			 string Message { get; set; }
		}
		public class ManageMembersMessage : IMessage
		{
			public string Message { get; set; }
			public WebHookEventType Type { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public long UserId { get; set; }

			public ManageMembersMessage(TempUserModel t,WebHookEventType type,String message)
			{
				FirstName = t.FirstName;
				LastName = t.LastName;
				Type = type;
				UserId = t.UserOrganizationId;

				Message = message;
			}
		}

		public static ManageMembersMessage GenerateManageMembersMessage(TempUserModel t, WebHookEventType? eventType)
		{
			if (eventType == null)
				return null;

			var st = eventType.Value;
			switch (st)
			{
				case WebHookEventType.Send:			break;
				case WebHookEventType.Hard_bounce:
					return (new ManageMembersMessage(t, st, "Email address for " + t.FirstName + " " + t.LastName + " (" + t.Email + ")  does not exist. It may be spelled incorrectly."));
				case WebHookEventType.Soft_bounce: break;
				case WebHookEventType.Open: break;
				case WebHookEventType.Click: break;
				case WebHookEventType.Spam:
					return (new ManageMembersMessage(t, st, "Email invitation was marked as spam for " + t.FirstName + " " + t.LastName + " (" + t.Email + ")."));
				case WebHookEventType.Unsub:
					return (new ManageMembersMessage(t, st, "" + t.FirstName + " " + t.LastName + " ("+t.Email+") has unsubscribed from our emails. Email invitation could not be sent."));
				case WebHookEventType.Reject:
					return (new ManageMembersMessage(t, st, "Email invitation to " + t.FirstName + " " + t.LastName + " ("+t.Email+") could not be sent. Email was rejected."));
				case WebHookEventType.Deferral: break;
				case WebHookEventType.Inbound: break;
				default: break;
			}
			return null;
		}

		public static List<ManageMembersMessage> GetManageMembers_Messages(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);

					var tus =s.QueryOver<TempUserModel>().Where(x => x.EmailStatusUnseen && x.OrganizationId == organizationId).List().ToList();
					var list = new List<ManageMembersMessage>();
					foreach (var t in tus){
						t.EmailStatusUnseen = false;
						s.Update(t);
						var st = t.EmailStatus;

						list.Add(GenerateManageMembersMessage(t,st));
					}
					
					tx.Commit();
					s.Flush();

					return list.Where(x=>x!=null).ToList();
				}
			}
		}
	}
}