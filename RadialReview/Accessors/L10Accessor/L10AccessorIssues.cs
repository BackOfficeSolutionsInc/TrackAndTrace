using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.AspNet.SignalR;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.NHibernate;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {


		#region Issues			
		public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, DateTime? meetingStart = null) {
			var mstart = meetingStart ?? DateTime.MaxValue;
			perms.ViewL10Recurrence(recurrenceId);
			//TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.

			var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x =>
					x.DeleteTime == null && x.Recurrence.Id == recurrenceId &&
					(x.CloseTime == null || x.CloseTime >= mstart)
				).Fetch(x => x.Issue).Eager
				.List().ToList();

			return _PopulateChildrenIssues(issues);
		}

		public static List<IssueModel.IssueModel_Recurrence> GetSolvedIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, DateRange range) {
			perms.ViewL10Recurrence(recurrenceId);

			var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
				.Where(x => x.CloseTime >= range.StartTime && x.CloseTime <= range.EndTime)
				.Fetch(x => x.Issue).Eager
				.List().ToList();

			return _PopulateChildrenIssues(issues);
		}

		public static List<IssueModel.IssueModel_Recurrence> GetIssuesForMeeting(UserOrganizationModel caller, long meetingId, bool includeResolved) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var meeting = s.Get<L10Meeting>(meetingId);
					var recurrenceId = meeting.L10RecurrenceId;
					var perms = PermissionsUtility.Create(s, caller);
					return GetIssuesForRecurrence(s, perms, recurrenceId, meeting.StartTime);
				}
			}
		}

		public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeResolved) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetIssuesForRecurrence(s, perms, recurrenceId);
				}
			}
		}

		public static List<IssueModel.IssueModel_Recurrence> GetAllIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeCompleted = true, DateRange range = null) {
			perms.ViewL10Recurrence(recurrenceId);

			//TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.
			var issuesQ = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId);

			if (range != null && includeCompleted) {
				var st = range.StartTime.AddDays(-1);
				var et = range.EndTime.AddDays(1);
				issuesQ = issuesQ.Where(x => x.CloseTime == null || (x.CloseTime >= st && x.CloseTime <= et));
			}

			if (!includeCompleted)
				issuesQ = issuesQ.Where(x => x.CloseTime == null);

			var issues = issuesQ.Fetch(x => x.Issue).Eager.List().ToList();

			return _PopulateChildrenIssues(issues);
		}

		public static async Task CompleteIssue(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceIssue) {
			var issue = s.Get<IssueModel.IssueModel_Recurrence>(recurrenceIssue);
			perm.EditL10Recurrence(issue.Recurrence.Id);
			if (issue.CloseTime != null)
				throw new PermissionsException("Issue already deleted.");
			await IssuesAccessor.EditIssue(OrderedSession.Indifferent(s), perm, recurrenceIssue, complete: true);
		}

		public static void UpdateIssues(UserOrganizationModel caller, long recurrenceId, /*IssuesDataList*/L10Controller.IssuesListVm model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var ids = model.GetAllIds();
					var found = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
						.WhereRestrictionOn(x => x.Id).IsIn(ids)
						//.Fetch(x=>x.Issue).Eager
						.List().ToList();

					if (model.orderby != null) {
						var recur = s.Get<L10Recurrence>(recurrenceId);
						recur.OrderIssueBy = model.orderby;
						s.Update(recur);
					}


					var ar = SetUtility.AddRemove(ids, found.Select(x => x.Id));

					if (ar.RemovedValues.Any())
						throw new PermissionsException("You do not have permission to edit this issue.");
					if (ar.AddedValues.Any())
						throw new PermissionsException("Unreachable.");

					var recurrenceIssues = found.ToList();

					foreach (var e in model.GetIssueEdits()) {
						var f = recurrenceIssues.First(x => x.Id == e.RecurrenceIssueId);
						var update = false;
						if (f.ParentRecurrenceIssue.NotNull(x => x.Id) != e.ParentRecurrenceIssueId) {
							f.ParentRecurrenceIssue = (e.ParentRecurrenceIssueId == null) ? null : recurrenceIssues.First(x => x.Id == e.ParentRecurrenceIssueId);
							update = true;
						}

						if (f.Ordering != e.Order) {
							f.Ordering = e.Order;
							update = true;
						}

						if (update)
							s.Update(f);
					}

					var json = Json.Encode(model);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), model.connectionId);

					//group.deserializeIssues(".issues-list", model);
					group.setIssueOrder(model.issues);
					var issues = GetAllIssuesForRecurrence(s, perm, recurrenceId)
						.OrderBy(x => x.Ordering)
						.Select(x => new AngularIssue(x))
						.ToList();

					group.update(new AngularRecurrence(recurrenceId) {
						IssuesList = new AngularIssuesList(recurrenceId) {
							Issues = AngularList.Create(AngularListType.ReplaceAll, issues)
						}
					});
          

					Audit.L10Log(s, caller, recurrenceId, "UpdateIssues", ForModel.Create<L10Recurrence>(recurrenceId));

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static VtoItem_String MoveIssueToVto(UserOrganizationModel caller, long issue_recurrence, string connectionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var perm = PermissionsUtility.Create(s, caller);

						var str = IssuesAccessor.MoveIssueToVto(s, perm, issue_recurrence, connectionId);


						tx.Commit();
						s.Flush();
						return str;
					}
				}
			}
		}

		public async static Task<IssueModel.IssueModel_Recurrence> MoveIssueFromVto(UserOrganizationModel caller, long vtoIssue) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;
					var perm = PermissionsUtility.Create(s, caller);

					var issueRecur = await IssuesAccessor.MoveIssueFromVto(s, perm, vtoIssue);

					
					tx.Commit();
					s.Flush();
					return issueRecur;
				}
			}
		}

		#endregion
	}
}