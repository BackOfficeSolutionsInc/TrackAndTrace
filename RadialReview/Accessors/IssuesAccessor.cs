using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.DataTypes;
using System.Text;
using System.Web;
using RadialReview.Utilities.RealTime;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Todo;
using SpreadsheetLight;
using static RadialReview.Accessors.IssuesAccessor;
using RadialReview.Utilities.Synchronize;
using RadialReview.Utilities.NHibernate;

namespace RadialReview.Accessors {

	public class IssueCreation {
		private string Message { get; set; }
		private string Details { get; set; }
		private long? OwnerId { get; set; }
		private long? CreatedDuringMeetingId { get; set; }

		private long RecurrenceId { get; set; }
		private DateTime? Now { get; set; }
		private string ForModelType { get; set; }
		private long ForModelId { get; set; }
		private bool _ensured { get; set; }
		private int Priority { get; set; }

		private IssueCreation(string message, string details, long? ownerId, long? createdDuringMeetingId, int priority, long recurrenceId, DateTime? now, string forModelType, long forModelId) {
			Message = message;
			Details = details;
			OwnerId = ownerId;
			CreatedDuringMeetingId = createdDuringMeetingId;
			RecurrenceId = recurrenceId;
			Now = now;
			ForModelType = forModelType;
			ForModelId = forModelId;
			Priority = priority;
		}

		public static IssueCreation CreateL10Issue(string message, string details, long? ownerId, long recurrenceId, long? createdDuringMeeting=null,int priority=0, string modelType = "IssueModel", long modelId = -1, DateTime? now = null) {
			return new IssueCreation(message, details, ownerId, createdDuringMeeting,priority, recurrenceId, now, modelType, modelId);
		}


		public IssueOutput Generate(ISession s, PermissionsUtility perms) {
			UserOrganizationModel creator = perms.GetCaller();
			EnsurePermitted(perms, creator.Organization.Id);

			var duringMeeting = CreatedDuringMeetingId > 0 ? CreatedDuringMeetingId : null;
			Now = Now ?? DateTime.UtcNow;

			var issue = new IssueModel {
				CreatedById = OwnerId ?? creator.Id,
				CreatedBy = s.Load<UserOrganizationModel>(OwnerId ?? creator.Id),
				CreatedDuringMeetingId = duringMeeting,
				CreatedDuringMeeting = duringMeeting.NotNull(x => s.Load<L10Meeting>(x)),
				CreateTime = Now.Value,
				Description = Details,
				ForModel = ForModelType,
				ForModelId = ForModelId,
				Message = Message,
				Organization = creator.Organization,
				OrganizationId = creator.Organization.Id,
				_Priority = Priority,
			};

			var issueRecur = new IssueModel.IssueModel_Recurrence() {
				CopiedFrom = null,
				Issue = issue,
				CreatedBy = issue.CreatedBy,
				CreateTime = issue.CreateTime,
				Recurrence = s.Load<L10Recurrence>(RecurrenceId),
				Owner = s.Load<UserOrganizationModel>(OwnerId ?? creator.Id),
				Priority = issue._Priority
			};

			return new IssueOutput {
				IssueModel = issue,
				IssueRecurrenceModel = issueRecur,
			};

		}

		private void EnsurePermitted(PermissionsUtility perms, long orgId) {
			_ensured = true;

			if (CreatedDuringMeetingId != null && CreatedDuringMeetingId > 0)
				perms.ViewL10Meeting(CreatedDuringMeetingId.Value);
			perms.ViewOrganization(orgId);			
			if (OwnerId != null)
				perms.ViewUserOrganization(OwnerId.Value, false);

			perms.EditL10Recurrence(RecurrenceId);
		}

	}



	public class IssuesAccessor : BaseAccessor {



		public class IssueOutput {
			public IssueModel IssueModel { get; set; }
			public IssueModel.IssueModel_Recurrence IssueRecurrenceModel { get; set; }
		}
		
