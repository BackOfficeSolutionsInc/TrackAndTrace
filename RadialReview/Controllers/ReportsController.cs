using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ReportsController : BaseController
    {
        //
        // GET: /Reports/
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index(int page=0)
        {
            Session["Report"] = "View";
            var user = GetUser();
            var reviewCount = _ReviewAccessor.GetNumberOfReviewsWithVisibleReportsForUser(user, user.Id);
            var reviews = _ReviewAccessor.GetReviewsWithVisibleReports(user, user.Id, page, user.CountPerPage);
            var output = new ReviewsListViewModel()
            {
                ForUser = user,
                Reviews = reviews,
                Page = page,
                NumPages = reviewCount / (double)user.CountPerPage
            };
            return View(output);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Generate(int page = 0)
        {
            Session["Reports"] = "Create";
            double pageSize = 10;

            var reviews = _ReviewAccessor.GetReviewsForOrganization(GetUser(), GetUser().Organization.Id, false,true,true, (int)pageSize, page,DateTime.MinValue);
            var model = new OrgReviewsViewModel()
            {
                Reviews = reviews.Select(x => new ReviewsViewModel(x)).ToList(),
                NumPages = (int)Math.Ceiling(_ReviewAccessor.GetNumberOfReviewsForOrganization(GetUser(), GetUser().Organization.Id) / pageSize),
                Page = page
            };

            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult List(long id)
        {
            var reviewContainerId = id; 
            //var reviewContainerAnswers = _ReviewAccessor.GetReviewContainerAnswers(GetUser(), id);

            //var reviewContainerPeople = reviewContainerAnswers.GroupBy(x => x.AboutUserId);
            var user = GetUser().Hydrate().ManagingUsers(true).Execute();

            var reviewContainer = _ReviewAccessor.GetReviewContainer(user, id, false, true);
            //reviewContainer.Reviews = _ReviewAccessor.GetReviewsForReviewContainer(GetUser(), id);
            
            foreach (var r in reviewContainer.Reviews)
            {
                if (r.ForUser.Id == GetUser().Id && reviewContainer.CreatedById == GetUser().Id)
                {
                    r.ForUser.SetPersonallyManaging(true);
                }
                else
                {
                    //r.ForUser.PopulateLevel(GetUser(), user.AllSubordinates);
                    r.ForUser.PopulatePersonallyManaging(user, user.AllSubordinates);
                }
            }

            var model = new ReviewsViewModel(reviewContainer);

            return View(model);

        }
	}
}