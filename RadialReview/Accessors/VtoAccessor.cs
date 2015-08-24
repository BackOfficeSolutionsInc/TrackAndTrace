using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Angular.VTO;
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
					var perms = PermissionsUtility.Create(s, caller);
					return GetVTO(s, perms, vtoId);
				}
			}
		}


		public static VtoModel GetVTO(ISession s, PermissionsUtility perms, long vtoId)
		{
			perms.ViewVTO(vtoId);
			var model = s.Get<VtoModel>(vtoId);
			return model;
		}

		public static AngularVTO GetAngularVTO(UserOrganizationModel caller, long vtoId)
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller);
					var vto = GetVTO(s, perms, vtoId);

					var ang = AngularVTO.Create(vto);
					return ang;
				}
			}
		}

		public static VtoModel CreateVTO(UserOrganizationModel caller,long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller)
						.ViewOrganization(organizationId)
						.CreateVTO(organizationId);

					var model = new VtoModel();
					model.Organization = s.Get<OrganizationModel>(organizationId);
					s.Save(model.MarketingStrategy);
					s.Save(model.OrganizationWide);
					s.Save(model.CoreFocus);
					s.Save(model.ThreeYearPicture);
					s.Save(model.OneYearPlan);
					s.Save(model.QuarterlyRocks);

					s.SaveOrUpdate(model);

					model.Name.Vto = model;
					//s.Update(model.Name);

					model.OrganizationWide.Vto = model;
					//s.Update(model.OrganizationWide);

					model.CoreFocus.Vto = model;
					model.CoreFocus.Niche.Vto = model;
					model.CoreFocus.Purpose.Vto = model;
					
					//s.Update(model.CoreFocus);

					model.MarketingStrategy.Vto = model;
					model.MarketingStrategy.Guarantee.Vto = model;
					model.MarketingStrategy.ProvenProcess.Vto = model;
					model.MarketingStrategy.TargetMarket.Vto = model;
					model.MarketingStrategy.TenYearTarget.Vto = model;


					model.OneYearPlan.Vto = model;
					model.OneYearPlan.FutureDate.Vto = model;
					model.OneYearPlan.Measurables.Vto = model;
					model.OneYearPlan.Profit.Vto = model;
					model.OneYearPlan.Revenue.Vto = model;

					model.QuarterlyRocks.Vto = model;
					model.QuarterlyRocks.Measurables.Vto = model;
					model.QuarterlyRocks.Profit.Vto = model;
					model.QuarterlyRocks.Revenue.Vto = model;

					model.ThreeYearPicture.Vto = model;
					model.ThreeYearPicture.FutureDate.Vto = model;
					model.ThreeYearPicture.Measurables.Vto = model;
					model.ThreeYearPicture.Profit.Vto = model;
					model.ThreeYearPicture.Revenue.Vto = model;
					
					s.Update(model);

					tx.Commit();
					s.Flush();

					return model;
				}
			}
		}
	}
}