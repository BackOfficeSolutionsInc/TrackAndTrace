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

namespace RadialReview.Accessors {
	public class AskableAccessor : BaseAccessor{

		public List<Askable> GetAskablesForUser(UserOrganizationModel caller, long forUserId, long? periodId, DateRange range)
		{
			using (var s = HibernateSession.GetCurrentSession()) {
				using (s.BeginTransaction()){
					var perm = PermissionsUtility.Create(s, caller);
					return GetAskablesForUser(caller, s.ToQueryProvider(true), perm, forUserId, periodId,range);
				}
			}
		}
		/*[Obsolete("Includes dead askables. Call with ToListAlive()", false)]*/
		public static List<Askable> GetAskablesForUser(UserOrganizationModel caller, AbstractQuery queryProvider, PermissionsUtility perms, long forRGMId,long? periodId, DateRange range)
		{
			var allAskables = new List<Askable>();

			var rgm = queryProvider.Get<ResponsibilityGroupModel>(forRGMId);
			if (rgm == null || rgm.Organization == null)
				return allAskables;

			var orgId = rgm.Organization.Id;

			if (rgm is OrganizationModel){
				allAskables.AddRange(OrganizationAccessor.AskablesAboutOrganization(queryProvider,perms,orgId,range));
			}else if (rgm is UserOrganizationModel){
				allAskables.AddRange(ApplicationAccessor.GetApplicationQuestions(queryProvider));
				allAskables.AddRange(ResponsibilitiesAccessor.GetResponsibilitiesForUser(caller, queryProvider, perms, forRGMId, range));
				allAskables.AddRange(QuestionAccessor.GetQuestionsForUser(queryProvider, perms, forRGMId, range));
				allAskables.AddRange(RockAccessor.GetRocks(queryProvider, perms, forRGMId, periodId, range));
				allAskables.AddRange(RoleAccessor.GetRoles(queryProvider, perms, forRGMId, range));
				allAskables.AddRange(OrganizationAccessor.GetCompanyValues(queryProvider, perms, orgId, range));
			}

			return allAskables.ToList();
		}
	}
}