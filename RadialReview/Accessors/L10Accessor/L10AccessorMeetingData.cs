using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using Amazon.EC2.Model;

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
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Enums;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Base;
//using System.Web.WebPages.Html;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Periods;
using RadialReview.Models.Interfaces;
using System.Dynamic;
using Newtonsoft.Json;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.VideoConference;
using System.Linq.Expressions;
using NHibernate.SqlCommand;
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Rocks;
using System.Web.Mvc;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using static RadialReview.Utilities.EventUtil;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using RadialReview.Accessors;
using RadialReview.Models.UserModels;

namespace RadialReview.Accessors {

    public class LoadMeeting {
        public bool LoadUsers { get; set; }
        public bool LoadMeasurables { get; set; }
        public bool LoadRocks { get; set; }
        public bool LoadVideos { get; set; }
        public bool LoadNotes { get; internal set; }
        public bool LoadPages { get; internal set; }

        public bool AnyTrue() {
            return LoadUsers || LoadMeasurables || LoadRocks || LoadVideos || LoadNotes || LoadPages;
        }

        public static LoadMeeting True() {
            return new LoadMeeting() {
                LoadMeasurables = true,
                LoadVideos = true,
                LoadRocks = true,
                LoadUsers = true,
                LoadPages = true,
                LoadNotes = true
            };
        }

        public static LoadMeeting False() {
            return new LoadMeeting() {
                LoadMeasurables = false,
                LoadVideos = false,
                LoadRocks = false,
                LoadUsers = false,
                LoadNotes = false,
                LoadPages =false,
            };
        }
    }
    public partial class L10Accessor : BaseAccessor {

        #region Get Meeting Data
           

