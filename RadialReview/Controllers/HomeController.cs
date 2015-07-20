using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.ViewModels;

namespace RadialReview.Controllers
{
    public class BackendViewModel
    {
        public UserViewModel User {get;set;}
        public OutstandingReviewViewModel OutstandingReview { get; set; }
        public bool IncludeTodos { get; set; }
        public bool IncludeScorecard { get; set; }
        public bool IncludeRocks { get; set; }
    }

    public class OutstandingReviewViewModel
    {
        public String Name { get; set; }
        public long ReviewContainerId { get; set; }

    }


    public class HomeController : BaseController
    {
        [Access(AccessLevel.Any)]
        public ActionResult Index()
        {
            if (IsLoggedIn())
            {
                var model = new BackendViewModel();

                try
                {
                    var user=GetUser();
					model.User = new UserViewModel(){User = user.User};
                    if (user.IsManager())
                    {
                        var recentReview = _ReviewAccessor.GetMostRecentReviewContainer(GetUser(), GetUser().Id);
	                    if (recentReview != null){
		                    model.OutstandingReview = new OutstandingReviewViewModel(){
			                    Name = recentReview.ReviewName,
			                    ReviewContainerId = recentReview.Id,
		                    };
	                    }
                    }
                    model.IncludeTodos      = GetUser().Organization.Settings.EnableL10;
                    model.IncludeScorecard  = GetUser().Organization.Settings.EnableL10;
                    model.IncludeRocks      = GetUser().Organization.Settings.EnableL10;

                }catch(Exception){
	                model.User = new UserViewModel(){User = GetUserModel()};
                }



                return View("Backend", model);
            }
            return RedirectToAction("Login","Account");
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