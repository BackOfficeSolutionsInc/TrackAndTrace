using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Periods;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class PeriodAccessor
	{
		public static List<PeriodModel> GetPeriods(UserOrganizationModel caller,long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<PeriodModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == organizationId)
						.List()
						.OrderByDescending(x => x.StartTime).ThenBy(x => x.EndTime)
						.ToList();
				}
			}
		}

		public static void AddPeriod(UserOrganizationModel caller, long organizationId, DateTime start, DateTime end, String name)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditOrganization(organizationId);

					s.Save(new PeriodModel(){
						Name = name,
						StartTime = start,
						EndTime = end,
						OrganizationId = organizationId,
					});


					tx.Commit();
					s.Flush(); 

				}
			}
		}


		public static PeriodModel GetPeriod(UserOrganizationModel caller, long id)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var period = s.Get<PeriodModel>(id);
					PermissionsUtility.Create(s, caller).ViewOrganization(period.OrganizationId);
					return period;
				}
			}
		}

		public static PeriodModel EditPeriod(UserOrganizationModel caller, long id, long organizationId, DateTime start, DateTime end, String name)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PeriodModel found;
					if (id==0)
						found = new PeriodModel(){OrganizationId = organizationId};
					else
						found = s.Get<PeriodModel>(id);

					if (organizationId!=found.OrganizationId)
						throw new PermissionsException("Period is not part of this organization.");
					
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id,found.OrganizationId);

					found.Name = name;
					found.StartTime = start;
					found.EndTime = end;
					s.SaveOrUpdate(found);

					tx.Commit();
					s.Flush(); 
					return found;

				}
			}
		}

		public static void Delete(UserOrganizationModel caller, long periodId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var found = s.Get<PeriodModel>(periodId);
					PermissionsUtility.Create(s, caller).EditOrganization(found.OrganizationId);
					found.DeleteTime = DateTime.UtcNow;
					s.SaveOrUpdate(found);
					tx.Commit();
					s.Flush();

				}
			}
		}

		public static PeriodModel GetPeriodForReviewContainer(UserOrganizationModel caller, long reviewContainerId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var found = s.Get<ReviewsModel>(reviewContainerId);
					PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId, false);
					if (found.Period != null){
						var f = s.Get<PeriodModel>(found.Period.Id);
						var a = f.Id;
						var b = f.Name;
						return f;
					}
					return null;
				}
			}
		}

        public static PeriodModel GetCurrentPeriod(ISession s, PermissionsUtility perms, long organizationdId)
        {
            perms.ViewOrganization(organizationdId);
            var now = DateTime.UtcNow;
            return s.QueryOver<PeriodModel>()
                .Where(x=> x.DeleteTime == null && x.StartTime < now && now <= x.EndTime)
                .List().OrderByDescending(x=>x.StartTime)
                .FirstOrDefault();

        }

        public static PeriodModel GetCurrentPeriod(UserOrganizationModel caller, long organizationdId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return GetCurrentPeriod(s, PermissionsUtility.Create(s, caller), organizationdId);
                }
            }
        }
    }
}