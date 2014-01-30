using RadialReview.Accessors;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ResultsController : BaseController
    {
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();

        //
        // GET: /Results/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            var reviews = _ReviewAccessor.GetReviewsForOrganization(GetUser(), GetUser().Organization.Id, false);
            var model = new OrgReviewsViewModel()
            {
                Reviews = reviews.Select(x => new ReviewsViewModel(x)).ToList()
            };

            return View(model);
        }
	}
}