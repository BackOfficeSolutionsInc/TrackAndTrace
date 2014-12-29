using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;

namespace RadialReview.Accessors {
	public class RoleAccessor {



		public List<RoleModel> GetRoles(UserOrganizationModel caller, long id)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetRoles(s.ToQueryProvider(true), perms, id);
				}
			}
		}

		public static List<RoleModel> GetRoles(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId)
		{
			perms.ViewUserOrganization(forUserId, false);
			return queryProvider.Where<RoleModel>(x => x.ForUserId == forUserId && x.DeleteTime == null);
		}

		public void EditRoles(UserOrganizationModel caller, long userId, List<RoleModel> roles)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					if (roles.Any(x => x.ForUserId != userId))
						throw new PermissionsException("Role UserId does not match UserId");

					PermissionsUtility.Create(s, caller).ManagesUserOrganization(userId, false);
					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;
					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);


					foreach (var r in roles)
					{
						r.Category = category;
						r.OrganizationId = orgId;
						s.SaveOrUpdate(r);
					}

					user.NumRoles = roles.Count(x => x.DeleteTime == null);
					s.SaveOrUpdate(user);

					tx.Commit();
					s.Flush();
				}
			}
		}

	}
}