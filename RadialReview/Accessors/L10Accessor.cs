using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using Amazon.EC2.Model;
using Amazon.ElasticMapReduce.Model;
using FluentNHibernate.Conventions;
using ImageResizer.Configuration.Issues;
using MathNet.Numerics;
using Microsoft.AspNet.SignalR;
using NHibernate.Criterion;
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
using RadialReview.Models.Permissions;
using RadialReview.Models.Scheduler;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Synchronize;
using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Enums;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Base;
using System.Web.WebPages.Html;

namespace RadialReview.Accessors
{
    public class L10Accessor : BaseAccessor
    {
        public static MeasurableModel TodoMeasurable = new MeasurableModel()
        {
            Id = -10001,
            Title = "To-Do Completion",
            _Editable = false,
            Goal = 90,
            GoalDirection = LessGreater.GreaterThan,
            UnitType = UnitType.Percent,
        };

        public static MeasurableModel GenerateTodoMeasureable(UserOrganizationModel forUser)
        {
            return new MeasurableModel()
            {
                Id = -10001 - forUser.Id,
                Title = "To-Do Completion " + forUser.GetName(),
                _Editable = false,
                Goal = 90,
                GoalDirection = LessGreater.GreaterThan,
                UnitType = UnitType.Percent,

            };
        }


