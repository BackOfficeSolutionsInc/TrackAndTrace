using Amazon.DataPipeline.Model;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;

namespace RadialReview.Accessors {
	public class AskableAccessor : BaseAccessor{

		public List<Askable> GetAskablesForUser(UserOrganizationModel caller, long forUserId,long periodId)
		{
			using (var s = HibernateSession.GetCurrentSession()) {
				using (s.BeginTransaction()){
					var perm = PermissionsUtility.Create(s, caller);
					return GetAskablesForUser(caller, s.ToQueryProvider(true), perm, forUserId, periodId);
				}
			}
		}
		[Obsolete("Includes dead askables. Call with ToListAlive()", false)]
		public static List<Askable> GetAskablesForUser(UserOrganizationModel caller, AbstractQuery queryProvider, PermissionsUtility perms, long forUserId,long periodId)
		{
			var allAskables = new List<Askable>();

			var orgId = queryProvider.Get<UserOrganizationModel>(forUserId).Organization.Id;

			allAskables.AddRange(ApplicationAccessor.GetApplicationQuestions(queryProvider));
			allAskables.AddRange(ResponsibilitiesAccessor.GetResponsibilitiesForUser(caller, queryProvider, perms, forUserId));
			allAskables.AddRange(QuestionAccessor.GetQuestionsForUser(queryProvider, perms, forUserId));
			allAskables.AddRange(RockAccessor.GetRocks(queryProvider, perms, forUserId,periodId));
			allAskables.AddRange(RoleAccessor.GetRoles(queryProvider, perms, forUserId));
			allAskables.AddRange(OrganizationAccessor.GetCompanyValues(queryProvider, perms, orgId));

			return allAskables.ToList();
		}
	}
}