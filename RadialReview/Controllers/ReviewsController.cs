using RadialReview.Accessors;
using RadialReview.Models.Json;
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
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();


        [Access(AccessLevel.Manager)]
        public ActionResult Details(long id)
        {
            //var reviewContainerAnswers = _ReviewAccessor.GetReviewContainerAnswers(GetUser(), id);

            //var reviewContainerPeople = reviewContainerAnswers.GroupBy(x => x.AboutUserId);

            var reviewContainer = _ReviewAccessor.GetReviewContainer(GetUser(), id,true);
            //reviewContainer.Reviews = _ReviewAccessor.GetReviewsForReviewContainer(GetUser(), id);
            foreach (var r in reviewContainer.Reviews)
            {
                r.ForUser = r.ForUser.Hydrate().PersonallyManaging(GetUser()).Execute();
            }

            var model = new ReviewsViewModel(reviewContainer);

            return View(model);
        }

        public class UpdateReviewsViewModel
        {
            public long Id {get;set;}
            public List<SelectListItem> AdditionalUsers { get; set; }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Update(long id)
        {
            var review =_ReviewAccessor.GetReviewContainer(GetUser(),id,false);
            var users  =_ReviewAccessor.GetUsersInReview(GetUser(),id);

            var orgMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), review.ForOrganization.Id);

            var model = new UpdateReviewsViewModel()
            {
                Id=id,
                AdditionalUsers=orgMembers.Where(x => !users.Any(y => y.Id == x.Id)).ToListAlive().ToSelectList(x=>x.GetNameAndTitle(youId:GetUser().Id),x=>x.Id)
            };
            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult Update(UpdateReviewsViewModel model)
        {
            return Json(ResultObject.Success);
        }


        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult AddToReview(long id, long userId)
        {
            return Json(_ReviewAccessor.AddUserToReviewContainer(GetUser(), id, userId));
        }

	}


}