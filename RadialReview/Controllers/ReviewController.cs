using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ReviewController : BaseController
    {
        protected static NexusAccessor _NexusAccessor = new NexusAccessor();
        protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();

        //
        // GET: /Review/
        public ActionResult Index(String id,long? organizationId)
        {
            var user=GetOneUserOrganization(organizationId);
            var reviews=_ReviewAccessor.GetReviewsForUser(user, user);
            var output = new ReviewsListViewModel() { ForUser = user, Reviews = reviews };            
            return View(output);
        }

        public ActionResult Take(long id,long? organizationId)
        {
            var user = GetOneUserOrganization(organizationId);
            var review = _ReviewAccessor.GetReviewForUser(user, id);
            //Required followed by not required
            foreach (var required in true.AsList(false))
            {
                //Sliders and thumbs not complete and required
                var sliderQuestions = review.Answers.Where(x => (x is SliderAnswer || x is ThumbsAnswer) && !x.Complete);
                if (sliderQuestions.Any(x => x.Required == required))
                    return View("Slider", sliderQuestions.ToList());

                //Feedback not complete and required      
                var feedback = review.Answers.Where(x => x is FeedbackAnswer && !x.Complete);
                if (feedback.Any(x=>x.Required == required))
                    return View("Feedback", feedback.ToList());

                //Relative Comparison not complete and required      
                var relativeComparison = review.Answers.Where(x => x is RelativeComparisonAnswer && !x.Complete);
                if (relativeComparison.Any(x => x.Required == required))
                    return View("RelativeComparison", relativeComparison.ToList());
                ViewBag.Message = RadialReview.Properties.DisplayNameStrings.remainingIsOptional;
            }
            TempData["Message"] = RadialReview.Properties.DisplayNameStrings.youveCompletedThisReview;
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Create()
        {
            //TODO correct the time zone.
            var today = DateTime.UtcNow.ToLocalTime(); 
            var user = GetOneUserOrganization(null).Hydrate().ManagingUsers(subordinates: true).Organization().Execute();
            return PartialView(new IssueReviewViewModel() { Today = today, ForUsers=user.AllSubordinates });
        }

        [HttpPost]
        public JsonResult Create(String Date,String name)
        {
            try
            {
                var dueDate = DateTime.Parse(Date);
                var user = GetOneUserOrganization(null).Hydrate().ManagingUsers(subordinates: true).Organization().Execute();

                if (!user.ManagingOrganization)
                    throw new PermissionsException();

                var organization = user.Organization;
                var subordinates = user.AllSubordinatesAndSelf();
                var reviewContainer = new ReviewsModel()
                {
                    DateCreated = DateTime.UtcNow,
                    DueDate = dueDate,
                    ReviewName = name,
                    CreatedById = user.Id,
                };
                _ReviewAccessor.CreateReviewContainer(user, reviewContainer);

                List<Exception> exceptions = new List<Exception>();

                foreach (var s in subordinates)
                {
                    try
                    {
                        Guid guid = Guid.NewGuid();
                        NexusModel nexus = new NexusModel(guid);
                        nexus.ForUserId = s.Id;
                        nexus.ActionCode = NexusActions.TakeReview;
                        _NexusAccessor.Put(nexus);

                        var review = _QuestionAccessor.GenerateReviewForUser(user, s, reviewContainer);
                        //review.ForReviewsId = reviewContainer.Id;
                        //review.DueDate = reviewContainer.DueDate;
                        //review.Name = reviewContainer.ReviewName;
                        //_ReviewAccessor.UpdateIndividualReview(user, review);

                        var subject = String.Format(RadialReview.Properties.EmailStrings.NewReview_Subject, organization.Name.Translate());
                        var body = String.Format(EmailStrings.NewReview_Body, user.Name(), dueDate.ToShortDateString(), ProductStrings.BaseUrl + "n/" + guid, ProductStrings.BaseUrl + "n/" + guid, ProductStrings.ProductName);

                        Emailer.SendEmail(s.EmailAtOrganization, subject, body);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }




                return Json(JsonObject.Create(dueDate));
            }catch(Exception e)
            {
                return Json(new JsonObject(e));
            }
        }

	}
}