		public static async Task<IssueOutput> CreateIssue(ISession s, PermissionsUtility perms, IssueCreation issueCreator) {
			//var o = new IssueOutput();

			#region Deleted

			//perms.EditL10Recurrence(recurrenceId);
			////perms.ViewL10Recurrence(recurrenceId);

			//if (issue.Id != 0)
			//	throw new PermissionsException("Id was not zero");

			//perms.ViewUserOrganization(ownerId, false);

			//if (issue.CreatedDuringMeetingId == -1)
			//	issue.CreatedDuringMeetingId = null;
			//perms.ConfirmAndFix(issue,
			//	x => x.CreatedDuringMeetingId,
			//	x => x.CreatedDuringMeeting,
			//	x => x.ViewL10Meeting);

			//if (issue.OrganizationId == 0 && issue.Organization == null)
			//	issue.OrganizationId = perms.GetCaller().Organization.Id;
			//perms.ConfirmAndFix(issue,
			//	x => x.OrganizationId,
			//	x => x.Organization,
			//	x => x.ViewOrganization);

			//if (issue.CreatedById == 0 && issue.CreatedBy == null)
			//	issue.CreatedById = perms.GetCaller().Id;
			//perms.ConfirmAndFix(issue,
			//	x => x.CreatedById,
			//	x => x.CreatedBy,
			//	x => y => x.ViewUserOrganization(y, false));
			///*if (issue.CreatedDuringMeetingId != null)
			//    issue.CreatedDuringMeeting = s.Get<L10Meeting>(issue.CreatedDuringMeetingId);
			//		issue.MeetingRecurrence = s.Get<L10Recurrence>(issue.MeetingRecurrenceId);
			//		issue.CreatedBy = s.Get<UserOrganizationModel>(issue.CreatedById);
			//*/

			//if (String.IsNullOrWhiteSpace(issue.PadId))
			//	issue.PadId = Guid.NewGuid().ToString(); 
			#endregion

			var io = issueCreator.Generate(s, perms);

			if (!string.IsNullOrWhiteSpace(io.IssueModel.Description))
				await PadAccessor.CreatePad(io.IssueModel.PadId, io.IssueModel.Description);


			s.Save(io.IssueModel);
			s.Save(io.IssueRecurrenceModel);
			//o.IssueModel = io;
			var recurrenceId = io.IssueRecurrenceModel.Recurrence.Id;
			var r = s.Get<L10Recurrence>(recurrenceId);

			#region Deleted
			// r.Pristine = false;
			//await L10Accessor.Depristine_Unsafe(s, perms.GetCaller(), r);
			//s.Update(r);

			//var recur = new IssueModel.IssueModel_Recurrence() {
			//	CopiedFrom = null,
			//	Issue = issue,
			//	CreatedBy = issue.CreatedBy,
			//	Recurrence = r,
			//	CreateTime = issue.CreateTime,
			//	Owner = s.Load<UserOrganizationModel>(ownerId),
			//	Priority = issue._Priority

			//};
			//s.Save(recur);
			//o.IssueRecurrenceModel = recur;
			//if (r.OrderIssueBy == "data-priority") {
			//	var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
			//		.Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Priority > io._Priority && x.ParentRecurrenceIssue == null)
			//		.Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
			//	var max = -1L;
			//	if (order.Any())
			//		max = order.Max() ?? -1;
			//	max += 1;
			//	recur.Ordering = max;
			//	s.Update(recur);
			//}
			//if (r.OrderIssueBy == "data-rank") {
			//	var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
			//		.Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Rank > io._Rank && x.ParentRecurrenceIssue == null)
			//		.Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
			//	var max = -1L;
			//	if (order.Any())
			//		max = order.Max() ?? -1;
			//	max += 1;
			//	recur.Ordering = max;
			//	s.Update(recur);
			//}
			#endregion

			if (r.OrderIssueBy == "data-priority") {
				var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
					.Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Priority > io.IssueModel._Priority && x.ParentRecurrenceIssue == null)
					.Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
				var max = -1L;
				if (order.Any())
					max = order.Max() ?? -1;
				max += 1;
				io.IssueRecurrenceModel.Ordering = max;
				s.Update(io.IssueRecurrenceModel);
			}
			if (r.OrderIssueBy == "data-rank") {
				var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
					.Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Rank > io.IssueModel._Rank && x.ParentRecurrenceIssue == null)
					.Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
				var max = -1L;
				if (order.Any())
					max = order.Max() ?? -1;
				max += 1;
				io.IssueRecurrenceModel.Ordering = max;
				s.Update(io.IssueRecurrenceModel);
			}

