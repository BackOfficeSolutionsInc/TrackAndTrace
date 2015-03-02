using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using ImageResizer.Configuration.Issues;
using Microsoft.AspNet.SignalR;
using NHibernate.Linq;
using NHibernate.Transform;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using NHibernate;
using ListExtensions = WebGrease.Css.Extensions.ListExtensions;

namespace RadialReview.Accessors
{
	public class L10Accessor
	{
		public static void EditL10Recurrence(UserOrganizationModel caller, L10Recurrence l10Recurrence)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditL10Meeting(caller.Organization.Id, l10Recurrence.Id);

					//s.UpdateLists(l10Recurrence,DateTime.UtcNow,x=>x.DefaultAttendees,x=>x.DefaultMeasurables);
					/*if (l10Recurrence.Id != 0){
						var old = s.Get<L10Recurrence>(l10Recurrence.Id);
						//SetUtility.AddRemove(old.DefaultAttendees,l10Recurrence.DefaultAttendees,x=>x.)
					}*/
					var old = s.Get<L10Recurrence>(l10Recurrence.Id);
					_LoadRecurrences(s, false, false, old);

					var oldMeeting = _GetCurrentL10Meeting(s, caller, l10Recurrence.Id, true, true);
					SetUtility.AddedRemoved<MeasurableModel> updateMeasurables=null;
					if (oldMeeting != null){
						updateMeasurables = SetUtility.AddRemove(oldMeeting._MeetingMeasurables.Select(x=>x.Measurable),l10Recurrence._DefaultMeasurables.Select(x=>x.Measurable) ,x=>x.Id);
					}


					var now = DateTime.UtcNow;
					s.UpdateList(old.NotNull(x => x._DefaultAttendees), l10Recurrence._DefaultAttendees, now);
					s.UpdateList(old.NotNull(x => x._DefaultMeasurables), l10Recurrence._DefaultMeasurables, now);

					s.Evict(old);

					s.SaveOrUpdate(l10Recurrence);
					if (updateMeasurables != null)
					{
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateMeasurables.AddedValues)
						{
							s.Save(new L10Meeting.L10Meeting_Measurable(){
								L10Meeting = oldMeeting,
								Measurable = a,
							});
						}
						foreach (var a in updateMeasurables.RemovedValues){
							var o= oldMeeting._MeetingMeasurables.First(x => x.Measurable.Id == a.Id);
							o.DeleteTime = now;
							s.Update(o);
						}
					}



