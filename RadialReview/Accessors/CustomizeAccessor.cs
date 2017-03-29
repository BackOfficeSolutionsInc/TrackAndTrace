using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Customize;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public static class CUSTOMIZABLE
	{
		public const String Segue_Subheading = "Segue_Subheading";
		public const String CustomerEmployeeHeadlines_Subheading = "CustomerEmployeeHeadlines_Subheading";
		public const String CustomerEmployeeHeadlines_Heading = "CustomerEmployeeHeadlines_Heading";
	}
	public class CustomizeAccessor
	{
		


		public static List<SelectListItem> AllProperties()
		{
			return new List<SelectListItem>(){
				new SelectListItem(){Text = "Customer Employee Headlines Subheading",Value = CUSTOMIZABLE.CustomerEmployeeHeadlines_Subheading},
				new SelectListItem(){Text = "Segue Subheading",Value =CUSTOMIZABLE.Segue_Subheading},
				new SelectListItem(){Text = "Customer Employee Headlines Heading",Value =CUSTOMIZABLE.CustomerEmployeeHeadlines_Heading}
			};
		} 

		public static void EditCustomizeProperty(UserOrganizationModel caller, CustomText custom)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ManagingOrganization(custom.OrgId);


					if (custom.Id != 0){
						var found = s.Get<CustomText>(custom.Id);
						
						if (found.OrgId != custom.OrgId)
							throw new PermissionsException("Organizations do not match.");
						s.Evict(found);
					}

					if (AllProperties().All(x => x.Value != custom.PropertyName))
						throw new PermissionsException("Property does not exist.");

					s.SaveOrUpdate(custom);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<CustomText> GetCustomizations(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);

					return s.QueryOver<CustomText>()
						.Where(x => x.DeleteTime == null && x.OrgId == organizationId)
						.OrderBy(x=>x.CreateTime).Desc
						.List()
						.GroupBy(x=>x.PropertyName).Select(x=>x.First()).ToList();
				}
			}
		}

		public static CustomText GetCustomization(UserOrganizationModel caller, long customizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var found = s.Get<CustomText>(customizationId);

					if (found==null)
						throw new PermissionsException("Does not exist.");
					PermissionsUtility.Create(s, caller).ViewOrganization(found.OrgId);

					return found;
				}
			}
		}

		public static string GetSpecificCustomization(UserOrganizationModel caller, long orgId, string propertyName,string dflt=null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewOrganization(orgId);
					var found = s.QueryOver<CustomText>()
						.Where(x => x.DeleteTime == null && x.OrgId == orgId && x.PropertyName == propertyName)
						.OrderBy(x => x.CreateTime).Desc
						.Take(1).SingleOrDefault()
						.NotNull(x => x.NewText);

					if (dflt != null && string.IsNullOrWhiteSpace(found)){
						return dflt;
					}
					return found;
				}
			}

		}
	}
}