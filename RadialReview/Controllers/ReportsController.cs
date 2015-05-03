using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class ReportsController : BaseController
    {
	    private ReviewsViewModel GenModel(long reviewContainerId)
	    {
       
			var user = GetUser().Hydrate().ManagingUsers(true).Execute();

			//TODO remove first true, we dont want all answers

			var reviewContainer = _ReviewAccessor.GetReviewContainer(user, reviewContainerId, true, true);
		
			var directSubs = user.ManagingUsers.Select(x => x.Subordinate).ToList();

			var acceptedReviews = new List<ReviewModel>();
			foreach (var r in reviewContainer.Reviews)
			{
				var add = false;
				r.ForUser.PopulateDirectlyManaging(user, directSubs);
				if (r.ForUser.Id == GetUser().Id && reviewContainer.CreatedById == GetUser().Id)
				{
					r.ForUser.SetPersonallyManaging(true);
					add = true;
				}
				else
				{
					add = r.ForUser.PopulatePersonallyManaging(user, user.AllSubordinates);
				}
				if (add)
				{
					acceptedReviews.Add(r);
				}
			}
			reviewContainer.Reviews = acceptedReviews;
			
			var model = new ReviewsViewModel(reviewContainer);

		    var viewSurvey = _PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagerAtOrganization(GetUser().Id, reviewContainer.ForOrganizationId).Or(y => y.EditReviewContainer(reviewContainerId)));
		    if (viewSurvey){
			    model.SurveyAnswers = reviewContainer.Reviews.SelectMany(x => x.Answers.Where(y => y.AboutUserId == reviewContainer.ForOrganizationId)).ToList();
		    }
			return model;
	    }


        //
        // GET: /Reports/
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index(long id/*,int page=0*/)
		{
			return View(GenModel(id));
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Stats(long id/*,int page=0*/)
		{
			return View(GenModel(id));
		}




	    [Access(AccessLevel.Manager)]
	    public ActionResult Details(int id,int userId)
	    {
		    var reviewContainerId = id;
		    long reviewId = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var found = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == id && x.ForUserId == userId).SingleOrDefault();
					if (found==null)
						throw new PermissionsException("Report does not exist");
					reviewId = found.Id;
				}
			}
		    return RedirectToAction("Details", "Review",new{id=reviewId});
	    }

	    /*[Access(AccessLevel.Manager)]
	    public ActionResult Affinity(int id)
	    {


	    }*/


	    [Access(AccessLevel.Manager)]
        public ActionResult Generate(int page = 0)
	    {
		    new Cache().Push(CacheKeys.REPORTS_PAGE, "Create");

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
        public ActionResult List(long id){
            var reviewContainerId = id; 
            var user = GetUser().Hydrate().ManagingUsers(true).Execute();
            var reviewContainer = _ReviewAccessor.GetReviewContainer(user, id, false, true);
            var directSubs = user.ManagingUsers.Select(x => x.Subordinate).ToList();

            var acceptedReviews = new List<ReviewModel>();
            foreach (var r in reviewContainer.Reviews)
            {
                var add = false;
                r.ForUser.PopulateDirectlyManaging(user, directSubs);
                if (r.ForUser.Id == GetUser().Id && reviewContainer.CreatedById == GetUser().Id){
                    r.ForUser.SetPersonallyManaging(true);
                    add = true;
                }else{
                    add = r.ForUser.PopulatePersonallyManaging(user, user.AllSubordinates);
                }
                if (add){
                    acceptedReviews.Add(r);
                }
            }
            reviewContainer.Reviews = acceptedReviews;

            var model = new ReviewsViewModel(reviewContainer);
            return View(model);

        }
	}
}