using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;

namespace RadialReview.Accessors {
	public class RoleAccessor {



		public List<RoleModel> GetRoles(UserOrganizationModel caller, long id, DateRange range=null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetRoles(s.ToQueryProvider(true), perms, id, range);
				}
			}
		}

		public static List<RoleModel> GetRoles(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId, DateRange range)
		{
			perms.ViewUserOrganization(forUserId, false);
			return queryProvider.Where<RoleModel>(x => x.ForUserId == forUserId).FilterRange(range).ToList();
		}

		public void EditRoles(UserOrganizationModel caller, long userId, List<RoleModel> roles,bool updateOutstanding)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					if (roles.Any(x => x.ForUserId != userId))
						throw new PermissionsException("Role UserId does not match UserId");

					var perms =PermissionsUtility.Create(s, caller).EditQuestionForUser(userId);
					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;
					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					var outstanding = ReviewAccessor.OutstandingReviewsForOrganization_Unsafe(s, orgId);


					foreach (var r in roles)
					{
						r.Category = category;
						r.OrganizationId = orgId;
						var added = r.Id == 0;
						s.SaveOrUpdate(r);

						if (updateOutstanding && added){
							foreach (var o in outstanding){
								ReviewAccessor.AddResponsibilityAboutUserToReview(s, caller, perms, o.Id, userId, r.Id);
							}
						}
					}

					user.NumRoles = roles.Count(x => x.DeleteTime == null);
					s.SaveOrUpdate(user);
					user.UpdateCache(s);
					

					tx.Commit();
					s.Flush();
				}
			}
		}

	}
}