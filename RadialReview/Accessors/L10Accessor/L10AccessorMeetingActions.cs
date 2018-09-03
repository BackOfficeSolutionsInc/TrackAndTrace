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
using Hangfire;
using RadialReview.Hangfire;
using RadialReview.Crosscutting.Schedulers;

namespace RadialReview.Accessors {
    public partial class L10Accessor : BaseAccessor {

        #region Meeting Actions

        private static IEnumerable<L10Recurrence.L10Recurrence_Page> GenerateMeetingPages(long recurrenceId, MeetingType meetingType, DateTime createTime) {

            if (meetingType == MeetingType.L10) {
                #region L10 Pages
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "Segue",
                    Subheading = "Share good news from the last 7 days.<br/> One personal and one professional.",
                    PageType = L10Recurrence.L10PageType.Segue,
                    _Ordering = 0,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "Scorecard",
                    Subheading = "",
                    PageType = L10Recurrence.L10PageType.Scorecard,
                    _Ordering = 1,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "Rock Review",
                    Subheading = "",
                    PageType = L10Recurrence.L10PageType.Rocks,
                    _Ordering = 2,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "People Headlines",
                    Subheading = "Share headlines about customers/clients and people in the company.<br/> Good and bad. Drop down (to the issues list) anything that needs discussion.",
                    PageType = L10Recurrence.L10PageType.Headlines,
                    _Ordering = 3,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "To-do List",
                    Subheading = "",
                    PageType = L10Recurrence.L10PageType.Todo,
                    _Ordering = 4,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 60,
                    Title = "IDS",
                    Subheading = "",
                    PageType = L10Recurrence.L10PageType.IDS,
                    _Ordering = 5,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "Conclude",
                    Subheading = "",
                    PageType = L10Recurrence.L10PageType.Conclude,
                    _Ordering = 6,
                    AutoGen = true
                };
                #endregion
            } else if (meetingType == MeetingType.SamePage) {
                #region Same Page Meeting pages
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "Check In",
                    Subheading = "How are you doing? State of mind?</br> Business and personal stuff?",
                    PageType = L10Recurrence.L10PageType.Empty,
                    _Ordering = 0,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "Build Issues List",
                    Subheading = "List all of your issues, concerns, ideas and disconnects.",
                    PageType = L10Recurrence.L10PageType.Empty,
                    _Ordering = 1,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 50,
                    Title = "IDS",
                    Subheading = "IDS all of your issues.",
                    PageType = L10Recurrence.L10PageType.IDS,
                    _Ordering = 2,
                    AutoGen = true
                };
                yield return new L10Recurrence.L10Recurrence_Page() {
                    CreateTime = createTime,
                    L10RecurrenceId = recurrenceId,
                    Minutes = 5,
                    Title = "Conclude",
                    Subheading = "",
                    PageType = L10Recurrence.L10PageType.Conclude,
                    _Ordering = 3,
                    AutoGen = true
                };
                #endregion
            }
        }

        public static async Task<L10Recurrence> CreateBlankRecurrence(UserOrganizationModel caller, long orgId, bool addCreator, MeetingType meetingType = MeetingType.L10) {
            L10Recurrence recur;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    recur = await CreateBlankRecurrence(s, perms, orgId, addCreator, meetingType);
                    tx.Commit();
                    s.Flush();
                }
            }
            return recur;
        }

        public static async Task<L10Recurrence> CreateBlankRecurrence(ISession s, PermissionsUtility perms, long orgId, bool addCreator, MeetingType meetingType = MeetingType.L10) {
            L10Recurrence recur;
            var caller = perms.GetCaller();
            perms.CreateL10Recurrence(orgId);
            recur = new L10Recurrence() {
                OrganizationId = orgId,
                Pristine = true,
                VideoId = Guid.NewGuid().ToString(),
                EnableTranscription = false,
                HeadlinesId = Guid.NewGuid().ToString(),
                CountDown = true,
                CreatedById = caller.Id,
                CreateTime = DateTime.UtcNow
            };

            if (meetingType == MeetingType.SamePage) {
                recur.TeamType = L10TeamType.SamePageMeeting;
            }

            s.Save(recur);

            foreach (var page in GenerateMeetingPages(recur.Id, meetingType, recur.CreateTime)) {
                s.Save(page);
            }


            var vto = VtoAccessor.CreateRecurrenceVTO(s, perms, recur.Id);
            s.Save(new PermItem() {
                CanAdmin = true,
                CanEdit = true,
                CanView = true,
                AccessorType = PermItem.AccessType.Creator,
                AccessorId = caller.Id,
                ResType = PermItem.ResourceType.L10Recurrence,
                ResId = recur.Id,
                CreatorId = caller.Id,
                OrganizationId = caller.Organization.Id,
                IsArchtype = false,
            });
            s.Save(new PermItem() {
                CanAdmin = true,
                CanEdit = true,
                CanView = true,
                AccessorType = PermItem.AccessType.Members,
                AccessorId = -1,
                ResType = PermItem.ResourceType.L10Recurrence,
                ResId = recur.Id,
                CreatorId = caller.Id,
                OrganizationId = caller.Organization.Id,
                IsArchtype = false,
            });
            s.Save(new PermItem() {
                CanAdmin = true,
                CanEdit = true,
                CanView = true,
                AccessorId = -1,
                AccessorType = PermItem.AccessType.Admins,
                ResType = PermItem.ResourceType.L10Recurrence,
                ResId = recur.Id,
                CreatorId = caller.Id,
                OrganizationId = caller.Organization.Id,
                IsArchtype = false,
            });

            await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.CreateRecurrence(ses, recur));

            if (addCreator) {
                using (var rt = RealTimeUtility.Create()) {
                    await AddAttendee(s, perms, rt, recur.Id, caller.Id);
                }
            }

            return recur;
        }

        public static async Task Depristine_Unsafe(ISession s, UserOrganizationModel caller, L10Recurrence recur) {
            if (recur.Pristine == true) {
                recur.Pristine = false;
                s.Update(recur);
                await Trigger(x => x.Create(s, EventType.CreateMeeting, caller, recur, message: recur.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));
            }
        }

        public static async Task<MvcHtmlString> GetMeetingSummary(UserOrganizationModel caller, long meetingId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewL10Meeting(meetingId);

                    var meeting = s.Get<L10Meeting>(meetingId);
                    var completeTime = meeting.CompleteTime;

                    var completedIssues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                                            .Where(x => x.DeleteTime == null && x.CloseTime == completeTime && x.Recurrence.Id == meeting.L10RecurrenceId)
                                            .List().ToList();

                    var pads = completedIssues.Select(x => x.Issue.PadId).ToList();
                    var padTexts = await PadAccessor.GetHtmls(pads);

                    return new MvcHtmlString((await IssuesAccessor.BuildIssuesSolvedTable(completedIssues, showDetails: true, padLookup: padTexts)).ToString());
                }
            }
        }

        public static string GetDefaultStartPage(L10Recurrence recurrence) {

            var page = recurrence._Pages.FirstOrDefault();
            if (page != null) {
                return "page-" + page.Id;
            } else {
                return "nopage";
            }
            ////UNREACHABLE...
            /*var p = "segue";
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
			return p;*/
        }

        public static async Task<L10Meeting> StartMeeting(UserOrganizationModel caller, UserOrganizationModel meetingLeader, long recurrenceId, List<long> attendees, bool preview) {
            L10Recurrence recurrence;
            L10Meeting meeting;

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
                    if (caller.Id != meetingLeader.Id)
                        PermissionsUtility.Create(s, meetingLeader).ViewL10Recurrence(recurrenceId);
                    lock ("Recurrence_" + recurrenceId) {
                        //Make sure we're unstarted
                        try {
                            var perms = PermissionsUtility.Create(s, caller);
                            _GetCurrentL10Meeting(s, perms, recurrenceId, false);
                            throw new MeetingException(recurrenceId, "Meeting has already started.", MeetingExceptionType.AlreadyStarted);
                        } catch (MeetingException e) {
                            if (e.MeetingExceptionType != MeetingExceptionType.Unstarted)
                                throw;
                        }
                        var now = DateTime.UtcNow;
                        recurrence = s.Get<L10Recurrence>(recurrenceId);
                        meeting = new L10Meeting {
                            CreateTime = now,
                            StartTime = now,
                            L10RecurrenceId = recurrenceId,
                            L10Recurrence = recurrence,
                            OrganizationId = recurrence.OrganizationId,
                            MeetingLeader = meetingLeader,
                            MeetingLeaderId = meetingLeader.Id,
                            Preview = preview,
                        };
                        s.Save(meeting);
                        tx.Commit();
                        s.Flush();
                    }
                }
            }
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    recurrence.MeetingInProgress = meeting.Id;
                    s.Update(recurrence);

                    _LoadRecurrences(s, LoadMeeting.True(), recurrence);

                    foreach (var m in recurrence._DefaultMeasurables) {
                        if (m.Id > 0) {
                            var mm = new L10Meeting.L10Meeting_Measurable() {
                                L10Meeting = meeting,
                                Measurable = m.Measurable,
                                _Ordering = m._Ordering,
                                IsDivider = m.IsDivider
                            };
                            s.Save(mm);
                            meeting._MeetingMeasurables.Add(mm);
                        }
                    }
                    foreach (var m in attendees) {
                        var mm = new L10Meeting.L10Meeting_Attendee() {
                            L10Meeting = meeting,
                            User = s.Load<UserOrganizationModel>(m),
                        };
                        s.Save(mm);
                        meeting._MeetingAttendees.Add(mm);
                    }

                    foreach (var r in recurrence._DefaultRocks) {
                        var state = RockState.Indeterminate;
                        state = r.ForRock.Completion;
                        var mm = new L10Meeting.L10Meeting_Rock() {
                            ForRecurrence = recurrence,
                            L10Meeting = meeting,
                            ForRock = r.ForRock,
                            Completion = state,
                            VtoRock = r.VtoRock,
                        };
                        s.Save(mm);
                        meeting._MeetingRocks.Add(mm);
                    }
                    var perms2 = PermissionsUtility.Create(s, caller);
                    var todos = GetTodosForRecurrence(s, perms2, recurrence.Id, meeting.Id);
                    var i = 0;
                    foreach (var t in todos.OrderBy(x => x.AccountableUser.NotNull(y => y.GetName()) ?? ("" + x.AccountableUserId)).ThenBy(x => x.Message)) {
                        t.Ordering = i;
                        s.Update(t);
                        i += 1;
                    }
                    Audit.L10Log(s, caller, recurrenceId, "StartMeeting", ForModel.Create(meeting));
                    tx.Commit();
                    s.Flush();
                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting)).setupMeeting(meeting.CreateTime.ToJavascriptMilliseconds(), meetingLeader.Id);

                }
            }

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.StartMeeting(ses, recurrence, meeting));
                    if (recurrence.TeamType == L10TeamType.LeadershipTeam)
                        await Trigger(x => x.Create(s, EventType.StartLeadershipMeeting, caller, recurrence, message: recurrence.Name));
                    if (recurrence.TeamType == L10TeamType.DepartmentalTeam)
                        await Trigger(x => x.Create(s, EventType.StartDepartmentMeeting, caller, recurrence, message: recurrence.Name));

                    tx.Commit();
                    s.Flush();
                }
            }

            return meeting;
        }

        public async static Task ConcludeMeeting(UserOrganizationModel caller, long recurrenceId, List<System.Tuple<long, decimal?>> ratingValues, ConcludeSendEmail sendEmail, bool closeTodos, bool closeHeadlines, string connectionId) {
            L10Recurrence recurrence = null;
            L10Meeting meeting = null;

            try {
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        var now = DateTime.UtcNow;
                        //Make sure we're unstarted
                        var perms = PermissionsUtility.Create(s, caller);
                        meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
                        perms.ViewL10Meeting(meeting.Id);

                        var todoRatio = new Ratio();
                        var todos = GetTodosForRecurrence(s, perms, recurrenceId, meeting.Id);

                        foreach (var todo in todos) {
                            if (todo.CreateTime < meeting.StartTime) {
                                if (todo.CompleteTime != null) {
                                    todo.CompleteDuringMeetingId = meeting.Id;
                                    if (closeTodos) {
                                        todo.CloseTime = now;
                                    }
                                    s.Update(todo);
                                }
                                todoRatio.Add(todo.CompleteTime != null ? 1 : 0, 1);
                            }
                        }

                        var headlines = GetHeadlinesForMeeting(s, perms, recurrenceId);
                        if (closeHeadlines) {
                            CloseHeadlines_Unsafe(meeting.Id, s, now, headlines);
                        }


                        //Conclude the forum
                        recurrence = s.Get<L10Recurrence>(recurrenceId);
                        await SendConclusionTextMessages_Unsafe(recurrenceId, recurrence, s, now);

                        CloseIssuesOnConclusion_Unsafe(recurrenceId, meeting, s, now);

                        meeting.TodoCompletion = todoRatio;
                        meeting.CompleteTime = now;
                        meeting.SendConcludeEmailTo = sendEmail;
                        s.Update(meeting);

                        var attendees = GetMeetingAttendees_Unsafe(meeting.Id, s);
                        var raters = SetConclusionRatings_Unsafe(ratingValues, meeting, s, attendees);

                        CloseLogsOnConclusion_Unsafe(meeting, s, now);

                        //Close all sub issues
                        IssueModel issueAlias = null;
                        var issue_recurParents = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                            .Where(x => x.DeleteTime == null && x.CloseTime >= meeting.StartTime && x.CloseTime <= meeting.CompleteTime && x.Recurrence.Id == recurrenceId)
                            .List().ToList();
                        _RecursiveCloseIssues(s, issue_recurParents.Select(x => x.Id).ToList(), now);


                        recurrence.MeetingInProgress = null;
                        recurrence.SelectedVideoProvider = null;
                        s.Update(recurrence);

                        var sendEmailTo = new List<L10Meeting.L10Meeting_Attendee>();

                        //send emails
                        if (sendEmail != ConcludeSendEmail.None) {
                            switch (sendEmail) {
                                case ConcludeSendEmail.AllAttendees:
                                    sendEmailTo = attendees;
                                    break;
                                case ConcludeSendEmail.AllRaters:
                                    sendEmailTo = raters.ToList();
                                    break;
                                default:
                                    break;
                            }
                        }

                        ConclusionItems.Save_Unsafe(recurrenceId, meeting.Id, s, todos, headlines, issue_recurParents, sendEmailTo);

                        await Trigger(x => x.Create(s, EventType.ConcludeMeeting, caller, recurrence, message: recurrence.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));

                        Audit.L10Log(s, caller, recurrenceId, "ConcludeMeeting", ForModel.Create(meeting));
                        tx.Commit();
                        s.Flush();
                    }
                }
                if (meeting != null) {
                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting), connectionId).concludeMeeting();
                }

                Scheduler.Enqueue(() => SendConclusionEmail_Unsafe(meeting.Id, null));

                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.ConcludeMeeting(ses, recurrence, meeting));
                        tx.Commit();
                        s.Flush();
                    }
                }
            } catch (Exception e) {
                int a = 0;
            }
        }



        public class ConclusionItems {
            public List<IssueModel.IssueModel_Recurrence> ClosedIssues { get; set; }
            public List<TodoModel> OutstandingTodos { get; set; }
            public List<PeopleHeadline> MeetingHeadlines { get; set; }
            public List<L10Meeting.L10Meeting_Attendee> SendEmailsTo { get; set; }
            public long MeetingId { get; private set; }

            public static void Save_Unsafe(long recurrenceId, long meetingId, ISession s, List<TodoModel> todos, List<PeopleHeadline> headlines, List<IssueModel.IssueModel_Recurrence> issue_recurParents, List<L10Meeting.L10Meeting_Attendee> sendEmailTo) {
                //Emails
                foreach (var emailed in sendEmailTo)
                    s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(emailed), L10Meeting.ConclusionDataType.SendEmailSummaryTo));

                //Closed Issues
                foreach (var issue in issue_recurParents)
                    s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(issue), L10Meeting.ConclusionDataType.CompletedIssue));

                //All todos
                foreach (var todo in todos)
                    s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(todo), L10Meeting.ConclusionDataType.OutstandingTodo));

                //All headlines
                foreach (var headline in headlines)
                    s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(headline), L10Meeting.ConclusionDataType.MeetingHeadline));

            }

            public static ConclusionItems Get_Unsafe(ISession s, long meetingId) {
                var meetingItems = s.QueryOver<L10Meeting.L10Meeting_ConclusionData>().Where(x => x.DeleteTime == null && x.L10MeetingId == meetingId).List().ToList();

                var issueIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.CompletedIssue).Select(x => x.ForModel.ModelId).ToArray();
                var headlineIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.MeetingHeadline).Select(x => x.ForModel.ModelId).ToArray();
                var todoIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.OutstandingTodo).Select(x => x.ForModel.ModelId).ToArray();
                var attendeeIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.SendEmailSummaryTo).Select(x => x.ForModel.ModelId).ToArray();

                var issueQ = s.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issueIds).Future();
                var headlineQ = s.QueryOver<PeopleHeadline>().WhereRestrictionOn(x => x.Id).IsIn(headlineIds).Future();
                var todoQ = s.QueryOver<TodoModel>().WhereRestrictionOn(x => x.Id).IsIn(todoIds).Future();
                var attendeeQ = s.QueryOver<L10Meeting.L10Meeting_Attendee>().WhereRestrictionOn(x => x.Id).IsIn(attendeeIds).Future();

                return new ConclusionItems() {
                    MeetingId = meetingId,
                    ClosedIssues = issueQ.ToList(),
                    MeetingHeadlines = headlineQ.ToList(),
                    OutstandingTodos = todoQ.ToList(),
                    SendEmailsTo = attendeeQ.ToList()
                };
            }
        }


        private static void CloseHeadlines_Unsafe(long meetingId, ISession s, DateTime now, List<PeopleHeadline> headlines) {
            foreach (var headline in headlines) {
                if (headline.CloseTime == null) {
                    headline.CloseDuringMeetingId = meetingId;
                    headline.CloseTime = now;
                }
                s.Update(headline);
            }
        }

        #region Unsafe conclusion methods
        private static void CloseLogsOnConclusion_Unsafe(L10Meeting meeting, ISession s, DateTime now) {
            //End all logs 
            var logs = s.QueryOver<L10Meeting.L10Meeting_Log>()
                .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.EndTime == null)
                .List().ToList();
            foreach (var l in logs) {
                l.EndTime = now;
                s.Update(l);
            }
        }

        private static IEnumerable<L10Meeting.L10Meeting_Attendee> SetConclusionRatings_Unsafe(List<Tuple<long, decimal?>> ratingValues, L10Meeting meeting, ISession s, List<L10Meeting.L10Meeting_Attendee> attendees) {
            var ids = ratingValues.Select(x => x.Item1).ToArray();

            //Set rating for attendees
            var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));
            var raterCount = 0m;
            var raterValue = 0m;

            foreach (var a in raters) {
                a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
                s.Update(a);

                if (a.Rating != null) {
                    raterCount += 1;
                    raterValue += a.Rating.Value;
                }
            }

            meeting.AverageMeetingRating = new Ratio(raterValue, raterCount);
            s.Update(meeting);
            return raters;
        }

        private static List<L10Meeting.L10Meeting_Attendee> GetMeetingAttendees_Unsafe(long meetingId, ISession s) {
            return s.QueryOver<L10Meeting.L10Meeting_Attendee>()
                            .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId)
                            .List().ToList();
        }

        private static void CloseIssuesOnConclusion_Unsafe(long recurrenceId, L10Meeting meeting, ISession s, DateTime now) {
            var issuesToClose = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                                    .Where(x => x.DeleteTime == null && x.MarkedForClose && x.Recurrence.Id == recurrenceId && x.CloseTime == null)
                                    .List().ToList();
            foreach (var i in issuesToClose) {
                i.CloseTime = now;
                s.Update(i);
            }
        }

        private static async Task SendConclusionTextMessages_Unsafe(long recurrenceId, L10Recurrence recurrence, ISession s, DateTime now) {
            var externalForumNumbers = s.QueryOver<ExternalUserPhone>()
                                                        .Where(x => x.DeleteTime > now && x.ForModel.ModelId == recurrenceId && x.ForModel.ModelType == ForModel.GetModelType<L10Recurrence>())
                                                        .List().ToList();
            if (externalForumNumbers.Any()) {
                try {
                    var twilioData = Config.Twilio();
                    TwilioClient.Init(twilioData.Sid, twilioData.AuthToken);

                    var allMessages = new List<Task<MessageResource>>();
                    foreach (var number in externalForumNumbers) {
                        try {
                            if (twilioData.ShouldSendText) {

                                var to = new PhoneNumber(number.UserNumber);
                                var from = new PhoneNumber(number.SystemNumber);

                                var url = Config.BaseUrl(null, "/su?id=" + number.LookupGuid);
                                var message = MessageResource.CreateAsync(to, from: from,
                                    body: "Thanks for participating in the " + recurrence.Name + "!\nWant a demo of Traction Tools? Click here\n" + url
                                );
                                allMessages.Add(message);
                            }
                        } catch (Exception e) {
                            log.Error("Particular Forum text was not sent", e);
                        }

                        number.DeleteTime = now;
                        s.Update(number);
                    }
                    await Task.WhenAll(allMessages);

                } catch (Exception e) {
                    log.Error("Forum texts were not sent", e);
                }
            }
        }

        public static string BuildConcludeStatsTable(int tzOffset, Ratio todoCompletion, Ratio meetingRating, DateTime? start, DateTime? end, int issuesSolved) {
            var table = new StringBuilder();
            try {
                var meetingRatingStr = !meetingRating.IsValid() ? "N/A" : "" + (Math.Round(meetingRating.GetValue(0) * 10) / 10m);

                var startTime = "...";
                var endTime = "...";
                var duration = "unconcluded";

                var ellapse = "";
                var unit = "";

                if (start != null)
                    startTime = TimeData.ConvertFromServerTime(start.Value, tzOffset).ToString("HH:mm");
                if (end != null)
                    endTime = TimeData.ConvertFromServerTime(end.Value, tzOffset).ToString("HH:mm");

                if (end != null && start != null) {
                    var durationMins = (end.Value - start.Value).TotalMinutes;
                    var durationSecs = (end.Value - start.Value).TotalSeconds;

                    if (durationMins < 0) {
                        duration = "";
                        ellapse = "1";
                        unit = "Minute";
                    } else if (durationMins > 1) {
                        duration = (int)durationMins + " minute".Pluralize((int)durationMins);
                        ellapse = "" + (int)Math.Max(1, durationMins);
                        unit = "Minute".Pluralize((int)durationMins);
                    } else {
                        ellapse = "" + (int)Math.Max(1, durationSecs);
                        duration = (int)(durationSecs) + " second".Pluralize((int)durationSecs);
                        unit = "Second".Pluralize((int)durationSecs);
                    }
                }

                table.Append(@"<table width=""100%""><tr><td valign=""middle"" align=""center"">");
                table.Append(@"<table width=""500px""  border=""0"" cellpadding=""0"" cellspacing=""10"" style=""font-family:Areal, Helvetica, sans-serif"">");
                table.Append(@"	<tr>");
                table.Append(@"		<td width=""250px"" height=""100px"" valign=""middle"" align=""center"" style=""background-color:#f8f8f8"">");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(issuesSolved).Append("</td></tr></table>");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">Issues solved</td></tr></table>");
                table.Append(@"		</td>");
                table.Append(@"		<td width=""250px"" height=""100px"" valign=""middle"" align=""center""  style=""background-color:#f8f8f8"">");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(todoCompletion.ToPercentage("N/A")).Append("</td></tr></table>");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">To-do completion</td></tr></table>");
                table.Append(@"		</td>");
                table.Append(@"	</tr>");
                table.Append(@"	<tr>");
                table.Append(@"		<td width=""250px""  height=""100px"" valign=""middle"" align=""center""  style=""background-color:#f8f8f8"">");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(meetingRatingStr).Append("</td></tr></table>");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">Average Rating</td></tr></table>");
                table.Append(@"		</td>");
                table.Append(@"		<td width=""250px""  height=""100px"" valign=""middle"" align=""center""  style=""background-color:#f8f8f8"">");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(ellapse).Append("</td></tr></table>");
                table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">").Append(unit).Append("</td></tr></table>");
                //table.Append(@"		<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(startTime).Append(" - ").Append(endTime).Append("</td></tr></table>");
                //table.Append(@"		<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">").Append(duration).Append("</td></tr></table>");
                table.Append(@"		</td>");
                table.Append(@"	</tr>");
                table.Append(@"</table>");
                table.Append(@"</td></tr></table>");

            } catch (Exception e) {
                log.Error(e);
            }

            return table.ToString();
        }

        [Queue(HangfireQueues.Immediate.CONCLUSION_EMAIL)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public static async Task SendConclusionEmail_Unsafe(long meetingId, long? onlySendToUser) {

            var unsent = new List<Mail>();
            long recurrenceId = 0;

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    try {
                        var meeting = s.Get<L10Meeting>(meetingId);
                        recurrenceId = meeting.L10RecurrenceId;

                        var recurrence = s.Get<L10Recurrence>(recurrenceId);
                        var attendees = GetMeetingAttendees_Unsafe(meetingId, s);

                        var conclusionItems = ConclusionItems.Get_Unsafe(s, meetingId);
                        var headlines = conclusionItems.MeetingHeadlines;
                        var todoList = conclusionItems.OutstandingTodos;//s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId && x.CompleteTime == null).List().ToList();
                        var issuesForTable = conclusionItems.ClosedIssues.Where(x => !x.AwaitingSolve);
                        var sendEmailTo = conclusionItems.SendEmailsTo;


                        if (onlySendToUser != null) {
                            sendEmailTo = sendEmailTo.Where(x => x.UserId == onlySendToUser.Value).ToList();
                        }

                        //All awaitables 
                        //headline.CloseDuringMeetingId = meeting.Id;


                        var pads = issuesForTable.Select(x => x.Issue.PadId).ToList();
                        pads.AddRange(todoList.Select(x => x.PadId));
                        pads.AddRange(headlines.Select(x => x.HeadlinePadId));
                        var padTexts = await PadAccessor.GetHtmls(pads);

                        /////
                        var headlineTable = await HeadlineAccessor.BuildHeadlineTable(headlines.ToList(), "Headlines", recurrenceId, true, padTexts);

                        var issueTable = await IssuesAccessor.BuildIssuesSolvedTable(issuesForTable.ToList(), "Issues Solved", recurrenceId, true, padTexts);
                        var todosTable = new DefaultDictionary<long, string>(x => "");
                        var hasTodos = new DefaultDictionary<long, bool>(x => false);

                        var allUserIds = todoList.Select(x => x.AccountableUserId).ToList();
                        allUserIds.AddRange(attendees.Select(x => x.User.Id));
                        allUserIds = allUserIds.Distinct().ToList();
                        var allUsers = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(allUserIds).List().ToList();

                        var auLu = new DefaultDictionary<long, UserOrganizationModel>(x => null);
                        foreach (var u in allUsers) {
                            auLu[u.Id] = u;
                        }

                        foreach (var personTodos in todoList.GroupBy(x => x.AccountableUserId)) {
                            var user = auLu[personTodos.First().AccountableUserId];
                            //var email = user.GetEmail();

                            if (personTodos.Any())
                                hasTodos[personTodos.First().AccountableUserId] = true;

                            var tzOffset = personTodos.First().AccountableUser.GetTimezoneOffset();
                            var timeFormat = personTodos.First().AccountableUser.GetTimeSettings().DateFormat;

                            var todoTable = await TodoAccessor.BuildTodoTable(personTodos.ToList(), tzOffset, timeFormat, "Outstanding To-dos", true, padLookup: padTexts);


                            var output = new StringBuilder();

                            output.Append(todoTable.ToString());
                            output.Append("<br/>");
                            todosTable[user.Id] = output.ToString();
                        }

                        foreach (var userAttendee in sendEmailTo) {
                            var output = new StringBuilder();
                            var user = auLu[userAttendee.User.Id];
                            var email = user.GetEmail();
                            var toSend = false;

                            var concludeStats = BuildConcludeStatsTable(user.GetTimezoneOffset(), meeting.TodoCompletion, meeting.AverageMeetingRating, meeting.StartTime, meeting.CompleteTime, conclusionItems.ClosedIssues.Count);

                            output.Append(concludeStats);
                            toSend = true;//Always send, we have stats now.

                            output.Append("<br/>");

                            if (hasTodos[userAttendee.User.Id]) {
                                toSend = true;
                            }

                            output.Append(todosTable[user.Id]);
                            if (issuesForTable.Any()) {
                                output.Append(issueTable.ToString());
                                toSend = true;
                            }


                            if (headlines.Any()) {
                                output.Append(headlineTable.ToString());
                                output.Append("<br/>");
                                toSend = true;
                            }


                            var mail = Mail.To(EmailTypes.L10Summary, email)
                                .Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
                                .Body(EmailStrings.MeetingSummary_Body, user.GetName(), output.ToString(), Config.ProductName(recurrence.Organization));
                            if (toSend) {
                                unsent.Add(mail);
                            }
                        }

                    } catch (Exception e) {
                        log.Error("Emailer issue(1):" + recurrenceId, e);
                    }

                    tx.Commit();
                    s.Flush();
                }
            }

            try {
                if (unsent.Any()) {
                    await Emailer.SendEmails(unsent);
                }
            } catch (Exception e) {
                log.Error("Emailer issue(2):" + recurrenceId, e);
            }

        }

        #endregion

        public async static Task UpdateRating(UserOrganizationModel caller, List<System.Tuple<long, decimal?>> ratingValues, long meetingId, string connectionId) {

            L10Meeting meeting = null;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var now = DateTime.UtcNow;
                    //Make sure we're unstarted
                    var perms = PermissionsUtility.Create(s, caller);
                    meeting = s.QueryOver<L10Meeting>().Where(t => t.Id == meetingId).SingleOrDefault();
                    perms.ViewL10Meeting(meeting.Id);


                    var ids = ratingValues.Select(x => x.Item1).ToArray();

                    //Set rating for attendees
                    var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
                        .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
                        .List().ToList();
                    var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));

                    foreach (var a in raters) {
                        a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
                        s.Update(a);
                    }

                    Audit.L10Log(s, caller, meeting.L10RecurrenceId, "UpdateL10Rating", ForModel.Create(meeting));
                    tx.Commit();
                    s.Flush();
                }
            }
        }



        public static IEnumerable<L10Recurrence.L10Recurrence_Connection> GetConnected(UserOrganizationModel caller, long recurrenceId, bool load = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
                    var connections = s.QueryOver<L10Recurrence.L10Recurrence_Connection>().Where(x => x.DeleteTime >= DateTime.UtcNow && x.RecurrenceId == recurrenceId).List().ToList();
                    if (load) {
                        var userIds = connections.Select(x => x.UserId).Distinct().ToArray();
                        var tiny = TinyUserAccessor.GetUsers_Unsafe(s, userIds).ToDefaultDictionary(x => x.UserOrgId, x => x, null);
                        foreach (var c in connections) {
                            c._User = tiny[c.UserId];
                        }
                    }
                    return connections;
                }
            }
        }

        public static L10Meeting.L10Meeting_Connection JoinL10Meeting(UserOrganizationModel caller, long recurrenceId, string connectionId) {
            var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    //var perms = PermissionsUtility.
                    if (recurrenceId == -3) {
                        var recurs = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null)
                            .WhereRestrictionOn(x => x.User.Id).IsIn(caller.UserIds)
                            .Select(x => x.L10Recurrence.Id)
                            .List<long>().ToList();
                        //Hey.. this doesnt grab all visible meetings.. it should be adjusted when we know that GetVisibleL10Meetings_Tiny is optimized
                        //GetVisibleL10Meetings_Tiny(s, perms, caller.Id);
                        foreach (var r in recurs) {
                            hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(r));
                        }
                        hub.Groups.Add(connectionId, MeetingHub.GenerateUserId(caller.Id));
                    } else {
						PermissionsAccessor.Permitted(caller, x => x.ViewL10Recurrence(recurrenceId));
                        hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(recurrenceId));
                        Audit.L10Log(s, caller, recurrenceId, "JoinL10Meeting", ForModel.Create(caller));

                        //s.QueryOver<L10Recurrence.L10Recurrence_Connection>().where
