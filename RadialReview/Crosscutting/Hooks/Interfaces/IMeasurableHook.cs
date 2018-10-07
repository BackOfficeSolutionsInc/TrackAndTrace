using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public class IMeasurableHookUpdates {
		public bool MessageChanged { get; set; }
		public bool UnitTypeChanged { get; set; }
		public bool GoalDirectionChanged { get; set; }
		public bool GoalChanged { get; set; }
		public bool AccountableUserChanged { get; set; }
		public bool AdminUserChanged { get; set; }
		public bool AlternateGoalChanged { get; set; }

        public bool ShowCumulativeChanged { get; set; }
        public bool CumulativeRangeChanged { get; set; }

        public bool ShowAverageChanged { get; set; }
        public bool AverageRangeChanged { get; set; }

        public long OriginalAccountableUserId { get; set; }
		public long OriginalAdminUserId { get; set; }
		public DateTime UpdateAboveWeek { get; set; }

	}


	public interface IMeasurableHook : IHook {
		Task CreateMeasurable(ISession s, MeasurableModel measurable);
		Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates);
		Task DeleteMeasurable(ISession s, MeasurableModel measurable);
	}
}
