using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.L10.AV;
using RadialReview.Utilities;
using log4net;

namespace RadialReview.Hubs {
	[Obsolete("stop using",true)]
	public partial class MeetingHub : BaseHub {

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		public void PushLog(string method, params string[] args) {
			var a = string.Join("\t", args);
			System.Diagnostics.Debug.WriteLine(String.Format("WebRTC\t{0}\t{1}\t{2}\t{3}", DateTime.UtcNow.ToString(), method, Context.ConnectionId, a));
		}

		public class UserMeetingModel {
			public long UserId { get; set; }
			public long StartSelection { get; set; }
			public long EndSelection { get; set; }
			public string FocusId { get; set; }

			public UserMeetingModel(L10Meeting.L10Meeting_Connection conn) {
				UserId = conn.User.Id;
				StartSelection = conn.StartSelection;
				EndSelection = conn.EndSelection;
				FocusId = conn.FocusId;

			}
		}

		public void Join(long meetingId, string connectionId) {
			log.Info("Meeting.Join (" + meetingId + ", " + connectionId + ")");
			try {
				//var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), meetingId);
				if (connectionId != Context.ConnectionId)
					throw new PermissionsException("Ids do not match");

				var conn = L10Accessor.JoinL10Meeting(GetUser(), meetingId, Context.ConnectionId);
				//return new UserMeetingModel(conn);
				//SendUserListUpdate();
			} catch (Exception e) {
				log.Error("Meeting.Join  (" + meetingId + ", " + connectionId + ")", e);
				throw;
			}
			return;
		}

		public void CallMeeting(long recurrenceId, bool audio = true, bool video = true) {
			// They are here, so tell them someone wants to talk
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, GetUser(s)).ViewVideoL10Recurrence(recurrenceId);
					var found = s.QueryOver<AudioVideoUser>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId && /*x.User.Id == GetUser().Id*/ x.ConnectionId == Context.ConnectionId).SingleOrDefault();
					var now = DateTime.UtcNow;
					if (found != null) {
						found.DeleteTime = now;
						s.Update(found);
					}

					var newAVU = new AudioVideoUser() {
						CreateTime = now,
						Audio = audio,
						Video = video,
						ConnectionId = Context.ConnectionId,
						RecurrenceId = recurrenceId,
						User = GetUser(s),
					};