#pragma warning disable CS0618 // Type or member is obsolete
                        var connection = new L10Recurrence.L10Recurrence_Connection() { Id = connectionId, RecurrenceId = recurrenceId, UserId = caller.Id };
#pragma warning restore CS0618 // Type or member is obsolete

                        s.SaveOrUpdate(connection);

                        connection._User = TinyUser.FromUserOrganization(caller);

                        var perms = PermissionsUtility.Create(s, caller);
                        var currentMeeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
                        if (currentMeeting != null) {
                            var isAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.L10Meeting.Id == currentMeeting.Id && x.User.Id == caller.Id && x.DeleteTime == null).RowCount() > 0;
                            if (!isAttendee) {
                                var potentialAttendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == caller.Id && x.L10Recurrence.Id == recurrenceId).RowCount() > 0;
                                if (potentialAttendee) {
                                    s.Save(new L10Meeting.L10Meeting_Attendee() {
                                        L10Meeting = currentMeeting,
                                        User = caller,
                                    });
                                }
                            }
                        }

                        tx.Commit();
                        s.Flush();

                        var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
                        meetingHub.userEnterMeeting(connection);
                        //?meetingHub.userEnterMeeting(caller.Id, connectionId, caller.GetName(), caller.ImageUrl(true));
                    }
                }
            }

            return null;
        }
        #endregion
    }
}