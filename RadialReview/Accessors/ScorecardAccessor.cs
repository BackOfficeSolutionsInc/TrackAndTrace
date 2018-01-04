﻿using System;
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
using RadialReview.Models.Angular.Meeting;
using System.Threading.Tasks;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using static RadialReview.Accessors.L10Accessor;
using RadialReview.Models.Enums;
using RadialReview.Models.ViewModels;
using System.Web.Mvc;
using Dangl.Calculator;
using static RadialReview.Utilities.GraphUtility;
using static RadialReview.Utilities.FormulaUtility;
using NHibernate.Criterion;

namespace RadialReview.Accessors {

    public class MeasurableBuilder {


        private long AccountableUserId { get; set; }
        private long? AdminUserId { get; set; }
        private string Message { get; set; }
        private decimal Goal { get; set; }
        private UnitType UnitType { get; set; }
        private LessGreater GoalDirection { get; set; }
        private long? TemplateItemId { get; set; }
        private decimal? AlternateGoal { get; set; }

        private bool ShowCumulative { get; set; }
        private DateTime? CumulativeRange { get; set; }
        private DateTime Now { get; set; }

        private bool _ensured { get; set; }

        private MeasurableBuilder(string message, decimal goal, UnitType unitType, LessGreater goalDirection, long accountableUserId, long? adminUserId, decimal? alternateGoal, long? templateItemId, bool showCumulative, DateTime? cumulativeRange, DateTime? now = null) {
            AccountableUserId = accountableUserId;
            AdminUserId = adminUserId;
            Message = message;
            Goal = goal;
            UnitType = unitType;
            TemplateItemId = templateItemId;
            AlternateGoal = alternateGoal;
            ShowCumulative = showCumulative;
            CumulativeRange = cumulativeRange;
            Now = now ?? DateTime.UtcNow;
            GoalDirection = goalDirection;
        }

        public static MeasurableBuilder Build(string message, long accountableUserId, long? adminUserId = null, UnitType type = UnitType.None, decimal goal = 0, LessGreater goalDirection = LessGreater.GreaterThan, decimal? alternateGoal = null, bool showCumulative = false, DateTime? cumulativeRange = null, DateTime? now = null) {
            return new MeasurableBuilder(message, goal, type, goalDirection, accountableUserId, adminUserId, alternateGoal, null, showCumulative, cumulativeRange, now);
        }
        public static MeasurableBuilder CreateMeasurableFromTemplate() {
            throw new NotImplementedException();
        }

        private void EnsurePermitted(PermissionsUtility perms, long orgId) {
            _ensured = true;

            perms.ViewOrganization(orgId);
            perms.ViewUserOrganization(AccountableUserId, false);
            perms.CreateMeasurableForUser(AccountableUserId);
            if (AdminUserId.HasValue) {
                perms.ViewUserOrganization(AdminUserId.Value, false);
                perms.CreateMeasurableForUser(AdminUserId.Value);
            }
        }

        public MeasurableModel Generate(ISession s, PermissionsUtility perms) {
            var creator = perms.GetCaller();
            var orgId = creator.Organization.Id;
            EnsurePermitted(perms, orgId);

            var adminId = AdminUserId ?? AccountableUserId;

            return new MeasurableModel() {
                AccountableUserId = AccountableUserId,
                AccountableUser = s.Load<UserOrganizationModel>(AccountableUserId),
                AdminUserId = adminId,
                AdminUser = s.Load<UserOrganizationModel>(adminId),
                AlternateGoal = AlternateGoal,
                CumulativeRange = CumulativeRange,
                CreateTime = Now,
                //DueTime -- fuck it...
                //DueDate
                FromTemplateItemId = TemplateItemId,
                Goal = Goal,
                GoalDirection = GoalDirection,
                //NextGeneration
                Organization = creator.Organization,
                OrganizationId = orgId,
                ShowCumulative = ShowCumulative,
                Title = Message,
                UnitType = UnitType,
            };
        }
    }

    public class ScorecardAccessor {

        #region Create

