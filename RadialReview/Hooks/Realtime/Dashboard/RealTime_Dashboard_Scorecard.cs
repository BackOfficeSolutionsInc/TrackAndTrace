using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Scorecard;

namespace RadialReview.Hooks.Realtime.Dashboard {
	public class RealTime_Dashboard_Scorecard : IMeasurableHook {
		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}


		private void AddRemoveMeas(long userId, MeasurableModel meas,AngularListType type) {
			RealTimeHelpers.GetUserHubForRecurrence(userId).update(new AngularUpdate() {
					new AngularScorecard(-1) {
						Measurables = AngularList.CreateFrom(type, new AngularMeasurable(meas))
					}
			});
		}
		

		public async Task CreateMeasurable(ISession s, MeasurableModel measurable) {
			//add
			AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.ReplaceIfNewer);
			AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.ReplaceIfNewer);
		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//remove
			AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.Remove);
			AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.Remove);
		}

		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (updates.AccountableUserChanged) {
				AddRemoveMeas(updates.OriginalAccountableUserId, measurable, AngularListType.Remove);
				AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.ReplaceIfNewer);
			}
			if (updates.AdminUserChanged) {
				AddRemoveMeas(updates.OriginalAdminUserId, measurable, AngularListType.Remove);
				AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.ReplaceIfNewer);
			}
		}
	}
}