        public static L10Recurrence GetL10Recurrence(UserOrganizationModel caller, long recurrenceId, LoadMeeting load) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetL10Recurrence(s, perms, recurrenceId, load);
                }
            }
        }
        public static L10Recurrence GetL10Recurrence(ISession s, PermissionsUtility perms, long recurrenceId, LoadMeeting load) {
            perms.ViewL10Recurrence(recurrenceId);
            var found = s.Get<L10Recurrence>(recurrenceId);
            if (load.AnyTrue())
                _LoadRecurrences(s, load, found);
            return found;
        }
        public static L10Meeting GetPreviousMeeting(ISession s, PermissionsUtility perms, long recurrenceId) {
            perms.ViewL10Recurrence(recurrenceId);
            var previousMeeting = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == recurrenceId && x.CompleteTime != null).OrderBy(x => x.CompleteTime).Desc.Take(1).SingleOrDefault();
            return previousMeeting;
        }

        public static DateTime GetLastMeetingEndTime(UserOrganizationModel caller, long recurrenceId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var last = GetPreviousMeeting(s, perms, recurrenceId);
                    if (last == null || !last.CompleteTime.HasValue)
                        return DateTime.MinValue;
                    return last.CompleteTime.Value;
                }
            }
        }

        public static List<NameId> GetVisibleL10Meetings_Tiny(UserOrganizationModel caller, long userId, bool onlyPersonallyAttending = false, bool onlyDashboardRecurrences = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetVisibleL10Meetings_Tiny(s, perms, userId, onlyPersonallyAttending, onlyDashboardRecurrences);
                }
            }
        }
        public static List<NameId> GetVisibleL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId, bool onlyPersonallyAttending = false, bool onlyDashboardRecurrences = false) {
            List<long> personallyAttending;
            List<long> dashRecurs;
            var meetings = GetVisibleL10Meetings_Tiny(s, perms, userId, out personallyAttending, out dashRecurs);
            if (onlyPersonallyAttending) {
                meetings = meetings.Where(x => personallyAttending.Contains(x.Id)).ToList();
            }
            if (onlyDashboardRecurrences) {
                meetings = meetings.Where(x => dashRecurs.Contains(x.Id)).ToList();
            }
            return meetings;
        }
        public static List<NameId> GetVisibleL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId, out List<long> recurrencesPersonallyAttending, out List<long> recurrencesVisibleOnDashboard) {

            //IMPORTANT. Make sure the pristine flag is being set correctly on L10Recurrence.

            var caller = perms.GetCaller();
            perms.ViewUsersL10Meetings(userId);

            //Who should we get this data for? Just Self, or also subordiantes?
            var accessibleUserIds = new[] { userId };
            var user = s.Get<UserOrganizationModel>(userId);
            if (user.Organization.Settings.ManagersCanViewSubordinateL10)
                accessibleUserIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, userId).ToArray(); //DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, userId).ToArray();

            L10Recurrence alias = null;
            //var allRecurrences = new List<L10Recurrence>();
            var allRecurrenceIds = new List<NameId>();
            IEnumerable<object[]> orgRecurrences = null;
            if (caller.ManagingOrganization) {
                orgRecurrences = s.QueryOver<L10Recurrence>().Where(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null && !x.Pristine)
                    .Select(x => x.Name, x => x.Id)
                    .Future<object[]>();
            }

            var attendee_ReccurenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                .Where(x => x.DeleteTime == null)
                .WhereRestrictionOn(x => x.User.Id).IsIn(accessibleUserIds)
                .Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
                .Where(x => alias.DeleteTime == null)
                .Select(x => alias.Name, x => alias.Id, x => x.User.Id)
                .Future<object[]>();

            //Actually load the Recurrences

            var admin_MeasurableIds = s.QueryOver<MeasurableModel>().Where(x => x.AdminUserId == userId && x.DeleteTime == null).Select(x => x.Id).List<long>().ToList();
            var admin_RecurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null)
                .WhereRestrictionOn(x => x.Measurable.Id).IsIn(admin_MeasurableIds)
                .Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
                .Where(x => alias.DeleteTime == null)
                .Select(x => alias.Name, x => alias.Id)
                .List<object[]>().Select(x => new NameId((string)x[0], (long)x[1])).ToList();





            //From future
            var attendee_recurrences = attendee_ReccurenceIds.ToList().Select(x => new NameId((string)x[0], (long)x[1])).ToList();
            recurrencesPersonallyAttending = attendee_ReccurenceIds.Where(x => (long)x[2] == userId).Select(x => (long)x[1]).ToList();
            recurrencesPersonallyAttending = recurrencesPersonallyAttending.Distinct().ToList();
            recurrencesVisibleOnDashboard = recurrencesPersonallyAttending.ToList();



            allRecurrenceIds.AddRange(attendee_recurrences);
            allRecurrenceIds.AddRange(admin_RecurrenceIds);


            var allViewPerms = PermissionsAccessor.GetExplicitPermItemsForUser(s, perms, userId, PermItem.ResourceType.L10Recurrence).Where(x => x.CanView);
            var allViewPermsRecurrences = allRecurrenceIds.Where(allRecurrenceId => allViewPerms.Any(y => allRecurrenceId.Id == y.ResId)).ToList();
            recurrencesVisibleOnDashboard.AddRange(allViewPermsRecurrences.Select(x => x.Id));

            //Outside the company
            var additionalRecurrenceIdsFromPerms = allViewPerms.Where(allViewPermId => !allRecurrenceIds.Any(y => y.Id == allViewPermId.ResId)).ToList();
            var additionalRecurrenceFromViewPerms = s.QueryOver<L10Recurrence>()
                .Where(x => !x.Pristine && x.DeleteTime == null)
                .WhereRestrictionOn(x => x.Id).IsIn(additionalRecurrenceIdsFromPerms.Select(x => x.ResId).ToArray())
                .Select(x => x.Name, x => x.Id)
                .List<object[]>().Select(x => new NameId((string)x[0], (long)x[1])).ToList();
            allRecurrenceIds.AddRange(additionalRecurrenceFromViewPerms);
            recurrencesVisibleOnDashboard.AddRange(additionalRecurrenceFromViewPerms.Select(x => x.Id));




            if (orgRecurrences != null) {
                allRecurrenceIds.AddRange(orgRecurrences.ToList().Select(x => new NameId((string)x[0], (long)x[1])));
            }

            allRecurrenceIds = allRecurrenceIds.Distinct(x => x.Id).ToList();
            recurrencesVisibleOnDashboard = recurrencesVisibleOnDashboard.Distinct().ToList();


            if (caller.ManagingOrganization) {
                return allRecurrenceIds;
            }

            var available = new List<NameId>();
            foreach (var r in allRecurrenceIds) {

                try {
                    perms.CanView(PermItem.ResourceType.L10Recurrence, r.Id);
                    available.Add(r);
                } catch {
                }
            }
            return available;
        }



        public static List<L10VM> GetVisibleL10Recurrences(UserOrganizationModel caller, long userId, bool loadUsers) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    List<long> attendee_recurrences;
                    List<long> _nil;
                    var uniqueL10NameIds = GetVisibleL10Meetings_Tiny(s, perms, userId, out attendee_recurrences, out _nil);
                    var uniqueL10Ids = uniqueL10NameIds.Select(x => x.Id).ToList();


                    var allRecurrencesQ = s.QueryOver<L10Recurrence>()
                        .Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids)
                        .Select(x => x.Id, x => x.Name, x => x.MeetingInProgress)
                        .Future<object[]>().Select(x => new TinyRecurrence {
                            Id = (long)x[0],
                            Name = (string)x[1],
                            MeetingInProgress = (long?)x[2]
                        });


                    UserOrganizationModel userAlias = null;
                    var allAttendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                        .JoinAlias(x => x.User, () => userAlias)
                        .Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(uniqueL10Ids)
                        //.Select(x => x.L10Recurrence.Id, x => x.User.Id)
                        //.List<object[]>()
                        //.Select(x=>new { L10RecurrenceId = (long)x[0]},) 
                        .List()
                        .ToList();
                    //
                    //.List<object[]>().ToList();

                    var allRecurrences = allRecurrencesQ.ToList();

                    foreach (var a in allRecurrences) {
                        a._DefaultAttendees = allAttendees.Where(x => x.L10Recurrence.Id == a.Id)
                                                          .Select(x => TinyUser.FromUserOrganization(x.User))
                                                          .ToList();
                        a.IsAttendee = attendee_recurrences.Any(y => y == a.Id);
                    }
                    //.List().ToList();
                    //allRecurrences.AddRange(loadedL10);


                    //Load extra data
                    //var allRecurrencesDistinct = allRecurrences.Distinct(x => x.Id).ToList();
                    //_LoadRecurrences(s, loadUsers, false, false, false, allRecurrences.ToArray());

                    //Make a lookup for self attendance
                    //var attending = attendee_recurrences.Where(x => userId == x.User.Id).Select(x => x.L10Recurrence.Id).ToArray();
                    return allRecurrences.Select(x => new L10VM(x)).ToList();
                }
            }
        }
        public static string GetCurrentL10MeetingLeaderPage(UserOrganizationModel caller, long meetingId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var leaderId = s.Get<L10Meeting>(meetingId).MeetingLeader.Id;
                    var leaderpage = s.QueryOver<L10Meeting.L10Meeting_Log>()
                        .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == leaderId && x.EndTime == null)
                        .List().OrderByDescending(x => x.StartTime)
                        .FirstOrDefault();
                    return leaderpage.NotNull(x => x.Page);
                }
            }
        }

        public static L10Meeting GetCurrentL10Meeting(UserOrganizationModel caller, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return _GetCurrentL10Meeting(s, perms, recurrenceId, nullOnUnstarted, load, loadLogs);
                }
            }
        }
        public static List<L10Meeting> GetL10Meetings(UserOrganizationModel caller, long recurrenceId, bool load = false, bool excludePreviewMeeting = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

                    var o = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId);


                    if (excludePreviewMeeting) {
                        o = o.Where(x => x.Preview == false);
                    }

                    var oResolved = o.List().ToList();

                    if (load) {
                        _LoadMeetings(s, true, true, true, oResolved.ToArray());
                    }

                    return oResolved;
                }
            }
        }

        //Finds all first degree connectioned L10Recurrences
        public static List<L10Recurrence> GetAllConnectedL10Recurrence(UserOrganizationModel caller, long recurrenceId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return _GetAllConnectedL10Recurrence(s, caller, recurrenceId);
                }
            }
        }
        public static List<L10Recurrence> GetAllL10RecurrenceAtOrganization(UserOrganizationModel caller, long organizationId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return _GetAllL10RecurrenceAtOrganization(s, caller, organizationId);
                }
            }
        }
        public static L10Recurrence GetCurrentL10RecurrenceFromMeeting(UserOrganizationModel caller, long l10MeetingId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller).ViewL10Meeting(l10MeetingId);
                    var recurrence = s.Get<L10Meeting>(l10MeetingId).L10RecurrenceId;

                    return GetL10Recurrence(s, perms, recurrence, LoadMeeting.True());
                }
            }
        }
        public static long GetLatestMeetingId(UserOrganizationModel caller, long recurrenceId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
                    var meeting = s.QueryOver<L10Meeting>().Where(x => x.L10RecurrenceId == recurrenceId && x.DeleteTime == null).OrderBy(x => x.Id).Desc.Take(1).List().ToList();
                    var m = meeting.SingleOrDefault();
                    return m.NotNull(x => x.Id);
                }
            }
        }

        #endregion
    }

}
