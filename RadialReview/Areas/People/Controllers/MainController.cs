using RadialReview.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.People.Controllers
{
    public class MainController : BaseController
    {
		// GET: People/Main
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Diagonal() {
			return View();
		}
	}
}