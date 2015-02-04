using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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

		public void EditRocks(UserOrganizationModel caller, long userId, List<RockModel> rocks)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (rocks.Any(x => x.ForUserId != userId))
						throw new PermissionsException("Rock UserId does not match UserId");

					PermissionsUtility.Create(s, caller).ManagesUserOrganization(userId, false);
					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;
					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					foreach (var r in rocks)
					{
						r.OnlyAsk = AboutType.Self; //|| AboutType.Manager; 
						r.Category = category;
						r.OrganizationId = orgId;
						r.Period = s.Get<PeriodModel>(r.PeriodId);
						s.SaveOrUpdate(r);
					}

					user.NumRocks = rocks.Count(x => x.DeleteTime == null);
					s.SaveOrUpdate(user);

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
			perms.ViewUserOrganization(forUserId, false);
			return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId && x.DeleteTime == null);
		}


	}
}