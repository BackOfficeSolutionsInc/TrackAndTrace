using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using Amazon.EC2.Model;
using ImageResizer.Configuration.Issues;
using MathNet.Numerics;
using Microsoft.AspNet.SignalR;
using NHibernate.Linq;
using NHibernate.Transform;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Audit;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.AV;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scheduler;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using NHibernate;
using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Enums;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Base;

namespace RadialReview.Accessors
{
	public class L10Accessor : BaseAccessor
	{
		public static MeasurableModel TodoMeasurable = new MeasurableModel(){
				Id = -10001,
				Title = "To-Do Completion",
				_Editable = false,
				Goal = 90,
				GoalDirection = LessGreater.GreaterThan,
				UnitType = UnitType.Percent,
		};

		public static MeasurableModel GenerateTodoMeasureable(UserOrganizationModel forUser)
		{
			return new MeasurableModel(){
				Id = -10001-forUser.Id,
				Title = "To-Do Completion "+forUser.GetName(),
				_Editable = false,
				Goal = 90,
				GoalDirection = LessGreater.GreaterThan,
				UnitType = UnitType.Percent,
			};
		}

		#region Load Members
		public static void _LoadMeetingLogs(ISession s, params L10Meeting[] meetings)
		{
			var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();
			if (meetingIds.Any())
			{
				var allLogs = s.QueryOver<L10Meeting.L10Meeting_Log>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var now = DateTime.UtcNow;
				foreach (var m in meetings.Where(x => x != null))
				{
					m._MeetingLogs = allLogs.Where(x => m.Id == x.L10Meeting.Id).ToList();

					m._MeetingLeaderPageDurations = m._MeetingLogs
						.Where(x => x.User.Id == m.MeetingLeader.Id && x.EndTime != null)
						.GroupBy(x => x.Page)
						.Select(x =>
							Tuple.Create(
								x.First().Page,
								x.Sum(y => ((y.EndTime ?? now) - y.StartTime).TotalMinutes)
								)).ToList();

					var curPage = m._MeetingLogs
						.Where(x => x.User.Id == m.MeetingLeader.Id && x.EndTime == null)
						.OrderByDescending(x => x.StartTime)
						.FirstOrDefault();

					if (curPage != null)
					{
						m._MeetingLeaderCurrentPage = curPage.Page;
						m._MeetingLeaderCurrentPageStartTime = curPage.StartTime;
						m._MeetingLeaderCurrentPageBaseMinutes = m._MeetingLeaderPageDurations.Where(x => x.Item1 == curPage.Page).Sum(x => x.Item2);
					}
				}
			}
		}
		public static void _LoadMeetings(ISession s, bool loadUsers, bool loadMeasurables, bool loadRocks, params L10Meeting[] meetings)
		{
			var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();

			if (meetingIds.Any())
			{
				var allAttend = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var allMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var allRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();

				foreach (var m in meetings){
					allMeasurables.Add(new L10Meeting.L10Meeting_Measurable(){
						_Ordering = -1,
						Id = -1,
						L10Meeting = m,
						Measurable = TodoMeasurable
					});
				}

				foreach (var m in meetings.Where(x => x != null))
				{
					m._MeetingAttendees = allAttend.Where(x => m.Id == x.L10Meeting.Id).ToList();
					m._MeetingMeasurables = allMeasurables.Where(x => m.Id == x.L10Meeting.Id).ToList();
					m._MeetingRocks = allRocks.Where(x => m.Id == x.L10Meeting.Id).ToList();
					if (loadUsers)
					{
						foreach (var u in m._MeetingAttendees)
						{
							try{
								u.User.GetName();
								u.User.ImageUrl();
							}catch (Exception){
								
							}
						}
					}
					if (loadMeasurables)
					{
						foreach (var u in m._MeetingAttendees){
							m._MeetingMeasurables.Add(new L10Meeting.L10Meeting_Measurable()
							{
								_Ordering = -1,
								Id = -1,
								L10Meeting = m,
								Measurable = GenerateTodoMeasureable(u.User)
							});
						}

						foreach (var u in m._MeetingMeasurables)
						{
							try{
								u.Measurable.AccountableUser.GetName();
								u.Measurable.AccountableUser.ImageUrl();
								u.Measurable.AdminUser.GetName();
								u.Measurable.AdminUser.ImageUrl();
							}
							catch (Exception e){
								
							}
						}
					}
					if (loadRocks)
					{
						foreach (var u in m._MeetingRocks)
						{
							try{
								u.ForRock.AccountableUser.GetName();
								u.ForRock.AccountableUser.ImageUrl();
							}catch (Exception){
								
							}
						}
					}
				}
			}

			/*var recurrenceIds = meetings.Where(x => x != null).Select(x => x.L10RecurrenceId).Distinct().ToArray();

			if (recurrenceIds.Any()){
				
			}*/
		}
		public static void _LoadRecurrences(ISession s, bool loadUsers, bool loadMeasurables, bool loadRocks, params L10Recurrence[] all)
		{
			var recurrenceIds = all.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();

			if (recurrenceIds.Any())
			{
				var allAttend = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.List().ToList();
				var allMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.List().ToList();
				var allRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.List().ToList();
				var allNotes = s.QueryOver<L10Note>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.Recurrence.Id).IsIn(recurrenceIds)
					.List().ToList();
				/*	var allAgendaItems = s.QueryOver<L10Recurrence.L10AgendaItem>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Recurrence.Id).IsIn(recurrenceIds)
						.List().ToList();*/

				foreach (var a in all.Where(x => x != null))
				{
					a._DefaultAttendees = allAttend.Where(x => a.Id == x.L10Recurrence.Id).ToList();
					a._DefaultMeasurables = allMeasurables.Where(x => a.Id == x.L10Recurrence.Id).ToList();
					a._DefaultRocks = allRocks.Where(x => a.Id == x.L10Recurrence.Id).ToList();
					a._MeetingNotes = allNotes.Where(x => a.Id == x.Recurrence.Id).ToList();

					if (loadUsers)
					{
						foreach (var u in a._DefaultAttendees)
						{
							u.User.GetName();
							u.User.ImageUrl(true);
						}
					}
					if (loadMeasurables)
					{
						foreach (var u in a._DefaultMeasurables)
						{
							u.Measurable.AccountableUser.GetName();
							u.Measurable.AccountableUser.ImageUrl(true);
							u.Measurable.AdminUser.GetName();
							u.Measurable.AdminUser.ImageUrl(true);
						}
					}
					if (loadRocks)
					{
						foreach (var u in a._DefaultRocks)
						{
							var b = u.ForRock.Rock;
							var c = u.ForRock.Period.Name;
						}
					}
				}
			}

		}
		private static List<IssueModel.IssueModel_Recurrence> _PopulateChildrenIssues(List<IssueModel.IssueModel_Recurrence> list)
		{
			var output = list.Where(x => x.ParentRecurrenceIssue == null)/*.Select(x =>{
				x.Issue._Order = x.Ordering;
				x.Issue._RecurrenceIssueId = x.Id;
				return x.Issue;
			})*/.ToList();
			foreach (var o in output)
			{
				_RecurseChildrenIssues(o, list);
			}
			foreach (var o in output){
				try{
					if (o.Owner != null){
						o.Owner.GetName();
						o.Owner.GetImageUrl();
					}
				}catch (Exception){
					
				}
			}
			output = output.OrderBy(x => x.Ordering).ToList();

			return output;

		}
		private static void _RecurseChildrenIssues(IssueModel.IssueModel_Recurrence issue, IEnumerable<IssueModel.IssueModel_Recurrence> list)
		{
			if (issue._ChildIssues != null)
				return;
			issue._ChildIssues = list.Where(x => x.ParentRecurrenceIssue != null && x.ParentRecurrenceIssue.Id == issue.Id)/*.Select(x =>{
				x.Issue._Order = x.Ordering;
				x.Issue._RecurrenceIssueId = x.Id;
				return x.Issue;
			})*/.ToList();

			foreach (var i in issue._ChildIssues)
			{
				_RecurseChildrenIssues(i, list);
			}
		}

		#endregion

