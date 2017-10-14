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


		[Untested("Test me")]
		public async Task UpdateScore(ISession s, ScoreModel score, IScoreHookUpdates updates) {

			if (updates.ValueChanged) {
				var recurrenceIds = RealTimeHelpers.GetRecurrencesForScore(s, score);

				using (var rt = RealTimeUtility.Create()) {
					L10Accessor._RecalculateCumulative_Unsafe(s, rt, score.Measurable, recurrenceIds, score);
					rt.UpdateRecurrences(recurrenceIds).AddLowLevelAction(x => x.updateCumulative(score.Measurable.Id, score.Measurable._Cumulative.NotNull(y => y.Value.ToString("0.#####"))));
				}
			}
		}

		public async Task CreateMeasurable(ISession s, MeasurableModel m) {
			//noop
		}
		
		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			thrownew NotImplementedException();
		}
	}
}