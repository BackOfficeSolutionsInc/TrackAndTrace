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

namespace RadialReview.Hooks.Realtime.L10 {
	public class Realtime_L10Scorecard : IScoreHook, IMeasurableHook, IMeetingMeasurableHook {

		public bool CanRunRemotely() {
			return false;
		}

		[Untested("Test me", "Meeting", "Dash", "Archive")]
		public async Task UpdateScore(ISession s, ScoreModel score, IScoreHookUpdates updates) {
			if (updates.ValueChanged) {
				var recurIds = RealTimeHelpers.GetRecurrencesForScore(s, score);

				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var groupIds = recurIds.Select(rid => MeetingHub.GenerateMeetingGroupId(rid)).ToList();
				var group = hub.Clients.Groups(groupIds, RealTimeHelpers.GetConnectionString());


				var toUpdate = new AngularScore(score, false);
				group.updateScore(toUpdate); //L10 Updater

				toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
				toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();
				group.update(new AngularUpdate() { toUpdate });
			}
		}

		[Untested("Test me")]
		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {
			var recurrenceId = recurMeasurable.L10Recurrence.Id;
			var recur = s.Load<L10Recurrence>(recurrenceId);
			var current = L10Accessor._GetCurrentL10Meeting(s, PermissionsUtility.CreateAdmin(s), recurrenceId, true, false, false);
			var skipRealTime = false;
			var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id).List().ToList();

			using (var rt = RealTimeUtility.Create()) {
				if (current != null) {
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


							var ts = current.Organization.GetTimeSettings();
							ts.Descending = recur.ReverseScorecard;

							var weeks = TimingUtility.GetPeriods(ts, recurMeasurable.CreateTime, current.StartTime, false);

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
				}

				if (!skipRealTime) {
					rt.UpdateRecurrences(recurrenceId).UpdateScorecard(scores.Where(x => x.Measurable.Id == measurable.Id));
					rt.UpdateRecurrences(recurrenceId).SetFocus("[data-measurable='" + measurable.Id + "'] input:visible:first");
				}
			}
		}

		[Untested("test me")]
		public async Task DetatchMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
			using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(recurrenceId).Update(
						new AngularRecurrence(recurrenceId) {
							Scorecard = new AngularScorecard(recurrenceId) {
								Id = recurrenceId,
								Measurables = AngularList.CreateFrom(AngularListType.Remove, new AngularMeasurable(measurable))
							}
						}
					);
			}
		}
		
		public async Task CreateMeasurable(ISession s, MeasurableModel m) {
			//nothing to do
		}

		[Untested("Test all cases", "Dash", "wizard", "archive", "meeting")]
		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			var applySelf = false;
			using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var recurrenceIds = RealTimeHelpers.GetRecurrencesForMeasurable(s, m.Id);

				var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null && x.Measurable.Id == m.Id)
					.Select(x => x.Id)
					.List<long>().ToList();

				var rtRecur = rt.UpdateRecurrences(recurrenceIds);
				if (updates.AccountableUserChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "accountable", m.AccountableUser.NotNull(x => x.GetName()), m.AccountableUserId));


				if (updates.AdminUserChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "admin", m.AdminUser.NotNull(x => x.GetName()), m.AdminUserId));

				if (updates.AlternateGoalChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "altTarget", m.AlternateGoal.NotNull(x => x.Value.ToString("0.#####")) ?? ""));

				if (updates.ShowCumulativeChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "showCumulative", m.ShowCumulative));

				if (updates.CumulativeRangeChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "cumulativeRange", m.CumulativeRange));

				if (updates.CumulativeRangeChanged || updates.ShowCumulativeChanged)
					L10Accessor._RecalculateCumulative_Unsafe(s, rt, m, recurrenceIds);

				if (updates.GoalChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "target", m.Goal.ToString("0.#####")));

				if (updates.MessageChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "title", m.Title));

				if (updates.UnitTypeChanged) {
					applySelf = true;
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "unittype", m.UnitType.ToTypeString(), m.UnitType));
				}

				if (updates.GoalDirectionChanged)
					foreach (var mmid in meetingMeasurableIds)
						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "direction", m.GoalDirection.ToSymbol(), m.GoalDirection.ToString()));

				rtRecur.UpdateMeasurable(m, updatedScores, forceNoSkip: applySelf);

			}
		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//nothing to do
		}
	}
}