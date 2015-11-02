using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.ElasticTranscoder.Model;
using RadialReview.Models;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class InvoiceAccessor
	{

		public static List<InvoiceModel> GetInvoicesForOrganization(UserOrganizationModel caller, long orgid)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller).CanView(PermItem.ResourceType.InvoiceForOrganization, orgid, @this => @this.ManagingOrganization(orgid));
					var invoices = s.QueryOver<InvoiceModel>().Where(x => x.DeleteTime == null && x.Organization.Id == orgid).List().ToList();

					return invoices;

				}
			}
		}

		public static object GetInvoice(UserOrganizationModel caller, long invoiceId)
		{
			InvoiceModel invoice = null;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					invoice = s.Get<InvoiceModel>(invoiceId);
					var perms = PermissionsUtility.Create(s, caller).CanView(PermItem.ResourceType.InvoiceForOrganization, invoice.Organization.Id, @this => @this.ManagingOrganization(invoice.Organization.Id));
					foreach (var item in invoice.InvoiceItems){
						var a = item.Name;
						var b = item.Description;
						var c = item.AmountDue;
					}


				}
			}
			invoice.InvoiceItems = invoice.InvoiceItems.Where(x => x.DeleteTime == null).ToList();
			return invoice;
		}
	}
}