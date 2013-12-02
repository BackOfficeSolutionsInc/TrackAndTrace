using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class LegalController : BaseController
    {
        //
        // GET: /Legal/
        [Access(AccessLevel.Any)]
        public ActionResult Privacy()
        {
            return View();
        }
        [Access(AccessLevel.Any)]
        public ActionResult TOS()
        {
            return View();
        }
	}
}