using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Scorecard;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Base;
using RadialReview.Hubs;
using Microsoft.AspNet.SignalR;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.ViewModels;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Hooks.Realtime.L10 {
    public class Realtime_L10Scorecard : IScoreHook, IMeasurableHook, IMeetingMeasurableHook {

        public bool CanRunRemotely() {
            return false;
        }


        public HookPriority GetHookPriority() {
            return HookPriority.UI;
        }

        public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {

            var groupLookup = new Dictionary<string, dynamic>();
            var updateLookup = new Dictionary<string, List<ScoreAndUpdates>>();
            // var scoresUpdateLookup = new Dictionary<string, AngularScore>();


            foreach (var sau in scoreAndUpdates) {
                var updates = sau.updates;
                var score = sau.score;
                if (updates.ValueChanged) {
                    var connection = RealTimeHelpers.GetConnectionString();
                    if (updates.Calculated)
                        connection = null;

                    var recurIds = RealTimeHelpers.GetRecurrencesForScore(s, score);

                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    var groupIds = recurIds.Select(rid => MeetingHub.GenerateMeetingGroupId(rid)).ToList();

                    groupIds.Add(MeetingHub.GenerateUserId(score.AccountableUserId));
                    groupIds.Add(MeetingHub.GenerateUserId(score.Measurable.AdminUserId));

                    groupIds = groupIds.OrderBy(x => x).ToList();
                    var groupKey = string.Join("##", groupIds) + "###" + connection;
                    if (!groupLookup.ContainsKey(groupKey)) {
                        groupLookup[groupKey] = hub.Clients.Groups(groupIds, connection);// new List<IAngular>();
                    }

                    // var group = groupLookup[groupKey];



                    if (!updateLookup.ContainsKey(groupKey)) {
                        updateLookup[groupKey] = new List<ScoreAndUpdates>();
                    }
                    updateLookup[groupKey].Add(sau);
                }
            }
            foreach (var kv in updateLookup) {
                if (kv.Value.Any()) {
                    var group = groupLookup[kv.Key];
                    //Must be first..
                    group.receiveUpdateScore(kv.Value.Select(x => new AngularScore(x.score, x.updates.AbsoluteUpdateTime, false)).ToList()); //L10 Updater

                    var groupUpdate = new AngularUpdate();
                    foreach (var u in kv.Value) {
                        var score = u.score;
                        var updates = u.updates;
                        var toUpdate = new AngularScore(score, updates.AbsoluteUpdateTime, false);
                        toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
                        toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();
                        groupUpdate.Add(toUpdate);
                    }
                    group.update(groupUpdate);
                }
            }
        }

        public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {
            var recurrenceId = recurMeasurable.L10Recurrence.Id;
            var recur = s.Load<L10Recurrence>(recurrenceId);
            var current = L10Accessor._GetCurrentL10Meeting(s, PermissionsUtility.CreateAdmin(s), recurrenceId, true, false, false);
            var skipRealTime = false;

            var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id).List().ToList();

            using (var rt = RealTimeUtility.Create()) {
                if (current != null) {
                    var ts = recur.Organization.GetTimeSettings();
                    ts.Descending = recur.ReverseScorecard;
                    var weeks = TimingUtility.GetPeriods(ts, recurMeasurable.CreateTime, current.StartTime, true);

                    var additional = await ScorecardAccessor._GenerateScoreModels_AddMissingScores_Unsafe(s, weeks.Select(x => x.ForWeek), measurable.Id.AsList(), scores);
                    scores.AddRange(additional);

                    //make calculated uneditable..
                    if (measurable.HasFormula) {
                        foreach (var score in scores)
                            score._Editable = false;
                    }


                    var mm = new L10Meeting.L10Meeting_Measurable() {
                        L10Meeting = current,
                        Measurable = measurable,
                    };
                    s.Save(mm);

                    if (!skipRealTime) {

                        rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(g => {
                            var settings = current.Organization.Settings;
                            var sow = settings.WeekStart;
                            var offset = current.Organization.GetTimezoneOffset();
                            var scorecardType = settings.ScorecardPeriod;




                            //if (recur.ReverseScorecard)
                            //	weeks.Reverse();S:\repos\Radial\RadialReview\RadialReview\Hooks\Realtime\L10\Realtime_L10Scorecard.cs

                            //var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();
                            var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
                            var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
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
                            g.addMeasurable(first, second);
                        });
                    }
                } else {
                    var additional = await ScorecardAccessor._GenerateScoreModels_AddMissingScores_Unsafe(s, DateTime.UtcNow.AsList(), measurable.Id.AsList(), scores);
                    scores.AddRange(additional);
                }

                if (!skipRealTime) {
                    rt.UpdateRecurrences(recurrenceId).UpdateScorecard(scores.Where(x => x.Measurable.Id == measurable.Id), null);
                    rt.UpdateRecurrences(recurrenceId).SetFocus("[data-measurable='" + measurable.Id + "'] input:visible:first");
                }
            }
        }

        public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
            using (var rt = RealTimeUtility.Create()) {
                rt.UpdateRecurrences(recurrenceId).Update(
                        new AngularRecurrence(recurrenceId) {
                            Scorecard = new AngularScorecard(recurrenceId) {
                                Id = recurrenceId,
                                Measurables = AngularList.CreateFrom(AngularListType.Remove, new AngularMeasurable(measurable))
                            }
                        }
                    );

                rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(x => x.removeMeasurable(measurable.Id));
            }
        }

        public async Task CreateMeasurable(ISession s, MeasurableModel m) {
            //nothing to do
        }

        public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
            var applySelf = false;
            using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
                var recurrenceIds = RealTimeHelpers.GetRecurrencesForMeasurable(s, m.Id);

                //var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                //	.Where(x => x.DeleteTime == null && x.Measurable.Id == m.Id)
                //	.Select(x => x.Id)
                //	.List<long>().ToList();
                var mmid = m.Id;

                var rtRecur = rt.UpdateRecurrences(recurrenceIds);
                if (updates.AccountableUserChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "accountable", m.AccountableUser.NotNull(x => x.GetName()), m.AccountableUserId));


                if (updates.AdminUserChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "admin", m.AdminUser.NotNull(x => x.GetName()), m.AdminUserId));

                if (updates.AlternateGoalChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "altTarget", m.AlternateGoal.NotNull(x => x.Value.ToString("0.#####")) ?? ""));

                if (updates.ShowCumulativeChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "showCumulative", m.ShowCumulative));

                if (updates.CumulativeRangeChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "cumulativeRange", m.CumulativeRange));

                if (updates.CumulativeRangeChanged || updates.ShowCumulativeChanged)
                    L10Accessor._RecalculateCumulative_Unsafe(s, rt, m, recurrenceIds);

                if (updates.GoalChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "target", m.Goal.ToString("0.#####")));

                if (updates.MessageChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "title", m.Title));

                if (updates.UnitTypeChanged) {
                    applySelf = true;
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "unittype", m.UnitType.ToTypeString(), m.UnitType));
                }

                if (updates.GoalDirectionChanged)
                    rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "direction", m.GoalDirection.ToSymbol(), m.GoalDirection.ToString()));

                rtRecur.UpdateMeasurable(m, updatedScores, null, forceNoSkip: applySelf);

                if (updates.UpdateAboveWeek != null) {
                    rtRecur.AddLowLevelAction(group => group.updateScoresGoals(updates.UpdateAboveWeek.ToJsMs(), m.Id, new {
                        GoalDir = m.GoalDirection,
                        Goal = m.Goal,
                        AltGoal = m.AlternateGoal,
                    }));
                }
            }
        }

        public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
            //nothing to do
        }
    }
}