using System.Collections.Generic;
using System.Linq;
using NHibernate.Transform;
using RadialReview.Models;
using RadialReview.Models.Audit;
using RadialReview.Utilities;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Audit

		public static List<L10AuditModel> GetL10Audit(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var audits = s.QueryOver<L10AuditModel>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
						.Fetch(x => x.UserOrganization).Eager
						.TransformUsing(Transformers.DistinctRootEntity)
						.List().ToList();
					return audits;
				}
			}
		}
		#endregion
	}
}