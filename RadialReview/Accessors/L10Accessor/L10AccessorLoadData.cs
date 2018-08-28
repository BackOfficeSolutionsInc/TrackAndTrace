using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using NHibernate;
using NHibernate.SqlCommand;
using RadialReview.Models.UserModels;
namespace RadialReview.Accessors {
    public partial class L10Accessor : BaseAccessor {

        #region Load Data
        public static void _LoadMeetingLogs(ISession s, params L10Meeting[] meetings) {
            var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();
            if (meetingIds.Any()) {
                var allLogs = s.QueryOver<L10Meeting.L10Meeting_Log>()
                    .Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
                    .List().ToList();
                var now = DateTime.UtcNow;
                foreach (var m in meetings.Where(x => x != null)) {
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

                    if (curPage != null) {
                        m._MeetingLeaderCurrentPage = curPage.Page;
                        //m._MeetingLeaderCurrentPageType = GetPageType_Unsafe(s, curPage.Page);
                        m._MeetingLeaderCurrentPageStartTime = curPage.StartTime;
                        m._MeetingLeaderCurrentPageBaseMinutes = m._MeetingLeaderPageDurations.Where(x => x.Item1 == curPage.Page).Sum(x => x.Item2);
                    }
                }
            }
        }

        public static void _LoadMeetings(ISession s, bool loadUsers, bool loadMeasurables, bool loadRocks, params L10Meeting[] meetings) {
            var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();

            if (meetingIds.Any()) {
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
                foreach (var m in meetings) {
                    if (m.L10Recurrence.IncludeAggregateTodoCompletion) {
                        allMeasurables.Add(new L10Meeting.L10Meeting_Measurable() {
                            _Ordering = -2,
                            Id = -1,
                            L10Meeting = m,
                            Measurable = TodoMeasurable
                        });
                    }
                }
                foreach (var m in meetings.Where(x => x != null)) {
                    m._MeetingAttendees = allAttend.Where(x => m.Id == x.L10Meeting.Id).ToList();
                    m._MeetingMeasurables = allMeasurables.Where(x => m.Id == x.L10Meeting.Id).ToList();
                    m._MeetingRocks = allRocks.Where(x => m.Id == x.L10Meeting.Id).ToList();
                    if (m.L10Recurrence.IncludeIndividualTodos) {
                        foreach (var u in m._MeetingAttendees) {
                            m._MeetingMeasurables.Add(new L10Meeting.L10Meeting_Measurable() {
                                _Ordering = -1,
                                Id = -1,
                                L10Meeting = m,
                                Measurable = GenerateTodoMeasureable(u.User)
                            });
                        }
                    }
                    if (loadUsers) {
                        foreach (var u in m._MeetingAttendees) {
                            try {
                                u.User.GetName();
                                u.User.ImageUrl();
                            } catch (Exception) {
                            }
                        }
                    }
                    if (loadMeasurables) {
                        foreach (var u in m._MeetingMeasurables) {
                            try {
                                if (u.Measurable.AccountableUser != null) {
                                    u.Measurable.AccountableUser.GetName();
                                    u.Measurable.AccountableUser.ImageUrl();
                                }
                                if (u.Measurable.AdminUser != null) {
                                    u.Measurable.AdminUser.GetName();
                                    u.Measurable.AdminUser.ImageUrl();
                                }
                            } catch (Exception) {
                            }
                        }
                    }
                    if (loadRocks) {
                        foreach (var u in m._MeetingRocks) {
                            try {
                                u.ForRock.AccountableUser.GetName();
                                u.ForRock.AccountableUser.ImageUrl();
                            } catch (Exception) {
                            }
                        }
                    }
                }
            }
        }
        public static void _LoadRecurrences(ISession s, LoadMeeting load, params L10Recurrence[] all) {
            var recurrenceIds = all.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();

            if (recurrenceIds.Any()) {
                IEnumerable<UserOrganizationModel> allAttendUsersQ = null;
                IEnumerable<L10Recurrence.L10Recurrence_Attendee> allAttendQ = null;
                IEnumerable<L10Recurrence.L10Recurrence_Measurable> allMeasurablesQ = null;
                IEnumerable<L10Recurrence.L10Recurrence_Rocks> allRocksQ = null;
                IEnumerable<L10Note> allNotesQ = null;
                IEnumerable<L10Recurrence.L10Recurrence_Page> allPagesQ = null;
                IEnumerable<L10Recurrence.L10Recurrence_VideoConferenceProvider> allVCPQ = null;

                UserOrganizationModel userAlias = null;
                UserLookup userLookupAlias = null;
                RockModel rockAlias = null;

                //single pass attach users
                if (load.LoadUsers) {
                    var allAttendSubQ = QueryOver.Of<L10Recurrence.L10Recurrence_Attendee>()
                        .JoinAlias(x => x.User, () => userAlias)
                        .Where(x => x.DeleteTime == null && userAlias.DeleteTime == null)
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
                        .Select(Projections.Property<L10Recurrence.L10Recurrence_Attendee>(x => x.User.Id));
                    allAttendUsersQ = s.QueryOver<UserOrganizationModel>()
                        .WithSubquery.WhereProperty(x => x.Id).In(allAttendSubQ)
                        .Future();

                    allAttendQ = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                        .JoinAlias(x => x.User, () => userAlias)
                        .Where(x => x.DeleteTime == null && userAlias.DeleteTime == null)
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
                        .Future<L10Recurrence.L10Recurrence_Attendee>();
                }

                if (load.LoadMeasurables) {
                    MeasurableModel mAlias = null;
                    allMeasurablesQ = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                        .JoinAlias(x => x.Measurable, () => mAlias, JoinType.LeftOuterJoin)
                        .Where(x => x.DeleteTime == null && (x.Measurable == null || mAlias.DeleteTime == null))
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
                        .Future<L10Recurrence.L10Recurrence_Measurable>();
                }

                if (load.LoadRocks) {
                    allRocksQ = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
                        .JoinAlias(x => x.ForRock, () => rockAlias)
                        .Where(x => x.DeleteTime == null && rockAlias.DeleteTime == null)
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
                        .Future<L10Recurrence.L10Recurrence_Rocks>();
                }

                if (load.LoadNotes) {
                    allNotesQ = s.QueryOver<L10Note>()
                        .Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.Recurrence.Id).IsIn(recurrenceIds)
                        .Future<L10Note>();
                }

                if (load.LoadPages) {
                    allPagesQ = s.QueryOver<L10Recurrence.L10Recurrence_Page>()
                        .Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
                        .Future<L10Recurrence.L10Recurrence_Page>();
                }
                if (load.LoadVideos) {
                    allVCPQ = s.QueryOver<L10Recurrence.L10Recurrence_VideoConferenceProvider>()
                       .Where(x => x.DeleteTime == null)
                       .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
                       .Fetch(x => x.Provider).Eager
                       .Future<L10Recurrence.L10Recurrence_VideoConferenceProvider>();
                }


                List<L10Recurrence.L10Recurrence_Attendee> allAttend = null;
                List<UserOrganizationModel> allUsers = null;
                List<L10Recurrence.L10Recurrence_Measurable> allMeasurables = null;
                List<L10Recurrence.L10Recurrence_Rocks> allRocks = null;
                List<L10Note> allNotes = null;
                List<L10Recurrence.L10Recurrence_VideoConferenceProvider> allVCP = null;
                List<L10Recurrence.L10Recurrence_Page> allPages = null;
                if (load.LoadUsers)
                    allAttend = allAttendQ.ToList();
                if (load.LoadUsers)
                    allUsers = allAttendUsersQ.ToList();
                if (load.LoadMeasurables)
                    allMeasurables = allMeasurablesQ.ToList();
                if (load.LoadRocks)
                    allRocks = allRocksQ.ToList();
                if (load.LoadNotes)
                    allNotes = allNotesQ.ToList();
                if (load.LoadVideos)
                    allVCP = allVCPQ.ToList();
                if (load.LoadPages)
                    allPages = allPagesQ.ToList();


                foreach (var a in all.Where(x => x != null).ToList()) {
                    List<L10Recurrence.L10Recurrence_Measurable> measurables=new List<L10Recurrence.L10Recurrence_Measurable>();
                    if (load.LoadUsers) {
                        a._DefaultAttendees = allAttend.Where(x => a.Id == x.L10Recurrence.Id/* && x.User.DeleteTime == null;*/).ToList();
                    }
                    if (load.LoadMeasurables) {
                        measurables = allMeasurables.Where(x => a.Id == x.L10Recurrence.Id && ((x.Measurable != null && x.Measurable.DeleteTime == null) || (x.Measurable == null && x.IsDivider))).ToList();
                    }
                    if (load.LoadRocks) {
                        a._DefaultRocks = allRocks.Where(x => a.Id == x.L10Recurrence.Id /*&& x.ForRock.DeleteTime == null*/).ToList();
                    }
                    if (load.LoadNotes) {
                        a._MeetingNotes = allNotes.Where(x => a.Id == x.Recurrence.Id && x.DeleteTime == null).ToList();
                    }
                    if (load.LoadVideos) {
                        a._VideoConferenceProviders = allVCP.Where(x => a.Id == x.L10Recurrence.Id && x.DeleteTime == null).ToList();
                    }
                    if (load.LoadPages) {
                        a._Pages = allPages.Where(x => a.Id == x.L10Recurrence.Id && x.DeleteTime == null).OrderBy(x => x._Ordering).ToList();
                    }


                    var cache = new L10Recurrence.L10LookupCache(a.Id);
                    cache.SetAllMeasurablesAndDividers(allMeasurables);

                    a._CacheQueries = cache;

                    if (a.IncludeIndividualTodos && load.LoadMeasurables) {
                        foreach (var u in a._DefaultAttendees) {
                            measurables.Add(new L10Recurrence.L10Recurrence_Measurable() {
                                _Ordering = -1,
                                Id = -1,
                                L10Recurrence = a,
                                Measurable = GenerateTodoMeasureable(u.User)
                            });
                        }
                    }
                    if (load.LoadMeasurables) {
                        a._DefaultMeasurables = measurables;
                    }
                    if (load.LoadUsers) {
                        foreach (var u in a._DefaultAttendees) {
                            u.User.GetName();
                            u.User.ImageUrl(true);
                        }
                    }
                    if (load.LoadMeasurables) {
                        foreach (var u in a._DefaultMeasurables.Where(x => x.Measurable != null)) {
                            if (u.Measurable.AccountableUser != null) {
                                u.Measurable.AccountableUser.GetName();
                                u.Measurable.AccountableUser.ImageUrl(true);
                            }
                            if (u.Measurable.AdminUser != null) {
                                u.Measurable.AdminUser.GetName();
                                u.Measurable.AdminUser.ImageUrl(true);
                            }
                        }
                    }
                    if (load.LoadRocks) {
                        foreach (var u in a._DefaultRocks) {
                            var b = u.ForRock.Rock;
                            var c = u.ForRock.Period.NotNull(x => x.Name);
                        }
                    }
                    if (load.LoadVideos) {//Load video
                        foreach (var v in a._VideoConferenceProviders) {
                            var aa = v.Provider.GetVideoConferenceType();
                            var b = v.Provider.GetType();
                            var c = v.Provider.GetUrl();
                            var d = v.Provider.FriendlyName;
                        }
                    }
                }
            }
        }

        private static List<IssueModel.IssueModel_Recurrence> _PopulateChildrenIssues(List<IssueModel.IssueModel_Recurrence> list) {
            var output = list.Where(x => x.ParentRecurrenceIssue == null).ToList();
            foreach (var o in output) {
                _RecurseChildrenIssues(o, list);
            }
            foreach (var o in output) {
                try {
                    if (o.Owner != null) {
                        o.Owner.GetName();
                        o.Owner.GetImageUrl();
                    }
                } catch (Exception) {
                }
            }
            output = output.OrderBy(x => x.Ordering).ToList();
            return output;

        }
        private static void _RecurseChildrenIssues(IssueModel.IssueModel_Recurrence issue, IEnumerable<IssueModel.IssueModel_Recurrence> list) {
            if (issue._ChildIssues != null)
                return;
            issue._ChildIssues = list.Where(x => x.ParentRecurrenceIssue != null && x.ParentRecurrenceIssue.Id == issue.Id).ToList();
            foreach (var i in issue._ChildIssues) {
                _RecurseChildrenIssues(i, list);
            }
        }
        #endregion

    }
}