using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Models.L10;

namespace RadialReview.Accessors
{
	public class TranscriptAccessor
	{

		public static Transcript AddTranscript(UserOrganizationModel caller, string text, long? recurrenceId, long? meetingId, string connectionId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller);
					if (recurrenceId != null){
						perms.EditL10Recurrence(recurrenceId.Value);
					}
					if (meetingId != null){
						perms.ViewL10Meeting(meetingId.Value);
					}

					var t = new Transcript(){
						CreateTime = DateTime.UtcNow,
						MeetingId = meetingId,
						RecurrenceId = recurrenceId,
						Text = text,
						UserId = caller.Id
					};

					s.Save(t);

					if (recurrenceId != null){
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId.Value),connectionId)
							.addTranscription(text,caller.GetName(),DateTime.UtcNow.ToJavascriptMilliseconds(),t.Id);
					}

					tx.Commit();
					s.Flush();
					return t;
				}
			}
		}
		public static List<Transcript> GetRecurrenceTranscript(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var transcript = s.QueryOver<Transcript>()
						.Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId)
						.List().ToList();
					var userIds = transcript.Select(x => x.UserId).Distinct().ToList();
					var users = s.QueryOver<UserOrganizationModel>()
						.WhereRestrictionOn(x => x.Id).IsIn(userIds)
						.List().ToDictionary(x => x.Id, x => x);

					transcript.ForEach(x =>{
						x._User = users[x.UserId];
					});

					return transcript;
				}
			}
		}
		public static List<Transcript> GetMeetingTranscript(UserOrganizationModel caller, long meetingId)
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller).ViewL10Meeting(meetingId);
					var recurId = s.Get<L10Meeting>(meetingId).L10RecurrenceId;
					var transcript= s.QueryOver<Transcript>()
						.Where(x => x.DeleteTime == null && x.MeetingId == meetingId && x.RecurrenceId == recurId)
						.List().ToList();
					var userIds = transcript.Select(x => x.UserId).Distinct().ToList();
					var users = s.QueryOver<UserOrganizationModel>()
						.WhereRestrictionOn(x => x.Id).IsIn(userIds)
						.List().ToDictionary(x=>x.Id,x=>x);

					transcript.ForEach(x => {
						x._User = users[x.UserId];
					});

					return transcript;
				}
			}
		}
	}
}