using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using RadialReview.Models.VTO;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class VtoAccessor : BaseAccessor
	{
		public static List<VtoModel> GetAllVTOForOrganization(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s,caller).ManagingOrganization(organizationId);

					return s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
				}
			}
		}

		public static VtoModel GetVTO(UserOrganizationModel caller, long vtoId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewVTO(vtoId);
					var model = s.Get<VtoModel>(vtoId);
					return model;
				}
			}
		}

	}
}