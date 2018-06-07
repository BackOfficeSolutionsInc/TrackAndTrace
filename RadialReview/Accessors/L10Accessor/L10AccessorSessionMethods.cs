using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using NHibernate;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Session Methods
		public static L10Meeting.L10Meeting_Log _GetCurrentLog(ISession s, UserOrganizationModel caller, long meetingId, long userId, bool nullOnUnstarted = false) {
			var found = s.QueryOver<L10Meeting.L10Meeting_Log>()
				.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == userId && x.EndTime == null)
				.List().OrderByDescending(x => x.StartTime)
				.FirstOrDefault();
			if (found == null && !nullOnUnstarted)
				throw new PermissionsException("Meeting log does not exist");
			return found;
		}
		public static L10Meeting _GetCurrentL10Meeting(ISession s, PermissionsUtility perms, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
			var meeting = _GetCurrentL10Meeting_Unsafe(s, recurrenceId, nullOnUnstarted, load, loadLogs);
			if (meeting == null)
				return null;
			perms.ViewL10Meeting(meeting.Id);
			return meeting;
		}

		public static L10Meeting _GetCurrentL10Meeting_Unsafe(ISession s, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
			var found = s.QueryOver<L10Meeting>().Where(x =>
					x.StartTime != null &&
					x.CompleteTime == null &&
					x.DeleteTime == null &&
					x.L10RecurrenceId == recurrenceId
				).List().ToList();

			if (!found.Any()) {
				if (nullOnUnstarted)
					return null;
				throw new MeetingException(recurrenceId,"Meeting has not been started.", MeetingExceptionType.Unstarted);
			}
			if (found.Count != 1) {
				//throw new MeetingException("Too many open meetings.", MeetingExceptionType.TooMany);
				found = found.OrderByDescending(x => x.StartTime).ToList();
			}
			var meeting = found.First();
			if (load)
				_LoadMeetings(s, true, true, true, meeting);

			if (loadLogs)
				_LoadMeetingLogs(s, meeting);
			return meeting;
		}

		private static void _RecursiveCloseIssues(ISession s, List<long> parentIssue_RecurIds, DateTime now) {
			if (parentIssue_RecurIds.Count == 0)
				return;

			var children = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.CloseTime == null)
				.WhereRestrictionOn(x => x.ParentRecurrenceIssue.Id)
				.IsIn(parentIssue_RecurIds)
				.List().ToList();
			foreach (var c in children) {
				c.CloseTime = now;

				//Needs updating for RealTime

				s.Update(c);
			}
			_RecursiveCloseIssues(s, children.Select(x => x.Id).ToList(), now);
		}

		public static List<PermItem> GetAdmins(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return perms.GetAdmins(PermItem.ResourceType.L10Recurrence, recurrenceId);
				}
			}
		}

		public static List<L10Recurrence> _GetAllL10RecurrenceAtOrganization(ISession s, UserOrganizationModel caller, long organizationId) {
			PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
			return s.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
				.List().ToList();
		}
		public static List<L10Recurrence> _GetAllConnectedL10Recurrence(ISession s, UserOrganizationModel caller, long recurrenceId) {
			var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

			var userIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
				.Select(x => x.User.Id)
				.List<long>().ToList();

			var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.User.Id).IsIn(userIds)
				.Select(x => x.L10Recurrence.Id)
				.List<long>().ToList();

			return s.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Id).IsIn(recurrenceIds)
				.List().ToList();

		}
		#endregion
	}
}