        public static string GetDefaultStartPage(L10Recurrence recurrence)
        {
            var p = "segue";
            if (recurrence.SegueMinutes > 0)
                p = "segue";
            else if (recurrence.ScorecardMinutes > 0)
                p = "scorecard";
            else if (recurrence.RockReviewMinutes > 0)
                p = "rocks";
            else if (recurrence.HeadlinesMinutes > 0)
                p = "headlines";
            else if (recurrence.TodoListMinutes > 0)
                p = "todo";
            else if (recurrence.IDSMinutes > 0)
                p = "ids";
            else
                p = "conclusion";
            return p;
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


                foreach (var m in meetings)
                {
                    if (m.L10Recurrence.IncludeAggregateTodoCompletion)
                    {
                        allMeasurables.Add(new L10Meeting.L10Meeting_Measurable()
                        {
                            _Ordering = -2,
                            Id = -1,
                            L10Meeting = m,
                            Measurable = TodoMeasurable
                        });
                    }
                }

                foreach (var m in meetings.Where(x => x != null))
                {
                    m._MeetingAttendees = allAttend.Where(x => m.Id == x.L10Meeting.Id).ToList();
                    m._MeetingMeasurables = allMeasurables.Where(x => m.Id == x.L10Meeting.Id).ToList();
                    m._MeetingRocks = allRocks.Where(x => m.Id == x.L10Meeting.Id).ToList();
                    if (m.L10Recurrence.IncludeIndividualTodos)
                    {
                        foreach (var u in m._MeetingAttendees)
                        {
                            m._MeetingMeasurables.Add(new L10Meeting.L10Meeting_Measurable()
                            {
                                _Ordering = -1,
                                Id = -1,
                                L10Meeting = m,
                                Measurable = GenerateTodoMeasureable(u.User)
                            });
                        }
                    }
                    if (loadUsers)
                    {
                        foreach (var u in m._MeetingAttendees)
                        {
                            try
                            {
                                u.User.GetName();
                                u.User.ImageUrl();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                    if (loadMeasurables)
                    {

                        foreach (var u in m._MeetingMeasurables)
                        {
                            try
                            {
                                if (u.Measurable.AccountableUser != null)
                                {
                                    u.Measurable.AccountableUser.GetName();
                                    u.Measurable.AccountableUser.ImageUrl();
                                }
                                if (u.Measurable.AdminUser != null)
                                {
                                    u.Measurable.AdminUser.GetName();
                                    u.Measurable.AdminUser.ImageUrl();
                                }
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }
                    if (loadRocks)
                    {
                        foreach (var u in m._MeetingRocks)
                        {
                            try
                            {
                                u.ForRock.AccountableUser.GetName();
                                u.ForRock.AccountableUser.ImageUrl();
                            }
                            catch (Exception)
                            {

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
                    a._DefaultAttendees = allAttend.Where(x => a.Id == x.L10Recurrence.Id && x.User.DeleteTime == null).ToList();
                    var dm = allMeasurables.Where(x => a.Id == x.L10Recurrence.Id && ((x.Measurable != null && x.Measurable.DeleteTime == null) || (x.Measurable == null && x.IsDivider))).ToList();
                    a._DefaultRocks = allRocks.Where(x => a.Id == x.L10Recurrence.Id && x.ForRock.DeleteTime == null).ToList();
                    a._MeetingNotes = allNotes.Where(x => a.Id == x.Recurrence.Id && x.DeleteTime == null).ToList();

                    if (a.IncludeIndividualTodos)
                    {
                        foreach (var u in a._DefaultAttendees)
                        {

                            dm.Add(new L10Recurrence.L10Recurrence_Measurable()
                            {
                                _Ordering = -1,
                                Id = -1,
                                L10Recurrence = a,
                                Measurable = GenerateTodoMeasureable(u.User)
                            });
                        }
                    }
                    a._DefaultMeasurables = dm;

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
                        foreach (var u in a._DefaultMeasurables.Where(x => x.Measurable != null))
                        {
                            if (u.Measurable.AccountableUser != null)
                            {
                                u.Measurable.AccountableUser.GetName();
                                u.Measurable.AccountableUser.ImageUrl(true);
                            }
                            if (u.Measurable.AdminUser != null)
                            {
                                u.Measurable.AdminUser.GetName();
                                u.Measurable.AdminUser.ImageUrl(true);
                            }
                        }
                    }
                    if (loadRocks)
                    {
                        foreach (var u in a._DefaultRocks)
                        {
                            var b = u.ForRock.Rock;
                            var c = u.ForRock.Period.NotNull(x => x.Name);
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
            foreach (var o in output)
            {
                try
                {
                    if (o.Owner != null)
                    {
                        o.Owner.GetName();
                        o.Owner.GetImageUrl();
                    }
                }
                catch (Exception)
                {

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
                //throw new MeetingException("Too many open meetings.", MeetingExceptionType.TooMany);
                found = found.OrderByDescending(x => x.StartTime).ToList();
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

        public static List<NameId> GetVisibleL10Meetings_Tiny(UserOrganizationModel caller, long userId, bool onlyPersonallyAttending = false)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    List<long> nil;
                    var perms = PermissionsUtility.Create(s, caller);
                    var meetings = GetVisibleL10Meetings_Tiny(s, perms, userId, out nil);
                    if (onlyPersonallyAttending)
                    {
                        meetings = meetings.Where(x => nil.Contains(x.Id)).ToList();
                    }
                    return meetings;
                }
            }
        }
        public static List<NameId> GetVisibleL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId, out List<long> recurrencesPersonallyAttending)
        {

            var caller = perms.GetCaller();
            perms.ViewUsersL10Meetings(userId);

            L10Recurrence alias = null;
            //var allRecurrences = new List<L10Recurrence>();
            var allRecurrenceIds = new List<NameId>();
            if (caller.ManagingOrganization)
            {
                var orgRecurrences = s.QueryOver<L10Recurrence>().Where(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null)
                    .Select(x => x.Name, x => x.Id).List<object[]>().ToList();
                allRecurrenceIds.AddRange(orgRecurrences.Select(x => new NameId((string)x[0], (long)x[1])));
            }
            //Who should we get this data for? Just Self, or also subordiantes?
            var accessibleUserIds = new[] { userId };
            var user = s.Get<UserOrganizationModel>(userId);
            if (user.Organization.Settings.ManagersCanViewSubordinateL10)
                accessibleUserIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, userId).ToArray();

            var attendee_ReccurenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                .Where(x => x.DeleteTime == null)
                .WhereRestrictionOn(x => x.User.Id).IsIn(accessibleUserIds)
                .Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
                .Select(x => alias.Name, x => alias.Id, x => x.User.Id)
                .List<object[]>().ToList();
            var attendee_recurrences = attendee_ReccurenceIds.Select(x => new NameId((string)x[0], (long)x[1])).ToList();
            //var uniqueL10Ids = attendee_recurrences.Distinct(x => x.Id).ToList();
            allRecurrenceIds.AddRange(attendee_recurrences);

            recurrencesPersonallyAttending = attendee_ReccurenceIds.Where(x => (long)x[2] == userId).Select(x => (long)x[1]).ToList();
            //Actually load the Recurrences

            var admin_MeasurableIds = s.QueryOver<MeasurableModel>().Where(x => x.AdminUserId == userId && x.DeleteTime == null).Select(x => x.Id).List<long>().ToList();

            var admin_RecurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null)
                .WhereRestrictionOn(x => x.Measurable.Id).IsIn(admin_MeasurableIds)
                .Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
                .Select(x => alias.Name, x => alias.Id)
                .List<object[]>().Select(x => new NameId((string)x[0], (long)x[1])).ToList();
            allRecurrenceIds.AddRange(admin_RecurrenceIds);


            var allViewPerms = PermissionsAccessor.GetPermItemsForUser(s, perms, userId, PermItem.ResourceType.L10Recurrence);
            var additionalRecurrenceIdsFromPerms = allViewPerms.Where(x => !allRecurrenceIds.Any(y => y.Id == x)).ToList();

            var additionalRecurrenceFromViewPerms = s.QueryOver<L10Recurrence>()
                .WhereRestrictionOn(x => x.Id).IsIn(additionalRecurrenceIdsFromPerms)
                .Select(x => x.Name, x => x.Id)
                .List<object[]>().Select(x => new NameId((string)x[0], (long)x[1])).ToList();


            allRecurrenceIds.AddRange(additionalRecurrenceFromViewPerms);
            allRecurrenceIds = allRecurrenceIds.Distinct(x => x.Id).ToList();
            var available = new List<NameId>();
            foreach (var r in allRecurrenceIds)
            {
                try
                {
                    perms.CanView(PermItem.ResourceType.L10Recurrence, r.Id);
                    available.Add(r);
                }
                catch
                {
                }
            }
            return available;
        }

        public static List<L10VM> GetVisibleL10Meetings(UserOrganizationModel caller, long userId, bool loadUsers)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    List<long> attendee_recurrences;
                    var uniqueL10NameIds = GetVisibleL10Meetings_Tiny(s, perms, userId, out attendee_recurrences);
                    var uniqueL10Ids = uniqueL10NameIds.Select(x => x.Id).ToList();


                    var allRecurrences = s.QueryOver<L10Recurrence>()
                        .Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids)
                        .List().ToList();
                    //allRecurrences.AddRange(loadedL10);


                    //Load extra data
                    //var allRecurrencesDistinct = allRecurrences.Distinct(x => x.Id).ToList();

                    _LoadRecurrences(s, loadUsers, false, false, allRecurrences.ToArray());

                    //Make a lookup for self attendance
                    //var attending = attendee_recurrences.Where(x => userId == x.User.Id).Select(x => x.L10Recurrence.Id).ToArray();
                    return allRecurrences.Select(x => new L10VM(x)
                    {
                        IsAttendee = attendee_recurrences.Any(y => y == x.Id),
                        EditMeeting = perms.IsPermitted(y => y.AdminL10Recurrence(x.Id))
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

        private static Ratio TodoCompletion(List<TodoModel> todos, DateTime week, DateTime now)
        {
            var ratio = new Ratio(0, 0);
            foreach (var t in todos)
            {
                if (t.CreateTime < week.AddDays(-7))
                {
                    if (t.CompleteTime == null || week < t.CompleteTime.Value)
                    {
                        ratio.Add(0, 1);
                    }
                    else if (week.AddDays(-7) <= t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) && t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) < week)
                    {
                        ratio.Add(1, 1);
                    }
                }
                /*if (week.AddDays(-7) <= t.DueDate.StartOfWeek(DayOfWeek.Sunday) && t.DueDate.StartOfWeek(DayOfWeek.Sunday) < week)
                {

                    if (currentWeek){
                        //do something different...
                    }
                    else{

                        if (t.CompleteTime == null){
                            ratio.Add(0, 1);
                        }else{
                            if (t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) <= week)
                                ratio.Add(1, 1);
                            else
                                ratio.Add(0, 1);
                        }
                    }
                }*/
            }
            return ratio;


        }

        public static List<ScoreModel> GetScoresForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId, bool includeAutoGenerated = true)
        {
            perm.ViewL10Recurrence(recurrenceId);

            var r = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();
            var measurables = r.Where(x => x.Measurable != null).Distinct(x => x.Measurable.Id).Select(x => x.Measurable.Id).ToList();


            var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.MeasurableId).IsIn(measurables).List().ToList();

            var m = s.Get<L10Recurrence>(recurrenceId);

            if (includeAutoGenerated)
            {
                List<TodoModel> todoCompletion = null;
                if (m.IncludeAggregateTodoCompletion || m.IncludeIndividualTodos)
                {
                    todoCompletion = GetAllTodosForRecurrence(s, perm, recurrenceId);
                }


                if (m.IncludeAggregateTodoCompletion)
                {
                    var todoScores = scores.GroupBy(x => x.ForWeek).SelectMany(w =>
                    {
                        try
                        {
                            var ss = TodoCompletion(todoCompletion, w.Key, DateTime.UtcNow);
                            decimal? percent = null;
                            if (ss.IsValid())
                            {
                                percent = Math.Round(ss.GetValue(0) * 100m, 1);
                            }

                            return new ScoreModel()
                            {
                                _Editable = false,
                                AccountableUserId = -1,
                                ForWeek = w.Key,
                                Measurable = TodoMeasurable,
                                Measured = percent,
                                MeasurableId = TodoMeasurable.Id,

                            }.AsList();
                        }
                        catch (Exception e)
                        {
                            return new List<ScoreModel>();
                        }
                    });
                    scores.AddRange(todoScores);
                }


                if (m.IncludeIndividualTodos)
                {
                    var individualTodoScores = scores.GroupBy(x => x.ForWeek).SelectMany(ww =>
                    {
                        return todoCompletion.GroupBy(x => x.AccountableUserId).SelectMany(todos =>
                        {
                            var a = todos.First().AccountableUser;
                            try
                            {
                                var ss = TodoCompletion(todos.ToList(), ww.Key, DateTime.UtcNow);
                                decimal? percent = null;
                                if (ss.IsValid())
                                {
                                    percent = Math.Round(ss.GetValue(0) * 100m, 1);
                                }

                                var mm = GenerateTodoMeasureable(a);

                                return new ScoreModel()
                                {
                                    _Editable = false,
                                    AccountableUserId = a.Id,
                                    ForWeek = ww.Key,
                                    Measurable = mm,
                                    Measured = percent,
                                    MeasurableId = mm.Id,

                                }.AsList();
                            }
                            catch (Exception e)
                            {
                                return new List<ScoreModel>();
                            }
                        });
                    });
                    scores.AddRange(individualTodoScores);
                }
            }

            var userQueries = scores.SelectMany(x =>
            {
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
                try
                {
                    //	a.Measurable.AccountableUser = userLookup[];

                    var i = a.Measurable.Goal;
                    var u = a.Measurable.AccountableUser.GetName();
                    var v = a.Measurable.AccountableUser.ImageUrl(true);
                    var j = a.AccountableUser.GetName();
                    var k = a.AccountableUser.ImageUrl(true);
                    var u1 = a.Measurable.AdminUser.GetName();
                    var v1 = a.Measurable.AdminUser.ImageUrl(true);
                }
                catch (Exception e)
                {
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

        public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, DateTime? meetingStart = null)
        {
            var mstart = meetingStart ?? DateTime.MaxValue;
            perms.ViewL10Recurrence(recurrenceId);
            //TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.

            var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                .Where(x =>
                    x.DeleteTime == null && x.Recurrence.Id == recurrenceId &&
                    (x.CloseTime == null || x.CloseTime >= mstart)
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



        public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(UserOrganizationModel caller, long meetingId, bool includeResolved)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var meeting = s.Get<L10Meeting>(meetingId);
                    var recurrenceId = meeting.L10RecurrenceId;
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetIssuesForRecurrence(s, perms, recurrenceId, meeting.StartTime);
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
            bool wasCreated = false;
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perm = PermissionsUtility.Create(s, caller);
                    if (l10Recurrence.Id == 0)
                    {
                        perm.CreateL10Recurrence(caller.Organization.Id);
                        l10Recurrence.CreatedById = caller.Id;
                        wasCreated = true;
                    }
                    else
                        perm.AdminL10Recurrence(l10Recurrence.Id);

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
                        updateMeasurables = SetUtility.AddRemove(oldMeeting._MeetingMeasurables.Where(x => !x.IsDivider).Select(x => x.Measurable), l10Recurrence._DefaultMeasurables.Select(x => x.Measurable), x => x.Id);
                        var updateableMeasurables = ScorecardAccessor.GetVisibleMeasurables(s, perm, l10Recurrence.OrganizationId, false);
                        if (!updateMeasurables.AddedValues.All(x => updateableMeasurables.Any(y => y.Id == x.Id)))
                            throw new PermissionsException("You do not have access to add one or more measurables.");

                    }
                    SetUtility.AddedRemoved<RockModel> updateRocks = null;
                    if (oldMeeting != null)
                    {
                        updateRocks = SetUtility.AddRemove(
                            oldMeeting._MeetingRocks.Select(x => x.ForRock),
                            l10Recurrence._DefaultRocks.Select(x => x.ForRock),
                            x => x.Id);

                        var updatedRocks = RockAccessor.GetAllVisibleRocksAtOrganization(s, perm, l10Recurrence.OrganizationId, false);
                        if (!updateRocks.AddedValues.All(x => updatedRocks.Any(y => y.Id == x.Id)))
                            throw new PermissionsException("You do not have access to add one or more rock.");
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

                    if (old != null)
                    {
                        foreach (var m in l10Recurrence._DefaultMeasurables)
                        {
                            m._Ordering = old._DefaultMeasurables.FirstOrDefault(x => x.Measurable != null && m.Measurable != null && x.Measurable.Id == m.Measurable.Id).NotNull(x => x._Ordering);
                        }
                    }

                    s.UpdateList(old.NotNull(x => x._DefaultAttendees), l10Recurrence._DefaultAttendees, now);
                    s.UpdateList(old.NotNull(x => x._DefaultMeasurables), l10Recurrence._DefaultMeasurables, now);
                    s.UpdateList(old.NotNull(x => x._DefaultRocks), l10Recurrence._DefaultRocks, now);

                    s.Evict(old);

                    s.SaveOrUpdate(l10Recurrence);

                    if (wasCreated)
                    {

                        s.Save(new PermItem()
                        {
                            CanAdmin = true,
                            CanEdit = true,
                            CanView = true,
                            AccessorType = PermItem.AccessType.Creator,
                            AccessorId = caller.Id,
                            ResType = PermItem.ResourceType.L10Recurrence,
                            ResId = l10Recurrence.Id,
                            CreatorId = caller.Id,
                            OrganizationId = caller.Organization.Id,
                            IsArchtype = false,
                        });
                        s.Save(new PermItem()
                        {
                            CanAdmin = true,
                            CanEdit = true,
                            CanView = true,
                            AccessorType = PermItem.AccessType.Members,
                            AccessorId = -1,
                            ResType = PermItem.ResourceType.L10Recurrence,
                            ResId = l10Recurrence.Id,
                            CreatorId = caller.Id,
                            OrganizationId = caller.Organization.Id,
                            IsArchtype = false,
                        });
                    }

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
                            if (a.Id > 0)
                            { //Todo Completion is -10001
                                var o = oldMeeting._MeetingMeasurables.First(x => x.Measurable!=null && x.Measurable.Id == a.Id);
                                if (!o.IsDivider)
                                {
                                    o.DeleteTime = now;
                                    s.Update(o);
                                }
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
                            var o = oldMeeting._MeetingRocks.First(x => x.ForRock !=null && x.ForRock.Id == a.Id);
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
                            var o = oldMeeting._MeetingAttendees.First(x => x.User!=null && x.User.Id == a.Id);
                            o.DeleteTime = now;
                            s.Update(o);
                        }
                    }

                    Audit.L10Log(s, caller, l10Recurrence.Id, "EditL10Recurrence", ForModel.Create(l10Recurrence));

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
                    if (meeting == null)
                        return;
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
                        p = p.ToUpper()[0] + p.Substring(1);

                    Audit.L10Log(s, caller, recurrenceId, "UpdatePage", ForModel.Create(meeting), p);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public static void UpdateTodos(UserOrganizationModel caller, long recurrenceId, L10Controller.UpdateTodoVM model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
                    var ids = model.todos;// model.GetAllIds();
                    var existingTodos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId)
                        .WhereRestrictionOn(x => x.Id).IsIn(ids)
                        .List().ToList();

                    var ar = SetUtility.AddRemove(ids, existingTodos.Select(x => x.Id));

                    if (ar.RemovedValues.Any())
                        throw new PermissionsException("You do not have permission to edit this issue.");
                    if (ar.AddedValues.Any())
                        throw new PermissionsException("Unreachable.");

                    //var recurrenceIssues = existingTodos.ToList();
                    var i = 0;
                    foreach (var e in model.todos)
                    {
                        var f = existingTodos.First(x => x.Id == e);
                        var update = false;
                        /*if (f..NotNull(x => x.Id) != e.ParentRecurrenceIssueId)
                        {
                            f.ParentRecurrenceIssue = (e.ParentRecurrenceIssueId == null) ? null : recurrenceIssues.First(x => x.Id == e.ParentRecurrenceIssueId);
                            update = true;
                        }*/

                        if (f.Ordering != i)
                        {
                            f.Ordering = i;
                            update = true;
                        }
                        if (update)
                            s.Update(f);
                        i++;
                    }

                    var json = Json.Encode(model);

                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), model.connectionId);

                    //group.deserializeTodos(".todo-list", model);
                    group.setTodoOrder(model.todos);


                    group.update(new AngularRecurrence(recurrenceId)
                    {
                        Todos = existingTodos.OrderBy(x => x.Ordering).Select(x => new AngularTodo(x)).ToList()
                    });

                    Audit.L10Log(s, caller, recurrenceId, "UpdateTodos", ForModel.Create<L10Recurrence>(recurrenceId));
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public static void UpdateIssues(UserOrganizationModel caller, long recurrenceId, /*IssuesDataList*/L10Controller.IssuesListVm model)
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

                    if (model.orderby != null)
                    {
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
                    var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), model.connectionId);

                    //group.deserializeIssues(".issues-list", model);
                    group.setIssueOrder(model.issues);
                    var issues = GetAllIssuesForRecurrence(s, perm, recurrenceId)
                        .OrderBy(x => x.Ordering)
                        .Select(x => new AngularIssue(x))
                        .ToList();



                    group.update(new AngularRecurrence(recurrenceId)
                    {
                        Issues = AngularList.Create(AngularListType.ReplaceAll, issues)
                    });

                    Audit.L10Log(s, caller, recurrenceId, "UpdateIssues", ForModel.Create<L10Recurrence>(recurrenceId));

                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public static void UpdateRock(UserOrganizationModel caller, long id, String rockMessage, RockState? state, string connectionId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var rock = s.Get<RockModel>(id);
                    perms.EditRock(rock);
                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();

                    var now = DateTime.UtcNow;
                    var updated = false;
                    if (rockMessage != null)
                    {
                        rock.Rock = rockMessage;
                        s.Update(rock);
                        updated = true;
                        var rockRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
                        .Where(x => x.DeleteTime == null && x.ForRock.Id == id)
                        .List().ToList();

                        foreach (var r in rockRecurrences)
                        {
                            hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
                                .updateRockName(r.Id, rockMessage);
                        }
                    }
                    if (state != null)
                    {
                        SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateRockCompletion(id));

                        rock.Completion = state.Value;
                        if (state != RockState.Indeterminate && rock.Completion != state)
                        {
                            if (state == RockState.Complete)
                            {
                                rock.CompleteTime = now;
                            }
                            s.Update(rock);
                        }
                        else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate)
                        {
                            rock.Completion = RockState.Indeterminate;
                            rock.CompleteTime = null;
                            s.Update(rock);
                        }

                        s.Update(rock);

                        UpdateRock(id, state, connectionId, s, perms, rock, hub, now);
                    }

                    if (updated)
                    {
                        tx.Commit();
                        s.Flush();
                    }
                }
            }
        }

        private static void UpdateRock(long id, RockState? state, string connectionId, ISession s, PermissionsUtility perms, RockModel rock, IHubContext hub, DateTime now)
        {
            var rockRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
                                  .Where(x => x.DeleteTime == null && x.ForRock.Id == id)
                                  .List().ToList();



            foreach (var r in rockRecurrences)
            {

                var curMeeting = _GetCurrentL10Meeting(s, perms, r.L10Recurrence.Id, true, false, false);
                if (curMeeting != null)
                {
                    var meetingRock = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == curMeeting.Id && x.ForRock.Id == rock.Id).SingleOrDefault();
                    if (meetingRock != null)
                    {

                        if (state != RockState.Indeterminate && meetingRock.Completion != state)
                        {
                            meetingRock.Completion = state.Value;
                            if (state == RockState.Complete)
                            {
                                meetingRock.CompleteTime = now;
                            }
                            s.Update(meetingRock);
                        }
                        else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate)
                        {
                            meetingRock.Completion = RockState.Indeterminate;
                            meetingRock.CompleteTime = null;
                            s.Update(meetingRock);
                        }
                        hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
                            .updateRockCompletion(meetingRock.Id, state.ToString(), rock.Id);
                    }
                }
                else
                {
                    hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
                        .updateRockCompletion(0, state.ToString(), rock.Id);
                }

                hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
                    .update(new AngularUpdate() { new AngularRock(rock) });

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
                        {
                            rock.CompleteTime = now;
                            rock.ForRock.CompleteTime = now;
                        }
                        rock.Completion = state;
                        rock.ForRock.Completion = state;
                        s.Update(rock);
                        s.Update(rock.ForRock);
                        updated = true;
                    }
                    else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate)
                    {
                        rock.Completion = RockState.Indeterminate;
                        rock.CompleteTime = null;
                        rock.ForRock.Completion = RockState.Indeterminate;
                        rock.ForRock.CompleteTime = null;
                        s.Update(rock);
                        s.Update(rock.ForRock);
                        updated = true;
                    }

