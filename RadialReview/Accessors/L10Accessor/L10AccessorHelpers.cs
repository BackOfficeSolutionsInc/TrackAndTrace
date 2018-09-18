using System;
using System.Collections.Generic;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Models.Interfaces;

namespace RadialReview.Accessors {
    public partial class L10Accessor : BaseAccessor {

		#region Helpers
		public static List<AbstractTodoCreds> GetExternalLinksForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return ExternalTodoAccessor.GetExternalLinksForModel(s, PermissionsUtility.Create(s, caller), ForModel.Create<L10Recurrence>(recurrenceId));
				}
			}
		}

		public static bool _ProcessDeleted(ISession s, IDeletable item, bool? delete) {
			if (delete != null) {
				if (delete == true && item.DeleteTime == null) {
					item.DeleteTime = DateTime.UtcNow;
					s.Update(item);
					return true;
				} else if (delete == false && item.DeleteTime != null) {
					item.DeleteTime = null;
					s.Update(item);
					return true;
				}
			}
			return false;
		}
		public static object GetModel_Unsafe(ISession s, string type, long id) {
			if (id <= 0)
				return null;

			switch (type.ToLower()) {
				case "measurablemodel":
					return s.Get<MeasurableModel>(id);
				case "todomodel":
					return s.Get<TodoModel>(id);
				case "issuemodel":
					return s.Get<IssueModel>(id);
			}
			return null;
		}

		#endregion
	}
}