using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;

namespace RadialReview.Controllers {
	public class InvoiceController : BaseController {
		[Access(AccessLevel.UserOrganization)]
		public ActionResult List(long? id = null) {
			var orgid = id ?? GetUser().Organization.Id;
			var list = InvoiceAccessor.GetInvoicesForOrganization(GetUser(), orgid);

			return View(list);
		}
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Details(long id) {
			var invoice = InvoiceAccessor.GetInvoice(GetUser(), id);

			return View(invoice);
		}

	}
}