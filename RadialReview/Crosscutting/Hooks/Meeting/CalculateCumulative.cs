using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Scorecard;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.L10;
using RadialReview.Hooks.Realtime;
using RadialReview.Models;

namespace RadialReview.Hooks.Meeting {
	public class CalculateCumulative : IScoreHook, IMeasurableHook {
		public bool CanRunRemotely() {
			return true;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		private static void _UpdateCumulative(ISession s, long measurableId, ScoreModel updatedScore = null) {
			var recurrenceIds = RealTimeHelpers.GetRecurrencesForMeasurable(s, measurableId);
			var measurable = s.Get<MeasurableModel>(measurableId);
			using (var rt = RealTimeUtility.Create()) {
				L10Accessor._RecalculateCumulative_Unsafe(s, rt, measurable , recurrenceIds, updatedScore);
				rt.UpdateRecurrences(recurrenceIds).AddLowLevelAction(x => x.updateCumulative(measurableId, measurable._Cumulative.NotNull(y => y.Value.ToString("0.#####"))));
			}
		}
		
		public async Task UpdateScore(ISession s, ScoreModel score, IScoreHookUpdates updates) {

			if (updates.ValueChanged) {
                if (score.Measurable.ShowCumulative) {
                    _UpdateCumulative(s, score.MeasurableId, score);
                }
			}
		}


		public async Task CreateMeasurable(ISession s, MeasurableModel m) {
			//noop
		}
		
		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (updates.GoalChanged || updates.CumulativeRangeChanged || updates.ShowCumulativeChanged) {
				_UpdateCumulative(s, m.Id);
			}
		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//noop
		}

        public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
            foreach (var sau in scoreAndUpdates)
                await UpdateScore(s,sau.score,sau.updates);
        }
    }
}