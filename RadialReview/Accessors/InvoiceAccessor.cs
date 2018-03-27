using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.ElasticTranscoder.Model;
using RadialReview.Models;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class InvoiceAccessor {

		public static List<InvoiceModel> GetInvoicesForOrganization(UserOrganizationModel caller, long orgid) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanView(PermItem.ResourceType.InvoiceForOrganization, orgid, @this => @this.ManagingOrganization(orgid));
					var invoices = s.QueryOver<InvoiceModel>().Where(x => x.DeleteTime == null && x.Organization.Id == orgid).List().ToList();

					foreach (var i in invoices) {
						var a = i.Organization.GetName();
					}
					return invoices;
				}
			}
		}

		internal static List<InvoiceModel> AllOutstanding_Unsafe(UserOrganizationModel caller) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.RadialAdmin();
					var res = s.QueryOver<InvoiceModel>().Where(x =>
							x.DeleteTime == null &&
							x.AmountDue > 0 &&
							x.ForgivenBy == null &&
							x.PaidTime == null
						).List().ToList();
					foreach (var r in res) {
						var a = r.Organization.GetName();
					}
					return res;
				}
			}
		}

		public static async Task Forgive(UserOrganizationModel caller, long invoiceId, bool forgive = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.RadialAdmin(true);
					var invoice = s.Get<InvoiceModel>(invoiceId);
					if (invoice.PaidTime != null)
						throw new PermissionsException("Invoice already paid");

					var invoiceUpdates = new IInvoiceUpdates();

					if (forgive && invoice.ForgivenBy == null) {
						invoice.ForgivenBy = caller.Id;
						invoiceUpdates.ForgivenChanged = true;
					} else if (forgive == false) {
						invoice.ForgivenBy = null;
						invoiceUpdates.ForgivenChanged = true;
					}
					s.Update(invoice);
					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IInvoiceHook>((ses, x) => x.UpdateInvoice(ses, invoice, invoiceUpdates));
				}
			}
		}

		public static async Task MarkPaid(UserOrganizationModel caller, long invoiceId, DateTime paidTime, bool markPaid = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.RadialAdmin(true);
					var invoice = s.Get<InvoiceModel>(invoiceId);
					//automatically paid
					if (invoice.PaidTime != null && invoice.ManuallyMarkedPaidBy == null)
						throw new PermissionsException("Cannot update. Paid automatically.");

					var updates = new IInvoiceUpdates();


					if (invoice.ManuallyMarkedPaidBy == null && markPaid) {
						invoice.ManuallyMarkedPaidBy = caller.Id;
						invoice.PaidTime = paidTime;
						updates.PaidStatusChanged = true;
					} else if (invoice.ManuallyMarkedPaidBy != null && !markPaid) {
						invoice.ManuallyMarkedPaidBy = null;
						invoice.PaidTime = null;
						updates.PaidStatusChanged = true;
					} else if (invoice.ManuallyMarkedPaidBy != null && markPaid)
						throw new PermissionsException("Already marked paid");
					else if (invoice.ManuallyMarkedPaidBy == null && !markPaid)
						throw new PermissionsException("Already marked unpaid");

					s.Update(invoice);
					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IInvoiceHook>((ses, x) => x.UpdateInvoice(ses, invoice, updates));

				}
			}
		}


		public static object GetInvoice(UserOrganizationModel caller, long invoiceId) {
			InvoiceModel invoice = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					invoice = s.Get<InvoiceModel>(invoiceId);
					var perms = PermissionsUtility.Create(s, caller).CanView(PermItem.ResourceType.InvoiceForOrganization, invoice.Organization.Id, @this => @this.ManagingOrganization(invoice.Organization.Id));
					foreach (var item in invoice.InvoiceItems) {
						var a = item.Name;
						var b = item.Description;
						var c = item.AmountDue;
					}

					var d = invoice.Organization.GetName();


				}
			}
			invoice.InvoiceItems = invoice.InvoiceItems.Where(x => x.DeleteTime == null).ToList();
			return invoice;
		}
	}
}
