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
        public ActionResult Privacy()
        {
            return View();
        }
        public ActionResult TOS()
        {
            return View();
        }
	}
}