		#region Session Methods
		public static L10Meeting.L10Meeting_Log _GetCurrentLog(ISession s, UserOrganizationModel caller, long meetingId, long userId, bool nullOnUnstarted = false)
		{
			var found = s.QueryOver<L10Meeting.L10Meeting_Log>()
				.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == userId && x.EndTime == null)
				.List().OrderByDescending(x => x.StartTime)
				.FirstOrDefault();
			if (found == null && !nullOnUnstarted)
				throw new PermissionsException("Meeting log does not exist");
			return found;
		}
		public static L10Meeting _GetCurrentL10Meeting(ISession s, PermissionsUtility perms, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false)
		{
			var found = s.QueryOver<L10Meeting>().Where(x =>
					x.StartTime != null &&
					x.CompleteTime == null &&
					x.DeleteTime == null &&
					x.L10RecurrenceId == recurrenceId
				).List().ToList();

			if (!found.Any())
			{
				if (nullOnUnstarted)
					return null;
				throw new MeetingException("Meeting has not been started.", MeetingExceptionType.Unstarted);
			}
			if (found.Count != 1)
			{
				throw new MeetingException("Too many open meetings.", MeetingExceptionType.TooMany);
			}
			var meeting = found.First();
			perms.ViewL10Meeting(meeting.Id);
			if (load)
				_LoadMeetings(s, true, true, true, meeting);

			if (loadLogs)
				_LoadMeetingLogs(s, meeting);

			return meeting;
		}
		private static void _RecursiveCloseIssues(ISession s, List<long> parentIssue_RecurIds, DateTime now)
		{
			if (parentIssue_RecurIds.Count == 0)
				return;

			var children = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.CloseTime == null)
				.WhereRestrictionOn(x => x.ParentRecurrenceIssue.Id)
				.IsIn(parentIssue_RecurIds)
				.List().ToList();
			foreach (var c in children)
			{
				c.CloseTime = now;
				s.Update(c);
			}
			_RecursiveCloseIssues(s, children.Select(x => x.Id).ToList(), now);
		}
		public static List<L10Recurrence> _GetAllL10RecurrenceAtOrganization(ISession s, UserOrganizationModel caller, long organizationId)
		{
			PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
			return s.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
				.List().ToList();
		}
		public static List<L10Recurrence> _GetAllConnectedL10Recurrence(ISession s, UserOrganizationModel caller, long recurrenceId)
		{
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

		#region Get

		public static L10Meeting GetPreviousMeeting(ISession s, PermissionsUtility perms, long recurrenceId)
		{
			perms.ViewL10Recurrence(recurrenceId);
			var previousMeeting = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == recurrenceId && x.CompleteTime != null).OrderBy(x => x.CompleteTime).Desc.Take(1).SingleOrDefault();
			return previousMeeting;
		}

		public static L10Recurrence GetL10Recurrence(UserOrganizationModel caller, long recurrenceId, bool load)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetL10Recurrence(s, perms, recurrenceId, load);
				}
			}
		}
		public static L10Recurrence GetL10Recurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool load)
		{
			perms.ViewL10Recurrence(recurrenceId);
			var found = s.Get<L10Recurrence>(recurrenceId);
			if (load)
				_LoadRecurrences(s, true, true, true, found);
			return found;
		}


		public static List<L10VM> GetVisibleL10Meetings(UserOrganizationModel caller, long userId, bool loadUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewUsersL10Meetings(userId);

					var allRecurrences = new List<L10Recurrence>();
					if (caller.ManagingOrganization)
					{
						var orgRecurrences = s.QueryOver<L10Recurrence>().Where(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null).List().ToList();
						allRecurrences.AddRange(orgRecurrences);
					}
					//Who should we get this data for? Just Self, or also subordiantes?
					var accessibleUserIds = new[] { userId };
					var user = s.Get<UserOrganizationModel>(userId);
					if (user.Organization.Settings.ManagersCanViewSubordinateL10)
						accessibleUserIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, userId).ToArray();

					var attendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.User.Id).IsIn(accessibleUserIds)
						.List().ToList();
					var uniqueL10Ids = attendee.Distinct(x => x.L10Recurrence.Id).Select(x => x.L10Recurrence.Id).ToList();
					//Actually load the Recurrences
					var loadedL10 = s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids).List().ToList();
					allRecurrences.AddRange(loadedL10);

					//Load extra data
					allRecurrences = allRecurrences.Distinct(x => x.Id).ToList();
					_LoadRecurrences(s, true, false, false, allRecurrences.ToArray());

					//Make a lookup for self attendance
					var attending = attendee.Where(x => userId == x.User.Id).Select(x => x.L10Recurrence.Id).ToArray();
					return allRecurrences.Select(x => new L10VM(x)
					{
						IsAttendee = attending.Any(y => y == x.Id)
					}).ToList();
				}
			}
		}
		public static string GetCurrentL10MeetingLeaderPage(UserOrganizationModel caller, long meetingId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var leaderId = s.Get<L10Meeting>(meetingId).MeetingLeader.Id;
					var leaderpage = s.QueryOver<L10Meeting.L10Meeting_Log>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == leaderId && x.EndTime == null)
						.List().OrderByDescending(x => x.StartTime)
						.FirstOrDefault();
					return leaderpage.NotNull(x => x.Page);
				}
			}
		}
		//public static L10Meeting.L10Meeting_Connection GetConnection(ISession s, UserOrganizationModel caller, long recurrenceId)
		//{
		//	var meeting = _GetCurrentL10Meeting(s, caller, recurrenceId);
		//	var meetingId = meeting.Id;
		//	var found = s.QueryOver<L10Meeting.L10Meeting_Connection>().Where(x =>
		//			x.DeleteTime == null &&
		//			x.L10Meeting.Id == meetingId &&
		//			x.User.Id == caller.Id
		//		).SingleOrDefault<L10Meeting.L10Meeting_Connection>();
		//	if (found == null)
		//		throw new PermissionsException("You do not have access to this meeting.");
		//	return found;
		//}
		public static L10Meeting GetCurrentL10Meeting(UserOrganizationModel caller, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return _GetCurrentL10Meeting(s, perms, recurrenceId, nullOnUnstarted, load, loadLogs);
				}
			}
		}
		public static List<TodoModel> GetTodosForRecurrence(UserOrganizationModel caller, long recurrenceId, long meetingId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId).ViewL10Meeting(meetingId);

					var meeting = s.Get<L10Meeting>(meetingId);

					if (meeting.L10RecurrenceId != recurrenceId)
						throw new PermissionsException("Incorrect Recurrence Id");

					var previous = GetPreviousMeeting(s, perms, recurrenceId);

					var everythingAfter = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));

					if (previous != null)
						everythingAfter = previous.CompleteTime.Value;

					var todoList = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId);

					if (meeting.CompleteTime != null)
						todoList = todoList.Where(x => x.CompleteTime == null || (x.CompleteTime < meeting.CompleteTime && x.CreateTime < meeting.StartTime));
					else
					{
						todoList = todoList.Where(x => x.CompleteTime == null || x.CompleteTime > everythingAfter);
					}
					return todoList.Fetch(x => x.AccountableUser).Eager.List().ToList();
				}
			}
		}

		public static List<TodoModel> GetAllTodosForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId)
		{
			perms.ViewL10Recurrence(recurrenceId);

			var todoList = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId).List().ToList();
			foreach (var t in todoList)
			{
				var a = t.AccountableUser.GetName();
				var b = t.AccountableUser.ImageUrl(true);
			}
			return todoList;
		}
		public static List<IssueModel.IssueModel_Recurrence> GetAllIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId)
		{
			perms.ViewL10Recurrence(recurrenceId);

			//TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.

			var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
				.Fetch(x => x.Issue).Eager
				.List().ToList();

			return _PopulateChildrenIssues(issues);
		}


		public static List<ScoreModel> GetScoresForRecurrence(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller);//.ViewL10Recurrence(recurrenceId);
					return GetScoresForRecurrence(s, perm, recurrenceId);
				}

			}
		}
		public static List<ScoreModel> GetScoresForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId)
		{
			perm.ViewL10Recurrence(recurrenceId);

			var r = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();
			var measurables = r.Distinct(x => x.Measurable.Id).Select(x => x.Measurable.Id).ToList();

			var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.MeasurableId).IsIn(measurables).List().ToList();

			var todoCompletion = GetAllTodosForRecurrence(s, perm, recurrenceId);


			var todoScores = scores.GroupBy(x => x.ForWeek).SelectMany(w =>{
				try{
					var ss = todoCompletion.Where(x => w.Key.AddDays(-7) <= x.DueDate.StartOfWeek(DayOfWeek.Sunday) && x.DueDate.StartOfWeek(DayOfWeek.Sunday) < w.Key).Select(x =>
					{
						if (x.CompleteTime == null)
							return 0;
						if (x.CompleteTime.Value <= x.DueDate)
							return 1;
						return 0;
					}).ToList();
					decimal? percent = null;
					if (ss.Any()){
						percent = Math.Round(ss.Average(x => (decimal) x)*100m,1);
					}

					return new ScoreModel(){
						_Editable = false,
						AccountableUserId = -1,
						ForWeek = w.Key,
						Measurable = TodoMeasurable,
						Measured = percent,
						MeasurableId = TodoMeasurable.Id,
						
					}.AsList();
				}
				catch (Exception e){
					return new List<ScoreModel>();
				}
			});
			scores.AddRange(todoScores);

			var individualTodoScores = scores.GroupBy(x => x.ForWeek).SelectMany(ww =>{
				return ww.GroupBy(x => x.AccountableUserId).SelectMany(w =>{
					var a = w.First().AccountableUser;
					try{
						var ss = todoCompletion.Where(x => ww.Key.AddDays(-7) <= x.DueDate.StartOfWeek(DayOfWeek.Sunday) && x.DueDate.StartOfWeek(DayOfWeek.Sunday) < ww.Key && x.AccountableUserId==a.Id).Select(x =>{
							if (x.CompleteTime == null)
								return 0;
							if (x.CompleteTime.Value <= x.DueDate)
								return 1;
							return 0;
						}).ToList();
						decimal? percent = null;
						if (ss.Any()){
							percent = Math.Round(ss.Average(x => (decimal) x)*100m, 1);
						}

						var mm = GenerateTodoMeasureable(a);

						return new ScoreModel(){
							_Editable = false,
							AccountableUserId = a.Id,
							ForWeek = ww.Key,
							Measurable = mm,
							Measured = percent,
							MeasurableId = mm.Id,

						}.AsList();
					}
					catch (Exception e){
						return new List<ScoreModel>();
					}
				});
			});
			scores.AddRange(individualTodoScores);

			var userQueries = scores.SelectMany(x =>{
				var o = new List<long>(){
					x.Measurable.AccountableUser.NotNull(y => y.Id),
					x.AccountableUser.NotNull(y => y.Id),
					x.Measurable.AdminUser.NotNull(y => y.Id),
				};
				return o;
			}).Distinct().ToList();

			/*var userList = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(userQueries).List().ToList();
			userList.ForEach(x=>{x.GetName();x.ImageUrl(true));
	{
		 
	}
			var userLookup = userList.ToDictionary(x=>x.Id,x=>x);*/


			//Touch 
			foreach (var a in scores)
			{
				try{
				//	a.Measurable.AccountableUser = userLookup[];

					var i = a.Measurable.Goal;
					var u = a.Measurable.AccountableUser.GetName();
					var v = a.Measurable.AccountableUser.ImageUrl(true);
					var j = a.AccountableUser.GetName();
					var k = a.AccountableUser.ImageUrl(true);
					var u1 = a.Measurable.AdminUser.GetName();
					var v1 = a.Measurable.AdminUser.ImageUrl(true);
				}
				catch (Exception e){
					//Opps
				}
			}

			return scores;
		}
		public static List<L10Meeting> GetL10Meetings(UserOrganizationModel caller, long recurrenceId, bool load = false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var o = s.QueryOver<L10Meeting>()
						.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
						.List().ToList();
					if (load)
						_LoadMeetings(s, true, true, true, o.ToArray());

					return o;

				}
			}
		}
		public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(UserOrganizationModel caller, long meetingId, bool includeResolved)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var meeting = s.Get<L10Meeting>(meetingId);
					var recurrenceId = meeting.L10RecurrenceId;

					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					//TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.

					var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x =>
							x.DeleteTime == null && x.Recurrence.Id == recurrenceId &&
							(x.CloseTime == null || x.CloseTime >= meeting.StartTime)
						).Fetch(x => x.Issue).Eager
						.List().ToList();

					/*var query = s.QueryOver<IssueModel>();
					if (includeResolved)
						query = query.Where(x => x.DeleteTime == null);
					else
						query = query.Where(x => x.DeleteTime == null && x.CloseTime == null);					
					var issues =  query.WhereRestrictionOn(x => x.Id).IsIn(issueIds).List().ToList();*/

					return _PopulateChildrenIssues(issues);
				}
			}
		}
		//Finds all first degree connectioned L10Recurrences
		public static List<L10Recurrence> GetAllConnectedL10Recurrence(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return _GetAllConnectedL10Recurrence(s, caller, recurrenceId);
				}
			}
		}
		public static List<L10Recurrence> GetAllL10RecurrenceAtOrganization(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return _GetAllL10RecurrenceAtOrganization(s, caller, organizationId);
				}
			}
		}
		#endregion

		#region Update
		public static void EditL10Recurrence(UserOrganizationModel caller, L10Recurrence l10Recurrence)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller);
					if (l10Recurrence.Id == 0)
					{
						perm.CreateL10Recurrence(caller.Organization.Id);
						l10Recurrence.CreatedById = caller.Id;
					}
					else
						perm.EditL10Recurrence(l10Recurrence.Id);

					//s.UpdateLists(l10Recurrence,DateTime.UtcNow,x=>x.DefaultAttendees,x=>x.DefaultMeasurables);
					/*if (l10Recurrence.Id != 0){
						var old = s.Get<L10Recurrence>(l10Recurrence.Id);
						//SetUtility.AddRemove(old.DefaultAttendees,l10Recurrence.DefaultAttendees,x=>x.)
					}*/
					var old = s.Get<L10Recurrence>(l10Recurrence.Id);
					_LoadRecurrences(s, false, false, false, old);

					var oldMeeting = _GetCurrentL10Meeting(s, perm, l10Recurrence.Id, true, true);
					SetUtility.AddedRemoved<MeasurableModel> updateMeasurables = null;
					if (oldMeeting != null)
					{
						updateMeasurables = SetUtility.AddRemove(oldMeeting._MeetingMeasurables.Select(x => x.Measurable), l10Recurrence._DefaultMeasurables.Select(x => x.Measurable), x => x.Id);
					}
					SetUtility.AddedRemoved<RockModel> updateRocks = null;
					if (oldMeeting != null)
					{
						updateRocks = SetUtility.AddRemove(
							oldMeeting._MeetingRocks.Select(x => x.ForRock),
							l10Recurrence._DefaultRocks.Select(x => x.ForRock),
							x => x.Id);
					} 
					
					SetUtility.AddedRemoved<UserOrganizationModel> updateAttendees = null;
					if (oldMeeting != null)
					{
						updateAttendees = SetUtility.AddRemove(
							oldMeeting._MeetingAttendees.Select(x => x.User),
							l10Recurrence._DefaultAttendees.Select(x => x.User),
							x => x.Id);
					}

					var now = DateTime.UtcNow;
					s.UpdateList(old.NotNull(x => x._DefaultAttendees), l10Recurrence._DefaultAttendees, now);
					s.UpdateList(old.NotNull(x => x._DefaultMeasurables), l10Recurrence._DefaultMeasurables, now);
					s.UpdateList(old.NotNull(x => x._DefaultRocks), l10Recurrence._DefaultRocks, now);

					s.Evict(old);

					s.SaveOrUpdate(l10Recurrence);
					if (updateMeasurables != null)
					{
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateMeasurables.AddedValues)
						{
							s.Save(new L10Meeting.L10Meeting_Measurable()
							{
								L10Meeting = oldMeeting,
								Measurable = a,
							});
						}
						foreach (var a in updateMeasurables.RemovedValues)
						{
							if (a.Id > 0){ //Todo Completion is -10001
								var o = oldMeeting._MeetingMeasurables.First(x => x.Measurable.Id == a.Id);
								o.DeleteTime = now;
								s.Update(o);
							}
						}
					}
					if (updateRocks != null)
					{
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateRocks.AddedValues)
						{
							s.Save(new L10Meeting.L10Meeting_Rock()
							{
								L10Meeting = oldMeeting,
								ForRock = a,
								ForRecurrence = oldMeeting.L10Recurrence,
							});
						}
						foreach (var a in updateRocks.RemovedValues)
						{
							var o = oldMeeting._MeetingRocks.First(x => x.ForRock.Id == a.Id);
							o.DeleteTime = now;
							s.Update(o);
						}
					} 
					
					if (updateAttendees != null)
					{
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateAttendees.AddedValues)
						{
							s.Save(new L10Meeting.L10Meeting_Attendee()
							{
								L10Meeting = oldMeeting,
								User = a,
							});
						}
						foreach (var a in updateAttendees.RemovedValues)
						{
							var o = oldMeeting._MeetingAttendees.First(x => x.User.Id == a.Id);
							o.DeleteTime = now;
							s.Update(o);
						}
					}

					Audit.L10Log(s, caller, l10Recurrence.Id, "EditL10Recurrence");

					tx.Commit();
					s.Flush();
				}
			}

		}
		public static void UpdatePage(UserOrganizationModel caller, long forUserId, long recurrenceId, string pageName, string connection)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					var meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, true);
					if (meeting == null) return;
					//if (caller.Id != meeting.MeetingLeader.Id)	return;


					var forUser = s.Get<UserOrganizationModel>(forUserId);
					if (meeting.MeetingLeaderId == 0)
					{
						meeting.MeetingLeaderId = forUser.Id;
						meeting.MeetingLeader = forUser;
					}

					if (caller.Id != forUserId)
						PermissionsUtility.Create(s, forUser).ViewL10Meeting(meeting.Id);

					var log = _GetCurrentLog(s, caller, meeting.Id, forUserId, true);

					var now = DateTime.UtcNow;
					var addNew = true;
					if (log != null)
					{
						addNew = log.Page != pageName;
						if (addNew)
						{
							log.EndTime = now;//new DateTime(Math.Min(log.StartTime.AddMinutes(1).Ticks,now.Ticks));
							s.Update(log);
						}
					}

					if (addNew)
					{
						var newLog = new L10Meeting.L10Meeting_Log()
						{
							User = forUser,
							StartTime = now,
							L10Meeting = meeting,
							Page = pageName,
						};

						s.Save(newLog);



						if (meeting.MeetingLeader.NotNull(x => x.Id) == forUserId)
						{
							if (log != null)
							{
								//Add additional minutes from current page
								var cur = meeting._MeetingLeaderPageDurations.FirstOrDefault(x => x.Item1 == log.Page);
								var duration = (log.EndTime.Value - log.StartTime).TotalMinutes;
								if (cur == null)
								{
									meeting._MeetingLeaderPageDurations.Add(Tuple.Create(log.Page, duration));
								}
								else
								{
									for (var i = 0; i < meeting._MeetingLeaderPageDurations.Count; i++)
									{
										var x = meeting._MeetingLeaderPageDurations[i];
										if (x.Item1 == log.Page)
										{
											meeting._MeetingLeaderPageDurations[i] = Tuple.Create(x.Item1, x.Item2 + duration);
										}
									}
								}
							}
							var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
							var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting));
							var baseMins = meeting._MeetingLeaderPageDurations.SingleOrDefault(x => x.Item1 == pageName).NotNull(x => x.Item2);
							meetingHub.setCurrentPage(pageName.ToLower(), now.ToJavascriptMilliseconds(), baseMins);

							meetingHub.update(new AngularMeeting(recurrenceId) { CurrentPage = pageName });

							foreach (var a in meeting._MeetingLeaderPageDurations)
							{
								if (a.Item1 != pageName)
								{
									meetingHub.setPageTime(a.Item1, a.Item2);
								}
							}
						}

					}

					var p = pageName;
					if (!string.IsNullOrEmpty(p))
						p =p.ToUpper()[0]+ p.Substring(1);

					Audit.L10Log(s, caller, recurrenceId, "UpdatePage",p);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void UpdateTodos(UserOrganizationModel caller, long recurrenceId, TodoDataList model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var ids = model.GetAllIds();
					var existingTodos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId)
						.WhereRestrictionOn(x => x.Id).IsIn(ids)
						.List().ToList();

					var ar = SetUtility.AddRemove(ids, existingTodos.Select(x => x.Id));

					if (ar.RemovedValues.Any())
						throw new PermissionsException("You do not have permission to edit this issue.");
					if (ar.AddedValues.Any())
						throw new PermissionsException("Unreachable.");

					//var recurrenceIssues = existingTodos.ToList();

					foreach (var e in model.GetIssueEdits())
					{
						var f = existingTodos.First(x => x.Id == e.TodoId);
						var update = false;
						/*if (f..NotNull(x => x.Id) != e.ParentRecurrenceIssueId)
						{
							f.ParentRecurrenceIssue = (e.ParentRecurrenceIssueId == null) ? null : recurrenceIssues.First(x => x.Id == e.ParentRecurrenceIssueId);
							update = true;
						}*/

						if (f.Ordering != e.Order)
						{
							f.Ordering = e.Order;
							update = true;
						}
						if (update)
							s.Update(f);
					}

					var json = Json.Encode(model);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), model.connectionId);

					group.deserializeTodos(".todo-list", model);

					group.update(new AngularRecurrence(recurrenceId){
						Todos =existingTodos.OrderBy(x=>x.Ordering).Select(x=>new AngularTodo(x)).ToList()
					});

					Audit.L10Log(s, caller, recurrenceId, "UpdateTodos");
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void UpdateIssues(UserOrganizationModel caller, long recurrenceId, IssuesDataList model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var ids = model.GetAllIds();
					var found = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
						.WhereRestrictionOn(x => x.Id).IsIn(ids)
						//.Fetch(x=>x.Issue).Eager
						.List().ToList();

					var ar = SetUtility.AddRemove(ids, found.Select(x => x.Id));

					if (ar.RemovedValues.Any())
						throw new PermissionsException("You do not have permission to edit this issue.");
					if (ar.AddedValues.Any())
						throw new PermissionsException("Unreachable.");

					var recurrenceIssues = found.ToList();

					foreach (var e in model.GetIssueEdits())
					{
						var f = recurrenceIssues.First(x => x.Id == e.RecurrenceIssueId);
						var update = false;
						if (f.ParentRecurrenceIssue.NotNull(x => x.Id) != e.ParentRecurrenceIssueId)
						{
							f.ParentRecurrenceIssue = (e.ParentRecurrenceIssueId == null) ? null : recurrenceIssues.First(x => x.Id == e.ParentRecurrenceIssueId);
							update = true;
						}

						if (f.Ordering != e.Order)
						{
							f.Ordering = e.Order;
							update = true;
						}

						if (update)
							s.Update(f);
					}

					var json = Json.Encode(model);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group=hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), model.connectionId);
					
					group.deserializeIssues(".issues-list", model);

					var issues = GetAllIssuesForRecurrence(s, perm, recurrenceId)
						.OrderBy(x => x.Ordering)
						.Select(x => new AngularIssue(x))
						.ToList();

					group.update(new AngularRecurrence(recurrenceId){
						Issues = AngularList.Create(AngularListType.ReplaceAll, issues)
					});

					Audit.L10Log(s, caller, recurrenceId, "UpdateIssues");

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void UpdateRockCompletion(UserOrganizationModel caller, long recurrenceId, long meetingRockId, RockState state, string connectionId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var rock = s.Get<L10Meeting.L10Meeting_Rock>(meetingRockId);
					if (rock == null)
						throw new PermissionsException("Rock does not exist.");
					var now = DateTime.UtcNow;
					var updated = false;
					if (state != RockState.Indeterminate && rock.Completion != state)
					{
						if (state == RockState.Complete)
							rock.CompleteTime = now;
						rock.Completion = state;
						s.Update(rock);
						updated = true;
					}
					else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate)
					{
						rock.Completion = RockState.Indeterminate;
						rock.CompleteTime = null;
						s.Update(rock);
						updated = true;
					}

					if (updated)
					{

						Audit.L10Log(s, caller, recurrenceId, "UpdateRockCompletion","\""+rock.ForRock.Rock+"\" set to \""+state+"\"");

						tx.Commit();
						s.Flush();

						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId)
							.updateRockCompletion(meetingRockId, state.ToString());
					}
				}
			}
		}
		#endregion

		#region Meeting Actions

		public static void StartMeeting(UserOrganizationModel caller, UserOrganizationModel meetingLeader, long recurrenceId, List<UserOrganizationModel> attendees)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					if (caller.Id != meetingLeader.Id)
						PermissionsUtility.Create(s, meetingLeader).ViewL10Recurrence(recurrenceId);


					lock ("Recurrence_" + recurrenceId)
					{
						//Make sure we're unstarted
						try
						{
							var perms = PermissionsUtility.Create(s, caller);
							_GetCurrentL10Meeting(s, perms, recurrenceId, false);

							throw new MeetingException("Meeting has already started.", MeetingExceptionType.AlreadyStarted);
						}
						catch (MeetingException e)
						{
							if (e.MeetingExceptionType != MeetingExceptionType.Unstarted)
								throw;
						}

						var now = DateTime.UtcNow;
						var recurrence = s.Get<L10Recurrence>(recurrenceId);

						var meeting = new L10Meeting
						{
							CreateTime = now,
							StartTime = now,
							L10RecurrenceId = recurrenceId,
							L10Recurrence = recurrence,
							OrganizationId = recurrence.OrganizationId,
							MeetingLeader = meetingLeader,
							MeetingLeaderId = meetingLeader.Id
						};

						s.Save(meeting);

						recurrence.MeetingInProgress = meeting.Id;
						s.Update(recurrence);

						_LoadRecurrences(s, false, false, false, recurrence);

						foreach (var m in recurrence._DefaultMeasurables)
						{
							var mm = new L10Meeting.L10Meeting_Measurable()
							{
								L10Meeting = meeting,
								Measurable = m.Measurable,
								_Ordering = m._Ordering,
							};
							s.Save(mm);
							meeting._MeetingMeasurables.Add(mm);
						}
						foreach (var m in attendees)
						{
							var mm = new L10Meeting.L10Meeting_Attendee()
							{
								L10Meeting = meeting,
								User = m,
							};
							s.Save(mm);
							meeting._MeetingAttendees.Add(mm);
						}

						var previousRock = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.ForRecurrence.Id == recurrenceId).List().ToList()
							.OrderByDescending(x => x.Id).GroupBy(x => x.ForRock.Id)
							.ToDictionary(x => x.First().ForRock.Id, x => x.First().Completion);

						foreach (var r in recurrence._DefaultRocks)
						{
							var state = RockState.Indeterminate;
							if (previousRock.ContainsKey(r.ForRock.Id))
								state=previousRock[r.ForRock.Id];

							var mm = new L10Meeting.L10Meeting_Rock()
							{
								ForRecurrence = recurrence,
								L10Meeting = meeting,
								ForRock = r.ForRock,
								Completion = state// == RockState.Complete ? RockState.Complete : RockState.Indeterminate
							};
							s.Save(mm);
							meeting._MeetingRocks.Add(mm);
						}
						Audit.L10Log(s, caller, recurrenceId, "StartMeeting");
						tx.Commit();
						s.Flush();
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting)).setupMeeting(meeting.CreateTime.ToJavascriptMilliseconds(), meetingLeader.Id);
					}
				}
			}
		}
		public async static Task ConcludeMeeting(UserOrganizationModel caller, long recurrenceId, List<System.Tuple<long, decimal?>> ratingValues, bool sendEmail)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var now = DateTime.UtcNow;
					//Make sure we're unstarted
					var perms = PermissionsUtility.Create(s, caller);
					var meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
					perms.ViewL10Meeting(meeting.Id);

					meeting.CompleteTime = now;

					var recurrence = s.Get<L10Recurrence>(recurrenceId);
					s.Update(meeting);

					var ids = ratingValues.Select(x => x.Item1).ToArray();

					//Set rating for attendees
					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
						.WhereRestrictionOn(x => x.User.Id)
						.IsIn(ids)
						.List().ToList();
					foreach (var a in attendees)
					{
						a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
						s.Update(a);
					}
					//End all logs 
					var logs = s.QueryOver<L10Meeting.L10Meeting_Log>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.EndTime == null)
						.List().ToList();
					foreach (var l in logs)
					{
						l.EndTime = now;
						s.Update(l);
					}

					//Close all sub issues
					var issue_recurParents = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null && x.CloseTime >= meeting.StartTime && x.CloseTime <= meeting.CompleteTime)
						.Select(x => x.Id)
						.List<long>().ToList();
					_RecursiveCloseIssues(s, issue_recurParents, now);

					recurrence.MeetingInProgress = null;
					s.Update(recurrence);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting)).concludeMeeting();

					//send emails
					if (sendEmail)
					{
						try
						{
							var todoList = s.QueryOver<TodoModel>().Where(x =>
								x.DeleteTime == null &&
								x.ForRecurrenceId == recurrenceId &&
								x.CompleteTime == null
								).List().ToList();
							var unsent = new List<MailModel>();

							foreach (var personTodos in todoList.GroupBy(x => x.AccountableUser.GetEmail()))
							{
								var user = personTodos.First().AccountableUser;
								var email = user.GetEmail();

								var table = TodoAccessor.BuildTodoTable(personTodos.ToList());
								/*var table = new StringBuilder();
								table.Append(@"<table width=""100%""  border=""0"" cellpadding=""0"" cellspacing=""0"">");
								table.Append(@"<tr><th colspan=""2"" align=""left"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">To-do</th><th align=""right"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">Due Date</th></tr>");
								var i = 1;
								foreach (var todo in personTodos.OrderBy(x => x.DueDate.Date).ThenBy(x => x.Message))
								{
									table.Append(@"<tr><td width=""1px""><b>").Append(i).Append(@". </b></td><td align=""left""><b>").Append(todo.Message).Append(@"</b></td><td  align=""right"">").Append(todo.DueDate.ToShortDateString()).Append("</td></tr>");
									if (!String.IsNullOrWhiteSpace(todo.Details))
									{
										table.Append("<tr><td></td><td><i style=\"font-size:12px;\">&nbsp;&nbsp;").Append(todo.Details).Append("</i></td><td></td></tr>");
									}

									i++;
								}
								table.Append("</table>");*/

								var mail = MailModel.To(email)
									.Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
									.Body(EmailStrings.MeetingSummary_Body, user.GetName(), table.ToString(), Config.ProductName(meeting.Organization));
								unsent.Add(mail);
							}

							await Emailer.SendEmails(unsent);
						}
						catch (Exception e)
						{
							log.Error("Emailer issue:" + recurrence.Id, e);
						}
					}

					Audit.L10Log(s, caller, recurrenceId, "ConcludeMeeting");
					tx.Commit();
					s.Flush();
				}
			}
		}



		public static L10Meeting.L10Meeting_Connection JoinL10Meeting(UserOrganizationModel caller, long recurrenceId, string connectionId)
		{
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			if (recurrenceId == -3){
				using (var s = HibernateSession.GetCurrentSession()){
					using (var tx = s.BeginTransaction()){
						var recurs =s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == caller.Id)
							.Select(x => x.L10Recurrence.Id)
							.List<long>().ToList();
						foreach (var r in recurs){
							hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(r));
						}

						Audit.L10Log(s, caller, recurrenceId, "JoinL10Meeting");
					}
				}
			}
			else{
				new PermissionsAccessor().Permitted(caller,x=>x.ViewL10Recurrence(recurrenceId));
				hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(recurrenceId));

			}

			
			//hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId)).updateUserList(Users);

			return null;
			/*
			//Should already check permissions here
			var meeting = GetCurrentL10Meeting(caller, recurrenceId);
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//PermissionUtility.Create(...) not needed. Permissions checked above.
					var first = s.QueryOver<L10Meeting.L10Meeting_Connection>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.User.Id == caller.Id).SingleOrDefault<L10Meeting.L10Meeting_Connection>();
					//Already exists
					if (first != null)
					{
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(meeting));
						return first;
					}
					else
					{
						//Create a new connection
						var m = s.Get<L10Meeting>(meeting.Id);
						var u = s.Get<UserOrganizationModel>(caller.Id);
						var conn = new L10Meeting.L10Meeting_Connection
						{
							ConnectionId = connectionId,
							L10Meeting = m,
							User = u
						};
						s.Save(conn);
						tx.Commit();
						s.Flush();

						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(meeting));
						return conn;
					}
				}
			}*/
		}

		#endregion

		#region Notes
		public static void CreateNote(UserOrganizationModel caller, long recurrenceId, string name)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var note = new L10Note()
					{
						Name = name,
						Contents = "",
						Recurrence = s.Load<L10Recurrence>(recurrenceId)
					};
					s.Save(note);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
					group.createNote(note.Id, name);
					var rec = new AngularRecurrence(recurrenceId)
					{
						Notes = new List<AngularMeetingNotes>(){
							new AngularMeetingNotes(note)
						}
					};
					group.update(rec);

					Audit.L10Log(s, caller, recurrenceId, "CreateNote", name);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void EditNote(UserOrganizationModel caller, long noteId, string contents = null, string name = null, string connectionId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					PermissionsUtility.Create(s, caller).ViewL10Note(noteId);
					var note = s.Get<L10Note>(noteId);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();


					var now = DateTime.UtcNow;

					if (contents != null)
					{
						note.Contents = contents;
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteContents(noteId, contents, now.ToJavascriptMilliseconds());
					}
					if (name != null)
					{
						note.Name = name;
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteName(noteId, name);
					}
					s.Update(note);

					Audit.L10Log(s, caller, note.Recurrence.Id, "EditNote", note.Name+":\n"+note.Contents);

					tx.Commit();
					s.Flush();
				}
			}
		}
		public static L10Note GetNote(UserOrganizationModel caller, long noteId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Note(noteId);
					return s.Get<L10Note>(noteId);
				}
			}
		}
		#endregion

		public static List<L10Meeting.L10Meeting_Rock> GetRocksForRecurrence(UserOrganizationModel caller, long recurrenceId, long meetingId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId).ViewL10Meeting(meetingId);

					var found = s.QueryOver<L10Meeting.L10Meeting_Rock>()
						.Where(x => x.DeleteTime == null && x.ForRecurrence.Id == recurrenceId && x.L10Meeting.Id == meetingId)
						.Fetch(x => x.ForRock).Eager
						.List().ToList();
					foreach (var f in found)
					{
						var a = f.ForRock.AccountableUser.GetName();
						var b = f.ForRock.AccountableUser.ImageUrl(true, ImageSize._32);
					}
					return found;
				}
			}
		}
		

		public static object GetModel_Unsafe(ISession s, string type, long id)
		{
			if (id <= 0)
				return null;

			switch (type.ToLower())
			{
				case "measurablemodel": return s.Get<MeasurableModel>(id);
				case "todomodel": return s.Get<TodoModel>(id);
				case "issuemodel": return s.Get<IssueModel>(id);
			}
			return null;
		}

		public static long GuessUserId(IssueModel issueModel,long deflt=0)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					if (issueModel.ForModel.ToLower() == "issuemodel" && issueModel.Id == issueModel.ForModelId)
						return deflt;

					var found = GetModel_Unsafe(s, issueModel.ForModel, issueModel.ForModelId);
					if (found == null)
						return deflt;

					if (found is MeasurableModel)
					{
						return ((MeasurableModel)found).AccountableUserId;
					}
					if (found is TodoModel)
					{
						return ((TodoModel)found).AccountableUserId;
					}
					if (found is IssueModel)
					{
						return GuessUserId((IssueModel)found, deflt);
					}
					return deflt;
				}
			}
		}



		public static List<TodoModel> GetPreviousTodos(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var todos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId).List().ToList();

					foreach (var t in todos)
					{
						var a = t.AccountableUser.GetName();
					}
					return todos;
				}
			}
		}

		public static void GetVisibleTodos(UserOrganizationModel caller, long[] forUsers, bool includeComplete)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var p = PermissionsUtility.Create(s, caller);
					forUsers.Distinct().ForEach(x => p.ManagesUserOrganizationOrSelf(x));

					//s.QueryOver<TodoModel>().Where(x=>x.)
					throw new Exception("todo");

				}
			}
		}

		public static List<AbstractTodoCreds> GetExternalLinksForRecurrence(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return ExternalTodoAccessor.GetExternalLinksForModel(s, PermissionsUtility.Create(s, caller), ForModel.Create<L10Recurrence>(recurrenceId));
				}
			}
		}

		public static void UpdateTodo(UserOrganizationModel caller, long todoId, string message = null, string details = null, DateTime? dueDate = null, long? accountableUser = null, bool? complete = null, string connectionId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var todo = s.Get<TodoModel>(todoId);
					if (todo == null)
						throw new PermissionsException("To-do does not exist.");
					if (todo.ForRecurrenceId == null || todo.ForRecurrenceId == 0)
						throw new PermissionsException("Meeting does not exist.");
					PermissionsUtility.Create(s, caller).EditL10Recurrence(todo.ForRecurrenceId.Value);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(todo.ForRecurrenceId.Value), connectionId);

					var updatesText = new List<string>();

					if (message != null && todo.Message!=message)
					{
						todo.Message = message;
						group.updateTodoMessage(todoId, message);
						updatesText.Add("Message: "+todo.Message);
					}
					if (details != null && todo.Details!=details)
					{
						todo.Details = details;
						group.updateTodoDetails(todoId, details);
						updatesText.Add("Details: "+details);
					}
					if (dueDate != null && todo.DueDate!=dueDate.Value)
					{
						todo.DueDate = dueDate.Value;
						group.updateTodoDueDate(todoId, dueDate.Value.ToJavascriptMilliseconds());
						updatesText.Add("Due-Date: "+dueDate.Value.ToShortDateString());
					}
					if (accountableUser != null&& todo.AccountableUserId!=accountableUser.Value)
					{
						todo.AccountableUserId = accountableUser.Value;
						todo.AccountableUser = s.Get<UserOrganizationModel>(accountableUser.Value);
						group.updateTodoAccountableUser(todoId, accountableUser.Value, todo.AccountableUser.GetName(), todo.AccountableUser.ImageUrl(true, ImageSize._32));
						updatesText.Add("Accountable: "+todo.AccountableUser.GetName());
					}

					if (complete != null)
					{
						var now = DateTime.UtcNow;
						if (complete.Value && todo.CompleteTime == null)
						{
							todo.CompleteTime = now;
							s.Update(todo);
							updatesText.Add("Marked Complete");
							new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
						}
						else if (!complete.Value && todo.CompleteTime != null)
						{
							todo.CompleteTime = null;
							s.Update(todo);
							updatesText.Add("Marked Incomplete");
							new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
						}
						group.updateTodoCompletion(todoId, complete);
					}

					group.update(new AngularUpdate() { new AngularTodo(todo) });

					var updatedText = "Updated To-Do \"" + todo.Message + "\" \n " + String.Join("\n", updatesText);

					Audit.L10Log(s, caller, todo.ForRecurrenceId.Value, "UpdateTodo", updatedText);

					tx.Commit();
					s.Flush();
				}
			}
		}
		/*
		public static void UpdateTodoCompletion(UserOrganizationModel caller, long todoId, bool complete, string connectionId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var todo = s.Get<TodoModel>(todoId);	
					if (todo == null)
						throw new PermissionsException("To-Do does not exist.");
					var recurrenceId = todo.ForRecurrence.Id;
					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
				
					var now = DateTime.UtcNow;
					var updated = false;
					if (complete && todo.CompleteTime == null)
					{
						todo.CompleteTime = now;
						s.Update(todo);
						updated = true;
					}
					else if (!complete && todo.CompleteTime != null)
					{
						todo.CompleteTime = null;
						s.Update(todo);
						updated = true;
					}

					if (updated)
					{
						tx.Commit();
						s.Flush();

						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId)
							.updateTodoCompletion(todoId, complete);
					}
				}
			}
		}*/

		public static void UpdateIssue(UserOrganizationModel caller, long issueRecurrenceId,DateTime updateTime,string message = null, string details = null, bool? complete = null, string connectionId = null,long? owner=null,int? priority=null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
                    updateTime = Math2.Min(DateTime.UtcNow.AddSeconds(3), updateTime);

					var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
					if (issue == null)
						throw new PermissionsException("Issue does not exist.");

					var recurrenceId = issue.Recurrence.Id;
					if (recurrenceId == 0)
						throw new PermissionsException("Meeting does not exist.");
					PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId);

					var updatesText = new List<string>();
					if (message != null && message!= issue.Issue.Message)
					{
						issue.Issue.Message = message;
						group.updateIssueMessage(issueRecurrenceId, message);
						updatesText.Add("Message: " + issue.Issue.Message);
					}
					if (details != null && details != issue.Issue.Description)
					{
						issue.Issue.Description = details;
						group.updateIssueDetails(issueRecurrenceId, details);
						updatesText.Add("Description: " + issue.Issue.Description);
					}
					if (owner != null && (issue.Owner==null || owner != issue.Owner.Id)){
						var any=s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner).Take(1).List().ToList();
						if (!any.Any())
							throw new PermissionsException("Specified Owner cannot see meeting");

						issue.Owner = s.Get<UserOrganizationModel>(owner);
						group.updateIssueOwner(issueRecurrenceId, owner, issue.Owner.GetName(), issue.Owner.ImageUrl(true, ImageSize._32));
						updatesText.Add("Owner: " + issue.Owner.GetName());
                    }
                    if (priority != null && priority != issue.Priority && issue.LastUpdate_Priority<updateTime)
                    {
                        issue.LastUpdate_Priority = updateTime;
                        var old = issue.Priority;
                        issue.Priority = priority.Value;
                        group.updateIssuePriority(issueRecurrenceId, issue.Priority);
                        updatesText.Add("Priority from "+old+" to " + issue.Priority);
                    }
					var now = DateTime.UtcNow;
					if (complete != null)
					{
						if (complete.Value && issue.CloseTime == null)
						{
							issue.CloseTime = now;
							updatesText.Add("Marked Closed");
						}
						else if (!complete.Value && issue.CloseTime != null)
						{
							issue.CloseTime = null;
							updatesText.Add("Marked Open");
						}
						group.updateIssueCompletion(issueRecurrenceId, complete);
					}

					group.update(new AngularUpdate() { new AngularIssue(issue) });


					var updatedText = "Updated Issue \"" + issue.Issue.Message + "\" \n " + String.Join("\n", updatesText);

					Audit.L10Log(s, caller, recurrenceId, "UpdateIssue", updatedText);

					tx.Commit();
					s.Flush();
				}
			}
		}
		/*
		public static void UpdateIssueCompletion(UserOrganizationModel caller, long issue_recurrenceId, bool complete, string connectionId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var issue = s.Get<IssueModel.IssueModel_Recurrence>(issue_recurrenceId);
					if (issue == null)
						throw new PermissionsException("Issue does not exist.");
					var recurrenceId = issue.Recurrence.Id;
					var perm = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
					/*var issue =s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x=>x.DeleteTime==null && x.Issue.Id == issueId && x.Recurrence.Id==recurrenceId)
						.SingleOrDefault();*

					var now = DateTime.UtcNow;
					var updated = false;
					if (complete && issue.CloseTime == null)
					{
						issue.CloseTime = now;
						s.Update(issue.Issue);
						updated = true;
					}
					else if (!complete && issue.CloseTime != null)
					{
						issue.CloseTime = null;
						s.Update(issue.Issue);
						updated = true;
					}

					if (updated)
					{
						tx.Commit();
						s.Flush();

						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId)
							.updateIssueCompletion(issue_recurrenceId, complete);
					}
				}
			}
		}*/

		public static void UpdateScore(UserOrganizationModel caller, long scoreId, decimal? measured, string connectionId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var score = s.Get<ScoreModel>(scoreId);
					if (score == null)
						throw new PermissionsException("Score does not exist.");

					PermissionsUtility.Create(s, caller).EditScore(scoreId);

					score.Measured = measured;
					score.DateEntered = (measured == null) ? null : (DateTime?)DateTime.UtcNow;
					s.Update(score);
					L10Meeting meetingAlias = null;
					var possibleRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.Where(x => x.DeleteTime == null && x.Measurable.Id == score.MeasurableId)
						.Select(x => x.L10Recurrence.Id)
						.List<long>().ToList();

					foreach (var r in possibleRecurrences)
					{
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r), connectionId);
						var toUpdate = new AngularScore(score);

						toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
						toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();

						group.update(new AngularUpdate() { toUpdate });
						Audit.L10Log(s, caller, r, "UpdateScore", "\""+score.Measurable.Title+"\" updated to \""+measured+"\"");
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static ScoreModel _UpdateScore(ISession s, PermissionsUtility perms, long measurableId, long weekNumber, decimal? measured, string connectionId)
		{
			var now = DateTime.UtcNow;
			DateTime? nowQ = now;
			perms.EditMeasurable(measurableId);
			var m = s.Get<MeasurableModel>(measurableId);

			var measurableRecurs = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
				.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
				.Select(x => x.L10Recurrence.Id)
				.List<long>().ToList();

			var existingScores = s.QueryOver<ScoreModel>()
				.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
				.List().ToList();

			//adjust week..
			var week = TimingUtility.GetDateSinceEpoch(weekNumber).StartOfWeek(DayOfWeek.Sunday).Date;

			//See if we can find it given week.
			var score = existingScores.SingleOrDefault(x => (x.ForWeek == week));

			if (score != null)
			{
				//Found it with false id
				score.Measured = measured;
				score.DateEntered = (measured == null) ? null : nowQ;
				s.Update(score);
			}
			else
			{
				var ordered = existingScores.OrderBy(x => x.DateDue);
				var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now.AddDays(-7*13);
				var maxDate = ordered.LastOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;

				minDate = minDate.StartOfWeek(DayOfWeek.Sunday);
				maxDate = maxDate.StartOfWeek(DayOfWeek.Sunday);


				DateTime start, end;

				if (week > maxDate)
				{
					//Create going up until sufficient
					var n = maxDate;
					ScoreModel curr = null;
					while (n < week)
					{
						var nextDue = n.StartOfWeek(DayOfWeek.Sunday).Date.AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);
						curr = new ScoreModel()
						{
							AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
							AccountableUserId = m.AccountableUserId,
							DateDue = nextDue,
							MeasurableId = m.Id,
							Measurable = s.Load<MeasurableModel>(m.Id),
							OrganizationId = m.OrganizationId,
							ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday).Date
						};
						s.Save(curr);
						m.NextGeneration = nextDue;
						n = nextDue.StartOfWeek(DayOfWeek.Sunday).Date;
					}
					curr.DateEntered = (measured == null) ? null : nowQ;
					curr.Measured = measured;
					score = curr;
				}
				else if (week < minDate)
				{
					var n = week;
					var first = true;
					while (n < minDate)
					{
						var nextDue = n.StartOfWeek(DayOfWeek.Sunday).Date.AddDays((int)m.DueDate).Add(m.DueTime);
						var curr = new ScoreModel()
						{
							AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
							AccountableUserId = m.AccountableUserId,
							DateDue = nextDue,
							MeasurableId = m.Id,
							Measurable = s.Load<MeasurableModel>(m.Id),
							OrganizationId = m.OrganizationId,
							ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday).Date
						};
						if (first)
						{
							curr.Measured = measured;
							curr.DateEntered = (measured == null) ? null : nowQ;
							first = false;
							s.Save(curr);
							score = curr;
						}

						//m.NextGeneration = nextDue;
						n = nextDue.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
					}
				}
				else
				{
					// cant create scores between these dates..
					var curr = new ScoreModel()
					{
						AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
						AccountableUserId = m.AccountableUserId,
						DateDue = week.StartOfWeek(DayOfWeek.Sunday).Date.AddDays((int)m.DueDate).Add(m.DueTime),
						MeasurableId = m.Id,
						Measurable = s.Load<MeasurableModel>(m.Id),
						OrganizationId = m.OrganizationId,
						ForWeek = week.StartOfWeek(DayOfWeek.Sunday).Date,
						Measured = measured,
						DateEntered = (measured == null) ? null : nowQ
					};
					s.Save(curr);
					score = curr;
				}
				s.Update(m);
			}
			foreach (var recurrenceId in measurableRecurs)
			{
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
				var update = new AngularRecurrence(recurrenceId);
				update.Scorecard = new AngularScorecard();
				//score.Measured = score.Measured ?? Removed.Decimal();
				var angularScore = new AngularScore(score);
				angularScore.Measured = angularScore.Measured ?? Removed.Decimal();
				angularScore.ForWeek = TimingUtility.GetWeekSinceEpoch(angularScore.Week);
				update.Scorecard.Scores = new List<AngularScore>() { angularScore };
				group.update(update);

				Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateScore", "\""+score.NotNull(x=>x.Measurable.NotNull(y=>y.Title)) + "\" updated to \"" + measured+"\"");
			}
			return score;
		}

		public static void UpdateScore(UserOrganizationModel caller, long measurableId, long weekNumber, decimal? measured, string connectionId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					_UpdateScore(s, perms, measurableId, weekNumber, measured, connectionId);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void UpdateArchiveMeasurable(UserOrganizationModel caller, long measurableId, long recurrenceId,
		string name = null, LessGreater? direction = null, decimal? target = null,
		long? accountableId = null, long? adminId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var measurable = s.Get<MeasurableModel>(measurableId);
					var recurrence = s.Get<L10Recurrence>(recurrenceId);


					if (measurable == null)
						throw new PermissionsException("Measurable does not exist.");

					PermissionsUtility.Create(s, caller).Or(x => x.EditMeasurable(measurableId), x => x.EditL10Recurrence(recurrenceId));

					if (s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId && x.L10Recurrence.Id == recurrenceId).Take(1).SingleOrDefault() == null)
						throw new PermissionsException("Cannot edit this measurable.");

					//var recurrenceId = measurable.L10Meeting.L10RecurrenceId;
					if (recurrenceId == 0)
						throw new PermissionsException("Meeting does not exist.");
					var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

					var scores = GetScoresForRecurrence(s, perms, recurrenceId);

					var updateText = new List<String>();

					if (name != null && measurable.Title != name)
					{
						measurable.Title = name;
						//group.updateArchiveMeasurable(measurableId, "title", name);
						updateText.Add("Title: "+measurable.Title);

					}
					if (direction != null && measurable.GoalDirection != direction.Value)
					{
						measurable.GoalDirection = direction.Value;
						updateText.Add("Goal Direction: " + measurable.GoalDirection.ToSymbol());
						//group.updateArchiveMeasurable(measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());

					}
					if (target != null && measurable.Goal != target.Value)
					{
						measurable.Goal = target.Value;
						updateText.Add("Goal: " + measurable.Goal);
						//group.updateArchiveMeasurable(measurableId, "target", target.Value.ToString("0.#####"));
					}
					if (accountableId != null && measurable.AccountableUserId != accountableId.Value)
					{
						perms.ViewUserOrganization(accountableId.Value, false);
						var user = s.Get<UserOrganizationModel>(accountableId.Value);
						if (user != null)
							user.UpdateCache(s);

						measurable.AccountableUserId = accountableId.Value;
						updateText.Add("Accountable: " + user.GetName());
						//group.updateArchiveMeasurable(measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
					}
					if (adminId != null)
					{
						perms.ViewUserOrganization(adminId.Value, false);
						var user = s.Get<UserOrganizationModel>(adminId.Value);
						if (user != null)
							user.UpdateCache(s);
						measurable.AdminUserId = adminId.Value;
						updateText.Add("Admin: " + user.GetName());
						//group.updateArchiveMeasurable(measurableId, "admin", user.NotNull(x => x.GetName()), adminId.Value);
					}

					var scorecard = new AngularScorecard();
					scorecard.Measurables = new List<AngularMeasurable>() { };
					scorecard.Scores = new List<AngularScore>();
					foreach (var ss in scores.Where(x => x.Measurable.Id == measurable.Id))
					{
						scorecard.Scores.Add(new AngularScore(ss));
					}

					group.update(new AngularUpdate() { scorecard, new AngularMeasurable(measurable) });

					var updatedText = "Updated Measurable: \"" + measurable.Title+ "\" \n " + String.Join("\n", updateText);
					Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateArchiveMeasurable", updatedText);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void UpdateMeasurable(UserOrganizationModel caller, long meeting_measurableId,
			string name = null, LessGreater? direction = null, decimal? target = null,
			long? accountableId = null, long? adminId = null,UnitType? unitType=null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var measurable = s.Get<L10Meeting.L10Meeting_Measurable>(meeting_measurableId);
					if (measurable == null)
						throw new PermissionsException("Measurable does not exist.");

					var recurrenceId = measurable.L10Meeting.L10RecurrenceId;
					if (recurrenceId == 0)
						throw new PermissionsException("Meeting does not exist.");
					var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

					var updateText = new List<String>();
					if (name != null && measurable.Measurable.Title != name)
					{
						measurable.Measurable.Title = name;
						group.updateMeasurable(meeting_measurableId, "title", name);
						updateText.Add("Title: " + measurable.Measurable.Title);
					}
					if (direction != null && measurable.Measurable.GoalDirection != direction.Value)
					{
						measurable.Measurable.GoalDirection = direction.Value;
						group.updateMeasurable(meeting_measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());
						updateText.Add("Goal Direction: " + measurable.Measurable.GoalDirection.ToSymbol());
					}
					if (unitType != null && measurable.Measurable.UnitType != unitType.Value)
					{
						measurable.Measurable.UnitType = unitType.Value;
						group.updateMeasurable(meeting_measurableId, "unittype", unitType.Value.ToTypeString(), unitType.Value.ToString());
						updateText.Add("Unit Type: " + measurable.Measurable.UnitType);
					}
					if (target != null && measurable.Measurable.Goal != target.Value)
					{
						measurable.Measurable.Goal = target.Value;
						group.updateMeasurable(meeting_measurableId, "target", target.Value.ToString("0.#####"));
						updateText.Add("Goal: " + measurable.Measurable.Goal);
					}
					if (accountableId != null && measurable.Measurable.AccountableUserId != accountableId.Value)
					{
						perms.ViewUserOrganization(accountableId.Value, false);
						var user = s.Get<UserOrganizationModel>(accountableId.Value);
						if (user != null)
							user.UpdateCache(s);

						measurable.Measurable.AccountableUserId = accountableId.Value;
						group.updateMeasurable(meeting_measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
						updateText.Add("Accountable: " + user.NotNull(x=>x.GetName()));
					}
					if (adminId != null && measurable.Measurable.AccountableUserId != accountableId.Value)
					{
						perms.ViewUserOrganization(adminId.Value, false);
						var user = s.Get<UserOrganizationModel>(adminId.Value);
						if (user != null)
							user.UpdateCache(s);
						measurable.Measurable.AdminUserId = adminId.Value;
						group.updateMeasurable(meeting_measurableId, "admin", user.NotNull(x => x.GetName()), adminId.Value);
						updateText.Add("Admin: " + user.NotNull(x => x.GetName()));
					}

					var updatedText = "Updated Measurable: \"" + measurable.Measurable.Title + "\" \n " + String.Join("\n", updateText);
					Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateMeasurable", updatedText);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void CreateMeasurable(UserOrganizationModel caller, long recurrenceId, L10Controller.AddMeasurableVm model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);

					var recur = s.Get<L10Recurrence>(recurrenceId);


					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

					var now = DateTime.UtcNow;
					MeasurableModel measurable;

					var scores = new List<ScoreModel>();
					var wasCreated = false;
					if (model.SelectedMeasurable == -3)
					{
						//Create new
						if (model.Measurables == null)
							throw new PermissionsException("You must include a measurable to create.");

						measurable = model.Measurables.SingleOrDefault();
						if (measurable == null)
							throw new PermissionsException("You must include a measurable to create.");

						perm.ViewUserOrganization(measurable.AccountableUserId, false);
						perm.ViewUserOrganization(measurable.AdminUserId, false);

						measurable.OrganizationId = recur.OrganizationId;
						measurable.CreateTime = now;

						measurable.AccountableUser = s.Load<UserOrganizationModel>(measurable.AccountableUserId);
						measurable.AdminUser = s.Load<UserOrganizationModel>(measurable.AdminUserId);

						s.Save(measurable);

						measurable.AccountableUser.UpdateCache(s);
						measurable.AdminUser.UpdateCache(s);

						wasCreated = true;
					}
					else
					{
						//Find Existing
						measurable = s.Get<MeasurableModel>(model.SelectedMeasurable);
						if (measurable == null)
							throw new PermissionsException("Measurable does not exist.");
						perm.ViewMeasurable(measurable.Id);

						scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id).List().ToList();
						//weekData = scores.Select(x => new{week = x.ForWeek.ToJavascriptMilliseconds(), value = x.Measured}).ToList();

					}

					var rm = new L10Recurrence.L10Recurrence_Measurable()
					{
						CreateTime = now,
						L10Recurrence = recur,
						Measurable = measurable,
					};
					s.Save(rm);

					if (wasCreated)
					{
						var week = TimingUtility.GetWeekSinceEpoch(DateTime.UtcNow);
						for (var i = 0; i < 4; i++)
						{
							scores.Add(_UpdateScore(s, perm, measurable.Id, week - i, null, null));
						}
					}

					var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
					var l10Scores = L10Accessor.GetScoresForRecurrence(s, perm, recurrenceId);
					if (current != null)
					{


						var mm = new L10Meeting.L10Meeting_Measurable()
						{
							L10Meeting = current,
							Measurable = measurable,
						};
						s.Save(mm);

						//var serial=new{
						//	id=measurable.Id,
						//	accountableId=measurable.AccountableUserId,
						//	accountableName=measurable.AccountableUser.GetName(),
						//	adminName = measurable.AdminUser.GetName(),
						//	title=measurable.Title,
						//	direction = measurable.GoalDirection.ToString(),
						//	directionName = measurable.GoalDirection.ToSymbol(),
						//	target = measurable.Goal,
						//	measurable.AdminUserId,
						//	scores=weekData
						//};

						var sow = current.Organization.Settings.WeekStart;
						var offset = current.Organization.GetTimezoneOffset();
						var weeks = TimingUtility.GetWeeks(sow, offset, now, current.StartTime, l10Scores, false);

						var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();

						var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM
						{
							MeetingId = current.Id,
							RecurrenceId = recurrenceId,
							MeetingMeasurable = mm,
							Scores = scores,
							Weeks = weeks
						});
						row.ViewData["row"] = rowId-1;

						var first = row.Execute();
						row.ViewData["ShowRow"] = false;
						var second = row.Execute();
						group.addMeasurable(first,second);
					}
					var scorecard = new AngularScorecard();
					scorecard.Measurables = new List<AngularMeasurable>() { new AngularMeasurable(measurable) };
					scorecard.Scores = new List<AngularScore>();
					foreach (var ss in l10Scores.Where(x => x.Measurable.Id == measurable.Id))
					{
						scorecard.Scores.Add(new AngularScore(ss));
					}

					group.update(new AngularUpdate() { scorecard });

					Audit.L10Log(s, caller, recurrenceId, "CreateMeasurable", measurable.Title);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void DeleteL10(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
					var r = s.Get<L10Recurrence>(recurrenceId);
					r.DeleteTime = DateTime.UtcNow;

					s.Update(r);

					Audit.L10Log(s, caller, recurrenceId, "DeleteL10", r.Name);
					tx.Commit();
					s.Flush();
				}
			}
		}
		#region Angular
		public static AngularRecurrence GetAngularRecurrence(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var recurrence = s.Get<L10Recurrence>(recurrenceId);
					_LoadRecurrences(s, true, true, true, recurrence);

					var recur = new AngularRecurrence(recurrence);
					recur.Attendees = recurrence._DefaultAttendees.Select(x => AngularUser.CreateUser(x.User)).ToList();

					var scores = L10Accessor.GetScoresForRecurrence(s, perms, recurrenceId);
					var measurables = recurrence._DefaultMeasurables.Select(x => {
						var m = new AngularMeasurable(x.Measurable);
						m.Ordering = x._Ordering;
						return m;
					}).ToList();

					measurables.Add(new AngularMeasurable(TodoMeasurable));

					recur.Scorecard = new AngularScorecard(caller.Organization.Settings.WeekStart,caller.Organization.GetTimezoneOffset(), measurables, scores);
					recur.Rocks = recurrence._DefaultRocks.Select(x => new AngularRock(x.ForRock)).ToList();
					recur.Todos = GetAllTodosForRecurrence(s, perms, recurrenceId).Select(x => new AngularTodo(x)).ToList();
					recur.Issues = GetAllIssuesForRecurrence(s, perms, recurrenceId).Select(x => new AngularIssue(x)).ToList();

					recur.Notes = recurrence._MeetingNotes.Select(x => new AngularMeetingNotes(x)).ToList();

					return recur;
				}
			}
		}



		public static void Update(UserOrganizationModel caller, BaseAngular model, string connectionId)
		{
			if (model.Type == typeof(AngularIssue).Name)
			{
				var m = (AngularIssue)model;
				//UpdateIssue(caller, (long)model.GetOrDefault("Id", null), (string)model.GetOrDefault("Name", null), (string)model.GetOrDefault("Details", null), (bool?)model.GetOrDefault("Complete", null), connectionId);
				UpdateIssue(caller, m.Id,DateTime.UtcNow, m.Name ?? "", m.Details ?? "", m.Complete, connectionId);
			}
			else if (model.Type == typeof(AngularTodo).Name)
			{
				var m = (AngularTodo)model;
				UpdateTodo(caller, m.Id, m.Name ?? "", m.Details ?? "", m.DueDate, m.Owner.NotNull(x => x.Id), m.Complete, connectionId);
			}
			else if (model.Type == typeof(AngularScore).Name)
			{
				var m = (AngularScore)model;
				if (m.Id > 0)
					UpdateScore(caller, m.Id, m.Measured, connectionId);
				//else
				//	throw new Exception("Shouldn't get here");
				else
					UpdateScore(caller, m.Measurable.Id, m.ForWeek, m.Measured, connectionId);
			}
			else if (model.Type == typeof(AngularMeetingNotes).Name)
			{
				var m = (AngularMeetingNotes)model;
				EditNote(caller, m.Id, m.Contents, m.Title, connectionId);
			}
			else
			{
				throw new PermissionsException("Unhandled type: " + model.Type);
			}
		}


		#endregion

		public static L10Recurrence GetCurrentL10RecurrenceFromMeeting(UserOrganizationModel caller, long l10MeetingId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).ViewL10Meeting(l10MeetingId);
					var recurrence = s.Get<L10Meeting>(l10MeetingId).L10RecurrenceId;

					return GetL10Recurrence(s, perms, recurrence, true);
				}
			}
		}

		public static long GetLatestMeetingId(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var meeting = s.QueryOver<L10Meeting>().Where(x => x.L10RecurrenceId == recurrenceId && x.DeleteTime == null).OrderBy(x => x.Id).Desc.Take(1).List().ToList();
					var m = meeting.SingleOrDefault();
					return m.NotNull(x => x.Id);
				}
			}
		}

		public static void SetMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Meeting_Measurables)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var l10measurables =s.QueryOver<L10Meeting.L10Meeting_Measurable>().WhereRestrictionOn(x => x.Id).IsIn(orderedL10Meeting_Measurables).Where(x=>x.DeleteTime==null).List().ToList();

					if (!l10measurables.Any())
						throw new PermissionsException("None found.");
					if (l10measurables.GroupBy(x => x.L10Meeting.Id).Count()>1)
						throw new PermissionsException("Measurables must be part of the same meeting");
					if (l10measurables.First().L10Meeting.L10RecurrenceId!=recurrenceId)
						throw new PermissionsException("Not part of the specifed L10");
					var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();

					for (var i = 0; i < orderedL10Meeting_Measurables.Count; i++){
						var id = orderedL10Meeting_Measurables[i];
						var f = l10measurables.FirstOrDefault(x => x.Id == id);
						if (f != null){
							f._Ordering = i;
							s.Update(f);
							var g = recurMeasurables.FirstOrDefault(x => x.Measurable.Id == f.Measurable.Id);
							if (g != null){
								g._Ordering = i;
								s.Update(g);
							}
						}
					}

					Audit.L10Log(s, caller, recurrenceId, "SetMeasurableOrdering");

					tx.Commit();
					s.Flush();

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

					group.reorderMeasurables(orderedL10Meeting_Measurables);

					var updates = new AngularUpdate();
					foreach (var x in recurMeasurables){
						updates.Add( new AngularMeasurable(x.Measurable){Ordering = x._Ordering});
					}
					group.update(updates);


				}
			}


		}

		public static List<L10AuditModel> GetL10Audit(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var audits = s.QueryOver<L10AuditModel>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
						.Fetch(x=>x.UserOrganization).Eager
						.TransformUsing(Transformers.DistinctRootEntity)
						.List().ToList();
					return audits;
				}
			}
		}
	}
}



