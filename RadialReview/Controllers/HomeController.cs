using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.ViewModels;

namespace RadialReview.Controllers
{
    public class HomeController : BaseController
    {
        [Access(AccessLevel.Any)]
        public ActionResult Index()
        {
            if (IsLoggedIn())
            {
                return View("Backend", new UserViewModel() { User = GetUserModel() });
            }
            return View();
        }


        [Access(AccessLevel.Any)]
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        [Access(AccessLevel.Any)]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}