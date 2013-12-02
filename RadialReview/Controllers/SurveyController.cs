using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class SurveyController : BaseController
    {
        //
        // GET: /Survey/
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Create()
        {
            return View();
        }
	}
}