using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using log4net;
using RadialReview.Exceptions;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Models.L10;
using static CamundaCSharpClient.Query.Task.TaskQuery;
using RadialReview.Models.Askables;
using RadialReview.Utilities.CoreProcess;
using RadialReview.Utilities;
using RadialReview.Models.L10.AV;
using RadialReview.Models.Json;
using NHibernate;
using System.Threading.Tasks;
using RadialReview.Models.VTO;

namespace RadialReview.Hubs {
	public class RealTimeHub : BaseHub {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public class HubResources {
			public long[] recurrenceIds { get; set; }
			public string connectionId { get; set; }
			public Survey[] surveys { get; set; }
			public long[] organizationIds { get; set; }
			public long[] vtoIds { get; set; }

			public override string ToString() {
				return "Meetings: " + string.Join(",", recurrenceIds) + "; connectionId: " + connectionId;
			}

			public class Survey {
				public long? surveyContainerId { get; set; }
				public long? surveyId { get; set; }
			}
		}

		public ResultObject Join(HubResources settings) {
			log.Info("RealTimeHub.Join (" + settings.NotNull(x => x.ToString()) + ")");
			var errors = new List<string>();
			try {
				if (settings == null)
					throw new PermissionsException("Settings was null");
				if (settings.connectionId == null || settings.connectionId != Context.ConnectionId)
					throw new PermissionsException("Ids do not match");

				//Join User
				Groups.Add(Context.ConnectionId, Keys.UserId(GetUser().Id));


				//Join Meetings
				try {
					if (settings.recurrenceIds != null) {
						foreach (var meetingId in settings.recurrenceIds.Distinct()) {
							L10Accessor.JoinL10Meeting(GetUser(), meetingId, Context.ConnectionId);
						}
					}
				} catch (Exception e) {
					log.Error("RealTimeHub.Join(" + settings.NotNull(x => x.ToString()) + ").Meetings", e);
					errors.Add("Meetings");
				}


				//Join Surveys
				try {
					if (settings.surveys != null) {
						foreach (var survey in settings.surveys) {
							SurveyAccessor.JoinSurveyHub(GetUser(), survey.surveyContainerId, survey.surveyId, Context.ConnectionId);
						}
					}
				} catch (Exception e) {
					log.Error("RealTimeHub.Join(" + settings.NotNull(x => x.ToString()) + ").Surveys", e);
					errors.Add("Surveys");
				}

				//Join organizations
				try {
					if (settings.organizationIds == null) {
						settings.organizationIds = new long[] { };
					}
					settings.organizationIds = settings.organizationIds.Union(new[] { GetUser().Organization.Id }).Distinct().ToArray();
					foreach (var orgId in settings.organizationIds) {
						try {
							PermissionsAccessor.Permitted(GetUser(), x => x.ViewOrganization(orgId));
							Groups.Add(Context.ConnectionId, RealTimeHub.Keys.OrganizationId(orgId));
						} catch (Exception e) {
							log.Error("RealTimeHub.Join(" + settings.NotNull(x => x.ToString()) + ").Organization = " + orgId, e);
							errors.Add("Organizations(1)");
						}
					}
				} catch (Exception e) {
					log.Error("RealTimeHub.Join(" + settings.NotNull(x => x.ToString()) + ").Organizations", e);
					errors.Add("Organizations(2)");
				}


				//Join Vtos
				try {
					if (settings.vtoIds != null) {
						foreach (var vtoId in settings.vtoIds) {
							VtoAccessor.JoinVto(GetUser(), vtoId, Context.ConnectionId);
						}
					}
				} catch (Exception e) {
					log.Error("RealTimeHub.Join(" + settings.NotNull(x => x.ToString()) + ").vtos", e);
					errors.Add("Vtos");

				}

			} catch (Exception e) {
				int a = 0;
				log.Error("Unhandled error joining hub",e);
				throw new Exception("Unknown error joining hub.");
			}
			if (errors.Any())
				return ResultObject.CreateError("Join error".Pluralize(errors.Count()) + ". " + string.Join(",", errors));
			return ResultObject.SilentSuccess();
		}

		public class Keys {

