using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class UnitTestController : Controller
    {
        //
        // GET: /UnitTest/
        public ActionResult Index()
        {
            return View();
        }

        public String DbType()
        {
            return System.Configuration.ConfigurationManager.AppSettings["DBType"];
        }
	}
}