			#region Deleted
			//rt.UpdateRecurrences(recurrenceId).SetFocus("");
			//Audit.L10Log(s, perms.GetCaller(), recurrenceId, "CreateIssue", ForModel.Create(io.IssueModel), io.IssueModel.NotNull(x => x.Message));
			#endregion

			// Trigger webhook events
			await HooksRegistry.Each<IIssueHook>((ses, x) => x.CreateIssue(ses, io.IssueRecurrenceModel));

			return io;

		}

		
		public static async Task<IssueOutput> CreateIssue(UserOrganizationModel caller, IssueCreation creation) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var o = await CreateIssue(s, perms, creation);
					tx.Commit();
					s.Flush();
					return o;
				}
			}
		}

		public static async Task EditIssue(UserOrganizationModel caller, long issueRecurrenceId, string message = null, bool? complete = null,
			long? owner = null, int? priority = null, int? rank = null, bool? awaitingSolve = null, DateTime? now = null) {
			//using (var s = HibernateSession.GetCurrentSession()) {
			//	using (var tx = s.BeginTransaction()) {
            await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateIssueMessage(issueRecurrenceId),async s=>{
					var perms = PermissionsUtility.Create(s, caller);
					await EditIssue(s, perms, issueRecurrenceId, message, complete, owner, priority, rank, awaitingSolve, now);
            });
			//		tx.Commit();
			//		s.Flush();
			//	}
			//}
				}
        /// <summary>
        /// SyncAction.UpdateIssueMessage(issue.Issue.Id)
        /// </summary>
        /// <param name="s"></param>
        /// <param name="perms"></param>
        /// <param name="issueRecurrenceId"></param>
        /// <param name="message"></param>
        /// <param name="complete"></param>
        /// <param name="owner"></param>
        /// <param name="priority"></param>
        /// <param name="rank"></param>
        /// <param name="awaitingSolve"></param>
        /// <param name="now"></param>
        /// <returns></returns>
		public static async Task EditIssue(IOrderedSession s,PermissionsUtility perms, long issueRecurrenceId, string message=null,
			bool? complete=null, long? owner=null, int? priority=null, int? rank=null, /*bool? delete=null,*/ bool? awaitingSolve=null,
			DateTime? now = null) 
		{
			now = Math2.Min(DateTime.UtcNow.AddSeconds(3), now ?? DateTime.UtcNow);

			var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
			if (issue == null)
				throw new PermissionsException("Issue does not exist.");

			var recurrenceId = issue.Recurrence.Id;
			if (recurrenceId == 0)
				throw new PermissionsException("Meeting does not exist.");
			
			perms.EditL10Recurrence(recurrenceId);

			//var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			//var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId);
			//var updatesText = new List<string>();

			var updates = new IIssueHookUpdates();

			//bool IsMessageChange = false;
			if (message != null && message != issue.Issue.Message) {
				//SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateIssueMessage(issue.Issue.Id));
				issue.Issue.Message = message;
				updates.MessageChanged = true;
				//	group.updateIssueMessage(issueRecurrenceId, message);
				//	updatesText.Add("Message: " + issue.Issue.Message);
				//	IsMessageChange = true;
			}
			//if (details != null && details != issue.Issue.Description) {
			//	SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateIssueDetails(issue.Issue.Id));
			//	issue.Issue.Description = details;
			//	group.updateIssueDetails(issueRecurrenceId, details);
			//	updatesText.Add("Description: " + issue.Issue.Description);
			//}
			if (owner != null && (issue.Owner == null || owner != issue.Owner.Id) && owner > 0) {
				var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner).Take(1).List().ToList();
				if (!any.Any())
					throw new PermissionsException("Specified Owner cannot see meeting");

				issue.Owner = s.Get<UserOrganizationModel>(owner);
				updates.OwnerChanged = true;
				//group.updateIssueOwner(issueRecurrenceId, owner, issue.Owner.GetName(), issue.Owner.ImageUrl(true, ImageSize._32));
				//updatesText.Add("Owner: " + issue.Owner.GetName());
			}
			if (priority != null && priority != issue.Priority && issue.LastUpdate_Priority < now) {
				issue.LastUpdate_Priority = now.Value;
				updates.oldPriority = issue.Priority;
				issue.Priority = priority.Value;
				//group.updateIssuePriority(issueRecurrenceId, issue.Priority);
				//updatesText.Add("Priority from " + old + " to " + issue.Priority);
				s.Update(issue);
				updates.PriorityChanged = true;
			}
			if (rank != null && rank != issue.Rank && issue.LastUpdate_Priority < now) {
				issue.LastUpdate_Priority = now.Value;
				updates.oldRank = issue.Rank;
				issue.Rank = rank.Value;
				//	group.updateIssueRank(issueRecurrenceId, issue.Rank, true);
				//	updatesText.Add("Rank from " + old + " to " + issue.Rank);
				s.Update(issue);
				updates.RankChanged = true;
			}

			//_ProcessDeleted(s, issue, delete);

			var now1 = DateTime.UtcNow;
			//bool IsIssueStatusUpdated = false;
			if (complete != null) {
				//using (var rt = RealTimeUtility.Create(connectionId)) {
				//}
				if (complete.Value && issue.CloseTime == null) {
					updates.CompletionChanged = true;
					//		updatesText.Add("Marked Closed");
				} else if (!complete.Value && issue.CloseTime != null) {
					updates.CompletionChanged = true;
					//		updatesText.Add("Marked Open");
				}
				_UpdateIssueCompletion_Unsafe(s, issue, complete.Value, now1);
			}


			if (awaitingSolve != null && awaitingSolve != issue.AwaitingSolve) {
				issue.AwaitingSolve = awaitingSolve.Value;
				s.Update(issue);
				updates.AwaitingSolveChanged = true;
				//	group.updateIssueAwaitingSolve(issue.Id, awaitingSolve.Value);

			}
			//group.update(new AngularUpdate() { new AngularIssue(issue) });


			//var updatedText = "Updated Issue \"" + issue.Issue.Message + "\" \n " + String.Join("\n", updatesText);
			//Audit.L10Log(s, caller, recurrenceId, "UpdateIssue", ForModel.Create(issue), updatedText);

			//if (IsMessageChange) {
			//	// Webhook event trigger
			//	//?added await
			//	await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateMessage(ses, issue));
			//}

			//// Webhook register Marking complete for TODO
			//if (IsIssueStatusUpdated) {
			//	//?added await
			//	await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateCompletion(ses, issue));
			//}

			await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, perms.GetCaller(), issue, updates));
			
		}

		public static void _UpdateIssueCompletion_Unsafe(ISession s, /*RealTimeUtility rt,*/ IssueModel.IssueModel_Recurrence issue, bool complete, DateTime? now = null) {
			now = now ?? DateTime.UtcNow;
			bool? added = null;
			if (complete && issue.CloseTime == null) {
				issue.CloseTime = now;
				added = false;
			} else if (!complete && issue.CloseTime != null) {
				issue.CloseTime = null;
				added = true;
			}

			if (added != null) {
				s.Update(issue);
				/*var others = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.Issue.Id == issue.Issue.Id).List().ToList();

				//Not sure what I was thinking here...
				foreach (var o in others) {
					if (o.Id != issue.Id) {
						o.MarkedForClose = complete;
						s.Update(o);
					}
					//rt.UpdateRecurrences(o.Recurrence.Id).AddLowLevelAction(x => x.updateModedIssueSolve(o.Id, complete));
					//var recur = new AngularRecurrence(o.Recurrence.Id);
					//recur.IssuesList.Issues = AngularList.CreateFrom(added.Value ? AngularListType.Add : AngularListType.Remove, new AngularIssue(issue));
					//rt.UpdateRecurrences(o.Recurrence.Id).Update(recur);
				}*/
			}
		}




		public static IssueModel GetIssue(UserOrganizationModel caller, long issueId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewIssue(issueId);
					return s.Get<IssueModel>(issueId);
				}
			}
		}

		public static List<IssueModel.IssueModel_Recurrence> GetVisibleIssuesForUser(UserOrganizationModel caller, long userId) {
			//throw new NotImplementedException();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);

					// only get meetings visible to me.
					var list = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, caller.Id, true, false).Select(x => x.Id).ToList();

					return s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null
						&& x.CloseTime == null
						&& x.Owner.Id == userId).WhereRestrictionOn(x => x.Recurrence.Id).IsIn(list).Fetch(x => x.Issue).Eager.List().ToList();
				}
			}
		}


		public static List<IssueModel.IssueModel_Recurrence> GetRecurrenceIssuesForUser(UserOrganizationModel caller, long userId, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					// throw new NotImplementedException("query is incorrect");
					PermissionsUtility.Create(s, caller).ViewRecurrenceIssuesForUser(userId, recurrenceId);

					return s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null
						&& x.Recurrence.Id == recurrenceId
						&& x.CloseTime == null
						&& x.Owner.Id == userId).Fetch(x => x.Issue).Eager.List().ToList();
				}
			}
		}

		public static IssueModel.IssueModel_Recurrence GetIssue_Recurrence(UserOrganizationModel caller, long recurrence_issue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.Get<IssueModel.IssueModel_Recurrence>(recurrence_issue);

					PermissionsUtility.Create(s, caller)
						.ViewL10Recurrence(found.Recurrence.Id)
						.ViewIssue(found.Issue.Id);

					found.Issue = s.Get<IssueModel>(found.Issue.Id);
					found.Recurrence = s.Get<L10Recurrence>(found.Recurrence.Id);

					return found;
				}
			}
		}

		public static IssueModel.IssueModel_Recurrence CopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;

					var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);

					PermissionsUtility.Create(s, caller)
						.ViewL10Recurrence(parent.Recurrence.Id)
						.ViewIssue(parent.Issue.Id);

					var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

					if (childRecur.Organization.Id != caller.Organization.Id)
						throw new PermissionsException("You cannot copy an issue into this meeting.");
					if (parent.DeleteTime != null)
						throw new PermissionsException("Issue does not exist.");

					var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);
					if (possible.All(x => x.Id != childRecurrenceId)) {
						throw new PermissionsException("You do not have permission to copy this issue.");
					}

					var issue_recur = new IssueModel.IssueModel_Recurrence() {
						ParentRecurrenceIssue = null,
						CreateTime = now,
						CopiedFrom = parent,
						CreatedBy = caller,
						Issue = s.Load<IssueModel>(parent.Issue.Id),
						Recurrence = s.Load<L10Recurrence>(childRecurrenceId),
						Owner = parent.Owner
					};
					s.Save(issue_recur);
					var viewModel = IssuesData.FromIssueRecurrence(issue_recur);
					_RecurseCopy(s, viewModel, caller, parent, issue_recur, now);
					tx.Commit();
					s.Flush();

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(childRecurrenceId));

					meetingHub.appendIssue(".issues-list", viewModel);
					var issue = s.Get<IssueModel>(parent.Issue.Id);
					Audit.L10Log(s, caller, parent.Recurrence.Id, "CopyIssue", ForModel.Create(issue_recur), issue.NotNull(x => x.Message) + " copied into " + childRecur.NotNull(x => x.Name));
					return issue_recur;
				}
			}
		}

		private static void _RecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, IssueModel.IssueModel_Recurrence parent, DateTime now) {
			var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
				.List();
			var childrenVMs = new List<IssuesData>();
			foreach (var child in children) {
				var issue_recur = new IssueModel.IssueModel_Recurrence() {
					ParentRecurrenceIssue = parent,
					CreateTime = now,
					CopiedFrom = child,
					CreatedBy = caller,
					Issue = s.Load<IssueModel>(child.Issue.Id),
					Recurrence = s.Load<L10Recurrence>(parent.Recurrence.Id),
					Owner = s.Load<UserOrganizationModel>(parent.Owner.Id)
				};
				s.Save(issue_recur);
				var childVM = IssuesData.FromIssueRecurrence(issue_recur);
				childrenVMs.Add(childVM);
				_RecurseCopy(s, childVM, caller, child, issue_recur, now);
			}
			viewModel.children = childrenVMs.ToArray();
		}

        public static IssueModel.IssueModel_Recurrence UnCopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var now = DateTime.UtcNow;

                    var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);

                    PermissionsUtility.Create(s, caller)
                        .ViewL10Recurrence(parent.Recurrence.Id)
                        .ViewIssue(parent.Issue.Id);

                    var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

                    if (childRecur.Organization.Id != caller.Organization.Id)
                        throw new PermissionsException("You cannot Uncopy an issue into this meeting.");
                    if (parent.DeleteTime != null)
                        throw new PermissionsException("Issue does not exist.");

                    var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);
                    if (possible.All(x => x.Id != childRecurrenceId))
                    {
                        throw new PermissionsException("You do not have permission to uncopy this issue.");
                    }

                    var getL10RecurrenceChild = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                        .Where(x => x.DeleteTime == null && x.Recurrence.Id == childRecurrenceId && x.Issue.Id == parent.Issue.Id)
                        .SingleOrDefault();

                    if (getL10RecurrenceChild == null)
                    {
                        throw new PermissionsException("Issue Recurrence does not exist.");
                    }
                    
                    getL10RecurrenceChild.DeleteTime = now;
                    s.Update(getL10RecurrenceChild);

                    var viewModel = IssuesData.FromIssueRecurrence(getL10RecurrenceChild);
                    _UnRecurseCopy(s, viewModel, caller, parent, now);
                    tx.Commit();
                    s.Flush();

                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(childRecurrenceId));

                    meetingHub.removeIssueRow(getL10RecurrenceChild.Id);
                    var issue = s.Get<IssueModel>(parent.Issue.Id);
                    Audit.L10Log(s, caller, parent.Recurrence.Id, "UnCopyIssue", ForModel.Create(getL10RecurrenceChild), issue.NotNull(x => x.Message) + " Uncopied from " + childRecur.NotNull(x => x.Name));
                    return getL10RecurrenceChild;
                }
            }
        }

        private static void _UnRecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, DateTime now)
        {
            var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                .Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
                .List();
            var childrenVMs = new List<IssuesData>();
            foreach (var child in children)
            {
                child.DeleteTime = now;              
                s.Update(child);
                var childVM = IssuesData.FromIssueRecurrence(child);
                childrenVMs.Add(childVM);
                _UnRecurseCopy(s, childVM, caller, child, now);
            }
            viewModel.children = childrenVMs.ToArray();
        }

        public static Csv Listing(UserOrganizationModel caller, long organizationId) {
            using (var s = HibernateSession.GetCurrentSession()){
                using (var tx = s.BeginTransaction()){
					// var p = s.Get<PeriodModel>(period);

					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

					var sb = new StringBuilder();

					sb.Append("Id,Depth,Owner,Created,Closed,Issue");

					var csv = new Csv();

					IssueModel issueA = null;

					//var id = 0;
					var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.JoinAlias(x => x.Issue, () => issueA)
						.Where(x => x.DeleteTime == null)
						.Where(x => issueA.OrganizationId == organizationId)
						.Fetch(x => x.Issue).Eager
						.List().ToList();

					foreach (var t in issues) {
						var time = "";
						csv.Add("" + t.Id, "Owner", t.Owner.NotNull(x => x.GetName()));
						csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
						if (t.CloseTime != null)
							time = t.CloseTime.Value.ToShortDateString();
						csv.Add("" + t.Id, "Completed", time);
						csv.Add("" + t.Id, "Issue", "" + t.Issue.Message);

						//if (false /*&& includeDetails*/) {
						//	var padDetails = await PadAccessor.GetText(t.PadId);
						//	csv.Add("" + t.Id, "Details", "" + padDetails);
						//}
					}


					csv.SetTitle("Issues");

					return csv;
				}
			}
		}

		public class IssueAndTodos {
			public IssueModel.IssueModel_Recurrence Issue { get; set; }
			public List<TodoModel> Todos { get; set; }
		}
		public static async Task<SLDocument> GetIssuesAndTodosSpreadsheetAtOrganization(UserOrganizationModel caller, long orgId, bool loadDetails = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ManagingOrganization(orgId);

					IssueModel issueAlias = null;
					//var issues = s.QueryOver<IssueModel>().Where(x => x.OrganizationId == ordId && x.DeleteTime == null);
					var issuesQ = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.JoinAlias(x => x.Issue, () => issueAlias)
						.Where(x => x.DeleteTime == null && issueAlias.OrganizationId == orgId && issueAlias.DeleteTime == null)
						.Future();


					var todosQ = s.QueryOver<TodoModel>()
						.Where(x => x.ForModel == "IssueModel" && x.DeleteTime == null && x.OrganizationId == orgId)
						.Future();

					var result = new List<IssueAndTodos>();

					var allTodos = todosQ.ToList();


					var pads = new List<string>();
					foreach (var issue in issuesQ) {
						var iat = new IssueAndTodos();
						iat.Issue = issue;
						iat.Todos = allTodos.Where(x => x.ForModelId == issue.Issue.Id && x.ForModel == "IssueModel").ToList();
						pads.Add(issue.Issue.PadId);
						pads.AddRange(iat.Todos.Select(x => x.PadId));
						result.Add(iat);
					}

					var padLookup = new Dictionary<string, string>();

					if (loadDetails) {
						padLookup = await PadAccessor.GetTexts(pads);
					}

					var issuesSheet = new Csv("Issues");
					foreach (var iat in result) {
						var ir = iat.Issue;
						var issue = ir.Issue;
						var id = issue.Id;
						issuesSheet.Add("" + id, "Issue", issue.Message);
						if (loadDetails) {
							var details = padLookup.GetOrDefault(issue.PadId, "");
							issuesSheet.Add("" + id, "Details", details);
						}
						issuesSheet.Add("" + id, "Owner", ir.Owner.Name);
						issuesSheet.Add("" + id, "Completed", ir.CloseTime.NotNull(x => x.Value.ToShortDateString()));
						issuesSheet.Add("" + id, "Created", issue.CreateTime.ToShortDateString());

						issuesSheet.Add("" + id, "# Todos", "" + iat.Todos.Count());

					}

					var todoSheet = new Csv("Todos");
					foreach (var todo in result.SelectMany(x => x.Todos)) {
						var id = todo.Id;
						todoSheet.Add("" + id, "Todo", todo.Message);
						if (loadDetails) {
							var details = padLookup.GetOrDefault(todo.PadId, "");
							todoSheet.Add("" + id, "Details", details);
						}
						todoSheet.Add("" + id, "Owner", todo.AccountableUser.Name);
						todoSheet.Add("" + id, "Completed", todo.CompleteTime.NotNull(x => x.Value.ToShortDateString()));
						todoSheet.Add("" + id, "Created", todo.CreateTime.ToShortDateString());
						todoSheet.Add("" + id, "IssueId", "" + todo.ForModelId);
					}



					return CsvUtility.ToXls(issuesSheet, todoSheet);

				}
			}
		}

		public static async Task<StringBuilder> BuildIssuesSolvedTable(List<IssueModel.IssueModel_Recurrence> issues, string title = null, long? recurrenceId = null, bool showDetails = false, Dictionary<string, HtmlString> padLookup = null) {
			title = title.NotNull(x => x.Trim()) ?? "Issues";
			var table = new StringBuilder();
			try {

				table.Append(@"<table width=""100%""  border=""0"" cellpadding=""0"" cellspacing=""0"">");
				table.Append(@"<tr><th colspan=""2"" align=""left"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">" + title + @"</th></tr>");
				var i = 1;
				if (issues.Any()) {
					var org = issues.FirstOrDefault().NotNull(x => x.Issue.Organization);
					var now = issues.FirstOrDefault().NotNull(x => x.Issue.Organization.ConvertFromUTC(DateTime.UtcNow).Date);
					var format = org.NotNull(x => x.Settings.NotNull(y => y.GetDateFormat())) ?? "MM-dd-yyyy";
					foreach (var issue in issues.OrderBy(x => x.CloseTime)) {
						var url = "#";
						if (recurrenceId != null)
							url = Config.BaseUrl(org) + @"L10/Details/" + recurrenceId + "#/Issues";

						table.Append(@" <tr><td width=""1px"" style=""vertical-align: top;""><b><a style=""color:#333333;text-decoration:none;"" href=""" + url + @""">")
							.Append(i).Append(@". </a></b></td><td align=""left""><b><a style=""color:#333333;text-decoration:none;"" href=""" + url + @""">")
							.Append(issue.Issue.Message).Append(@"</a></b></td></tr>");

						if (showDetails) {
							HtmlString details = null;
							if (padLookup == null || !padLookup.ContainsKey(issue.Issue.PadId)) {
								details = await PadAccessor.GetHtml(issue.Issue.PadId);
							} else {
								details = padLookup[issue.Issue.PadId];
							}

							if (!String.IsNullOrWhiteSpace(details.ToHtmlString())) {
								table.Append(@"<tr><td></td><td><i style=""font-size:12px;"">&nbsp;&nbsp;<a style=""color:#333333;text-decoration: none;"" href=""" + url + @""">").Append(details.ToHtmlString()).Append("</a></i></td></tr>");
							}
						}

						i++;
					}
				}
			} catch (Exception e) {
				log.Error(e);
			}
			table.Append("</table>");
			return table;
		}
		
		#region Deleted
		// [Obsolete("Method is broken",true)]
		//private static void RecurseIssue(StringBuilder sb, int index, IssueModel.IssueModel_Recurrence parent, int depth, bool includeDetails) {
		//	var time = "";
		//	if (parent.CloseTime != null)
		//		time = parent.CloseTime.Value.ToShortDateString();
		//	sb.Append(index).Append(",")
		//		.Append(depth).Append(",")
		//		.Append(Csv.CsvQuote(parent.Owner.NotNull(x => x.GetName()))).Append(",")
		//		.Append(parent.CreateTime.ToShortDateString()).Append(",")
		//		.Append(time).Append(",");
		//	sb.Append(Csv.CsvQuote(parent.Issue.Message)).Append(",");

		//	sb.AppendLine();
		//	foreach (var child in parent._ChildIssues)
		//		RecurseIssue(sb, index, child, depth + 1, includeDetails);
		//}

		//public static object EditIssue(UserOrganizationModel caller, long issueRecurrenceId, string message, long? accountableUserId=null, int? priority=null) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			using (var rt = RealTimeUtility.Create()) {

		//				var perm = PermissionsUtility.Create(s, caller).EditIssueRecurrence(issueRecurrenceId);

		//				var found = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);

		//				if (message != null)
		//					found.Issue.Message = message;
		//				if (accountableUserId > 0) {
		//					perm.EditIssueRecurrence(found.Id).ViewUserOrganization(accountableUserId.Value,false);
		//					found.Owner = s.Load<UserOrganizationModel>(accountableUserId.Value);
		//				}
		//				if (priority != null) {
		//					found.Priority = priority.Value;
		//				}

		//				s.Update(found);

		//				if (found.Recurrence!=null && found.Recurrence.Id > 0)
		//					rt.UpdateRecurrences(found.Recurrence.Id).Update(new AngularIssue(found));


		//				tx.Commit();
		//				s.Flush();
		//				return found;

		//			}
		//		}
		//	}
		//} 
		#endregion

	}
}