        public static async Task<MeasurableModel> CreateMeasurable(UserOrganizationModel caller, MeasurableBuilder measurableBuilder) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var m = await CreateMeasurable(s, perms, measurableBuilder);
                    tx.Commit();
                    s.Flush();
                    return m;
                }
            }
        }
        public static async Task<MeasurableModel> CreateMeasurable(ISession s, PermissionsUtility perms, MeasurableBuilder measurableBuilder) {
            var m = measurableBuilder.Generate(s, perms);
            s.Save(m);
            await HooksRegistry.Each<IMeasurableHook>((ses, x) => x.CreateMeasurable(ses, m));
            return m;
        }

        [Obsolete("Commit afterwards")]
        public static async Task<List<ScoreModel>> _GenerateScoreModels_AddMissingScores_Unsafe(ISession s, DateRange range, List<long> measurableIds, List<ScoreModel> existing) {
            //var weeks = new List<DateTime>();
            //var i = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
            //var end = range.EndTime.AddDays(6.999).StartOfWeek(DayOfWeek.Sunday);
            //while (i<=end) {
            //	weeks.Add(i);
            //	i = i.AddDays(7);
            //}

            var weeks = TimingUtility.GetWeeksBetween(range);

            return await _GenerateScoreModels_AddMissingScores_Unsafe(s, weeks, measurableIds, existing);


        }

        [Obsolete("Commit afterwards")]
        public static async Task<List<ScoreModel>> _GenerateScoreModels_AddMissingScores_Unsafe(ISession s, IEnumerable<DateTime> weeks, List<long> measurableIds, List<ScoreModel> existing) {
            //var measurableLU = measurables.ToDefaultDictionary(x => x.Id, x => x, x => null);
            //var measurableIds = measurables.Select(x => x.Id).ToList();
            var weekMeasurables = new List<Tuple<DateTime, long>>();
            foreach (var week in weeks) {
                foreach (var mid in measurableIds) {
                    weekMeasurables.Add(Tuple.Create(week, mid));
                }
            }
            List<ScoreModel> added = await _GenerateScoreModels_AddMissingScores_Unsafe(s, weekMeasurables,existing);
            return added;
        }

        private static async Task<List<ScoreModel>> _GenerateScoreModels_AddMissingScores_Unsafe(ISession s, IEnumerable<Tuple<DateTime, long>> weekMeasurables, List<ScoreModel> existing) {
            var measurableToGet = new List<long>();
            var toAdd_WeekMeasurable = new List<Tuple<DateTime, long>>();
            foreach (var wm in weekMeasurables) {
                var week = wm.Item1;
                var mid = wm.Item2;
                if (!existing.Any(x => x.ForWeek == week && x.MeasurableId == mid)) {
                    measurableToGet.Add(mid);
                    toAdd_WeekMeasurable.Add(Tuple.Create(week, mid));
                }
            }

            var added = new List<ScoreModel>();
            if (measurableToGet.Any()) {
                var calc = new List<ScoreModel>();
                var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(measurableToGet.Distinct().ToList()).List().ToDefaultDictionary(x => x.Id, x => x, x => null);
                foreach (var d in toAdd_WeekMeasurable) {
                    var m = measurables[d.Item2];
                    var week = d.Item1;
                    if (m != null) {
                        var curr = new ScoreModel() {
                            AccountableUserId = m.AccountableUserId,
                            DateDue = DateTime.MaxValue,//fuck it..
                            MeasurableId = m.Id,
                            Measurable = m,
                            OrganizationId = m.OrganizationId,
                            ForWeek = week.StartOfWeek(DayOfWeek.Sunday),
                            OriginalGoal = m.Goal,
                            OriginalGoalDirection = m.GoalDirection,
                            AlternateOriginalGoal = m.AlternateGoal,
                            AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId)
                        };

                        if (m.HasFormula) {
                            calc.Add(curr);
                        }


                        s.Save(curr);
                        added.Add(curr);
                    }
                }
                await UpdateTheseCalculatedScores_Unsafe(s, calc);
            }

            return added;
        }

        [Obsolete("Commit afterwards")]
        public static async Task<bool> _GenerateScoreModels_Unsafe(ISession s, IEnumerable<DateTime> weeks, IEnumerable<long> measurableIds) {
            var any = false;
            weeks = weeks.Select(x => x.StartOfWeek(DayOfWeek.Sunday)).Distinct();

            if (weeks.Any()) {

                var min = weeks.Min();
                var max = weeks.Max();

                var existing = s.QueryOver<ScoreModel>()
                                    .Where(x => x.DeleteTime == null && x.ForWeek >= min && x.ForWeek <= max)
                                    .WhereRestrictionOn(x => x.MeasurableId).IsIn(measurableIds.ToArray())
                                    .List().ToList();

                //var measurables = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(measurableIds.ToArray()).List().ToList();//ToDefaultDictionary(x => x.Id, x => x, x => null);

                await _GenerateScoreModels_AddMissingScores_Unsafe(s, weeks, measurableIds.ToList(), existing);

                //foreach (var week in weeks) {
                //	foreach (var mid in measurableIds) {
                //		var m = measurableLU[mid];

                //		if (m != null && !existing.Any(x => x.ForWeek == week && x.MeasurableId == mid)) {
                //			any = true;
                //			var curr = new ScoreModel() {
                //				AccountableUserId = m.AccountableUserId,
                //				DateDue = DateTime.MaxValue,//fuck it..
                //				MeasurableId = m.Id,
                //				Measurable = m,
                //				OrganizationId = m.OrganizationId,
                //				ForWeek = week.StartOfWeek(DayOfWeek.Sunday),
                //				OriginalGoal = m.Goal,
                //				OriginalGoalDirection = m.GoalDirection,
                //				AlternateOriginalGoal = m.AlternateGoal,
                //				AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId)
                //			};
                //			s.Save(curr);
                //		}
                //	}
                //}
            }
            return any;
        }

        #endregion

        #region Getters

        public static async Task<AngularScorecard> GetAngularScorecardForUser(UserOrganizationModel caller, long userId, int periods) {
            var scorecardStart = TimingUtility.PeriodsAgo(DateTime.UtcNow, periods, caller.Organization.Settings.ScorecardPeriod);
            var scorecardEnd = DateTime.UtcNow.AddDays(14);
            return await ScorecardAccessor.GetAngularScorecardForUser(caller, userId, new DateRange(scorecardStart, scorecardEnd), true, now: DateTime.UtcNow);
        }

        public static async Task<AngularScorecard> GetAngularScorecardForUser(UserOrganizationModel caller, long userId, DateRange range, bool includeAdmin = true, bool includeNextWeek = true, DateTime? now = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var scorecard = await GetAngularScorecardForUser(s, perms, userId, range, includeAdmin, includeNextWeek, now);
                    //Commit is required!
                    tx.Commit();
                    s.Flush();
                    return scorecard;

                }
            }
        }

        [Obsolete("Commit after calling this")]
        private static async Task<AngularScorecard> GetAngularScorecardForUser(ISession s, PermissionsUtility perms, long userId, DateRange range, bool includeAdmin = true, bool includeNextWeek = true, DateTime? now = null) {
            var measurables = GetUserMeasurables(s, perms, userId, true, true, true);

            var scorecardStart = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
            var scorecardEnd = range.EndTime.AddDays(6).StartOfWeek(DayOfWeek.Sunday);

            var scores = await GetUserScoresAndFillIn(s, perms, userId, scorecardStart, scorecardEnd, includeAdmin);
            return new AngularScorecard(-1, perms.GetCaller(), measurables.Select(x => new AngularMeasurable(x)), scores.ToList(), now, range, includeNextWeek, now);
        }

        public static List<MeasurableModel> GetVisibleMeasurables(ISession s, PermissionsUtility perms, long organizationId, bool loadUsers) {
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
                            r.AdminUser.GetName();
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
                    results = results.Where(x => x != null).Distinct(x => x.Id).ToList();

                    if (loadUsers) {
                        foreach (var r in results) {
                            try {
                                r.AccountableUser.GetName();
                                r.AdminUser.GetName();
                            } catch (Exception) {

                            }
                        }
                    }

                    return results;
                }
            }
        }

        public static List<MeasurableModel> GetVisibleMeasurables(UserOrganizationModel caller, long organizationId, bool loadUsers) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    return GetVisibleMeasurables(s, perms, organizationId, loadUsers);
                }
            }
        }

        public static List<MeasurableModel> GetOrganizationMeasurables(ISession s, PermissionsUtility perms, long organizationId, bool loadUsers) {

            perms.ViewOrganizationScorecard(organizationId);
            var measurables = s.QueryOver<MeasurableModel>();
            if (loadUsers)
                measurables = measurables.Fetch(x => x.AccountableUser).Eager;
            return measurables.Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();

        }

        public static List<MeasurableModel> GetOrganizationMeasurables(UserOrganizationModel caller, long organizationId, bool loadUsers) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    return GetOrganizationMeasurables(s, perms, organizationId, loadUsers);

                }
            }
        }

        public static List<MeasurableModel> GetPotentialMeetingMeasurables(UserOrganizationModel caller, long recurrenceId, bool loadUsers) {
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

        public static List<MeasurableModel> GetUserMeasurables(ISession s, PermissionsUtility perms, long userId, bool loadUsers, bool ordered, bool includeAdmin) {
            perms.ViewUserOrganization(userId, false);
            var foundQuery = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null);
            if (includeAdmin)
                foundQuery = foundQuery.Where(x => x.AdminUserId == userId || x.AccountableUserId == userId);
            else
                foundQuery = foundQuery.Where(x => x.AccountableUserId == userId);
            var found = foundQuery.List().ToList();

            var userIds = found.SelectMany(x => new[] { x.AdminUserId, x.AccountableUserId }).Distinct().ToList();
            var __users = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(userIds).List().ToList();


            if (ordered) {
                var order = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                    .Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.Measurable.Id)
                    .IsIn(found.Select(x => x.Id).Distinct().ToArray())
                    .Select(x => x.Measurable.Id, x => x.L10Recurrence.Id, x => x._Ordering)
                    .List<object[]>()
                    .Select(x => new {
                        Measurable = (long)x[0],
                        Meeting = (long)x[1],
                        Order = (int?)x[2]
                    }).ToList();

                order = order.GroupBy(x => x.Meeting)
                    .OrderByDescending(x => x.Count())
                    .ThenBy(x => x.First().Meeting)
                    .Select(x => x.OrderBy(y => y.Order ?? int.MaxValue).ThenBy(y => y.Measurable))
                    .SelectMany(x => x)
                    .Distinct(x => x.Measurable)
                    .ToList();

                var lookup = order.Select((x, i) => Tuple.Create(x, i))
                    .ToDictionary(x => x.Item1.Measurable, x => x.Item2);

                foreach (var o in found) {
                    if (lookup.ContainsKey(o.Id))
                        o._Ordering = lookup[o.Id];
                }
                found = found.OrderBy(x => x._Ordering).ToList();
            }

            L10Accessor._RecalculateCumulative_Unsafe(s, null, found, null, null);


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

        public static List<MeasurableModel> GetUserMeasurables(UserOrganizationModel caller, long userId, bool ordered = false, bool includeAdmin = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetUserMeasurables(s, perms, userId, true, ordered, includeAdmin);
                }
            }
        }

        public static List<ScoreModel> GetMeasurableScores(UserOrganizationModel caller, long measurableId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var measureable = s.Get<MeasurableModel>(measurableId);
                    PermissionsUtility.Create(s, caller).Or(
                        x => x.ViewMeasurable(measurableId),
                        x => x.ViewOrganizationScorecard(measureable.OrganizationId)
                    );
                    return s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == measurableId && x.DeleteTime == null).List().ToList();
                }
            }
        }


        public static MeasurableModel GetMeasurable(UserOrganizationModel caller, long id) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewMeasurable(id);
                    return s.Get<MeasurableModel>(id);
                }
            }
        }

        public static async Task<ScoreModel> GetScore(UserOrganizationModel caller, long measurableId, long weekId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var score = await GetScore(s, perms, measurableId, weekId);
                    tx.Commit();
                    s.Flush();
                    return score;
                }
            }
        }

        [Obsolete("Call commit")]
        private static async Task<ScoreModel> GetScore_Unsafe(ISession s, long measurableId, long weekId) {
            var week = TimingUtility.GetDateSinceEpoch(weekId);
            await _GenerateScoreModels_Unsafe(s, week.AsList(), measurableId.AsList());
            var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.ForWeek == week && x.MeasurableId == measurableId).List().ToList();
            var found = scores.OrderBy(x => x.Id).FirstOrDefault();
            return found;
        }

        [Obsolete("Call commit")]
        private static async Task<ScoreModel> GetScore(ISession s, PermissionsUtility perms, long measurableId, long weekId) {
            perms.ViewMeasurable(measurableId);
            return await GetScore_Unsafe(s, measurableId, weekId);
        }

        [Obsolete("Call commit")]
        private static async Task<ScoreModel> GetScore(ISession s, PermissionsUtility perms, long measurableId, DateTime week) {
            return await GetScore(s, perms, measurableId, TimingUtility.GetWeekSinceEpoch(week));
        }

        public static ScoreModel GetScore(UserOrganizationModel caller, long id) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var found = s.Get<ScoreModel>(id);
                    PermissionsUtility.Create(s, caller).ViewMeasurable(found.MeasurableId);
                    return found;
                }
            }
        }


        [Obsolete("Commit after calling this")]
        private static async Task<List<ScoreModel>> GetUserScoresAndFillIn(ISession s, PermissionsUtility perms, long userId, DateTime sd, DateTime ed, bool includeAdmin = false) {
            perms.ViewUserOrganization(userId, false);
            var scorecardStart = sd.StartOfWeek(DayOfWeek.Sunday);
            var scorecardEnd = ed.AddDays(6.999).StartOfWeek(DayOfWeek.Sunday);

            var weeks = TimingUtility.GetWeeksBetween(scorecardStart, scorecardEnd);
            var measurableIdQs = s.QueryOver<MeasurableModel>();

            if (includeAdmin)
                measurableIdQs = measurableIdQs.Where(x => x.DeleteTime == null && (x.AdminUserId == userId || x.AccountableUserId == userId));
            else
                measurableIdQs = measurableIdQs.Where(x => x.DeleteTime == null && x.AccountableUserId == userId);
            var measurableIds = measurableIdQs.Select(x => x.Id).List<long>().ToList();


            var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.ForWeek >= scorecardStart && x.ForWeek <= scorecardEnd);

            if (includeAdmin) {
                //var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && (x.AdminUserId == userId || x.AccountableUserId == userId)).Select(x => x.Id).List<long>().ToList();
                scoresQ = scoresQ.WhereRestrictionOn(x => x.MeasurableId).IsIn(measurableIds);
            } else {
                scoresQ = scoresQ.Where(x => x.AccountableUserId == userId); //already checked delete time above
            }
            var scoresWithDups = scoresQ.List().ToList();

            var scores = scoresWithDups.OrderBy(x => x.Id).Distinct(x => Tuple.Create(x.ForWeek, x.Measurable.Id)).ToList();



            //Generate blank ones
            var extra = await _GenerateScoreModels_AddMissingScores_Unsafe(s, weeks, measurableIds, scores);
            scores.AddRange(extra);

            return scores;
        }

        //public static async Task<List<ScoreModel>> GetUserScores(UserOrganizationModel caller, long userId, DateTime sd, DateTime ed, bool includeAdmin = false) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var perms = PermissionsUtility.Create(s, caller);
        //			return await GetUserScores(s, perms, userId, sd, ed, includeAdmin);
        //		}
        //	}
        //}
        #endregion

        #region Edit

        public static async Task UpdateMeasurable(UserOrganizationModel caller, long measurableId, string name = null, LessGreater? direction = null, decimal? target = null, long? accountableId = null, long? adminId = null, string connectionId = null, bool updateFutureOnly = true, decimal? altTarget = null, bool? showCumulative = null, DateTime? cumulativeRange = null, UnitType? unitType = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    await UpdateMeasurable(s, perms, measurableId, name, direction, target, accountableId, adminId, connectionId, updateFutureOnly, altTarget, showCumulative, cumulativeRange, unitType);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static async Task UpdateMeasurable(ISession s, PermissionsUtility perms, long measurableId, string name = null, LessGreater? direction = null, decimal? target = null, long? accountableId = null, long? adminId = null, string connectionId = null, bool updateFutureOnly = true, decimal? altTarget = null, bool? showCumulative = null, DateTime? cumulativeRange = null, UnitType? unitType = null) {
            var measurable = s.Get<MeasurableModel>(measurableId);

            if (measurable == null)
                throw new PermissionsException("Measurable does not exist.");
            perms.EditMeasurable(measurableId);

            var updates = new IMeasurableHookUpdates();
            var scoresToUpdate = new List<ScoreModel>();

            var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                .Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Id)
                .Select(x => x.Id)
                .List<long>().ToList();

            //Message
            if (name != null && measurable.Title != name) {
                measurable.Title = name;
                updates.MessageChanged = true;
            }

            //Show Cumulative
            if (showCumulative != null && measurable.ShowCumulative != showCumulative) {
                measurable.ShowCumulative = showCumulative.Value;
                updates.ShowCumulativeChanged = true;
            }

            //Cumulative Range
            if (cumulativeRange != null && measurable.CumulativeRange != cumulativeRange) {
                measurable.CumulativeRange = cumulativeRange.Value;
                updates.CumulativeRangeChanged = true;
            }

            //Direction
            if ((direction != null && measurable.GoalDirection != direction.Value) || !updateFutureOnly) {
                measurable.GoalDirection = direction.Value;
                var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);

                updates.UpdateAboveWeek = DateTime.MinValue;
                if (updateFutureOnly) {
                    var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
                    scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
                    updates.UpdateAboveWeek = nowSunday;
                }
                var scores = scoresQ.List().ToList();
                foreach (var score in scores) {
                    score.OriginalGoalDirection = direction.Value;
                    s.Update(score);
                }
                scoresToUpdate = scores;
                updates.GoalDirectionChanged = true;
            }

            //Target
            if ((target != null && measurable.Goal != target.Value) || !updateFutureOnly) {
                if (target != null) {
                    measurable.Goal = target.Value;
                    updates.GoalChanged = true;
                }
                var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
                updates.UpdateAboveWeek = DateTime.MinValue;
                if (updateFutureOnly) {
                    var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
                    scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
                    updates.UpdateAboveWeek = nowSunday;
                }
                var scores = scoresQ.List().ToList();
                foreach (var score in scores) {
                    score.OriginalGoal = measurable.Goal;
                    s.Update(score);
                }
                scoresToUpdate = scores;
                //updates.GoalChanged=true is above.						
            }

            //Alt Target
            if ((altTarget != null && measurable.AlternateGoal != altTarget.Value) || !updateFutureOnly) {
                if (altTarget != null) {
                    measurable.AlternateGoal = altTarget.Value;
                    updates.AlternateGoalChanged = true;
                }
                var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
                updates.UpdateAboveWeek = DateTime.MinValue;
                if (updateFutureOnly) {
                    var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
                    scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
                    updates.UpdateAboveWeek = nowSunday;
                }
                var scores = scoresQ.List().ToList();
                foreach (var score in scores) {
                    score.AlternateOriginalGoal = measurable.AlternateGoal;
                    s.Update(score);
                }
                scoresToUpdate = scores;
                //updates.AlternateGoalChanged=true is above.
            }

            //Accountable User
            updates.OriginalAccountableUserId = measurable.AccountableUserId;
            if (accountableId != null && measurable.AccountableUserId != accountableId.Value) {
                perms.ViewUserOrganization(accountableId.Value, false);
                var user = s.Get<UserOrganizationModel>(accountableId.Value);

                measurable.AccountableUserId = accountableId.Value;
                measurable.AccountableUser = user;

                updates.AccountableUserChanged = true;
            }

            //Admin User
            updates.OriginalAdminUserId = measurable.AdminUserId;
            if (adminId != null) {
                perms.ViewUserOrganization(adminId.Value, false);
                var user = s.Get<UserOrganizationModel>(adminId.Value);

                measurable.AdminUserId = adminId.Value;
                measurable.AdminUser = user;

                updates.AdminUserChanged = true;
            }

            //User type
            if (unitType != null && measurable.UnitType != unitType.Value) {
                measurable.UnitType = unitType.Value;
                s.Update(measurable);
                var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
                var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
                scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
                var scores = scoresQ.List().ToList();
                scoresToUpdate = scores;

                updates.UnitTypeChanged = true;
            }

            await HooksRegistry.Each<IMeasurableHook>((ses, x) => x.UpdateMeasurable(ses, perms.GetCaller(), measurable, scoresToUpdate, updates));
        }


        public static async Task<ScoreModel> UpdateScore(UserOrganizationModel caller, long scoreId, decimal? value) {
            return await UpdateScore(caller, scoreId, 0, DateTime.MinValue, value);
        }

        public static async Task<ScoreModel> UpdateScore(UserOrganizationModel caller, long measurableId, DateTime week, decimal? value) {
            return await UpdateScore(caller, 0, measurableId, week, value);
        }

        public static async Task<ScoreModel> UpdateScore(UserOrganizationModel caller, long scoreId, long measurableId, DateTime week, decimal? value) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var score = await UpdateScore(s, perms, scoreId, measurableId, week, value);
                    tx.Commit();
                    s.Flush();
                    return score;
                }
            }
        }

        public static async Task<ScoreModel> UpdateScore(ISession s, PermissionsUtility perms, long measurableId, DateTime week, decimal? value) {
            return await UpdateScore(s, perms, 0, measurableId, week, value);
        }

        public static async Task<ScoreModel> UpdateScore(ISession s, PermissionsUtility perms, long scoreId, long measurableId, DateTime week, decimal? value) {
            if (scoreId <= 0)
                scoreId = (await GetScore(s, perms, measurableId, week)).Id;
            return await UpdateScore(s, perms, scoreId, value);
        }

        public static async Task<ScoreModel> UpdateScore(ISession s, PermissionsUtility perms, long scoreId, decimal? value) {
            perms.EditScore(scoreId);
            SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(scoreId));
            return await UpdateScore_Unsafe(s, scoreId, value);
        }

        protected class ScoreUpdates {
            public ScoreUpdates(ScoreModel score, decimal? value) {
                Score = score;
                Value = value;
            }

            public ScoreModel Score { get; set; }
            public decimal? Value { get; set; }
            public bool Calculated { get; set; }

        }

        protected static async Task<ScoreModel> UpdateScore_Unsafe(ISession s, long scoreId, decimal? value, DateTime? absoluteUpdateTime = null) {
            var updates = new List<ScoreUpdates> {
                new ScoreUpdates(s.Get<ScoreModel>(scoreId),value)
            };
            return (await UpdateScore_Unsafe(s, updates, absoluteUpdateTime)).First();
        }

        protected static async Task<List<ScoreModel>> UpdateScore_Unsafe(ISession s, List<ScoreUpdates> scoreUpdates, DateTime? absoluteUpdateTime = null) {
            var o = new List<ScoreModel>();
            absoluteUpdateTime = absoluteUpdateTime ?? HibernateSession.GetDbTime(s);
            var updateLater = new List<ScoreAndUpdates>();
            foreach (var scoreUpdate in scoreUpdates) {
                var score = scoreUpdate.Score;
                var value = scoreUpdate.Value;
                var updates = new IScoreHookUpdates();
                // var score = s.Get<ScoreModel>(scoreId);
                if (score.Measured != value) {
                    if (value == null)
                        score.DateEntered = null;
                    else
                        score.DateEntered = DateTime.UtcNow;
                    score.Measured = value;
                    updates.ValueChanged = true;
                }

                updates.AbsoluteUpdateTime = absoluteUpdateTime.Value;
                s.Update(score);

                updates.Calculated = scoreUpdate.Calculated;
                updateLater.Add(new ScoreAndUpdates {
                    score = score,
                    updates = updates
                });
                o.Add(score);
            }
            if (updateLater.Any()) {
                await HooksRegistry.Each<IScoreHook>((ses, x) => x.UpdateScores(ses, updateLater));
            }
            return o;
        }

        #endregion

        #region Delete Measurable
        public static async Task DeleteMeasurable(ISession s, PermissionsUtility perms, long measurableId) {
            perms.EditMeasurable(measurableId);
            var m = s.Get<MeasurableModel>(measurableId);
            m.DeleteTime = null;
            m.Archived = true;
            s.Update(m);

            await HooksRegistry.Each<IMeasurableHook>((ses, x) => x.DeleteMeasurable(s, m));
        }

        public static void UndeleteMeasurable(UserOrganizationModel caller, long measurableId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).EditMeasurable(measurableId);
                    var m = s.Get<MeasurableModel>(measurableId);
                    m.DeleteTime = null;
                    m.Archived = false;
                    s.Update(m);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static void RemoveAdmin(UserOrganizationModel caller, long measurableId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).EditMeasurable(measurableId);
                    var m = s.Get<MeasurableModel>(measurableId);
                    m.AdminUserId = m.AccountableUserId;
                    s.Update(m);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        #endregion

        public Csv Listing(UserOrganizationModel caller, long organizationId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

                    var scores = s.QueryOver<ScoreModel>()
                        .Where(x => x.DeleteTime == null && x.OrganizationId == organizationId)
                        .List().ToList();

                    var data = ScorecardData.FromScores(scores);
                    var csv = ExportAccessor.GenerateScorecardCsv("Measurable", data);
                    return csv;
                }
            }
        }

        protected class FormulaVariable {
            public long MeasurableId { get; set; }
            public int Offset { get; set; }
            public string Variable { get; set; }
            //public DateRange Range { get; set; }
        }

        protected static List<FormulaVariable> GetVariables(FormulaUtility.ParsedFormula formula) {

            return formula.GetVariables().Select(x => {
                var split = x.Split('(');
                var offset = 0;
                var mid = long.Parse(split[0]);
                if (split.Count() > 1) {
                    offset = int.Parse(split[1].SplitAndTrim(')', ',')[0]);
                }
                //var r = new DateRange(range.StartTime, range.EndTime);
                //r.StartTime = TimingUtility.PeriodsFromNow(r.StartTime, offset, period);
                //r.EndTime = TimingUtility.PeriodsFromNow(r.EndTime, offset, period);

                //if (r.StartTime > DateTime.MinValue.AddDays(7))
                //    r.StartTime = r.StartTime.AddDays(-7);

                //if (r.EndTime < DateTime.MaxValue.AddDays(-7))
                //    r.EndTime = r.EndTime.AddDays(7);


                //fullRange.StartTime = Math2.Min(r.StartTime, fullRange.StartTime);
                //fullRange.EndTime = Math2.Max(r.EndTime, fullRange.EndTime);

                return new FormulaVariable { Variable = x, MeasurableId = mid, Offset = offset };
            }).ToList();
        }

        public static async Task SetFormula(UserOrganizationModel caller, long measurableId, string formula) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    
                    await SetFormula(s, perms, measurableId, formula);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static async Task SetFormula(ISession s, PermissionsUtility perms, long measurableId, string formula) {

            perms.EditMeasurable(measurableId);
            var parsed = FormulaUtility.Parse(formula);
            var variables = GetVariables(parsed);

            foreach (var v in variables) {
                perms.ViewMeasurable(v.MeasurableId);
            }


            var measurable = s.Get<MeasurableModel>(measurableId);
            var oldVariables = GetVariables(FormulaUtility.Parse(measurable.Formula ?? ""));

            measurable.Formula = formula;
            ConfirmNoCircularRefs(s, measurable);
            s.Update(measurable);

            //update all backreferences
            var addRemove = SetUtility.AddRemove(oldVariables.Select(x => x.MeasurableId).Distinct(), variables.Select(x => x.MeasurableId).Distinct());
            foreach (var v in addRemove.RemovedValues) {
                var m = s.Get<MeasurableModel>(v);
                var list = m.BackReferenceMeasurables.ToList();
                list.RemoveAll(x => x == measurableId);
                m.BackReferenceMeasurables = list.Distinct().ToArray();
                s.Update(m);
            }

            foreach (var v in addRemove.AddedValues) {
                var m = s.Get<MeasurableModel>(v);
                var list = m.BackReferenceMeasurables.ToList();
                list.Add(measurableId);
                m.BackReferenceMeasurables = list.Distinct().ToArray();
                s.Update(m);
            }

            await UpdateAllCalculatedScores_Unsafe(s, measurable);
        }

        protected static void _ConfirmNoCircularRefs(ISession s, MeasurableModel m, List<Node> existingNodes, List<long> visitedMeasurables) {

            var backNodes = m.BackReferenceMeasurables
                                    .Select(x => new Node() { ParentId = m.Id, Id = x })
                                    .ToList();

            //var alreadySeen = seen.Any(x => backNodes.Any(y => y.Id == x.Id && y.ParentId == x.ParentId));
            //if (alreadySeen)
            //    throw new Exception("A circular reference was found.");
            //existingNodes.AddRange(backNodes);            

            var forwardNodes = GetVariables(FormulaUtility.Parse(m.Formula))
                                    .Select(x => new Node() { Id = m.Id, ParentId = x.MeasurableId })
                                    .ToList();

            //alreadySeen = seen.Any(x => backNodes.Any(y => y.Id == x.Id && y.ParentId == x.ParentId));
            //if (alreadySeen)
            //    throw new Exception("A circular reference was found.");
            var potentialNewNodes = backNodes.ToList();
            potentialNewNodes.AddRange(forwardNodes);

            existingNodes.AddRange(potentialNewNodes);

            var circular = GraphUtility.HasCircularDependency(existingNodes);
            if (circular)
                throw new PermissionsException("Formula Error: circular reference found.") {
                    NoErrorReport = true
                };
            visitedMeasurables.Add(m.Id);

            var allNewMeasurableIds = potentialNewNodes
                .SelectMany(x => new long?[] { x.Id, x.ParentId })
                .Where(x => x != null)
                .Select(x => x.Value)
                .Where(x => !visitedMeasurables.Any(y => y == x))
                .ToList();

            var newMeasurables = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(allNewMeasurableIds).List().ToList();
            foreach (var meas in newMeasurables) {
                _ConfirmNoCircularRefs(s, meas, existingNodes, visitedMeasurables);
            }

        }

        protected static void ConfirmNoCircularRefs(ISession s, MeasurableModel editedMeasurable) {
            //var measurableIds = variables.Select(x => x.MeasurableId).Distinct().ToList();
            //measurableIds.Add(measurableId);
            //var measurables = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(measurableIds).List().ToList();
            _ConfirmNoCircularRefs(s, editedMeasurable, new List<Node>(), new List<long>());
        }

        [Obsolete("Expensive")]
        public static async Task UpdateAllCalculatedScores_Unsafe(ISession s, MeasurableModel measurable) {
            var measurableId = measurable.Id;
            var formula = measurable.Formula;
            if (string.IsNullOrWhiteSpace(formula))
                return;


            //perms.ViewMeasurable(measurableId);
            var parsed = FormulaUtility.Parse(formula);
            //var variables = parsed.GetVariables();

            //need to get all the relavent scores..
            //var fullRange = DateRange.CopyFrom(range);
            var measurables = GetVariables(parsed);



            var uniqueMeasurableIds = measurables.Select(x => x.MeasurableId).Union(new[] { measurableId }).Distinct().ToList();

            //foreach (var mid in uniqueMeasurableIds)
            //    perms.ViewMeasurable(mid);

            var scores = s.QueryOver<ScoreModel>()
                .Where(x => x.DeleteTime == null)
                .WhereRestrictionOn(x => x.MeasurableId).IsIn(uniqueMeasurableIds)
                .List().ToList();

            var scoreLookup = scores.GroupBy(x => x.MeasurableId).ToDictionary(
                                x => x.Key,
                                x => x.ToDefaultDictionary(
                                        y => TimingUtility.GetWeekSinceEpoch(y.ForWeek),
                                        y => (double?)y.Measured,
                                        y => null
                                   )
                             );

            var measurableLookup = measurables.Distinct(x=>x.Variable).ToDictionary(x => x.Variable, x => x);

            var i = DateTime.UtcNow;
            var end = DateTime.UtcNow;

            if (scores.Any()) {
                i = scores.Min(x => x.ForWeek);
                end = scores.Max(x => x.ForWeek);
            }

            //var minOffset = 0;
            //var maxOffset = 0;

            //if (measurables.Any()) {
            //    minOffset = measurables.Min(x => x.Offset);
            //    maxOffset = measurables.Max(x => x.Offset);
            //}

            //i = Math2.Min(i, TimingUtility.PeriodsFromNow(i, minOffset, ScorecardPeriod.Weekly));
            //end = Math2.Max(i, TimingUtility.PeriodsFromNow(end, maxOffset, ScorecardPeriod.Weekly));

            i = TimingUtility.ToScorecardDate(i);
            end = TimingUtility.ToScorecardDate(end);

            var allMeasurableScores = scores.Where(x => x.MeasurableId == measurableId).ToList();
            var gen = await _GenerateScoreModels_AddMissingScores_Unsafe(s, new DateRange(i, end), measurableId.AsList(), allMeasurableScores);
            allMeasurableScores.AddRange(gen);

            var allMeasurableScoresLookup = allMeasurableScores.ToDictionary(x => x.ForWeek, x => x);

            var updates = new List<ScoreUpdates>();
            while (i <= end) {
                var update = GenerateUpdateForCalculatedScore_Unsafe(parsed, scoreLookup, measurableLookup, TimingUtility.GetWeekSinceEpoch(i), allMeasurableScoresLookup[i]);
                updates.Add(update);
                i = TimingUtility.ToScorecardDate(TimingUtility.PeriodsFromNow(i, 1, ScorecardPeriod.Weekly));
            }

            await UpdateScore_Unsafe(s, updates);
        }
        /// <summary>
        /// Perform one-off recalculation
        /// </summary>
        /// <param name="parsed">the parsed formula</param>
        /// <param name="scoreLookup">lookup containing all score needed to evaluate this cell. [measurableId][weekId] </param>
        /// <param name="variableLookup">lookup containing parsed variable information. [variableStr] </param>
        /// <param name="weekId">current week</param>
        /// <param name="scoreToUpdate">the score to update with calculated value</param>
        /// <returns></returns>
        private static ScoreUpdates GenerateUpdateForCalculatedScore_Unsafe(FormulaUtility.ParsedFormula parsed, Dictionary<long, DefaultDictionary<long, double?>> scoreLookup, Dictionary<string, FormulaVariable> variableLookup, long weekId, ScoreModel scoreToUpdate) {
            try {
                var value = parsed.Evaluate(variable => {
                    var item = variableLookup[variable];
                    return scoreLookup[item.MeasurableId][weekId + item.Offset];
                });
                if (value.HasValue && double.IsNaN(value.Value))
                    value = null;

                return new ScoreUpdates(scoreToUpdate, (decimal?)value) {
                    Calculated = true
                };
            } catch (InvalidOperationException e) {
                throw new PermissionsException("Formula Error: " + e.Message, true) {
                    NoErrorReport = true,
                };
            }
        }

        public static async Task UpdateTheseCalculatedScores_Unsafe(ISession s, List<ScoreModel> scores) {

            var measurableToUpdate = scores.Distinct(x => x.MeasurableId).Select(x => x.Measurable).ToList();
            var measurableLookup = measurableToUpdate.ToDictionary(x => x.Id, x => x);
            var variablesLookup = new DefaultDictionary<long, ParsedFormula>(x => FormulaUtility.Parse(measurableLookup[x].Formula));

            var dataNeeded = scores.SelectMany(c => {
                return GetVariables(variablesLookup[c.MeasurableId])
                            .Select(x => new {
                                measurableId = x.MeasurableId,
                                weekId = TimingUtility.GetWeekSinceEpoch(c.ForWeek) + x.Offset
                            });
            }).Distinct().ToList();


            //Get queries for data needed for calculations
            IEnumerable<ScoreModel> actualScoreDataQ;
            {
                var criteria = s.CreateCriteria<ScoreModel>();
                var ors = Restrictions.Disjunction();
                foreach (var cell in dataNeeded) {
                    var ands = Restrictions.Conjunction();
                    ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.ForWeek), TimingUtility.GetDateSinceEpoch(cell.weekId)));
                    ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.MeasurableId), cell.measurableId));
                    ors.Add(ands);
                }
                criteria.Add(ors);
                actualScoreDataQ = criteria.Future<ScoreModel>();
                //.SetProjection(
                //    Projections.Property<ScoreModel>(x => x.ForWeek),
                //    Projections.Property<ScoreModel>(x => x.MeasurableId),
                //    Projections.Property<ScoreModel>(x => x.Measured)
                //)
            }
            var actualScores = actualScoreDataQ.ToList();
           


            var genOnlyData = dataNeeded.Select(x => Tuple.Create(TimingUtility.GetDateSinceEpoch(x.weekId), x.measurableId)).Distinct().ToList();
            var gen = await _GenerateScoreModels_AddMissingScores_Unsafe(s, genOnlyData, actualScores);
            actualScores.AddRange(gen);

            var actualData = actualScores.Select(x => new {
                week = TimingUtility.GetWeekSinceEpoch(x.ForWeek),//(DateTime)x[0]),
                measurableId = x.MeasurableId,//(long)x[1],
                measured = x.Measured,//(decimal?)x[2]
            }).ToList();

            var scoreLookup = actualData.GroupBy(x => x.measurableId).ToDictionary(x => x.Key, x => x.ToDefaultDictionary(y => y.week, y => (double?)y.measured, y => null));

            var dbUpdates = new List<ScoreUpdates>();

            foreach (var u in scores) {
                var parsed = variablesLookup[u.MeasurableId];
                var variables = GetVariables(parsed).ToDictionary(x => x.Variable, x => x);
                var theScore = u;//actualScores.SingleOrDefault(x => TimingUtility.GetWeekSinceEpoch(x.ForWeek) == u.weekId && x.MeasurableId == u.measurableId);
                if (theScore != null) {
                    var dbUpdate = GenerateUpdateForCalculatedScore_Unsafe(parsed, scoreLookup, variables, TimingUtility.GetWeekSinceEpoch(u.ForWeek), theScore);
                    dbUpdates.Add(dbUpdate);
                }
            }
            await UpdateScore_Unsafe(s, dbUpdates);
        }

        /// <summary>
        /// When these scores are updated, also update their dependencies
        /// </summary>
        /// <param name="s"></param>
        /// <param name="scores"></param>
        /// <returns></returns>
        public static async Task UpdateCalculatedScores_FromUpdatedScore_Unsafe(ISession s, List<ScoreModel> scores) {

            if (!scores.Any())
                return;

            //Get all measurables that need updating.
            var measurableToUpdate = s.QueryOver<MeasurableModel>()
                .WhereRestrictionOn(x => x.Id).IsIn(scores.SelectMany(x=>x.Measurable.BackReferenceMeasurables).Distinct().ToList())
                .List().ToList();


            var measurableLookup = measurableToUpdate.ToDictionary(x => x.Id, x => x);
            var variablesLookup = new DefaultDictionary<long, ParsedFormula>(x => FormulaUtility.Parse(measurableLookup[x].Formula));

            //Get all cells needing updates
            var cellsToUpdate = scores.SelectMany(score => {
                var curMeasurable = score.MeasurableId;
                var curWeek = TimingUtility.GetWeekSinceEpoch(score.ForWeek);
                return measurableToUpdate.Where(m => m.Formula != null).SelectMany(m => {
                    var variables = GetVariables(variablesLookup[m.Id]);
                    return variables.Where(x => x.MeasurableId == curMeasurable).Select(x => new {
                        measurableId = m.Id,
                        weekId = curWeek - x.Offset,
                    });
                });
            }).Distinct().ToList();

            //Get all data needed to update cells
            var dataNeeded = cellsToUpdate.SelectMany(c => {
                return GetVariables(variablesLookup[c.measurableId]).Select(x => new { measurableId = x.MeasurableId, weekId = c.weekId + x.Offset });
            }).Distinct().ToList();


            //Get queries for cells to update
            IEnumerable<ScoreModel> actualScoresToUpdateQ;
            {
                var criteria = s.CreateCriteria<ScoreModel>();
                criteria.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.DeleteTime),null));
                var ors = Restrictions.Disjunction();
                foreach (var cell in cellsToUpdate) {
                    var ands = Restrictions.Conjunction();
                    ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.ForWeek), TimingUtility.GetDateSinceEpoch(cell.weekId)));
                    ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.MeasurableId), cell.measurableId));
                    ors.Add(ands);
                }
                criteria.Add(ors);
                actualScoresToUpdateQ = criteria.Future<ScoreModel>();
            }
            //Get queries for data needed for calculations
            IEnumerable<object[]> actualScoreDataQ;
            {
                var criteria = s.CreateCriteria<ScoreModel>();
                criteria.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.DeleteTime),null));
                var ors = Restrictions.Disjunction();
                foreach (var cell in dataNeeded) {
                    var ands = Restrictions.Conjunction();
                    ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.ForWeek), TimingUtility.GetDateSinceEpoch(cell.weekId)));
                    ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.MeasurableId), cell.measurableId));
                    ors.Add(ands);
                }
                criteria.Add(ors);
                actualScoreDataQ = criteria.SetProjection(
                                            Projections.Property<ScoreModel>(x => x.ForWeek),
                                            Projections.Property<ScoreModel>(x => x.MeasurableId),
                                            Projections.Property<ScoreModel>(x => x.Measured)
                                        ).Future<object[]>();
            }

            var actualScores = actualScoresToUpdateQ.ToList();

            //Generate some missing datas...
            var datesNeeded = dataNeeded.Select(x => TimingUtility.GetDateSinceEpoch(x.weekId)).ToList();
            datesNeeded.AddRange(cellsToUpdate.Select(x => TimingUtility.GetDateSinceEpoch(x.weekId)));
            var measurablesNeeded = dataNeeded.Select(x => x.measurableId).ToList();
            measurablesNeeded.AddRange(cellsToUpdate.Select(x => x.measurableId));

            var genOnlyData = cellsToUpdate.Select(x => Tuple.Create(TimingUtility.GetDateSinceEpoch(x.weekId), x.measurableId)).Distinct().ToList();
            var gen = await _GenerateScoreModels_AddMissingScores_Unsafe(s, genOnlyData, actualScores);
            actualScores.AddRange(gen);

            var actualData = actualScoreDataQ.Select(x => new {
                week = TimingUtility.GetWeekSinceEpoch((DateTime)x[0]),
                measurableId = (long)x[1],
                measured = (decimal?)x[2]
            }).ToList();

            var scoreLookup = actualData.GroupBy(x => x.measurableId).ToDictionary(x => x.Key, x => x.ToDefaultDictionary(y => y.week, y => (double?)y.measured, y => null));

            var dbUpdates = new List<ScoreUpdates>();

            foreach (var u in cellsToUpdate) {
                var parsed = variablesLookup[u.measurableId];
                var variables = GetVariables(parsed).Distinct(x=>x.Variable).ToDictionary(x => x.Variable, x => x);
                var theScore = actualScores.SingleOrDefault(x => TimingUtility.GetWeekSinceEpoch(x.ForWeek) == u.weekId && x.MeasurableId == u.measurableId);

                if (theScore != null) {
                    var dbUpdate = GenerateUpdateForCalculatedScore_Unsafe(parsed, scoreLookup, variables, u.weekId, theScore);
                    dbUpdates.Add(dbUpdate);
                } else {
                    var a = 0;
                }
            }

            await UpdateScore_Unsafe(s, dbUpdates);

            //Get all variables needed for updates.
            //var toUpdate = measurableToUpdate.Where(m=> m.Formula !=null).SelectMany(m => {
            //    var parsed = FormulaUtility.Parse(m.Formula);
            //    return GetVariables(parsed).Select(v=>new { parsed, toUpdateweek = curWeek - v.Offset, measurableId = m.Id, variable=v});                
            //});

            ////Generate queries
            ////Scores needed for updates


            ////Scores needed for calculations 
            //var scoresMeasuredQ = toUpdate.Select(u => new {
            //   measuredQ = s.QueryOver<ScoreModel>()
            //                .Where(x => x.DeleteTime == null &&
            //                            x.ForWeek == TimingUtility.GetDateSinceEpoch(u.week) && 
            //                            x.MeasurableId == u.measurableId)
            //                .Select(x => x.Measured)
            //                .FutureValue<decimal?>(),
            //   update = u
            //});
            ////var scoresQ = toUpdate.Select(u => new {
            ////    s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.ForWeek == TimingUtility.GetDateSinceEpoch(u.week)
            ////    });

            ////[MeasurableId][weekId]
            //var scoreLookup_mid_week = scoresMeasuredQ.GroupBy(x=>x.update.measurableId).ToDictionary(x=>x.Key,x=>x.ToDictionary(y=>y.update.week,y=>y.measuredQ.Value));
            //var variableLookup = toUpdate.ToDictionary(x => x.variable.Variable, x => x);




        }


        //public async Task<List<ScoreModel>> EvaluateCalculatedRow(UserOrganizationModel caller, long measurableId) {

        //    using (var s = HibernateSession.GetCurrentSession()) {
        //        using (var tx = s.BeginTransaction()) {
        //            var perms = PermissionsUtility.Create(s, caller);
        //            perms.ViewMeasurable(measurableId);
        //            var measurable = s.Get<MeasurableModel>(measurableId);

        //            range = range ?? DateRange.Full();
        //            if (range.StartTime > DateTime.MinValue.AddDays(7))
        //                range.StartTime = range.StartTime.AddDays(-7);

        //            if (range.EndTime < DateTime.MaxValue.AddDays(-7))
        //                range.EndTime = range.EndTime.AddDays(7);

        //            if (measurable.Formula == null) {
        //                return s.QueryOver<ScoreModel>()
        //                    .Where(x => x.MeasurableId == measurableId && x.DeleteTime == null)
        //                    .Where(range.Filter<ScoreModel>(x => x.ForWeek))
        //                    .List().ToList();
        //            } else {
        //                adf 
        //            }
        //        }
        //    }
        //    var formula = "5+5";
        //    var calculation = Calculator.Calculate(formula);
        //    return null;
        //}


        #region Removed

        #region Deprecated
        [Obsolete("Avoid using")]
        [Untested("attach to meeting")]
        public static async Task EditMeasurables(UserOrganizationModel caller, long userId, List<MeasurableModel> measurables, bool updateAllL10s) {
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

                                    await L10Accessor.AttachMeasurable(s, perm, o.Id, r1.Id, true, now: now);

                                    //await L10Accessor.AddMeasurable(s, perm, rt, o.Id, new Controllers.L10Controller.AddMeasurableVm() {
                                    //	RecurrenceId = o.Id,
                                    //	SelectedMeasurable = r1.Id,
                                    //});
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

        [Obsolete("Avoid using")]
        public static async Task<AngularRecurrence> GetReview_Scorecard(UserOrganizationModel caller, long reviewId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return await GetReview_Scorecard(s, perms, reviewId);
                }
            }
        }

        [Obsolete("Avoid using")]
        public static async Task<AngularRecurrence> GetReview_Scorecard(ISession s, PermissionsUtility perms, long reviewId) {

            var review = ReviewAccessor.GetReview(s, perms, reviewId, false, false);
            var start = review.DueDate.AddDays(-7 * 13);
            var end = review.DueDate.AddDays(14);

            var scorecard = await GetAngularScorecardForUser(s, perms, review.ReviewerUserId, new DateRange(start, end), includeNextWeek: true, now: review.DueDate);
            foreach (var m in scorecard.Measurables) {
                m.Disabled = true;
            }
            foreach (var ss in scorecard.Scores) {
                ss.Disabled = true;
                if (ss.Measurable != null)
                    ss.Measurable.Disabled = true;
            }

            var container = new AngularRecurrence(-1) {
                Scorecard = scorecard,
                date = new AngularDateRange() {
                    startDate = start,
                    endDate = end
                }
            };
            return container;
        }

        public static async Task<List<MeasurableModel>> Search(UserOrganizationModel caller, long orgId, string search, long[] excludeLong = null, int take = int.MaxValue) {
            excludeLong = excludeLong ?? new long[] { };

            var visible = ScorecardAccessor.GetVisibleMeasurables(caller, orgId, true)
                .Where(x => !excludeLong.Any(y => y == x.Id))
                .Where(x => x.Id > 0);


            var splits = search.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var dist = new DiscreteDistribution<MeasurableModel>(0, 9, true);



            foreach (var u in visible) {

                var fname = false;
                var lname = false;
                var ordered = false;
                var fnameStart = false;
                var lnameStart = false;
                var wasFirst = false;
                var exactFirst = false;
                var exactLast = false;
                var containsText = false;

                var names = new List<string[]>();
                names.Add(new string[] {
                    u.AccountableUser.GetFirstName().ToLower(),
                    u.AccountableUser.GetLastName().ToLower(),
                });
                if (u.AccountableUserId != u.AdminUserId) {
                    names.Add(new string[] {
                        u.AdminUser.GetFirstName().ToLower(),
                        u.AdminUser.GetLastName().ToLower(),
                    });
                }

                foreach (var n in names) {
                    var f = n[0];
                    var l = n[1];
                    foreach (var t in splits) {
                        if (f.Contains(t))
                            fname = true;
                        if (f == t)
                            exactFirst = true;
                        if (f.StartsWith(t))
                            fnameStart = true;
                        if (l.Contains(t))
                            lname = true;
                        if (l.StartsWith(t))
                            lnameStart = true;
                        if (fname && !wasFirst && lname)
                            ordered = true;
                        if (l == t)
                            exactLast = true;

                        if (u.Title != null && u.Title.ToLower().Contains(t))
                            containsText = true;

                        wasFirst = true;
                    }
                }

                var score = fname.ToInt() + lname.ToInt() + ordered.ToInt() + fnameStart.ToInt() + lnameStart.ToInt() + exactFirst.ToInt() + exactLast.ToInt() + containsText.ToInt() * 2;
                if (score > 0)
                    dist.Add(u, score);
            }
            return dist.GetProbabilities().OrderByDescending(x => x.Value).Select(x => x.Key).Take(take).ToList();
        }


        public static CreateMeasurableViewModel BuildCreateMeasurableVM(UserOrganizationModel caller, dynamic ViewBag, List<SelectListItem> potentialUsers = null) {

            if (potentialUsers == null) {
                potentialUsers = TinyUserAccessor.GetOrganizationMembers(caller, caller.Organization.Id, false).Select((x, i) => new SelectListItem() {
                    Selected = i == 0 || x.UserOrgId == caller.Id,
                    Text = x.Name,
                    Value = "" + x.UserOrgId,
                }).OrderBy(x => x.Text).ToList();
            }

            if (!potentialUsers.Any())
                throw new PermissionsException("No users");

            var selected = potentialUsers.LastOrDefault(x => x.Selected);
            if (selected == null)
                selected = potentialUsers.First();

            return new CreateMeasurableViewModel() {
                AccountableUser = selected.Value.ToLong(),
                PotentialUsers = potentialUsers,
            };
        }

        #endregion

        //[Obsolete("Use UpdateScore", true)]
        //public static ScoreModel UpdateScoreInMeeting(ISession s, PermissionsUtility perms, long recurrenceId, long scoreId, DateTime week, long measurableId, decimal? value, string dom, string connectionId) {
        //	var now = DateTime.UtcNow;
        //	DateTime? nowQ = now;

        //	perms.EditL10Recurrence(recurrenceId);

        //	var meeting = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId);
        //	var score = s.Get<ScoreModel>(scoreId);


        //	if (score != null && score.DeleteTime == null && false) {
        //		TestUtilities.Log("Score found. Updating.");

        //		SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(scoreId));

        //		//Editable in this meeting?
        //		var ms = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
        //			.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == score.MeasurableId)
        //			.SingleOrDefault<L10Meeting.L10Meeting_Measurable>();
        //		if (ms == null)
        //			throw new PermissionsException("You do not have permission to edit this score.");

        //		var all = s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == score.MeasurableId && x.ForWeek == score.ForWeek).List().ToList();
        //		foreach (var sc in all) {
        //			sc.Measured = value;
        //			sc.DateEntered = (value == null) ? null : (DateTime?)now;
        //			s.Update(sc);
        //		}
        //		//L10Accessor._RecalculateCumulative_Unsafe(s, score.Measurable, score);
        //	} else {
        //		var meetingMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
        //			.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == measurableId)
        //			.SingleOrDefault<L10Meeting.L10Meeting_Measurable>();

        //		if (meetingMeasurables == null)
        //			throw new PermissionsException("You do not have permission to edit this score.");
        //		var m = meetingMeasurables.Measurable;

        //		var existingScores = s.QueryOver<ScoreModel>()
        //			.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
        //			.List().ToList();

        //		//adjust week..
        //		week = week.StartOfWeek(DayOfWeek.Sunday);

        //		//See if we can find it given week.
        //		var scores = existingScores.OrderBy(x => x.Id).Where(x => (x.ForWeek == week)).ToList();
        //		if (scores.Any()) {
        //			TestUtilities.Log("Found one or more score. Updating All.");

        //			foreach (var sc in scores) {
        //				SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(sc.Id));
        //				if (sc.Measured != value) {
        //					sc.Measured = value;
        //					sc.DateEntered = (value == null) ? null : (DateTime?)now;
        //					s.Update(sc);
        //				}
        //				score = sc;
        //			}
        //			//L10Accessor._RecalculateCumulative_Unsafe(s, score.Measurable, score);
        //		} else {
        //			var ordered = existingScores.OrderBy(x => x.DateDue);
        //			var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;
        //			var maxDate = ordered.LastOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;

        //			minDate = minDate.StartOfWeek(DayOfWeek.Sunday);
        //			maxDate = maxDate.StartOfWeek(DayOfWeek.Sunday);


        //			//DateTime start, end;

        //			if (week > maxDate) {
        //				var scoresCreated = 0;
        //				TestUtilities.Log("Score not found. Score above boundry. Creating scores up to value.");
        //				//Create going up until sufficient
        //				var n = maxDate;
        //				ScoreModel curr = null;
        //				var measurable = s.Get<MeasurableModel>(m.Id);

        //				while (n < week) {
        //					var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);
        //					curr = new ScoreModel() {
        //						AccountableUserId = m.AccountableUserId,
        //						DateDue = nextDue,
        //						MeasurableId = m.Id,
        //						Measurable = measurable,
        //						OrganizationId = m.OrganizationId,
        //						ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday),
        //						OriginalGoal = measurable.Goal,
        //						AlternateOriginalGoal = measurable.AlternateGoal,
        //						OriginalGoalDirection = measurable.GoalDirection
        //					};
        //					s.Save(curr);
        //					scoresCreated++;
        //					m.NextGeneration = nextDue;
        //					n = nextDue.StartOfWeek(DayOfWeek.Sunday);
        //				}
        //				curr.DateEntered = (value == null) ? null : nowQ;
        //				curr.Measured = value;
        //				curr.Measurable = s.Get<MeasurableModel>(curr.MeasurableId);
        //				score = curr;
        //				//L10Accessor._RecalculateCumulative_Unsafe(s, curr.Measurable, curr);
        //				TestUtilities.Log("Scores created: " + scoresCreated);
        //			} else if (week < minDate) {
        //				TestUtilities.Log("Score not found. Score below boundry. Creating scores down to value.");
        //				var n = week;
        //				var first = true;
        //				var scoresCreated = 0;
        //				var measurable = s.Get<MeasurableModel>(m.Id);

        //				while (n < minDate) {
        //					var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime);
        //					var curr = new ScoreModel() {
        //						AccountableUserId = m.AccountableUserId,
        //						DateDue = nextDue,
        //						MeasurableId = m.Id,
        //						Measurable = measurable,
        //						OrganizationId = m.OrganizationId,
        //						ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday),
        //						OriginalGoal = measurable.Goal,
        //						OriginalGoalDirection = measurable.GoalDirection,
        //						AlternateOriginalGoal = measurable.AlternateGoal,
        //					};
        //					if (first) {
        //						curr.Measured = value;
        //						curr.DateEntered = (value == null) ? null : nowQ;
        //						first = false;
        //						s.Save(curr);
        //						scoresCreated++;
        //						score = curr;
        //						//L10Accessor._RecalculateCumulative_Unsafe(s, curr.Measurable, curr);
        //					}

        //					//m.NextGeneration = nextDue;
        //					n = nextDue.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
        //					curr.Measurable = s.Get<MeasurableModel>(curr.MeasurableId);

        //				}
        //				TestUtilities.Log("Scores created: " + scoresCreated);
        //			} else {
        //				TestUtilities.Log("Score not found. Score inside boundry. Creating score.");
        //				// cant create scores between these dates..
        //				var measurable = s.Get<MeasurableModel>(m.Id);
        //				var curr = new ScoreModel() {
        //					AccountableUserId = m.AccountableUserId,
        //					DateDue = week.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime),
        //					MeasurableId = m.Id,
        //					Measurable = measurable,
        //					OrganizationId = m.OrganizationId,
        //					ForWeek = week.StartOfWeek(DayOfWeek.Sunday),
        //					Measured = value,
        //					DateEntered = (value == null) ? null : nowQ,
        //					OriginalGoal = measurable.Goal,
        //					AlternateOriginalGoal = measurable.AlternateGoal,
        //					OriginalGoalDirection = measurable.GoalDirection
        //				};
        //				s.Save(curr);
        //				//L10Accessor._RecalculateCumulative_Unsafe(s, curr.Measurable, curr);

        //				curr.Measurable = s.Get<MeasurableModel>(curr.MeasurableId);
        //				score = curr;
        //				TestUtilities.Log("Scores created: 1");
        //			}
        //			s.Update(m);

        //		}
        //	}
        //	var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
        //	var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting), connectionId);
        //	group.updateTextContents(dom, value);

        //	if (score != null) {
        //		using (var rt = RealTimeUtility.Create()) {
        //			L10Accessor._RecalculateCumulative_Unsafe(s, rt, score.Measurable, recurrenceId.AsList(), score);
        //			rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(x => x.updateCumulative(score.Measurable.Id, score.Measurable._Cumulative.NotNull(y => y.Value.ToString("0.#####"))));
        //		}

        //		var toUpdate = new AngularScore(score, false);
        //		toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
        //		toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();
        //		group.update(new AngularUpdate() { toUpdate });
        //	}

        //	Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateScoreInMeeting", ForModel.Create(score), score.NotNull(x => x.Measurable.NotNull(y => y.Title)) + " updated to " + value);
        //	return score;
        //}

        //[Obsolete("Use UpdateScore", true)]
        //public static ScoreModel UpdateScoreInMeeting(UserOrganizationModel caller, long recurrenceId, long scoreId, DateTime week, long measurableId, decimal? value, string dom, string connectionId) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {

        //			var perms = PermissionsUtility.Create(s, caller);
        //			var output = UpdateScoreInMeeting(s, perms, recurrenceId, scoreId, week, measurableId, value, dom, connectionId);

        //			tx.Commit();
        //			s.Flush();
        //			return output;
        //		}
        //	}
        //}

        //public static List<ScoreModel> GetScores(UserOrganizationModel caller, long organizationId, DateTime start, DateTime end, bool loadUsers) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(organizationId);
        //			var scores = s.QueryOver<ScoreModel>();
        //			if (loadUsers)
        //				scores = scores.Fetch(x => x.AccountableUser).Eager;
        //			return scores.Where(x => x.OrganizationId == organizationId && x.DateDue >= start && x.DateDue <= end).List().ToList();
        //		}
        //	}
        //}
        //public static List<ScoreModel> GetUserScoresIncomplete(UserOrganizationModel caller, long userId, DateTime? now = null) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
        //			var nowPlus = (now ?? DateTime.UtcNow).Add(TimeSpan.FromDays(1));
        //			var scorecards = s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == userId && x.DateDue < nowPlus && x.DateEntered == null && x.DeleteTime == null).List().ToList();
        //			scorecards = scorecards.Where(x => x.Measurable.DeleteTime == null).ToList();
        //			return scorecards;
        //		}
        //	}
        //}
        //public static void EditUserScores(UserOrganizationModel caller, List<ScoreModel> scores) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var oldScores = scores.ToList();
        //			scores = s.QueryOver<ScoreModel>().WhereRestrictionOn(x => x.Id).IsIn(scores.Select(x => x.Id).ToArray()).List().ToList();
        //			var now = DateTime.UtcNow;
        //			var uid = scores.EnsureAllSame(x => x.AccountableUserId);
        //			PermissionsUtility.Create(s, caller).EditUserScorecard(uid);
        //			foreach (var x in scores) {
        //				x.Measured = oldScores.FirstOrDefault(y => y.Id == x.Id).NotNull(y => y.Measured);
        //				if (x.Measured == null) {
        //					x.DateEntered = null;
        //					x.DeleteTime = now;
        //				} else {
        //					x.DeleteTime = null;
        //					x.DateEntered = now;
        //				}
        //				s.Update(x);
        //			}
        //			tx.Commit();
        //			s.Flush();
        //		}
        //	}
        //}
        //public static void _RecalculateCumulative_Unsafe(ISession s, RealTimeUtility rt, MeasurableModel measurable, List<long> recurIds, ScoreModel updatedScore = null, bool forceNoSkip = true) {
        //	var recurs = s.QueryOver<L10Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(recurIds).List().ToList();
        //	_RecalculateCumulative_Unsafe(s, rt, measurable.AsList(), recurs, updatedScore);
        //}
        //public static void _RecalculateCumulative_Unsafe(ISession s, RealTimeUtility rt, List<MeasurableModel> measurables, List<L10Recurrence> recurs, ScoreModel updatedScore = null, bool forceNoSkip = true) {
        //	var cumulativeByMeasurable = new Dictionary<long, IEnumerable<object[]>>();
        //	//Grab Cumulative Values
        //	foreach (var mm in measurables.Where(x => x.ShowCumulative && x.Id > 0).Distinct(x => x.Id)) {
        //		cumulativeByMeasurable[mm.Id] = s.QueryOver<ScoreModel>()
        //		.Where(x => x.MeasurableId == mm.Id && x.DeleteTime == null && x.Measured != null && x.ForWeek > mm.CumulativeRange.Value.AddDays(-7))
        //		.Select(x => x.ForWeek, x => x.Measured)
        //		.Future<object[]>();
        //	}
        //	var defaultDay = measurables.FirstOrDefault().NotNull(x => x.Organization.NotNull(y => y.Settings.WeekStart));
        //	//Set Cumulative Values
        //	if (recurs == null || recurs.Count == 0) {
        //		recurs = new List<L10Recurrence>() { null };
        //	}
        //	foreach (var recur in recurs) {
        //		var startOfWeek = defaultDay;
        //		if (recur != null) {
        //			startOfWeek = recur.StartOfWeekOverride ?? recur.Organization.Settings.WeekStart;
        //		}
        //		foreach (var k in cumulativeByMeasurable.Keys) {
        //			foreach (var mm in measurables.Where(x => x.Id == k).ToList()) {
        //				var foundScores = cumulativeByMeasurable[k].Select(x => new {
        //					ForWeek = (DateTime)x[0],
        //					Measured = (decimal?)x[1]
        //				}).Where(x => x.ForWeek > mm.CumulativeRange.Value.AddDays(-(int)startOfWeek)).ToList();
        //				//Use the updated score if we have it.
        //				if (updatedScore != null) {
        //					for (var i = 0; i < foundScores.Count; i++) {
        //						if (updatedScore.ForWeek == foundScores[i].ForWeek)
        //							foundScores[i] = new { ForWeek = updatedScore.ForWeek, Measured = updatedScore.Measured };
        //					}
        //				}

        //				mm._Cumulative = foundScores.GroupBy(x => x.ForWeek)
        //									.Select(x => x.FirstOrDefault(y => y.Measured != null).NotNull(y => y.Measured))
        //									.Where(x => x != null)
        //									.Sum();
        //			}
        //		}
        //	}
        //	if (rt != null) {
        //		foreach (var mm in measurables.Where(x => x.ShowCumulative && x.Id > 0).Distinct(x => x.Id)) {
        //			rt.UpdateRecurrences(recurs.Select(x => x.Id)).UpdateMeasurable(mm, forceNoSkip: forceNoSkip);
        //		}
        //	}

        //} 
        //[Obsolete("Use GetScore", true)]
        //public static ScoreModel GetScoreInMeeting(UserOrganizationModel caller, long scoreId, long recurrenceId) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var perms = PermissionsUtility.Create(s, caller);
        //			var meeting = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId);
        //			var score = s.Get<ScoreModel>(scoreId);


        //			if (score != null && score.DeleteTime == null) {
        //				//Editable in this meeting?
        //				var ms = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
        //					.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == score.MeasurableId)
        //					.SingleOrDefault<L10Meeting.L10Meeting_Measurable>();
        //				if (ms == null)
        //					throw new PermissionsException("You do not have permission to edit this score.");

        //				var a = score.Measurable.AccountableUser.GetName();
        //				var b = score.Measurable.AdminUser.GetName();
        //				var c = score.AccountableUser.GetName();

        //				return score;
        //			}
        //			return null;
        //		}
        //	}
        //}

        //[Obsolete("Do not use", true)]
        //public static async Task CreateMeasurable(UserOrganizationModel caller, MeasurableModel measurable, bool checkEditDetails) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var perms = PermissionsUtility.Create(s, caller);
        //			await CreateMeasurable(s, perms, measurable, checkEditDetails);

        //			tx.Commit();
        //			s.Flush();
        //		}
        //	}
        //}

        //[Untested("hook")]
        //[Obsolete("Do not use", true)]
        //public static async Task CreateMeasurable(ISession s, PermissionsUtility perm, MeasurableModel measurable, bool checkEditDetails) {
        //	//Create new
        //	if (measurable == null)
        //		throw new PermissionsException("You must include a measurable to create.");
        //	if (measurable.OrganizationId == null)
        //		throw new PermissionsException("You must include an organization id.");
        //	if (checkEditDetails) {
        //		perm.EditUserDetails(measurable.AccountableUser.Id);
        //	}
        //	perm.ViewOrganization(measurable.OrganizationId);

        //	perm.ViewUserOrganization(measurable.AccountableUserId, false);
        //	perm.ViewUserOrganization(measurable.AdminUserId, false);

        //	measurable.OrganizationId = measurable.OrganizationId;

        //	measurable.AccountableUser = s.Load<UserOrganizationModel>(measurable.AccountableUserId);
        //	measurable.AdminUser = s.Load<UserOrganizationModel>(measurable.AdminUserId);

        //	s.Save(measurable);

        //	measurable.AccountableUser.UpdateCache(s);
        //	measurable.AdminUser.UpdateCache(s);

        //	await HooksRegistry.Each<IMeasurableHook>((ses, x) => x.CreateMeasurable(ses, measurable));
        //}
        #endregion


    }
}