using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Utilities.Synchronize;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Application;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Accessors {
    public class ScorecardAccessor {
        public static AngularScorecard GetAngularScorecardForUser(UserOrganizationModel caller, long userId, DateRange range, bool includeAdmin = true, bool includeNextWeek = true, DateTime? now = null)
        {
            var measurables = ScorecardAccessor.GetUserMeasurables(caller, userId, ordered: true, includeAdmin: true);

            var scorecardStart = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
            var scorecardEnd = range.EndTime.AddDays(6).StartOfWeek(DayOfWeek.Sunday);

            var scores = ScorecardAccessor.GetUserScores(caller, userId, scorecardStart, scorecardEnd, includeAdmin: includeAdmin);
            return new AngularScorecard(-1, caller, measurables.Select(x => new AngularMeasurable(x) { }), scores.ToList(), now, range, includeNextWeek, now);
        }

        public static List<ScoreModel> GetScores(UserOrganizationModel caller, long organizationId, DateTime start, DateTime end, bool loadUsers)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(organizationId);
                    var scores = s.QueryOver<ScoreModel>();
                    if (loadUsers)
                        scores = scores.Fetch(x => x.AccountableUser).Eager;
                    return scores.Where(x => x.OrganizationId == organizationId && x.DateDue >= start && x.DateDue <= end).List().ToList();
                }
            }
        }
        public static List<MeasurableModel> GetVisibleMeasurables(ISession s, PermissionsUtility perms, long organizationId, bool loadUsers)
        {
            var caller = perms.GetCaller();

            var managing = caller.Organization.Id == organizationId && caller.ManagingOrganization;
            IQueryOver<MeasurableModel, MeasurableModel> q;

            List<long> userIds = null;
            List<NameId> visibleMeetings = null;
            var getUserIds = new Func<List<long>>(() => { userIds = userIds ?? DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id); return userIds; });
            var getVisibleMeetings = new Func<List<NameId>>(() => {
                if (visibleMeetings == null)
                    visibleMeetings = getUserIds().SelectMany(x => L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, x, true)).Distinct(x => x.Id).ToList();
                return visibleMeetings;
            });


            if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou && !managing) {
                //var userIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, caller.Id);
                q = s.QueryOver<MeasurableModel>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).WhereRestrictionOn(x => x.AccountableUserId).IsIn(getUserIds());
                if (loadUsers)
                    q = q.Fetch(x => x.AccountableUser).Eager;

                var results = q.List().ToList();

                var visibleMeetingIds = getVisibleMeetings().Select(x => x.Id).ToList();
                var additionalFromL10 = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(visibleMeetingIds)
                    .Select(x => x.Measurable).List<MeasurableModel>().ToList();
                results.AddRange(additionalFromL10);
                results = results.Where(x => x != null).Distinct(x => x.Id).ToList();
                if (loadUsers) {
                    foreach (var r in results) {
                        try {
                            r.AccountableUser.GetName();
                        } catch (Exception) {

                        }
                    }
                }

                return results;
            } else {
                //q = s.QueryOver<MeasurableModel>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null);
                if (perms.IsPermitted(x => x.ViewOrganizationScorecard(organizationId))) {
                    return GetOrganizationMeasurables(s, perms, organizationId, loadUsers);
                } else {
                    var results = GetUserMeasurables(s, perms, perms.GetCaller().Id, loadUsers, false, true);

                    var visibleMeetingIds = getVisibleMeetings().Select(x => x.Id).ToList();
                    var additionalFromL10 = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(visibleMeetingIds)
                        .Select(x => x.Measurable).List<MeasurableModel>().ToList();
                    results.AddRange(additionalFromL10);
                    results = results.Distinct(x => x.Id).ToList();

                    if (loadUsers) {
                        foreach (var r in results) {
                            try {
                                r.AccountableUser.GetName();
                            } catch (Exception) {

                            }
                        }
                    }

                    return results;
                }
            }

            /*
            if (perms.IsPermitted(x => x.ViewOrganizationScorecard(organizationId))){
                return GetOrganizationMeasurables(s, perms, organizationId, loadUsers);
            }else{
                return GetUserMeasurables(s, perms, perms.GetCaller().Id, loadUsers,false);
            }*/

        }


		public Csv Listing(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					// var p = s.Get<PeriodModel>(period);

					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);


					var scores = s.QueryOver<ScoreModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == organizationId)
						.List().ToList();

					var csv = new Csv();
					csv.SetTitle("Measurable");
					foreach (var sss in scores.GroupBy(x => x.MeasurableId)) {
						var ss = sss.First();
						csv.Add(ss.Measurable.Title, "Owner", ss.Measurable.AccountableUser.NotNull(x => x.GetName()));
						csv.Add(ss.Measurable.Title, "Admin", ss.Measurable.AdminUser.NotNull(x => x.GetName()));
						csv.Add(ss.Measurable.Title, "Goal", "" + ss.Measurable.Goal);
						csv.Add(ss.Measurable.Title, "GoalDirection", "" + ss.Measurable.GoalDirection);
					}
					foreach (var ss in scores.OrderBy(x => x.ForWeek)) {
						csv.Add(ss.Measurable.Title, ss.ForWeek.ToShortDateString(), ss.Measured.NotNull(x => x.Value.ToString()) ?? "");
					}
					return csv;

					//var rocksQ = s.QueryOver<ScoreModel>()
					//	.Where(x => x.DeleteTime == null && x.OrganizationId == organizationId);
					////if (!caller.ManagingOrganization && !caller.IsRadialAdmin){
					////    var subs = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);
					////    rocksQ= rocksQ.WhereRestrictionOn(x=>x.ForUserId).IsIn(subs);
					////}
					//var rocks = rocksQ.List().ToList();

					//var csv = new Csv();
					//csv.SetTitle(caller.Organization.Settings.RockName);

					//foreach (var r in rocks) {
					//	csv.Add(r.Rock, "Owner", r.AccountableUser.GetName());
					//	//csv.Add(r.Rock, "Manager", string.Join(" & ", r.AccountableUser.ManagedBy.Select(x => x.Manager.GetName())));
					//	csv.Add(r.Rock, "Status", "" + r.Completion);
					//	csv.Add(r.Rock, "CreateTime", "" + r.CreateTime);
					//	csv.Add(r.Rock, "CompleteTime", "" + r.CompleteTime);
					//}
					//return csv;
				}
			}
		}

		public static List<MeasurableModel> GetVisibleMeasurables(UserOrganizationModel caller, long organizationId, bool loadUsers)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    return GetVisibleMeasurables(s, perms, organizationId, loadUsers);
                }
            }
        }

        public static List<MeasurableModel> GetOrganizationMeasurables(ISession s, PermissionsUtility perms, long organizationId, bool loadUsers)
        {

            perms.ViewOrganizationScorecard(organizationId);
            var measurables = s.QueryOver<MeasurableModel>();
            if (loadUsers)
                measurables = measurables.Fetch(x => x.AccountableUser).Eager;
            return measurables.Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();

        }

        public static List<MeasurableModel> GetOrganizationMeasurables(UserOrganizationModel caller, long organizationId, bool loadUsers)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    return GetOrganizationMeasurables(s, perms, organizationId, loadUsers);

                }
            }
        }

        public static List<MeasurableModel> GetPotentialMeetingMeasurables(UserOrganizationModel caller, long recurrenceId, bool loadUsers)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

                    var userIds = L10Accessor.GetL10Recurrence(s, perms, recurrenceId, true)._DefaultAttendees.Select(x => x.User.Id).ToList();
                    if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
                        userIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id).Intersect(userIds).ToList();
                    }

                    var measurables = s.QueryOver<MeasurableModel>();
                    if (loadUsers)
                        measurables = measurables.Fetch(x => x.AccountableUser).Eager;

                    return measurables.Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.AccountableUserId).IsIn(userIds).List().ToList();
                }
            }
        }

        public static List<MeasurableModel> GetUserMeasurables(ISession s, PermissionsUtility perms, long userId, bool loadUsers, bool ordered, bool includeAdmin)
        {
            perms.ViewUserOrganization(userId, false);
            var foundQuery = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null);

            if (includeAdmin)
                foundQuery = foundQuery.Where(x => x.AdminUserId == userId || x.AccountableUserId == userId);
            else
                foundQuery = foundQuery.Where(x => x.AccountableUserId == userId);

            var found = foundQuery.List().ToList();

            if (ordered) {
                var measuableIds = found.Select(x => x.Id).ToList();
                var ordering = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                    .Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.Measurable.Id).IsIn(measuableIds)
                    .Select(x => x.Measurable.Id, x => x._Ordering, x => x.L10Recurrence.Id)
                    .List<object[]>().ToList();
                var orderedOrdering = ordering.GroupBy(x => (long?)x[2] ?? 0).SelectMany(y => y.OrderBy(x => (int?)x[1] ?? 0)).Distinct(x => (long)x[0]).ToList();

                var newOrder = new List<MeasurableModel>();
                var i = 0;
                foreach (var o in orderedOrdering) {
                    var newOrderItem = found.FirstOrDefault(x => x.Id == (long)o[0]);
                    if (newOrderItem != null) {
                        newOrder.Add(newOrderItem);
                        newOrderItem._Ordering = i;
                        i++;
                    }

                }
                found = newOrder;

            }


            if (loadUsers) {
                foreach (var f in found) {
                    var a = f.AdminUser.GetName();
                    var b = f.AdminUser.GetImageUrl();
                    var c = f.AccountableUser.GetName();
                    var d = f.AccountableUser.GetImageUrl();
                }
            }
            return found;
        }

        public static List<MeasurableModel> GetUserMeasurables(UserOrganizationModel caller, long userId, bool ordered = false, bool includeAdmin = false)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetUserMeasurables(s, perms, userId, true, ordered, includeAdmin);
                }
            }
        }

        public static List<ScoreModel> GetMeasurableScores(UserOrganizationModel caller, long measurableId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var measureable = s.Get<MeasurableModel>(measurableId);

                    PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(measureable.OrganizationId);
                    return s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == measurableId && x.DeleteTime == null).List().ToList();
                }
            }

        }

        /*public static List<ScoreModel> GetUnfinishedScores(UserOrganizationModel caller, long organizationId, DateTime start, DateTime end)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(organizationId);
                    return scores;
                }
            }
        }*/

        public static void EditMeasurables(UserOrganizationModel caller, long userId, List<MeasurableModel> measurables, bool updateAllL10s)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create()) {
                        if (measurables.Any(x => x.AccountableUserId != userId))
                            throw new PermissionsException("Measurable UserId does not match UserId");

                        var perm = PermissionsUtility.Create(s, caller).EditQuestionForUser(userId);
                        var user = s.Get<UserOrganizationModel>(userId);
                        var orgId = user.Organization.Id;
                        var added = measurables.Where(x => x.Id == 0).ToList();
                        foreach (var r in measurables) {
                            r.OrganizationId = orgId;
                            //var added = r.Id == 0;
                            if (r.Id == 0)
                                s.Save(r);
                            else
                                s.Merge(r);
                        }
                        var now = DateTime.UtcNow;

                        var toDelete = measurables.Where(x => x.DeleteTime != null).Select(x => x.Id).ToList();
                        if (toDelete.Any()) {
                            var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                                .Where(x => x.DeleteTime == null)
                                .WhereRestrictionOn(x => x.Measurable.Id).IsIn(toDelete)
                                .List();
                            foreach (var m in recurMeasurables) {
                                m.DeleteTime = now;
                                s.Update(m);
                            }
                        }

                        if (updateAllL10s) {
                            var allL10s = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userId).List().Where(x => x.L10Recurrence.DeleteTime == null).ToList();

                            foreach (var r in added) {
                                var r1 = r;
                                foreach (var o in allL10s.Select(x => x.L10Recurrence)) {

                                    if (o.OrganizationId != caller.Organization.Id)
                                        throw new PermissionsException("Cannot access the Level 10");
                                    perm.UnsafeAllow(PermItem.AccessLevel.View, PermItem.ResourceType.L10Recurrence, o.Id);
                                    perm.UnsafeAllow(PermItem.AccessLevel.Edit, PermItem.ResourceType.L10Recurrence, o.Id);
                                    L10Accessor.AddMeasurable(s, perm, rt, o.Id, new Controllers.L10Controller.AddMeasurableVm() {
                                        RecurrenceId = o.Id,
                                        SelectedMeasurable = r1.Id,
                                    });
                                }
                            }
                        }

                        s.SaveOrUpdate(user);
                        user.UpdateCache(s);

                        tx.Commit();
                        s.Flush();

                    }
                }
            }
        }

        public static List<ScoreModel> GetUserScoresIncomplete(UserOrganizationModel caller, long userId, DateTime? now = null)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
                    var nowPlus = (now ?? DateTime.UtcNow).Add(TimeSpan.FromDays(1));
                    var scorecards = s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == userId && x.DateDue < nowPlus && x.DateEntered == null && x.DeleteTime == null).List().ToList();

                    scorecards = scorecards.Where(x => x.Measurable.DeleteTime == null).ToList();

                    return scorecards;
                }
            }


        }

        public static List<ScoreModel> GetUserScores(UserOrganizationModel caller, long userId, DateTime sd, DateTime ed, bool includeAdmin = false)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
                    var scorecardStart = sd.StartOfWeek(DayOfWeek.Sunday);
                    var scorecardEnd = ed.AddDays(6.999).StartOfWeek(DayOfWeek.Sunday);

                    var query = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.ForWeek >= scorecardStart && x.ForWeek <= scorecardEnd);


                    if (includeAdmin) {
                        var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && (x.AdminUserId == userId || x.AccountableUserId == userId)).Select(x => x.Id).List<long>().ToList();
                        query = query.WhereRestrictionOn(x => x.MeasurableId).IsIn(measurables);
                    } else {
                        query = query.Where(x => x.AccountableUserId == userId);
                    }
                    return query.List().ToList();
                }
            }
        }

        public static void EditUserScores(UserOrganizationModel caller, List<ScoreModel> scores)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var oldScores = scores.ToList();

                    scores = s.QueryOver<ScoreModel>().WhereRestrictionOn(x => x.Id).IsIn(scores.Select(x => x.Id).ToArray()).List().ToList();

                    var now = DateTime.UtcNow;

                    var uid = scores.EnsureAllSame(x => x.AccountableUserId);
                    PermissionsUtility.Create(s, caller).EditUserScorecard(uid);
                    foreach (var x in scores) {
                        x.Measured = oldScores.FirstOrDefault(y => y.Id == x.Id).NotNull(y => y.Measured);
                        if (x.Measured == null) {
                            x.DateEntered = null;
                            x.DeleteTime = now;
                        } else {
                            x.DeleteTime = null;
                            x.DateEntered = now;
                        }

                        s.Update(x);
                    }
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static ScoreModel UpdateScoreInMeeting(ISession s, PermissionsUtility perms, long recurrenceId, long scoreId, DateTime week, long measurableId, decimal? value, string dom, string connectionId)
        {
            var now = DateTime.UtcNow;
            DateTime? nowQ = now;

            perms.EditL10Recurrence(recurrenceId);

            var meeting = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId);
            var score = s.Get<ScoreModel>(scoreId);


            if (score != null && score.DeleteTime == null && false) {
                TestUtilities.Log("Score found. Updating.");

                SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(scoreId));

                //Editable in this meeting?
                var ms = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                    .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == score.MeasurableId)
                    .SingleOrDefault<L10Meeting.L10Meeting_Measurable>();
                if (ms == null)
                    throw new PermissionsException("You do not have permission to edit this score.");

                var all = s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == score.MeasurableId && x.ForWeek == score.ForWeek).List().ToList();
                foreach (var sc in all) {
                    sc.Measured = value;
                    sc.DateEntered = (value == null) ? null : (DateTime?)now;
                    s.Update(sc);
                }
                //score.Measured = value;
                //score.DateEntered = (value == null) ? null : nowQ;
                //s.Update(score);
            } else {
                var meetingMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                    .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == measurableId)
                    .SingleOrDefault<L10Meeting.L10Meeting_Measurable>();

                if (meetingMeasurables == null)
                    throw new PermissionsException("You do not have permission to edit this score.");
                var m = meetingMeasurables.Measurable;

                //var SoW = m.Organization.Settings.WeekStart;

                var existingScores = s.QueryOver<ScoreModel>()
                    .Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
                    .List().ToList();

                //adjust week..
                week = week.StartOfWeek(DayOfWeek.Sunday);

                //See if we can find it given week.
                var scores = existingScores.OrderBy(x => x.Id).Where(x => (x.ForWeek == week)).ToList();
                if (scores.Any()) {
                    TestUtilities.Log("Found one or more score. Updating All.");

                    foreach (var sc in scores) {
                        SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(sc.Id));
                        if (sc.Measured != value) {
                            sc.Measured = value;
                            sc.DateEntered = (value == null) ? null : (DateTime?)now;
                            s.Update(sc);
                        }
                        score = sc;
                    }
                } else {
                    var ordered = existingScores.OrderBy(x => x.DateDue);
                    var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;
                    var maxDate = ordered.LastOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;

                    minDate = minDate.StartOfWeek(DayOfWeek.Sunday);
                    maxDate = maxDate.StartOfWeek(DayOfWeek.Sunday);


                    //DateTime start, end;

                    if (week > maxDate) {
                        var scoresCreated = 0;
                        TestUtilities.Log("Score not found. Score above boundry. Creating scores up to value.");
                        //Create going up until sufficient
                        var n = maxDate;
                        ScoreModel curr = null;
                        var measurable = s.Get<MeasurableModel>(m.Id);

                        while (n < week) {
                            var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);
                            curr = new ScoreModel() {
                                AccountableUserId = m.AccountableUserId,
                                DateDue = nextDue,
                                MeasurableId = m.Id,
                                Measurable = measurable,
                                OrganizationId = m.OrganizationId,
                                ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday),
                                OriginalGoal = measurable.Goal,
                                OriginalGoalDirection = measurable.GoalDirection
                            };
                            s.Save(curr);
                            scoresCreated++;
                            m.NextGeneration = nextDue;
                            n = nextDue.StartOfWeek(DayOfWeek.Sunday);
                        }
                        curr.DateEntered = (value == null) ? null : nowQ;
                        curr.Measured = value;
                        curr.Measurable = s.Get<MeasurableModel>(curr.MeasurableId);
                        score = curr;
                        TestUtilities.Log("Scores created: " + scoresCreated);
                    } else if (week < minDate) {
                        TestUtilities.Log("Score not found. Score below boundry. Creating scores down to value.");
                        var n = week;
                        var first = true;
                        var scoresCreated = 0;
                        var measurable = s.Get<MeasurableModel>(m.Id);

                        while (n < minDate) {
                            var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime);
                            var curr = new ScoreModel() {
                                AccountableUserId = m.AccountableUserId,
                                DateDue = nextDue,
                                MeasurableId = m.Id,
                                Measurable = measurable,
                                OrganizationId = m.OrganizationId,
                                ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday),
                                OriginalGoal = measurable.Goal,
                                OriginalGoalDirection = measurable.GoalDirection
                            };
                            if (first) {
                                curr.Measured = value;
                                curr.DateEntered = (value == null) ? null : nowQ;
                                first = false;
                                s.Save(curr);
                                scoresCreated++;
                                score = curr;
                            }

                            //m.NextGeneration = nextDue;
                            n = nextDue.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
                            curr.Measurable = s.Get<MeasurableModel>(curr.MeasurableId);

                        }
                        TestUtilities.Log("Scores created: " + scoresCreated);
                    } else {
                        TestUtilities.Log("Score not found. Score inside boundry. Creating score.");
                        // cant create scores between these dates..
                        var measurable = s.Get<MeasurableModel>(m.Id);
                        var curr = new ScoreModel() {
                            AccountableUserId = m.AccountableUserId,
                            DateDue = week.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime),
                            MeasurableId = m.Id,
                            Measurable = measurable,
                            OrganizationId = m.OrganizationId,
                            ForWeek = week.StartOfWeek(DayOfWeek.Sunday),
                            Measured = value,
                            DateEntered = (value == null) ? null : nowQ,
                            OriginalGoal = measurable.Goal,
                            OriginalGoalDirection = measurable.GoalDirection
                        };
                        s.Save(curr);
                        curr.Measurable = s.Get<MeasurableModel>(curr.MeasurableId);
                        score = curr;
                        TestUtilities.Log("Scores created: 1");
                    }
                    s.Update(m);

                }
                #region comments
                //if (score != null && score.Measured != value)
                //{
                //    //Found it with false id
                //    SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(score.Id));
                //    score.Measured = value;
                //    score.DateEntered = (value == null) ? null : nowQ;
                //    s.Update(score);
                //}
                //else
                //{
                //    var ordered = existingScores.OrderBy(x => x.DateDue);
                //    var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;
                //    var maxDate = ordered.LastOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;

                //    minDate = minDate.StartOfWeek(DayOfWeek.Sunday);
                //    maxDate = maxDate.StartOfWeek(DayOfWeek.Sunday);


                //    DateTime start, end;

                //    if (week > maxDate)
                //    {
                //        //Create going up until sufficient
                //        var n = maxDate;
                //        ScoreModel curr = null;
                //        while (n < week)
                //        {
                //            var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);
                //            curr = new ScoreModel()
                //            {
                //                AccountableUserId = m.AccountableUserId,
                //                DateDue = nextDue,
                //                MeasurableId = m.Id,
                //                OrganizationId = m.OrganizationId,
                //                ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday)
                //            };
                //            s.Save(curr);
                //            m.NextGeneration = nextDue;
                //            n = nextDue.StartOfWeek(DayOfWeek.Sunday);
                //        }
                //        curr.DateEntered = (value == null) ? null : nowQ;
                //        curr.Measured = value;
                //        score = curr;
                //    }
                //    else if (week < minDate)
                //    {
                //        var n = week;
                //        var first = true;
                //        while (n < minDate)
                //        {
                //            var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime);
                //            var curr = new ScoreModel()
                //            {
                //                AccountableUserId = m.AccountableUserId,
                //                DateDue = nextDue,
                //                MeasurableId = m.Id,
                //                OrganizationId = m.OrganizationId,
                //                ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday)
                //            };
                //            if (first)
                //            {
                //                curr.Measured = value;
                //                curr.DateEntered = (value == null) ? null : nowQ;
                //                first = false;
                //                s.Save(curr);
                //            }

                //            //m.NextGeneration = nextDue;
                //            n = nextDue.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
                //            score = curr;
                //        }
                //    }
                //    else
                //    {
                //        // cant create scores between these dates..
                //        var curr = new ScoreModel()
                //        {
                //            AccountableUserId = m.AccountableUserId,
                //            DateDue = week.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime),
                //            MeasurableId = m.Id,
                //            OrganizationId = m.OrganizationId,
                //            ForWeek = week.StartOfWeek(DayOfWeek.Sunday),
                //            Measured = value,
                //            DateEntered = (value == null) ? null : nowQ
                //        };
                //        s.Save(curr);
                //        score = curr;
                //    }
                //    s.Update(m);
                //}
                #endregion

            }
            var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
            var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting), connectionId);
            group.updateTextContents(dom, value);

            if (score != null) {
                var toUpdate = new AngularScore(score, false);
                toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
                toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();
                group.update(new AngularUpdate() { toUpdate });
            }

            Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateScoreInMeeting", ForModel.Create(score), score.NotNull(x => x.Measurable.NotNull(y => y.Title)) + " updated to " + value);
            return score;
        }

        public static ScoreModel UpdateScoreInMeeting(UserOrganizationModel caller, long recurrenceId, long scoreId, DateTime week, long measurableId, decimal? value, string dom, string connectionId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    var output = UpdateScoreInMeeting(s, perms, recurrenceId, scoreId, week, measurableId, value, dom, connectionId);

                    tx.Commit();
                    s.Flush();
                    return output;
                }
            }
        }

        public static ScoreModel GetScoreInMeeting(UserOrganizationModel caller, long scoreId, long recurrenceId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var meeting = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId);
                    var score = s.Get<ScoreModel>(scoreId);


                    if (score != null && score.DeleteTime == null) {
                        //Editable in this meeting?
                        var ms = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                            .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == score.MeasurableId)
                            .SingleOrDefault<L10Meeting.L10Meeting_Measurable>();
                        if (ms == null)
                            throw new PermissionsException("You do not have permission to edit this score.");

                        var a = score.Measurable.AccountableUser.GetName();
                        var b = score.Measurable.AdminUser.GetName();
                        var c = score.AccountableUser.GetName();

                        return score;
                    }
                    return null;
                }
            }
        }

        public static MeasurableModel GetMeasurable(UserOrganizationModel caller, long id)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewMeasurable(id);
                    return s.Get<MeasurableModel>(id);
                }
            }
        }

        public static ScoreModel GetScore(UserOrganizationModel caller, long id)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var found = s.Get<ScoreModel>(id);
                    PermissionsUtility.Create(s, caller).ViewMeasurable(found.MeasurableId);
                    return found;
                }
            }
        }

        public static void CreateMeasurable(ISession s, PermissionsUtility perm, MeasurableModel measurable, bool checkEditDetails)
        {
            //Create new
            if (measurable == null)
                throw new PermissionsException("You must include a measurable to create.");
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
			if (measurable.OrganizationId == null)
				throw new PermissionsException("You must include an organization id.");
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
            if (checkEditDetails) {
                perm.EditUserDetails(measurable.AccountableUser.Id);
            }
            perm.ViewOrganization(measurable.OrganizationId);

            perm.ViewUserOrganization(measurable.AccountableUserId, false);
            perm.ViewUserOrganization(measurable.AdminUserId, false);

            measurable.OrganizationId = measurable.OrganizationId;

            measurable.AccountableUser = s.Load<UserOrganizationModel>(measurable.AccountableUserId);
            measurable.AdminUser = s.Load<UserOrganizationModel>(measurable.AdminUserId);

            s.Save(measurable);

            measurable.AccountableUser.UpdateCache(s);
            measurable.AdminUser.UpdateCache(s);

        }

    }
}