using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Scorecard;
using System.Threading.Tasks;
using RadialReview.Accessors;

namespace RadialReview.Crosscutting.Hooks.CrossCutting.Formula {
    public class CascadeScorecardFormulaUpdates : IScoreHook {
        public bool CanRunRemotely() {
            return false;
        }

        public HookPriority GetHookPriority() {
            return HookPriority.High;
        }

        public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
            foreach (var sau in scoreAndUpdates) {
                var score = sau.score;
                foreach (var mid in score.Measurable.BackReferenceMeasurables)
                    await ScorecardAccessor.UpdateCalculatedScores_FromUpdatedScore_Unsafe(s, score);
            }
        }
    }
}