using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class GetStartedController : BaseController {
        // GET: Onboard
        [Access(AccessLevel.SignedOut)]
        public ActionResult Index()
        {
            var u = OnboardingAccessor.GetOrCreate(this.Request, this.Response);
            return RedirectToAction(u.CurrentPage);
        }
        // GET: Onboard
        [Access(AccessLevel.SignedOut)]
        public ActionResult TheBasics()
        {
            var u = OnboardingAccessor.GetOrCreate(this.Request, this.Response,"TheBasics");
            return View(u);
        }
    }
}