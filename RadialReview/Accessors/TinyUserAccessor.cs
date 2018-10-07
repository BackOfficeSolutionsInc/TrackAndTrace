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
		
		private static Func<object[], TinyUser> Unpackage = new Func<object[], TinyUser>(x => {
			var fname = (string)x[0];
			var lname = (string)x[1];
			var email = (string)x[5];
			var uoId = (long)x[2];
			var imageGuid = (string)x[7];
			if (fname == null && lname == null) {
				fname = (string)x[3];
				lname = (string)x[4];
				email = (string)x[6];
			}
			return new TinyUser() {
				FirstName = fname,
				LastName = lname,
				Email = email,
				UserOrgId = uoId,
				ImageGuid = imageGuid
			};
		});

		public class TinyUserAndOrganization {
			public long UserId { get; set; }
			public long OrganizationId { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public string Organization { get; set; }
		}

		public static TinyUserAndOrganization GetUserAndOrganization_Unsafe(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var user = s.Get<UserOrganizationModel>(userId);

					return new TinyUserAndOrganization() {
						FirstName = user.GetFirstName(),
						LastName = user.GetLastName(),
						Organization = user.Organization.GetName(),
						OrganizationId = user.Organization.Id,
						UserId = userId
					};
				}
			}
		}


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
					.Select(x => userAlias.FirstName, x => userAlias.LastName, x => x.Id, x => tempUserAlias.FirstName, x => tempUserAlias.LastName, x => userAlias.UserName, x => tempUserAlias.Email,x=>userAlias.ImageGuid)
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

			return q.Select(x => userAlias.FirstName, x => userAlias.LastName, x => x.Id, x => tempUserAlias.FirstName, x => tempUserAlias.LastName, x => userAlias.UserName, x => tempUserAlias.Email, x => userAlias.ImageGuid)
				.Future<object[]>()
				.Select(Unpackage);

		}

	}
}