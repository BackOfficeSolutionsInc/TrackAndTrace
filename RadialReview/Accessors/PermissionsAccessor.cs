using FluentNHibernate.Utils;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
	public class PermissionsAccessor
	{

		public void Permitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					ensurePermitted(PermissionsUtility.Create(s, caller));
				}
			}
		}
		public bool IsPermitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted)
		{
			try
			{
				Permitted(caller, ensurePermitted);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public List<PermissionOverride> AllPermissionsAtOrganization(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);
					var ps = s.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.Organization.Id == organizationId).List().ToList();

					return ps;
				}
			}
		}

		public static bool AnyTrue(ISession s, UserOrganizationModel caller, PermissionType? type, Predicate<UserOrganizationModel> predicate)
		{
			if (predicate(caller))
				return true;
			if (type != null){
				var ids = s.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.Permissions == type && x.ForUser.Id == caller.Id).Select(x => x.AsUser.Id).List<long>().ToList();
				var uorgs = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids).List().ToList();

				if (uorgs.Any(o => predicate(o))){
					return true;
				}
			}
			return false;
		}

		public bool AnyTrue(UserOrganizationModel caller, PermissionType type, Predicate<UserOrganizationModel> predicate)
		{
			if (predicate(caller))
				return true;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					return AnyTrue(s, caller, type, predicate);
				}
			}
		}

		public static PermissionOverride GetPermission(UserOrganizationModel caller, long overrideId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (overrideId == 0)
						return new PermissionOverride();

					var p = s.Get<PermissionOverride>(overrideId);
					PermissionsUtility.Create(s, caller).ViewOrganization(p.Organization.Id);
					return p;
				}
			}

		}

		public static void EditPermission(UserOrganizationModel caller, long permissionsOverrideId, long forUserId, PermissionType permissionType, long copyFromUserId, DateTime? deleteTime = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var p = (permissionsOverrideId == 0) ? new PermissionOverride() : s.Get<PermissionOverride>(permissionsOverrideId);
					PermissionsUtility.Create(s, caller).EditPermissionOverride(p.Id);

					p.ForUser = s.Load<UserOrganizationModel>(forUserId);
					p.AsUser = s.Load<UserOrganizationModel>(copyFromUserId);
					p.Permissions = permissionType;
					p.DeleteTime = deleteTime;

					if (p.Id == 0)
					{
						p.Organization = caller.Organization;
					}

					s.SaveOrUpdate(p);

					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}