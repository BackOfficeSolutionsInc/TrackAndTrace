using Amazon.DataPipeline.Model;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using RadialReview.Models.Reviews;

namespace RadialReview.Accessors {
	public class AskableAccessor : BaseAccessor {

		public List<Askable> GetAskablesForUser(UserOrganizationModel caller, Reviewee forReviewee, DateRange range) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					return GetAskablesForUser(s.ToQueryProvider(true), perm, forReviewee, range);
				}
			}
		}
		/*[Obsolete("Includes dead askables. Call with ToListAlive()", false)]*/
		public static List<Askable> GetAskablesForUser(AbstractQuery queryProvider, PermissionsUtility perms, Reviewee reviewee, DateRange range) {
			var allAskables = new List<Askable>();

			var rgm = queryProvider.Get<ResponsibilityGroupModel>(reviewee.RGMId);
			if (rgm == null || rgm.Organization == null)
				return allAskables;

			var orgId = rgm.Organization.Id;

			if (rgm is OrganizationModel) {
				allAskables.AddRange(OrganizationAccessor.AskablesAboutOrganization(queryProvider, perms, orgId, range));
			} else if (rgm is UserOrganizationModel) {
				allAskables.AddRange(ApplicationAccessor.GetApplicationQuestions(queryProvider));
#pragma warning disable CS0618 // Type or member is obsolete
				allAskables.AddRange(ResponsibilitiesAccessor.GetResponsibilitiesForUser(queryProvider, perms, reviewee.RGMId, range));
				allAskables.AddRange(QuestionAccessor.GetQuestionsForUser(queryProvider, perms, reviewee.RGMId, range));
#pragma warning restore CS0618 // Type or member is obsolete
				allAskables.AddRange(RockAccessor.GetRocks(queryProvider, perms, reviewee.RGMId/*, periodId*/, range));
				allAskables.AddRange(RoleAccessor.GetRolesForReviewee(queryProvider, perms, reviewee, range));
				allAskables.AddRange(OrganizationAccessor.GetCompanyValues(queryProvider, perms, orgId, range));
			}

			return allAskables.ToList();
		}
	}
}