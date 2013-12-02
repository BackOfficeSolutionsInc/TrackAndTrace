using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class PaymentController :BaseController
    {
        [Access(AccessLevel.Any)]
        public ActionResult Index(int? count)
        {
            GetUserModel();

            if (count == null)
                return RedirectToAction("Index", "Organization");

            ViewBag.Count = count;
            return View();
        }
    }
}