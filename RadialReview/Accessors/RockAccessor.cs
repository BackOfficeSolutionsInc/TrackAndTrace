using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Utils;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;

namespace RadialReview.Accessors
{
	public class RockAccessor
	{
		public List<RockModel> GetRocks(UserOrganizationModel caller, long forUserId, long? periodId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller);
					return GetRocks(s.ToQueryProvider(true), perm, forUserId, periodId);
				}
			}
		}
		public static List<RockModel> GetRocks(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId,long? periodId)
		{
			perms.ViewUserOrganization(forUserId, false);
			if (periodId == null)
				return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId && x.DeleteTime == null);
			return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId && x.DeleteTime == null && x.PeriodId==periodId);
		}
		public void EditCompanyRocks(UserOrganizationModel caller, long organizationId, List<RockModel> rocks)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (rocks.Any(x => x.OrganizationId != organizationId))
						throw new PermissionsException("Rock OrgId does not match OrgId");

					var perm = PermissionsUtility.Create(s, caller);
					//var user = s.Get<UserOrganizationModel>(userId);
					var org = s.Get<OrganizationModel>(organizationId);

					long orgId = -1;

					//if (user == null && org != null){
						perm.ManagingOrganization(organizationId);
						orgId = org.Id;
					/*}else if (user != null && org == null){
					perm.ManagesUserOrganization(userId, false);
					orgId = user.Organization.Id;
					user.NumRocks = rocks.Count(x => x.DeleteTime == null);
					s.SaveOrUpdate(user);
					}else{
						throw new PermissionsException("What?");
					}*/

					var ar=SetUtility.AddRemove(OrganizationAccessor.GetAllUserOrganizations(s,perm,organizationId),rocks.Select(x=>x.ForUserId));
					if (ar.AddedValues.Any())
						throw new PermissionsException("User does not belong to organization");

					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					foreach (var r in rocks){
						r.AccountableUser = s.Load<UserOrganizationModel>(r.ForUserId);
						r.OnlyAsk = AboutType.Self; //|| AboutType.Manager; 
						r.Category = category;
						r.OrganizationId = orgId;
						r.Period = s.Get<PeriodModel>(r.PeriodId);
						r.CompanyRock = true;
						s.SaveOrUpdate(r);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}
		public void EditRocks(UserOrganizationModel caller, long userId, List<RockModel> rocks)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (rocks.Any(x => x.ForUserId != userId))
						throw new PermissionsException("Rock UserId does not match UserId");

					var perm = PermissionsUtility.Create(s, caller);
					var user = s.Get<UserOrganizationModel>(userId);
					//var org = s.Get<OrganizationModel>(userId_OR_organizationId);

					long orgId = -1;

					/*if (user == null && org != null){
						perm.ManagingOrganization(userId_OR_organizationId);
						orgId = org.Id;
					}else if (user != null && org == null){*/
						perm.ManagesUserOrganization(userId, false);
						orgId = user.Organization.Id;
						user.NumRocks = rocks.Count(x => x.DeleteTime == null);
						s.SaveOrUpdate(user);
					/*}else{
						throw new PermissionsException("What?");
					}*/

					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					foreach (var r in rocks)
					{
						r.OnlyAsk = AboutType.Self; //|| AboutType.Manager; 
						r.Category = category;
						r.OrganizationId = orgId;
						r.Period = s.Get<PeriodModel>(r.PeriodId);
						s.SaveOrUpdate(r);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public RockModel GetRock(UserOrganizationModel caller, long rockId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var rock = s.Get<RockModel>(rockId);
					var perm = PermissionsUtility.Create(s, caller).ViewUserOrganization(rock.ForUserId, false);
					return rock;
				}
			}
		}

		public static void DeleteRock(UserOrganizationModel caller, long rockId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var rock = s.Get<RockModel>(rockId);
					var perm = PermissionsUtility.Create(s, caller).ManagesUserOrganization(rock.ForUserId, false);
					rock.DeleteTime = DateTime.UtcNow;
					s.Update(rock);
					tx.Commit();
					s.Flush();
				}
			}
		}


		public List<RockModel> GetAllRocks(UserOrganizationModel caller, long forUserId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller);
					return GetAllRocks(s.ToQueryProvider(true), perm, forUserId);
				}
			}
		}
		public static List<RockModel> GetAllRocks(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId)
		{
			perms.Or(x => x.ViewUserOrganization(forUserId, false),x=>x.ViewOrganization(forUserId));
			return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId && x.DeleteTime == null);
		}

		public static List<RockModel> GetAllRocksAtOrganization(UserOrganizationModel caller, long orgId,bool populateUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Todo permissions not enough
					var perm = PermissionsUtility.Create(s, caller).ViewOrganization(orgId);
					var q = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null);
					if (populateUsers)
						q=q.Fetch(x=>x.AccountableUser).Eager;
					return q.List().ToList();
				}
			}
		} 
	}
}