					s.Save(newAVU);
					PushLog("CallMeeting", found.NotNull(x => x.ConnectionId));
					tx.Commit();
					s.Flush();
					Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), Context.ConnectionId).incomingCall(new AudioVisualUserVM(newAVU));
				}
			}
		}

		public void PromptInitiate() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var callingUser = s.QueryOver<AudioVideoUser>().Where(x => x.DeleteTime == null && x.ConnectionId == Context.ConnectionId).SingleOrDefault();

					PushLog("PromptInitiate", callingUser.NotNull(x => x.ConnectionId));
					// Make sure both users are valid
					if (callingUser == null) {
						return;
					}

					if (callingUser.RecurrenceId <= 0)
						throw new PermissionsException("Should be positive");

					// These folks are in a call together, let's let em talk WebRTC
					Clients.Group(MeetingHub.GenerateMeetingGroupId(callingUser.RecurrenceId), Context.ConnectionId).offerTo(new AudioVisualUserVM(callingUser));
				}
			}
		}

		// WebRTC Signal Handler
		public void SendSignal(string signal, string targetConnectionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var callingUser = s.QueryOver<AudioVideoUser>().Where(x => x.DeleteTime == null && x.ConnectionId == Context.ConnectionId).SingleOrDefault();
					var targetUser = s.QueryOver<AudioVideoUser>().Where(x => x.DeleteTime == null && x.ConnectionId == targetConnectionId).SingleOrDefault();

					PushLog("SendSignal", callingUser.NotNull(x => x.ConnectionId), targetUser.NotNull(x => x.ConnectionId));
					// Make sure both users are valid
					if (callingUser == null || targetUser == null) {
						return;
					}

					//Not in the same recurrence
					if (callingUser.RecurrenceId != targetUser.RecurrenceId)
						return;

					if (callingUser.RecurrenceId <= 0)
						throw new PermissionsException("Should be positive");

					// These folks are in a call together, let's let em talk WebRTC
					Clients.Group(MeetingHub.GenerateMeetingGroupId(callingUser.RecurrenceId), Context.ConnectionId).receiveSignal(new AudioVisualUserVM(callingUser), signal);
				}
			}
		}

		public void AnswerCall(bool acceptCall, string targetConnectionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var callingUser = s.QueryOver<AudioVideoUser>().Where(x => x.DeleteTime == null && x.ConnectionId == Context.ConnectionId).SingleOrDefault();
					var targetUser = s.QueryOver<AudioVideoUser>().Where(x => x.DeleteTime == null && x.ConnectionId == targetConnectionId).SingleOrDefault();

					PushLog("AnswerCall", callingUser.NotNull(x => x.ConnectionId), targetUser.NotNull(x => x.ConnectionId));
					if (callingUser == null || targetUser == null)
						return;
					if (callingUser.RecurrenceId != targetUser.RecurrenceId)
						return;

					Clients.Client(targetConnectionId).callAccepted(new AudioVisualUserVM(callingUser));
				}
			}
		}

		public ResultObject SendDisable(long recurrenceId, string domId, bool disabled) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, GetUser(s)).ViewL10Recurrence(recurrenceId);
					Clients.Group(GenerateMeetingGroupId(recurrenceId)).disableItem(domId, disabled);
					return ResultObject.SilentSuccess();
				}
			}
		}

		public ResultObject UpdateUserFocus(long meetingId, string domId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					tx.Commit();
					s.Flush();
					Clients.Group(GenerateMeetingGroupId(meetingId), Context.ConnectionId)
						.UpdateUserFocus(domId, GetUser(s).Id /*us.User.Id*/);
					return ResultObject.SilentSuccess();
				}
			}
		}

		public ResultObject UpdateTextContents(long meetingId, string domId, string contents) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//var us = GetUserStatus(s,meetingId);
					Clients.Group(GenerateMeetingGroupId(meetingId), Context.ConnectionId)
						.updateTextContents(domId, contents);
					return ResultObject.SilentSuccess();
				}
			}
		}
				
		public static string GenerateMeetingGroupId(long recurrenceId) {
			if (recurrenceId == 0)
				throw new Exception();
			return "L10MeetingRecurrence_" + recurrenceId;
		}
		public static string GenerateUserId(long userId) {
			if (userId <= 0)
				throw new Exception();
			return "UserRecurrence_" + userId;
		}

		public static string GenerateMeetingGroupId(L10Meeting meeting) {
			var id = meeting.L10Recurrence.NotNull(x => x.Id);
			if (id == 0)
				id = meeting.L10RecurrenceId;
			return GenerateMeetingGroupId(id);
		}

		private L10Recurrence GetUserCall(ISession s, AudioVideoUser user) {
			return s.Get<L10Recurrence>(user.RecurrenceId);
		}

		public void HangUp() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					_HangUp(s, DateTime.UtcNow);
					tx.Commit();
					s.Flush();
				}
			}
		}

		private void _HangUp(ISession s, DateTime now) {
			var connectedAs = s.QueryOver<AudioVideoUser>().Where(x => x.ConnectionId == Context.ConnectionId && x.DeleteTime == null).List().ToList();

			foreach (var callingUser in connectedAs) {
				var currentRecurrence = callingUser.RecurrenceId;
				PushLog("_HangUp");
				// Send a hang up message to each user in the call, if there is one
				var g = Clients.Group(MeetingHub.GenerateMeetingGroupId(currentRecurrence), callingUser.ConnectionId);
				var gAll = Clients.Group(MeetingHub.GenerateMeetingGroupId(currentRecurrence));
				g.callEnded(callingUser.ConnectionId, string.Format("{0} has hung up.", callingUser.User.GetName()));
				gAll.userExitMeeting(Context.ConnectionId);
				// Remove the call from the list if there is only one (or none) person left.  This should
				// always trigger now, but will be useful when we implement conferencing.

				callingUser.DeleteTime = now;
				s.Update(callingUser);
			}
		}

		//Change in rtL10.js also
		public static TimeSpan PingTimeout = TimeSpan.FromMinutes(3);
		public static DateTime NowPlusPingTimeout() {
			return DateTime.UtcNow.Add(PingTimeout);
		}

		public void Ping() {
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var found = s.QueryOver<L10Recurrence.L10Recurrence_Connection>().Where(x => x.Id == Context.ConnectionId).List().ToList();
						foreach (var f in found) {
							f.DeleteTime = NowPlusPingTimeout();
							s.Update(f);
							var meetingHub = Clients.Group(MeetingHub.GenerateMeetingGroupId(f.RecurrenceId));
							f._User = TinyUserAccessor.GetUsers_Unsafe(s, new[] { f.UserId }).FirstOrDefault();
							meetingHub.stillAlive(f);
						}
						tx.Commit();
						s.Flush();
					}
				}
			} catch (Exception e) {
				log.Error("ping failed", e);
			}
		}

		public override Task OnConnected() {
			return base.OnConnected();
		}

		public override Task OnDisconnected(bool stopCalled) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.QueryOver<L10Recurrence.L10Recurrence_Connection>()
						.Where(x => x.Id == Context.ConnectionId && x.DeleteTime > DateTime.UtcNow && x.UserId == GetUser().Id)
						.List().ToList();

					var now = DateTime.UtcNow;
					if (found.Any()) {
						foreach (var f in found) {
							f.DeleteTime = now;
							s.Update(f);
							var meetingHub = Clients.Group(MeetingHub.GenerateMeetingGroupId(f.RecurrenceId));
							meetingHub.userExitMeeting(f.Id);
						}
					}

					_HangUp(s, now);
					tx.Commit();
					s.Flush();
				}
			}

			return base.OnDisconnected(stopCalled);
		}


		
	}
}