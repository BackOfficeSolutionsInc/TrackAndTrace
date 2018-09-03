using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;
using RadialReview.Accessors.PDF;

namespace RadialReview.Controllers
{
    public class ReportsController : BaseController
    {
	    private ReviewsViewModel GenModel(long reviewContainerId,bool deep=true)
	    {
       
			var user = GetUser().Hydrate().ManagingUsers(true).Execute();

			//TODO remove first true, we dont want all answers

			var reviewContainer = _ReviewAccessor.GetReviewContainer(user, reviewContainerId, deep && false, deep);
		
			var directSubs = user.ManagingUsers.Select(x => x.Subordinate).ToList();

			var acceptedReviews = new List<ReviewModel>();
		    var managesTeam = PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagingTeam(reviewContainer.ForTeamId));
			foreach (var r in reviewContainer.Reviews)
			{
				var add = false;
				r.ReviewerUser.PopulateDirectlyManaging(user, directSubs);

				if ((managesTeam) ||
					(user.ManagingOrganization && user.Organization.Id == reviewContainer.OrganizationId) ||
					(r.ReviewerUser.Id == GetUser().Id && reviewContainer.CreatedById == GetUser().Id))
				{
					r.ReviewerUser.SetPersonallyManaging(true);
					add = true;
				}
				else
				{
					add = r.ReviewerUser.PopulatePersonallyManaging(user, user.AllSubordinates);
				}
				if (add)
				{
					acceptedReviews.Add(r);
				}
			}
			reviewContainer.Reviews = acceptedReviews;
			
			var model = new ReviewsViewModel(reviewContainer);


			

			var viewSurvey = PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagerAtOrganization(GetUser().Id, reviewContainer.OrganizationId)
				.Or(
					y => y.AdminReviewContainer(reviewContainerId),
					y => y.ManagingTeam(reviewContainer.ForTeamId)
				));

			var simpleLookup = ReviewAccessor.GetSimpleAnswerLookup_Unsafe(reviewContainerId, viewSurvey, true);

			if (viewSurvey){
				model.SurveyAnswers = simpleLookup.SurveyAnswers;//reviewContainer.Reviews.SelectMany(x => x.Answers.Where(y => y.AboutUserId == reviewContainer.ForOrganizationId)).ToList();
		    }

			model.SimpleAnswersLookup = simpleLookup;
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
					var found = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == id && x.ReviewerUserId == userId).List().FirstOrDefault();
					if (found==null)
						throw new PermissionsException("Report does not exist");
					reviewId = found.Id;
				}
			}
		    return RedirectToAction("Details", "Review",new{id=reviewId});
	    }

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
		public ActionResult List(long id) {
			var reviewContainerId = id;
			var user = GetUser().Hydrate().ManagingUsers(true).Execute();
			var reviewContainer = _ReviewAccessor.GetReviewContainer(user, id, false, true, deduplicate: true);
			var directSubs = user.ManagingUsers.Select(x => x.Subordinate).ToList();

			var acceptedReviews = new List<ReviewModel>();
			foreach (var r in reviewContainer.Reviews) {
				var add = false;
				r.ReviewerUser.PopulateDirectlyManaging(user, directSubs);
				if (r.ReviewerUser.Id == GetUser().Id && reviewContainer.CreatedById == GetUser().Id) {
					r.ReviewerUser.SetPersonallyManaging(true);
					add = true;
				} else {
					add = r.ReviewerUser.PopulatePersonallyManaging(user, user.AllSubordinates);
				}
				if (add) {
					acceptedReviews.Add(r);
				}
			}
			reviewContainer.Reviews = acceptedReviews;
			reviewContainer.Reviews = reviewContainer.Reviews.GroupBy(x => x.ReviewerUserId).Select(x => x.First()).ToList();

			var model = new ReviewsViewModel(reviewContainer);
			return View(model);

		}
		
		[Access(AccessLevel.Manager)]
		public ActionResult PeopleAnalyzer(long id) {

			var settings = new PdfSettings(GetUser().Organization.Settings);
			var pad = FastReviewQueries.PeopleAnalyzerData(GetUser(), id);
			var pdf =PdfAccessor.GeneratePeopleAnalyzer(GetUser(), pad,settings);

			return Pdf(pdf);
		}


	
	}
}