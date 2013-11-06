using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ManageController : BaseController
    {
        //
        // GET: /Manage/
        public ActionResult Index()
        {
            var user = GetUser();
            return View(user);
        }

    }
}