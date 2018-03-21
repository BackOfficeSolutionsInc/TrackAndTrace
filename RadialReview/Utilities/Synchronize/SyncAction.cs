using System;

namespace RadialReview.Utilities.Synchronize {
	public class SyncAction {
		#region Internal 
		protected String ActionString;

		protected SyncAction(string actionString) {
			ActionString = actionString;
		}

		public override string ToString() {
			return ActionString;
		}
		#endregion

		public static SyncAction MeasurableReorder(long recurrenceId) {
			return new SyncAction("MsReord_" + recurrenceId);
		}
		public static SyncAction UpdateScore(long scoreId) {
			return new SyncAction("ScVal_" + scoreId);
		}
		public static SyncAction UpdateIssueMessage(long issueId) {
			return new SyncAction("IsMsg_" + issueId);
		}
		public static SyncAction UpdateIssueDetails(long issueId) {
			return new SyncAction("IsDet_" + issueId);
		}
		public static SyncAction UpdateTodoMessage(long todoId) {
			return new SyncAction("TdMsg_" + todoId);
		}
		public static SyncAction UpdateTodoDetails(long todoId) {
			return new SyncAction("TdDet_" + todoId);
		}
		public static SyncAction UpdateTodoCompletion(long todoId) {
			return new SyncAction("TdComp_" + todoId);
		}
		public static SyncAction UpdateRockCompletion(long rockId) {
			return new SyncAction("RckComp_" + rockId);
		}
		public static SyncAction UpdateVtoItem(long vtoItemId) {
			return new SyncAction("VtoItm_" + vtoItemId);
		}
		public static SyncAction UpdateVto(long vtoId) {
			return new SyncAction("Vto_" + vtoId);
		}
		public static SyncAction UpdateCompanyValue(long companyValueId) {
			return new SyncAction("CmpVal_" + companyValueId);
		}

		public static SyncAction UpdateRockOwner(long rockId) {
			return new SyncAction("RckOwn_" + rockId);
		}
		public static SyncAction UpdateRock(long rockId) {
			return new SyncAction("Rck_" + rockId);
		}

		public static SyncAction UpdateThreeYearPicture(long id) {
			return new SyncAction("3YP_" + id);
		}

		public static SyncAction UpdateQuarterlyRocks(long id) {
			return new SyncAction("QRcks_" + id);
		}

		public static SyncAction UpdateOneYearPlan(long id) {
			return new SyncAction("1YP_" + id);
		}

		public static SyncAction UpdateStrategy(long id) {
			return new SyncAction("Strt_" + id);
		}

		public static SyncAction UpdateCoreFocus(object id) {
			return new SyncAction("CoreF_" + id);
		}
		public static SyncAction UpdateRole(long id) {
			return new SyncAction("Role_" + id);
		}

		public static SyncAction UpdateHeadlineMessage(long id) {
			return new SyncAction("Headln_" + id);
		}
	}
}