			public static string UserId(long userId) {
				if (userId <= 0)
					throw new Exception();
				return "User_" + userId;
			}

			public static string OrganizationId(long organizationId) {
				return "OrganzationId_" + organizationId;
			}


			#region Meeting
			public static string GenerateMeetingGroupId(long recurrenceId) {
				if (recurrenceId == 0)
					throw new Exception();
				return "L10MeetingRecurrence_" + recurrenceId;
			}

			public static string GenerateMeetingGroupId(L10Meeting meeting) {
				var id = meeting.L10Recurrence.NotNull(x => x.Id);
				if (id == 0)
					id = meeting.L10RecurrenceId;
				return GenerateMeetingGroupId(id);
			}
			#endregion

			#region Survey
			[Obsolete("Be careful. This is SurveyContainer, not Survey.")]
			public static string GenerateSurveyContainerId(long surveyContainerId) {
				if (surveyContainerId <= 0)
					throw new Exception();
				return "SurveyContainer_" + surveyContainerId;
			}
			[Obsolete("Be careful. This is Survey, not SurveyContainer.")]
			public static string GenerateSurveyId(long surveyId) {
				if (surveyId <= 0)
					throw new Exception();
				return "Survey_" + surveyId;
			}
			#endregion

			#region Core Process
			public static string GenerateRgm(IdentityLink model) {
				if (model.userId != null)
					return "rspgrpmdl_" + model.userId.SubstringAfter("u_");
				if (model.groupId != null)
					return "rspgrpmdl_" + model.groupId.SubstringAfter("rgm_");

				throw new Exception();
			}

			public static string GenerateRgm(ResponsibilityGroupModel model) {
				if (model == null)
					throw new Exception();
				return "rspgrpmdl_" + model.Id;
			}
			[Obsolete("Use other method if possible.")]
			public static string GenerateRgm(string model) {
				var found = BpmnUtility.ParseCandidateGroupIds(model).Single();
				if (found <= 0)
					throw new Exception();
				return "rspgrpmdl_" + found;
			}
			[Obsolete("Use other method if possible.")]
			public static string GenerateRgm(long rgmId) {
				if (rgmId <= 0)
					throw new Exception();
				return "rspgrpmdl_" + rgmId;
			}
			#endregion

			#region VTO
			public static string GenerateVtoGroupId(long vto) {
				if (vto == 0)
					throw new Exception();
				return "VTO_" + vto;
			}

			public static string GenerateGroupId(VtoModel vto) {
				var id = vto.Id;
				return GenerateVtoGroupId(id);
			}

			#endregion

			#region MessageHub
			public static string GenerateGroupId(long orgId) {
				if (orgId == 0)
					throw new Exception();
				return "ManageOrganization_" + orgId;
			}
			#endregion

		}

		#region Legacy MeetingHub

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
					Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId), Context.ConnectionId).incomingCall(new AudioVisualUserVM(newAVU));
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
					Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(callingUser.RecurrenceId), Context.ConnectionId).offerTo(new AudioVisualUserVM(callingUser));
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
					Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(callingUser.RecurrenceId), Context.ConnectionId).receiveSignal(new AudioVisualUserVM(callingUser), signal);
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
					Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId)).disableItem(domId, disabled);
					return ResultObject.SilentSuccess();
				}
			}
		}

		public ResultObject UpdateUserFocus(long meetingId, string domId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					tx.Commit();
					s.Flush();
					Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(meetingId), Context.ConnectionId)
						.UpdateUserFocus(domId, GetUser(s).Id /*us.User.Id*/);
					return ResultObject.SilentSuccess();
				}
			}
		}

		public ResultObject UpdateTextContents(long meetingId, string domId, string contents) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//var us = GetUserStatus(s,meetingId);
					Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(meetingId), Context.ConnectionId)
						.updateTextContents(domId, contents);
					return ResultObject.SilentSuccess();
				}
			}
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
				var g = Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(currentRecurrence), callingUser.ConnectionId);
				var gAll = Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(currentRecurrence));
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
							var meetingHub = Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(f.RecurrenceId));
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
							var meetingHub = Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(f.RecurrenceId));
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

		#endregion
	}
}