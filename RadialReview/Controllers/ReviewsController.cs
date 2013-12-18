using RadialReview.Accessors;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ReviewsController : BaseController
    {
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();

        [Access(AccessLevel.Manager)]
        public ActionResult Details(long id)
        {
            var reviewContainer = _ReviewAccessor.GetReviewContainer(GetUser(), id, true);
            foreach (var r in reviewContainer.Reviews)
            {
                r.ForUser = r.ForUser.Hydrate().PersonallyManaging(GetUser()).Execute();
            }

            var model = new ReviewsViewModel(reviewContainer);

            return View(model);
        }

	}


}