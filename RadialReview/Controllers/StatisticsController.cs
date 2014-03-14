using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class StatisticsController : BaseController
    {

        [Access(Controllers.AccessLevel.UserOrganization)]
        public ActionResult Review(long id)
        {            
            var reviewContainer= _ReviewAccessor.GetReviewContainer(GetUser(), id, true, true);
            //Get all reports
            var user = GetUser().Hydrate().ManagingUsers(true).Execute();

            foreach (var review in reviewContainer.Reviews)
            {
                review.ForUser.PopulatePersonallyManaging(GetUser(), user.AllSubordinates);
            }

            return View(reviewContainer);
        }
	}
}