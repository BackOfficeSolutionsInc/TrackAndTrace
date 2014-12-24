using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
						.List().ToList();
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

				}
			}
		}
	}
}