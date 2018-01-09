﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using System.Threading.Tasks;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using NHibernate;

namespace RadialReview.Controllers {
    public class InvoiceController : BaseController {
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index() {
            return List();
        }

        [Access(AccessLevel.Radial)]
        public async Task<bool> ForceUpdateCard(long id) {
            var caller = GetUser();
            var t = PaymentAccessor.GetCards(GetUser(), GetUser().Organization.Id).LastOrDefault(x => x.Active).NotNull(x => x.GetToken());
            if (t != null) {
                await HooksRegistry.Each<IPaymentHook>((ses, x) => x.UpdateCard(ses, t));
                return true;
            }
            return false;

        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult List(long? id = null) {
            var orgid = id ?? GetUser().Organization.Id;
            var list = InvoiceAccessor.GetInvoicesForOrganization(GetUser(), orgid);

            return View("List", list);
        }
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Details(long id) {
            var invoice = InvoiceAccessor.GetInvoice(GetUser(), id);

            return View(invoice);
        }

        [Access(AccessLevel.Radial)]
        public ActionResult Outstanding() {
            var o = InvoiceAccessor.AllOutstanding_Unsafe(GetUser());
            ViewBag.ShowOrganization = true;
            return View("List", o);
        }

        [Access(AccessLevel.Radial)]
        public JsonResult Forgive(long id, bool forgive = true) {
            InvoiceAccessor.Forgive(GetUser(), id, forgive);
            return Json(ResultObject.SilentSuccess(new { id, forgive }), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Radial)]
        public JsonResult MarkPaid(long id, bool paid = true, DateTime? time = null) {
            time = time ?? DateTime.UtcNow;
            InvoiceAccessor.MarkPaid(GetUser(), id, time.Value, paid);
            return Json(ResultObject.SilentSuccess(new { id, paid, time = paid ? time : null }), JsonRequestBehavior.AllowGet);
        }

    }
}