					tx.Commit();
					s.Flush();
				}
			}

		}

		public static void _LoadMeetingLogs(ISession s, params L10Meeting[] meetings)
		{
			var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();
			if (meetingIds.Any()){
				var allLogs = s.QueryOver<L10Meeting.L10Meeting_Log>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var now = DateTime.UtcNow;
				foreach (var m in meetings.Where(x => x != null)){
					m._MeetingLogs = allLogs.Where(x => m.Id == x.L10Meeting.Id).ToList();

					m._MeetingLeaderPageDurations = m._MeetingLogs
						.Where(x => x.User.Id == m.MeetingLeader.Id && x.EndTime!=null)
						.GroupBy(x => x.Page)
						.Select(x =>
							Tuple.Create(
								x.First().Page,
								x.Sum(y => ((y.EndTime ?? now) - y.StartTime).TotalMinutes)
								)).ToList();
					var curPage = m._MeetingLogs.SingleOrDefault(x => x.User.Id == m.MeetingLeader.Id && x.EndTime == null);
					if (curPage != null){
						m._MeetingLeaderCurrentPage = curPage.Page;
						m._MeetingLeaderCurrentPageStartTime = curPage.StartTime;
						m._MeetingLeaderCurrentPageBaseMinutes = m._MeetingLeaderPageDurations.Where(x=>x.Item1==curPage.Page).Sum(x=>x.Item2);
					}
				}
			}
			
		}

		public static void _LoadMeetings(ISession s, bool loadUsers, bool loadMeasurables, params L10Meeting[] meetings)
		{
			var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();

			if (meetingIds.Any()){
				var allAttend = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var allMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();

				foreach (var m in meetings.Where(x => x != null)){
					m._MeetingAttendees = allAttend.Where(x => m.Id == x.L10Meeting.Id).ToList();
					m._MeetingMeasurables = allMeasurables.Where(x => m.Id == x.L10Meeting.Id).ToList();
					if (loadUsers){
						foreach (var u in m._MeetingAttendees)
						{
							u.User.GetName();
							u.User.ImageUrl();
						}
					}
					if (loadMeasurables){
						foreach (var u in m._MeetingMeasurables){
							u.Measurable.AccountableUser.GetName();
							u.Measurable.AccountableUser.ImageUrl();
						}
					}

				}
			}
		}

		public static void _LoadRecurrences(ISession s, bool loadUsers, bool loadMeasurables, params L10Recurrence[] all)
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

				foreach (var a in all.Where(x => x != null))
				{
					a._DefaultAttendees = allAttend.Where(x => a.Id == x.L10Recurrence.Id).ToList();
					a._DefaultMeasurables = allMeasurables.Where(x => a.Id == x.L10Recurrence.Id).ToList();
					if (loadUsers)
					{
						foreach (var u in a._DefaultAttendees)
						{
							u.User.GetName();
							u.User.ImageUrl();
						}
					}
					if (loadMeasurables)
					{
						foreach (var u in a._DefaultMeasurables)
						{
							u.Measurable.AccountableUser.GetName();
							u.Measurable.AccountableUser.ImageUrl();
						}
					}

				}
			}

		}

		public static L10Recurrence GetL10Recurrence(UserOrganizationModel caller, long recurrenceId,bool load)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var found = s.Get<L10Recurrence>(recurrenceId);
					if(load)
						_LoadRecurrences(s, true, true, found);
					return found;
				}
			}
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
					var loadedL10 = s.QueryOver<L10Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids).List().ToList();
					allRecurrences.AddRange(loadedL10);

					//Load extra data
					allRecurrences = allRecurrences.Distinct(x => x.Id).ToList();
					_LoadRecurrences(s, true, false, allRecurrences.ToArray());

					//Make a lookup for self attendance
					var attending = attendee.Where(x => userId == x.User.Id).Select(x => x.L10Recurrence.Id).ToArray();
					return allRecurrences.Select(x => new L10VM(x)
					{
						IsAttendee = attending.Any(y => y == x.Id)
					}).ToList();
				}
			}
		}

		public static L10Meeting.L10Meeting_Log _GetCurrentLog(ISession s, UserOrganizationModel caller, long meetingId, long userId, bool nullOnUnstarted = false)
		{
			var found= s.QueryOver<L10Meeting.L10Meeting_Log>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == userId && x.EndTime == null).List().SingleOrDefault();
			if (found == null && !nullOnUnstarted)
				throw new PermissionsException("Meeting log does not exist");
			return found;
		}


		public static L10Meeting _GetCurrentL10Meeting(ISession s, UserOrganizationModel caller, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs=false)
		{
			var found = s.QueryOver<L10Meeting>().Where(x =>
					x.StartTime != null &&
					x.CompleteTime == null &&
					x.DeleteTime == null &&
					x.L10RecurrenceId == recurrenceId
				).List().ToList();

			if (!found.Any()){
				if (nullOnUnstarted)
					return null;
				throw new MeetingException("Meeting has not been started.", MeetingExceptionType.Unstarted);
			}
			if (found.Count != 1){
				throw new MeetingException("Too many open meetings.", MeetingExceptionType.TooMany);
			}
			var meeting = found.First();
			PermissionsUtility.Create(s, caller).ViewL10Meeting(meeting.Id);
			if (load)
				_LoadMeetings(s, true, true, meeting);

			if (loadLogs)
				_LoadMeetingLogs(s, meeting);

			return meeting;
		}

		public static L10Meeting GetCurrentL10Meeting(UserOrganizationModel caller, long recurrenceId, bool nullOnUnstarted = false,bool load=false,bool loadLogs=false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return _GetCurrentL10Meeting(s, caller, recurrenceId, nullOnUnstarted, load, loadLogs);
				}
			}
		}

		public static List<ScoreModel> GetScoresForRecurrence(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var r = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();
					var measurables = r.Distinct(x => x.Measurable.Id).Select(x => x.Measurable.Id).ToList();

					var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.MeasurableId).IsIn(measurables).List().ToList();

					//Touch 
					foreach (var a in scores)
					{
						var i = a.Measurable.Goal;
						var u = a.Measurable.AccountableUser.GetName();
						var v = a.Measurable.AccountableUser.ImageUrl();
						var j = a.AccountableUser.GetName();
						var k = a.AccountableUser.ImageUrl();
					}

					return scores;
				}

			}
		}
		public static void StartMeeting(UserOrganizationModel caller, UserOrganizationModel meetingLeader, long recurrenceId, List<UserOrganizationModel> attendees)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					if (caller.Id != meetingLeader.Id)
						PermissionsUtility.Create(s, meetingLeader).ViewL10Recurrence(recurrenceId);
				

					lock ("Recurrence_" + recurrenceId){
						//Make sure we're unstarted
						try{
							_GetCurrentL10Meeting(s, caller, recurrenceId, false);

							throw new MeetingException("Meeting has already started.",MeetingExceptionType.AlreadyStarted);
						}
						catch (MeetingException e){
							if (e.MeetingExceptionType != MeetingExceptionType.Unstarted)
								throw;
						}

						var now = DateTime.UtcNow;
						var recurrence = s.Get<L10Recurrence>(recurrenceId);

						var meeting = new L10Meeting{
							CreateTime = now,
							StartTime = now,
							L10RecurrenceId = recurrenceId,
							L10Recurrence = recurrence,
							OrganizationId = recurrence.OrganizationId,
							MeetingLeader = meetingLeader,
							MeetingLeaderId = meetingLeader.Id
						};

						s.Save(meeting);

						_LoadRecurrences(s, false, false, recurrence);

						foreach (var m in recurrence._DefaultMeasurables){
							var mm = new L10Meeting.L10Meeting_Measurable(){
								L10Meeting = meeting,
								Measurable = m.Measurable,
							};
							s.Save(mm);
							meeting._MeetingMeasurables.Add(mm);
						}
						foreach (var m in attendees){
							var mm = new L10Meeting.L10Meeting_Attendee(){
								L10Meeting = meeting,
								User = m,
							};
							s.Save(mm);
							meeting._MeetingAttendees.Add(mm);
						}
						tx.Commit();
						s.Flush();
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting)).setupMeeting();
					}
				}
			}
		}
		public static void ConcludeMeeting(UserOrganizationModel caller, long recurrenceId, List<System.Tuple<long, int?>> ratingValues)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var now = DateTime.UtcNow;
					//Make sure we're unstarted
					var meeting = _GetCurrentL10Meeting(s, caller, recurrenceId, false);
					PermissionsUtility.Create(s, caller).ViewL10Meeting(meeting.Id);

					meeting.CompleteTime = now;

					var recurrence = s.Get<L10Recurrence>(recurrenceId);
					s.Update(meeting);

					var ids = ratingValues.Select(x => x.Item1).ToArray();

					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
						.WhereRestrictionOn(x => x.User.Id)
						.IsIn(ids)
						.List().ToList();

					foreach (var a in attendees){
						a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
						s.Update(a);
					}


					var logs = s.QueryOver<L10Meeting.L10Meeting_Log>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.EndTime==null)
						.List().ToList();
					foreach (var l in logs){
						l.EndTime = now;
						s.Update(l);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		/*
		public L10Recurrence GetL10Meeting(UserOrganizationModel userOrganizationModel, long meetingId)
		{
			throw new NotImplementedException();
		}*/

		public static L10Meeting.L10Meeting_Connection JoinL10Meeting(UserOrganizationModel caller, long recurrenceId, string connectionId)
		{
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
			}
		}

		public static L10Meeting.L10Meeting_Connection GetConnection(ISession s, UserOrganizationModel caller, long recurrenceId)
		{
			var meeting = _GetCurrentL10Meeting(s, caller, recurrenceId);
			var meetingId = meeting.Id;
			var found = s.QueryOver<L10Meeting.L10Meeting_Connection>().Where(x =>
					x.DeleteTime == null &&
					x.L10Meeting.Id == meetingId &&
					x.User.Id == caller.Id
				).SingleOrDefault<L10Meeting.L10Meeting_Connection>();
			if (found == null)
				throw new PermissionsException("You do not have access to this meeting.");
			return found;
		}



		public static List<L10Meeting> GetL10Meetings(UserOrganizationModel caller, long recurrenceId,bool load=false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var o = s.QueryOver<L10Meeting>()
						.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
						.List().ToList();
					if (load)
						_LoadMeetings(s, true, true, o.ToArray());

					return o;

				}
			}
		}

		public static void UpdatePage(UserOrganizationModel caller,long forUserId, long recurrenceId, string pageName)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var meeting = _GetCurrentL10Meeting(s, caller, recurrenceId, true, false,true);
					if (meeting == null)	return;
					//if (caller.Id != meeting.MeetingLeader.Id)	return;


					var forUser = s.Get<UserOrganizationModel>(forUserId);
					if (meeting.MeetingLeaderId == 0){
						meeting.MeetingLeaderId = forUser.Id;
						meeting.MeetingLeader = forUser;
					}

					if (caller.Id != forUserId)
						PermissionsUtility.Create(s, forUser).ViewL10Meeting(meeting.Id);

					var log = _GetCurrentLog(s, caller, meeting.Id, forUserId, true);

					var now = DateTime.UtcNow;
					var addNew = true;
					if (log != null){
						addNew = log.Page != pageName;

						if (addNew){
							log.EndTime = now;//new DateTime(Math.Min(log.StartTime.AddMinutes(1).Ticks,now.Ticks));
							s.Update(log);
						}
					}

					if (addNew){
						var newLog = new L10Meeting.L10Meeting_Log(){
							User = forUser,
							StartTime = now,
							L10Meeting = meeting,
							Page = pageName,
						};

						s.Save(newLog);

						

						if (meeting.MeetingLeader.NotNull(x=>x.Id) == forUserId){
							var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
							var meetingHub=hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting));
							var baseMins = meeting._MeetingLeaderPageDurations.SingleOrDefault(x=>x.Item1==pageName).NotNull(x=>x.Item2);
							meetingHub.setCurrentPage(pageName, now.ToJavascriptMilliseconds(), baseMins);

							foreach (var a in meeting._MeetingLeaderPageDurations){
								if (a.Item1 != pageName){
									meetingHub.setPageTime(a.Item1,a.Item2);
								}
							}
						}

					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static string GetCurrentL10MeetingLeaderPage(UserOrganizationModel caller, long meetingId)
		{
			using(var s = HibernateSession.GetCurrentSession())
			{
				using(var tx=s.BeginTransaction()){
					var leaderId = s.Get<L10Meeting>(meetingId).MeetingLeader.Id;
					var leaderpage = s.QueryOver<L10Meeting.L10Meeting_Log>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id==leaderId && x.EndTime == null)
						.SingleOrDefault();
					return leaderpage.NotNull(x => x.Page);
				}
			}
		}

		public static List<IssueModel> GetIssuesForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeResolved)
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
						////.JoinAlias(()=>linkAlias.Issue,()=>issueAlias)
						////.Where(()=>issueAlias.DeleteTime==null && (includeResolved || issueAlias.CloseTime==null))
						//.Select(x => x.Issue.Id)
						.Fetch(x=>x.Issue).Eager
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

		private static List<IssueModel> _PopulateChildrenIssues(List<IssueModel.IssueModel_Recurrence> list)
		{

			var output = list.Where(x => x.ParentIssue == null).Select(x =>{
				x.Issue._Order = x.Ordering;
				return x.Issue;
			}).ToList();
			foreach (var o in output){
				_RecurseChildrenIssues(o, list);
			}
			return output;

		}

		private static void _RecurseChildrenIssues(IssueModel issue, List<IssueModel.IssueModel_Recurrence> list)
		{
			if (issue._ChildIssues != null)
				return;
			issue._ChildIssues = list.Where(x => x.ParentIssue!=null && x.ParentIssue.Id == issue.Id).Select(x =>{
				x.Issue._Order = x.Ordering;
				return x.Issue;
			}).ToList();

			foreach (var i in issue._ChildIssues){
				_RecurseChildrenIssues(i,list);
			}
		}



		public static void UpdateIssues(UserOrganizationModel caller,long recurrenceId, IssuesDataList model)
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){

					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var ids = model.GetAllIds();
					var found = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.Recurrence.Id==recurrenceId)
						.WhereRestrictionOn(x => x.Issue.Id).IsIn(ids)
						//.Fetch(x=>x.Issue).Eager
						.List().ToList();

					var ar = SetUtility.AddRemove(ids,found.Select(x => x.Issue.Id));

					if (ar.RemovedValues.Any())
						throw new PermissionsException("You do not have permission to edit this issue.");
					if (ar.AddedValues.Any())
						throw new PermissionsException("Unreachable.");
					
					var issues = found.ToList();

					foreach (var e in model.GetIssueEdits()){
						var f = issues.First(x => x.Issue.Id == e.IssueId);
						var update = false;
						if (f.ParentIssue.NotNull(x=>x.Id) != e.ParentIssueId){
							f.ParentIssue = (e.ParentIssueId == null) ? null : issues.First(x => x.Issue.Id == e.ParentIssueId).Issue;
							update = true;
						}

						if (f.Ordering != e.Order){
							f.Ordering = e.Order;
							update = true;
						}

						if (update)
							s.Update(f);
					}

					var json = Json.Encode(model);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId),model.connectionId).deserializeIssues(".ids-list", model);
					
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}