                    if (updated)
                    {
                        Audit.L10Log(s, caller, recurrenceId, "UpdateRockCompletion", ForModel.Create(rock), "\"" + rock.ForRock.Rock + "\" set to \"" + state + "\"");
                        tx.Commit();
                        s.Flush();
                        var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                        hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId).updateRockCompletion(meetingRockId, state.ToString());

                        //hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId).update(new AngularUpdate() { new AngularRock(rock) });

                        UpdateRock(rock.ForRock.Id, state, connectionId, s, perm, rock.ForRock, hub, now);

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
                            if (m.Id > 0)
                            {
                                var mm = new L10Meeting.L10Meeting_Measurable()
                                {
                                    L10Meeting = meeting,
                                    Measurable = m.Measurable,
                                    _Ordering = m._Ordering,
                                    IsDivider = m.IsDivider
                                };
                                s.Save(mm);
                                meeting._MeetingMeasurables.Add(mm);
                            }
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
                            //if (previousRock.ContainsKey(r.ForRock.Id))
                            state = r.ForRock.Completion;//previousRock[r.ForRock.Id];

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
                        Audit.L10Log(s, caller, recurrenceId, "StartMeeting", ForModel.Create(meeting));
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
            var unsent = new List<Mail>();
            L10Meeting meeting = null;
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var now = DateTime.UtcNow;
                    //Make sure we're unstarted
                    var perms = PermissionsUtility.Create(s, caller);
                    meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
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

