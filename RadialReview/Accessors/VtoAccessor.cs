using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
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

					//model.Name.Vto = model;
					//s.Update(model.Name);

					//model.OrganizationWide.Vto = model;
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

		public static void UpdateVtoString(UserOrganizationModel caller, long vtoStringId, String message, string connectionId = null)
		{
			long? update_VtoId = null;
			VtoModel.VtoItem_String str = null;
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					str = s.Get<VtoModel.VtoItem_String>(vtoStringId);
					PermissionsUtility.Create(s, caller).EditVTO(str.Vto.Id);
					if (str.Data != message){
						/*newstr = new VtoModel.VtoItem_String(){
							BaseId = str.BaseId,
							CopiedFrom = str.Id,
							Data = message,
							Ordering = str.Ordering,
							Type = str.Type,
							Vto = str.Vto,
						};*/
						str.Data = message;
						if (str.BaseId == 0)
							str.BaseId = str.Id;

						s.Update(str);
						update_VtoId = str.Vto.Id;

						tx.Commit();
						s.Flush();
					}

				}
			}
			if (update_VtoId != null)
			{
				var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
				var group = hub.Clients.Group(VtoHub.GenerateMeetingGroupId(update_VtoId.Value), connectionId);
				str.Vto = null;
				group.update(str);
			}
		}

		public static void Update(UserOrganizationModel caller, BaseAngular model, string connectionId)
		{
			if (model.Type == typeof(AngularVtoString).Name)
			{
				var m = (AngularVtoString)model;
				UpdateVtoString(caller, m.Id, m.Data, connectionId);
			}
		}
	}
}