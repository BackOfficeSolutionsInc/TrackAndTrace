using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace RadialReview.Accessors {
	public class TinyUserAccessor {

		//private static TempUserModel tempUserAlias = null;
		//private static UserOrganizationModel userOrgAlias = null;
		//private static UserModel userAlias = null;
		//private static Expression<Func<UserOrganizationModel, object>>[] Package = new Expression<Func<UserOrganizationModel, object>>[]{			
		//};

		private static Func<object[], TinyUser> Unpackage = new Func<object[], TinyUser>(x => {
			var fname = (string)x[0];
			var lname = (string)x[1];
			var email = (string)x[5];
			var uoId = (long)x[2];
			if (fname == null && lname == null) {
				fname = (string)x[3];
				lname = (string)x[4];
				email = (string)x[6];
			}
			return new TinyUser() {
				FirstName = fname,
				LastName = lname,
				Email = email,
				UserOrgId = uoId
			};
		});


		public static IEnumerable<TinyUser> GetUsers_Unsafe(ISession s, IEnumerable<long> userIds, bool noDeleted = true) {
			TempUserModel tempUserAlias = null;
			UserOrganizationModel userOrgAlias = null;
			UserModel userAlias = null;

			var q = s.QueryOver<UserOrganizationModel>(() => userOrgAlias)
				.Left.JoinAlias(x => x.User, () => userAlias)
				.Left.JoinAlias(x => x.TempUser, () => tempUserAlias);
			if (noDeleted) {
				q = q.Where(x => x.DeleteTime == null);
			}

			return q.WhereRestrictionOn(x => x.Id).IsIn(userIds.ToArray())
					.Select(x => userAlias.FirstName, x => userAlias.LastName, x => x.Id, x => tempUserAlias.FirstName, x => tempUserAlias.LastName, x => userAlias.UserName, x => tempUserAlias.Email)
					.Future<object[]>()
					.Select(Unpackage);
		}

		public static List<TinyUser> GetOrganizationMembers(UserOrganizationModel caller, long organizationId,bool excludeClients=false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationMembers(s, perms, organizationId,excludeClients).ToList();
				}
			}
		}

		public static IEnumerable<TinyUser> GetOrganizationMembers(ISession s, PermissionsUtility perms, long organizationId,bool excludeClients = false) {
			TempUserModel tempUserAlias = null;
			UserOrganizationModel userOrgAlias = null;
			UserModel userAlias = null;

			perms.ViewOrganization(organizationId);
			var q = s.QueryOver<UserOrganizationModel>(() => userOrgAlias)
				.Left.JoinAlias(x => x.User, () => userAlias)
				.Left.JoinAlias(x => x.TempUser, () => tempUserAlias)
				.Where(x => x.Organization.Id == organizationId && x.DeleteTime == null);
			if (excludeClients)
				q = q.Where(x => !x.IsClient);

			return q.Select(x => userAlias.FirstName, x => userAlias.LastName, x => x.Id, x => tempUserAlias.FirstName, x => tempUserAlias.LastName, x => userAlias.UserName, x => tempUserAlias.Email)
				.Future<object[]>()
				.Select(Unpackage);

		}

	}
}