                            foreach (var personTodos in todoList.GroupBy(x => x.AccountableUser.GetEmail()))
                            {
                                var user = personTodos.First().AccountableUser;
                                var email = user.GetEmail();

                                var table = await TodoAccessor.BuildTodoTable(personTodos.ToList());
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

                                var mail = Mail.To(EmailTypes.L10Summary, email)
                                    .Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
                                    .Body(EmailStrings.MeetingSummary_Body, user.GetName(), table.ToString(), Config.ProductName(meeting.Organization));
                                unsent.Add(mail);
                            }

                        }
                        catch (Exception e)
                        {
                            log.Error("Emailer issue(1):" + recurrence.Id, e);
                        }
                    }

                    Audit.L10Log(s, caller, recurrenceId, "ConcludeMeeting", ForModel.Create(meeting));
                    tx.Commit();
                    s.Flush();
                }
            }
            try
            {
                if (sendEmail && unsent != null)
                {
                    await Emailer.SendEmails(unsent);
                }
            }
            catch (Exception e)
            {
                log.Error("Emailer issue(2):" + recurrenceId, e);
            }

            if (meeting != null)
            {
                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting)).concludeMeeting();
            }
        }



        public static L10Meeting.L10Meeting_Connection JoinL10Meeting(UserOrganizationModel caller, long recurrenceId, string connectionId)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (recurrenceId == -3)
                    {
                        var recurs = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == caller.Id)
                            .Select(x => x.L10Recurrence.Id)
                            .List<long>().ToList();
                        foreach (var r in recurs)
                        {
                            hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(r));
                        }
                    }
                    else
                    {
                        new PermissionsAccessor().Permitted(caller, x => x.ViewL10Recurrence(recurrenceId));
                        hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(recurrenceId));
                        Audit.L10Log(s, caller, recurrenceId, "JoinL10Meeting", ForModel.Create(caller));
                        var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
                        meetingHub.userEnterMeeting(caller.Id, connectionId, caller.GetName(), caller.ImageUrl(true));
                    }
                }
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
        public static string CreateNote(UserOrganizationModel caller, long recurrenceId, string name)
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
                    group.createNote(note.Id, name, note.PadId);
                    var rec = new AngularRecurrence(recurrenceId)
                    {
                        Notes = new List<AngularMeetingNotes>(){
							new AngularMeetingNotes(note)
						}
                    };
                    group.update(rec);

                    Audit.L10Log(s, caller, recurrenceId, "CreateNote", ForModel.Create(note), name);
                    tx.Commit();
                    s.Flush();
                    return note.PadId;
                }
            }
        }

        public static void EditNote(UserOrganizationModel caller, long noteId, string contents = null, string name = null, string connectionId = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var note = s.Get<L10Note>(noteId);
                    PermissionsUtility.Create(s, caller).EditL10Recurrence(note.Recurrence.Id);
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

                    Audit.L10Log(s, caller, note.Recurrence.Id, "EditNote", ForModel.Create(note), note.Name + ":\n" + note.Contents);

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

        public static List<L10Recurrence.L10Recurrence_Rocks> GetRocksForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId)
        {

            perms.ViewL10Recurrence(recurrenceId);

            var found = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
                .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                .Fetch(x => x.ForRock).Eager
                .List().ToList();
            foreach (var f in found)
            {
                var a = f.ForRock.AccountableUser.GetName();
                var b = f.ForRock.AccountableUser.ImageUrl(true, ImageSize._32);
            }
            return found;
        }

        public static List<L10Meeting.L10Meeting_Rock> GetRocksForMeeting(ISession s, PermissionsUtility perms, long recurrenceId, long meetingId)
        {

            perms.ViewL10Recurrence(recurrenceId).ViewL10Meeting(meetingId);

            var found = s.QueryOver<L10Meeting.L10Meeting_Rock>()
                .Where(x => x.DeleteTime == null && x.ForRecurrence.Id == recurrenceId && x.L10Meeting.Id == meetingId)
                .Fetch(x => x.ForRock).Eager
                .List().ToList();
            foreach (var f in found)
            {
                if (f.ForRock.AccountableUser == null)
                    f.ForRock.AccountableUser = s.Load<UserOrganizationModel>(f.ForRock.ForUserId);
                var a = f.ForRock.AccountableUser.NotNull(x => x.GetName());
                var b = f.ForRock.AccountableUser.NotNull(x => x.ImageUrl(true, ImageSize._32));
            }
            return found;
        }


        public static List<L10Meeting.L10Meeting_Rock> GetRocksForMeeting(UserOrganizationModel caller, long recurrenceId, long meetingId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetRocksForMeeting(s, perms, recurrenceId, meetingId);
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

        public static long GuessUserId(IssueModel issueModel, long deflt = 0)
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
                    //forUsers.Distinct().ForEach(x => p.ManagesUserOrganizationOrSelf(x));

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

                    if (message != null && todo.Message != message)
                    {
                        SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoMessage(todo.Id));
                        todo.Message = message;
                        group.updateTodoMessage(todoId, message);
                        updatesText.Add("Message: " + todo.Message);
                    }
                    if (details != null && todo.Details != details)
                    {
                        SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoDetails(todo.Id));
                        todo.Details = details;
                        group.updateTodoDetails(todoId, details);
                        updatesText.Add("Details: " + details);
                    }
                    if (dueDate != null && todo.DueDate != dueDate.Value)
                    {
                        todo.DueDate = dueDate.Value;
                        group.updateTodoDueDate(todoId, dueDate.Value.ToJavascriptMilliseconds());
                        updatesText.Add("Due-Date: " + dueDate.Value.ToShortDateString());
                    }
                    if (accountableUser != null && todo.AccountableUserId != accountableUser.Value)
                    {
                        todo.AccountableUserId = accountableUser.Value;
                        todo.AccountableUser = s.Get<UserOrganizationModel>(accountableUser.Value);
                        group.updateTodoAccountableUser(todoId, accountableUser.Value, todo.AccountableUser.GetName(), todo.AccountableUser.ImageUrl(true, ImageSize._32));
                        updatesText.Add("Accountable: " + todo.AccountableUser.GetName());
                    }

                    if (complete != null)
                    {
                        SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoCompletion(todo.Id));
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

                    Audit.L10Log(s, caller, todo.ForRecurrenceId.Value, "UpdateTodo", ForModel.Create(todo), updatedText);

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

        public static void UpdateIssue(UserOrganizationModel caller, long issueRecurrenceId, DateTime updateTime, string message = null, string details = null, bool? complete = null, string connectionId = null, long? owner = null, int? priority = null)
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
                    if (message != null && message != issue.Issue.Message)
                    {
                        SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateIssueMessage(issue.Issue.Id));
                        issue.Issue.Message = message;
                        group.updateIssueMessage(issueRecurrenceId, message);
                        updatesText.Add("Message: " + issue.Issue.Message);
                    }
                    if (details != null && details != issue.Issue.Description)
                    {
                        SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateIssueDetails(issue.Issue.Id));
                        issue.Issue.Description = details;
                        group.updateIssueDetails(issueRecurrenceId, details);
                        updatesText.Add("Description: " + issue.Issue.Description);
                    }
                    if (owner != null && (issue.Owner == null || owner != issue.Owner.Id))
                    {
                        var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner).Take(1).List().ToList();
                        if (!any.Any())
                            throw new PermissionsException("Specified Owner cannot see meeting");

                        issue.Owner = s.Get<UserOrganizationModel>(owner);
                        group.updateIssueOwner(issueRecurrenceId, owner, issue.Owner.GetName(), issue.Owner.ImageUrl(true, ImageSize._32));
                        updatesText.Add("Owner: " + issue.Owner.GetName());
                    }
                    if (priority != null && priority != issue.Priority && issue.LastUpdate_Priority < updateTime)
                    {
                        issue.LastUpdate_Priority = updateTime;
                        var old = issue.Priority;
                        issue.Priority = priority.Value;
                        group.updateIssuePriority(issueRecurrenceId, issue.Priority);
                        updatesText.Add("Priority from " + old + " to " + issue.Priority);
                        s.Update(issue);
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

                    Audit.L10Log(s, caller, recurrenceId, "UpdateIssue", ForModel.Create(issue), updatedText);

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

        public static void UpdateScore(UserOrganizationModel caller, long scoreId, decimal? measured, string connectionId = null, bool noSyncException = false)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var score = s.Get<ScoreModel>(scoreId);
                    if (score == null)
                        throw new PermissionsException("Score does not exist.");

                    SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateScore(scoreId), noSyncException);


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
                        Audit.L10Log(s, caller, r, "UpdateScore", ForModel.Create(score), "\"" + score.Measurable.Title + "\" updated to \"" + measured + "\"");
                    }

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static ScoreModel _UpdateScore(ISession s, PermissionsUtility perms, long measurableId, long weekNumber, decimal? measured, string connectionId, bool noSyncException = false)
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
                SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(score.Id), noSyncException);
                //Found it with false id
                score.Measured = measured;
                score.DateEntered = (measured == null) ? null : nowQ;
                s.Update(score);

            }
            else
            {
                var ordered = existingScores.OrderBy(x => x.DateDue);
                var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now.AddDays(-7 * 13);
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
                var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId);
                var update = new AngularRecurrence(recurrenceId);
                update.Scorecard = new AngularScorecard();
                //score.Measured = score.Measured ?? Removed.Decimal();
                var angularScore = new AngularScore(score);
                angularScore.Measured = angularScore.Measured ?? Removed.Decimal();
                angularScore.ForWeek = TimingUtility.GetWeekSinceEpoch(angularScore.Week);
                update.Scorecard.Scores = new List<AngularScore>() { angularScore };
                group.update(update);

                Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateScore", ForModel.Create(score), "\"" + score.NotNull(x => x.Measurable.NotNull(y => y.Title)) + "\" updated to \"" + measured + "\"");
            }
            return score;
        }

        public static void UpdateScore(UserOrganizationModel caller, long measurableId, long weekNumber, decimal? measured, string connectionId, bool noSyncException = false)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    _UpdateScore(s, perms, measurableId, weekNumber, measured, connectionId, noSyncException);
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

                    var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                        .Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Id)
                        .Select(x => x.Id)
                        .List<long>().ToList();

                    if (name != null && measurable.Title != name)
                    {
                        measurable.Title = name;
                        //group.updateArchiveMeasurable(measurableId, "title", name);
                        updateText.Add("Title: " + measurable.Title);
                        foreach (var mmid in meetingMeasurableIds)
                            group.updateMeasurable(mmid, "title", name);
                    }
                    if (direction != null && measurable.GoalDirection != direction.Value)
                    {
                        measurable.GoalDirection = direction.Value;
                        updateText.Add("Goal Direction: " + measurable.GoalDirection.ToSymbol());

                        foreach (var mmid in meetingMeasurableIds)
                            group.updateMeasurable(mmid, "direction", direction.Value.ToSymbol(), direction.Value.ToString());
                        //group.updateArchiveMeasurable(measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());

                    }
                    if (target != null && measurable.Goal != target.Value)
                    {
                        measurable.Goal = target.Value;
                        updateText.Add("Goal: " + measurable.Goal);
                        foreach (var mmid in meetingMeasurableIds)
                            group.updateMeasurable(mmid, "target", target.Value.ToString("0.#####"));
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

                        foreach (var mmid in meetingMeasurableIds)
                            group.updateMeasurable(mmid, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
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

                        foreach (var mmid in meetingMeasurableIds)
                            group.updateMeasurable(mmid, "admin", user.NotNull(x => x.GetName()), adminId.Value);
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

                    var updatedText = "Updated Measurable: \"" + measurable.Title + "\" \n " + String.Join("\n", updateText);
                    Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateArchiveMeasurable", ForModel.Create(measurable), updatedText);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public static void UpdateMeasurable(UserOrganizationModel caller, long meeting_measurableId,
            string name = null, LessGreater? direction = null, decimal? target = null,
            long? accountableId = null, long? adminId = null, UnitType? unitType = null)
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
                        var oldUser = s.Get<UserOrganizationModel>(measurable.Measurable.AccountableUserId);
                        if (user == null)
                            throw new PermissionsException("Cannot Update User");
                        user.UpdateCache(s);
                        if (oldUser != null)
                            oldUser.UpdateCache(s);

                        measurable.Measurable.AccountableUserId = accountableId.Value;
                        group.updateMeasurable(meeting_measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
                        updateText.Add("Accountable: " + user.NotNull(x => x.GetName()));
                        s.Update(measurable.Measurable);
                    }
                    if (adminId != null && measurable.Measurable.AdminUserId != adminId.Value)
                    {
                        perms.ViewUserOrganization(adminId.Value, false);
                        var user = s.Get<UserOrganizationModel>(adminId.Value);
                        var oldUser = s.Get<UserOrganizationModel>(measurable.Measurable.AdminUserId);
                        if (user == null)
                            throw new PermissionsException("Cannot Update User");
                        user.UpdateCache(s);
                        if (oldUser != null)
                            oldUser.UpdateCache(s);
                        measurable.Measurable.AdminUserId = adminId.Value;
                        group.updateMeasurable(meeting_measurableId, "admin", user.NotNull(x => x.GetName()), adminId.Value);
                        updateText.Add("Admin: " + user.NotNull(x => x.GetName()));
                        s.Update(measurable.Measurable);
                    }

                    var updatedText = "Updated Measurable: \"" + measurable.Measurable.Title + "\" \n " + String.Join("\n", updateText);
                    Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateMeasurable", ForModel.Create(measurable), updatedText);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static void DeleteMeetingMeasurableDivider(UserOrganizationModel caller, long l10Meeting_measurableId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var divider = s.Get<L10Meeting.L10Meeting_Measurable>(l10Meeting_measurableId);
                    if (divider == null)
                        throw new PermissionsException("Divider does not exist");

                    var recurrenceId = divider.L10Meeting.L10RecurrenceId;
                    var perm = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
                    if (!divider.IsDivider)
                        throw new PermissionsException("Not a divider");
                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

                    var matchingMeasurable = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.IsDivider && x._Ordering == divider._Ordering)
                        .List().FirstOrDefault();

                    var now = DateTime.UtcNow;
                    divider.DeleteTime = now;

                    if (matchingMeasurable != null)
                    {
                        matchingMeasurable.DeleteTime = now;
                        s.Update(matchingMeasurable);
                    }
                    else
                    {
                        int a = 0;
                    }

                    s.Update(divider);
                    tx.Commit();
                    s.Flush();
                    group.removeMeasurable(l10Meeting_measurableId);
                }
            }
        }

        public static void CreateMeasurableDivider(UserOrganizationModel caller, long recurrenceId, int ordering = -1)
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

                    var divider = new L10Recurrence.L10Recurrence_Measurable()
                    {
                        _Ordering = ordering,
                        IsDivider = true,
                        L10Recurrence = recur,
                        Measurable = null,
                    };

                    s.Save(divider);


                    var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
                    //var l10Scores = L10Accessor.GetScoresForRecurrence(s, perm, recurrenceId);
                    if (current != null)
                    {


                        var mm = new L10Meeting.L10Meeting_Measurable()
                        {
                            L10Meeting = current,
                            Measurable = null,
                            IsDivider = true,

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

                        var settings = current.Organization.Settings;
                        var sow = settings.WeekStart;
                        var offset = current.Organization.GetTimezoneOffset();
                        var scorecardType = settings.ScorecardPeriod;

                        var weeks = TimingUtility.GetPeriods(sow, offset, now, current.StartTime, /*l10Scores, */false, scorecardType, new YearStart(current.Organization));

                        var rowId=s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
                       // var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();

                        var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM
                        {
                            MeetingId = current.Id,
                            RecurrenceId = recurrenceId,
                            MeetingMeasurable = mm,
                            IsDivider = true,
                            Weeks = weeks
                        });
                        row.ViewData["row"] = rowId - 1;

                        var first = row.Execute();
                        row.ViewData["ShowRow"] = false;
                        var second = row.Execute();
                        group.addMeasurable(first, second);
                    }
                    var scorecard = new AngularScorecard();
                    scorecard.Measurables = new List<AngularMeasurable>() { AngularMeasurable.CreateDivider(divider._Ordering, divider.Id) };
                    scorecard.Scores = new List<AngularScore>();

                    group.update(new AngularUpdate() { scorecard });

                    Audit.L10Log(s, caller, recurrenceId, "CreateMeasurableDivider", ForModel.Create(divider));


                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public static void AddMeasurable(ISession s, PermissionsUtility perm, long recurrenceId, L10Controller.AddMeasurableVm model)
        {
            perm.EditL10Recurrence(recurrenceId);

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
            if (current != null)
            {

                //var l10Scores = L10Accessor.GetScoresForRecurrence(s, perm, recurrenceId,includeAutoGenerated:false);

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

                var settings = current.Organization.Settings;
                var sow = settings.WeekStart;
                var offset = current.Organization.GetTimezoneOffset();
                var scorecardType = settings.ScorecardPeriod;

                var weeks = TimingUtility.GetPeriods(sow, offset, now, current.StartTime, /*l10Scores,*/ false, scorecardType, new YearStart(current.Organization));

                //var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();
                var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
                var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM
                {
                    MeetingId = current.Id,
                    RecurrenceId = recurrenceId,
                    MeetingMeasurable = mm,
                    Scores = scores,
                    Weeks = weeks
                });
                row.ViewData["row"] = rowId - 1;

                var first = row.Execute();
                row.ViewData["ShowRow"] = false;
                var second = row.Execute();
                group.addMeasurable(first, second);
            }
            var scorecard = new AngularScorecard();
            scorecard.Measurables = new List<AngularMeasurable>() { new AngularMeasurable(measurable) };
            scorecard.Scores = new List<AngularScore>();
            foreach (var ss in scores.Where(x => x.Measurable.Id == measurable.Id))
            {
                scorecard.Scores.Add(new AngularScore(ss));
            }

            group.update(new AngularUpdate() { scorecard });

            Audit.L10Log(s, perm.GetCaller(), recurrenceId, "CreateMeasurable", ForModel.Create(measurable), measurable.Title);
        }

        public static void CreateMeasurable(UserOrganizationModel caller, long recurrenceId, L10Controller.AddMeasurableVm model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perm = PermissionsUtility.Create(s, caller);
                    AddMeasurable(s, perm, recurrenceId, model);
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
                    PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
                    var r = s.Get<L10Recurrence>(recurrenceId);
                    r.DeleteTime = DateTime.UtcNow;

                    s.Update(r);

                    Audit.L10Log(s, caller, recurrenceId, "DeleteL10", ForModel.Create(r), r.Name);
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
                    var measurables = recurrence._DefaultMeasurables.Select(x =>
                    {
                        if (x.IsDivider)
                        {
                            var m = AngularMeasurable.CreateDivider(x._Ordering, x.Id);
                            m.RecurrenceId = x.L10Recurrence.Id;
                            return m;
                        }
                        else
                        {
                            var m = new AngularMeasurable(x.Measurable);
                            m.Ordering = x._Ordering;
                            m.RecurrenceId = x.L10Recurrence.Id;
                            return m;
                        }
                    }).ToList();

                    if (recurrence.IncludeAggregateTodoCompletion)
                    {
                        measurables.Add(new AngularMeasurable(TodoMeasurable)
                        {
                            Ordering = -2
                        });
                    }

                    recur.Scorecard = new AngularScorecard(
                        -1,
                        caller.Organization.Settings.WeekStart,
                        caller.Organization.GetTimezoneOffset(),
                        measurables,
                        scores,
                        DateTime.UtcNow,
                        caller.Organization.Settings.ScorecardPeriod,
                        new YearStart(caller.Organization)
                    );
                    recur.Rocks = recurrence._DefaultRocks.Select(x => new AngularRock(x.ForRock)).ToList();
                    recur.Todos = GetAllTodosForRecurrence(s, perms, recurrenceId).Select(x => new AngularTodo(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();
                    recur.Issues = GetAllIssuesForRecurrence(s, perms, recurrenceId).Select(x => new AngularIssue(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();

                    recur.Notes = recurrence._MeetingNotes.Select(x => new AngularMeetingNotes(x)).ToList();

                    recur.date = new AngularDateRange()
                    {
                        startDate = DateTime.UtcNow.Date.AddDays(-9),
                        endDate = DateTime.UtcNow.Date.AddDays(1),
                    };

                    recur.HeadlinesUrl = Config.NotesUrl() + "p/" + recurrence.HeadlinesId + "?showControls=true&showChat=false";

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
                UpdateIssue(caller, m.Id, DateTime.UtcNow, m.Name ?? "", m.Details ?? "", m.Complete, connectionId, priority: m.Priority);
            }
            else if (model.Type == typeof(AngularTodo).Name)
            {
                var m = (AngularTodo)model;
                UpdateTodo(caller, m.Id, m.Name ?? "", null, m.DueDate, m.Owner.NotNull(x => x.Id), m.Complete, connectionId);
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
            else if (model.Type == typeof(AngularRock).Name)
            {
                var m = (AngularRock)model;
                UpdateRock(caller, m.Id, m.Name, m.Completion, connectionId);
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
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
                    var meeting = s.QueryOver<L10Meeting>().Where(x => x.L10RecurrenceId == recurrenceId && x.DeleteTime == null).OrderBy(x => x.Id).Desc.Take(1).List().ToList();
                    var m = meeting.SingleOrDefault();
                    return m.NotNull(x => x.Id);
                }
            }
        }

        public static void SetMeetingMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Meeting_Measurables)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

                    SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

                    var l10measurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>().WhereRestrictionOn(x => x.Id).IsIn(orderedL10Meeting_Measurables).Where(x => x.DeleteTime == null).List().ToList();

                    if (!l10measurables.Any())
                        throw new PermissionsException("None found.");
                    if (l10measurables.GroupBy(x => x.L10Meeting.Id).Count() > 1)
                        throw new PermissionsException("Measurables must be part of the same meeting");
                    if (l10measurables.First().L10Meeting.L10RecurrenceId != recurrenceId)
                        throw new PermissionsException("Not part of the specified L10");
                    var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();

                    for (var i = 0; i < orderedL10Meeting_Measurables.Count; i++)
                    {
                        var id = orderedL10Meeting_Measurables[i];
                        var f = l10measurables.FirstOrDefault(x => x.Id == id);
                        if (f != null)
                        {
                            f._Ordering = i;
                            s.Update(f);
                            var g = recurMeasurables.FirstOrDefault(x => (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) || ((x.Measurable == null && f.Measurable == null) && !x._WasModified));
                            if (g != null)
                            {
                                g._WasModified = true;
                                g._Ordering = i;
                                s.Update(g);
                            }
                        }
                    }

                    Audit.L10Log(s, caller, recurrenceId, "SetMeasurableOrdering", null);

                    tx.Commit();
                    s.Flush();

                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

                    group.reorderMeasurables(orderedL10Meeting_Measurables);

                    var updates = new AngularUpdate();
                    foreach (var x in recurMeasurables)
                    {
                        if (x.IsDivider)
                        {
                            updates.Add(AngularMeasurable.CreateDivider(x._Ordering, x.Id));
                        }
                        else
                        {
                            updates.Add(new AngularMeasurable(x.Measurable) { Ordering = x._Ordering });
                        }
                    }
                    group.update(updates);


                }
            }
        }

        public static void SetRecurrenceMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Recurrene_Measurables)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);


                    SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

                    /*var l10measurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                        .WhereRestrictionOn(x => x.Measurable.Id).IsIn(orderedL10Recurrene_Measurables)
                        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                        .List().ToList();*/
                    MeasurableModel mm = null;

                    var l10RecurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().JoinAlias(p => p.Measurable, () => mm)
                        .WhereRestrictionOn(() => mm.Id)
                        .IsIn(orderedL10Recurrene_Measurables.Where(x => x >= 0).ToArray())
                        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                        .List<L10Recurrence.L10Recurrence_Measurable>();

                    var dividers = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                        .WhereRestrictionOn(x => x.Id)
                        .IsIn(orderedL10Recurrene_Measurables.Where(x => x < 0).Select(x => -x).ToArray())
                        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                        .List<L10Recurrence.L10Recurrence_Measurable>();



                    if (!l10RecurMeasurables.Any())
                        throw new PermissionsException("None found.");
                    if (l10RecurMeasurables.GroupBy(x => x.L10Recurrence.Id).Count() > 1)
                        throw new PermissionsException("Measurables must be part of the same meeting");
                    if (l10RecurMeasurables.First().L10Recurrence.Id != recurrenceId)
                        throw new PermissionsException("Not part of the specified L10");
                    var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();

                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

                    var meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
                    if (meeting != null)
                    {
                        var l10MeetingMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                            .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
                            .List().ToList();/*.JoinAlias(p => p.Measurable, () => mm)
							.WhereRestrictionOn(() => mm.Id)
							.IsIn(orderedL10Recurrene_Measurables)
							.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
							.List<L10Meeting.L10Meeting_Measurable>();*/




                        var orderedL10Meeting_Measurables = new List<long>();
                        for (var i = 0; i < orderedL10Recurrene_Measurables.Count; i++)
                        {
                            var id = orderedL10Recurrene_Measurables[i];
                            var f = l10MeetingMeasurables.FirstOrDefault(x => (x.Measurable != null && x.Measurable.Id == id) || (x.Measurable == null && !x._WasModified));
                            if (f != null)
                            {
                                f._WasModified = true;
                                f._Ordering = i;
                                s.Update(f);
                                /*var g = l10MeetingMeasurables.FirstOrDefault(x => 
                                    (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) 
                                    || ((x.Measurable == null && f.Measurable == null) && !x._WasModified));
                                if (g != null)
                                {
                                    g._WasModified = true;
                                    g._Ordering = i;
                                    s.Update(g);
                                }*/
                                orderedL10Meeting_Measurables.Add(f.Id);

                            }
                        }

                        group.reorderMeasurables(orderedL10Meeting_Measurables);
                    }

                    for (var i = 0; i < orderedL10Recurrene_Measurables.Count; i++)
                    {
                        var id = orderedL10Recurrene_Measurables[i];
                        var f = l10RecurMeasurables.FirstOrDefault(x => x.Measurable.Id == id) ?? dividers.FirstOrDefault(x => x.Id == -id);
                        if (f != null)
                        {
                            f._Ordering = i;
                            s.Update(f);
                            /*var g = recurMeasurables.FirstOrDefault(x => (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) || (x.Measurable == null && f.Measurable == null && x.Id==f.Id));
                            if (g != null)
                            {
                                g._Ordering = i;
                                s.Update(g);
                            }*/
                        }
                        else
                        {
                            int a = 0;
                        }
                    }

                    Audit.L10Log(s, caller, recurrenceId, "SetMeasurableOrdering", null);

                    tx.Commit();
                    s.Flush();



                    group.reorderRecurrenceMeasurables(orderedL10Recurrene_Measurables);

                    var updates = new AngularUpdate();
                    foreach (var x in recurMeasurables)
                    {
                        if (x.IsDivider)
                        {
                            updates.Add(AngularMeasurable.CreateDivider(x._Ordering, x.Id));
                        }
                        else
                        {
                            updates.Add(new AngularMeasurable(x.Measurable) { Ordering = x._Ordering });
                        }
                    }
                    group.update(updates);


                }
            }
        }

        public static List<L10AuditModel> GetL10Audit(UserOrganizationModel caller, long recurrenceId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
                    var audits = s.QueryOver<L10AuditModel>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
                        .Fetch(x => x.UserOrganization).Eager
                        .TransformUsing(Transformers.DistinctRootEntity)
                        .List().ToList();
                    return audits;
                }
            }
        }



        public static L10MeetingStatsVM GetStats(UserOrganizationModel caller, long recurrenceId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
                    var recurrence = s.Get<L10Recurrence>(recurrenceId);
                    var o = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).List().ToList();
                    var meeting = o.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).FirstOrDefault();
                    var prevMeeting = o.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).Take(2).LastOrDefault();


                    int issuesSolved = 0;
                    int todoComplete = 0;
                    List<TodoModel> todosCreated;

                    var rating = double.NaN;

                    if (meeting == null || meeting.CompleteTime == null)
                    {
                        var createTime = meeting.NotNull(x => x.CreateTime);
                        issuesSolved = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.Recurrence.Id == recurrenceId && x.CloseTime > createTime).List().Count;
                        todosCreated = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CreateTime > createTime).List().ToList();
                        if (prevMeeting != null && prevMeeting.CompleteTime != null)
                            todoComplete = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CompleteTime > prevMeeting.CompleteTime).List().Count;
                    }
                    else
                    {
                        issuesSolved = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.Recurrence.Id == recurrenceId && x.CloseTime > meeting.CreateTime && x.CloseTime < meeting.CompleteTime).List().Count;
                        todosCreated = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CreateTime > meeting.CreateTime && x.CreateTime < meeting.CompleteTime).List().ToList();
                        if (prevMeeting != null && prevMeeting.CompleteTime != null)
                            todoComplete = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CompleteTime > prevMeeting.CompleteTime && x.CompleteTime < meeting.CompleteTime).List().Count;
                        var ratings = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.L10Meeting.Id == meeting.Id && x.DeleteTime == null).List().Where(x => x.Rating != null).Select(x => x.Rating.Value).ToList();
                        if (ratings.Any())
                        {
                            rating = (double)ratings.Average();
                        }

                    }

                    foreach (var todo in todosCreated)
                    {
                        todo.AccountableUser.NotNull(x => x.GetName());
                        todo.AccountableUser.NotNull(x => x.ImageUrl(true));
                    }


                    var stats = new L10MeetingStatsVM()
                    {
                        IssuesSolved = issuesSolved,
                        TodosCreated = todosCreated,
                        AllMeetings = o,
                        StartTime = meeting.NotNull(x => x.StartTime),
                        EndTime = meeting.NotNull(x => x.CompleteTime),
                        TodoCompleted = todoComplete,
                        AverageRating = rating
                    };

                    if (stats.StartTime != null)
                        stats.StartTime = caller.Organization.ConvertFromUTC(stats.StartTime.Value);
                    if (stats.EndTime != null)
                        stats.EndTime = caller.Organization.ConvertFromUTC(stats.EndTime.Value);

                    return stats;
                }
            }
        }

        public static void AddRock(ISession s, PermissionsUtility perm, long recurrenceId, L10Controller.AddRockVm model)
        {
            perm.EditL10Recurrence(recurrenceId);
            var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);

            var recur = s.Get<L10Recurrence>(recurrenceId);


            var now = DateTime.UtcNow;
            RockModel rock;

            var wasCreated = false;
            if (model.SelectedRock == -3)
            {
                //Create new
                if (model.Rocks == null)
                    throw new PermissionsException("You must include a rock to create.");

                rock = model.Rocks.SingleOrDefault();
                if (rock == null)
                    throw new PermissionsException("You must include a rock to create.");

                perm.ViewUserOrganization(rock.ForUserId, false);

                rock.OrganizationId = recur.OrganizationId;
                rock.CreateTime = now;
                rock.AccountableUser = s.Load<UserOrganizationModel>(rock.ForUserId);
                rock.Category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

                s.Save(rock);
                rock.AccountableUser.UpdateCache(s);
                wasCreated = true;
            }
            else
            {
                //Find Existing
                rock = s.Get<RockModel>(model.SelectedRock);
                if (rock == null)
                    throw new PermissionsException("Rock does not exist.");
                perm.ViewRock(rock);
            }

            var rm = new L10Recurrence.L10Recurrence_Rocks()
            {
                CreateTime = now,
                L10Recurrence = recur,
                ForRock = rock,
            };
            s.Save(rm);

            var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
            var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
            if (current != null)
            {
                var mm = new L10Meeting.L10Meeting_Rock()
                {
                    ForRecurrence = recur,
                    L10Meeting = current,
                    ForRock = rock,
                };
                s.Save(mm);

                var rocks = L10Accessor.GetRocksForMeeting(s, perm, recurrenceId, current.Id);

                var row = ViewUtility.RenderPartial("~/Views/L10/partial/RockGroup.cshtml", rocks);

                var first = row.Execute();
                group.updateRocks(first);
                var arecur = new AngularRecurrence(recurrenceId)
                {
                    Rocks = rocks.Select(x => new AngularRock(x.ForRock)).ToList(),
                };
                group.update(new AngularUpdate() { arecur });
            }
            else
            {

                var recurRocks = L10Accessor.GetRocksForRecurrence(s, perm, recurrenceId);
                var arecur = new AngularRecurrence(recurrenceId)
                {
                    Rocks = recurRocks.Select(x => new AngularRock(x.ForRock)).ToList(),
                };
                group.update(new AngularUpdate() { arecur });
            }
            Audit.L10Log(s, perm.GetCaller(), recurrenceId, "CreateRock", ForModel.Create(rm), rock.Rock);
        }

        public static void CreateRock(UserOrganizationModel caller, long recurrenceId, L10Controller.AddRockVm model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perm = PermissionsUtility.Create(s, caller);

                    AddRock(s, perm, recurrenceId, model);

                    tx.Commit();
                    s.Flush();
                }
            }
        }
    }
}



