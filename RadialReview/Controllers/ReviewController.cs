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
        public ActionResult Index(String id, long? organizationId)
        {
            var user = GetOneUserOrganization(organizationId);
            var reviews = _ReviewAccessor.GetReviewsForUser(user, user);
            var output = new ReviewsListViewModel() { ForUser = user, Reviews = reviews };
            return View(output);
        }

        [HttpPost]
        public ActionResult Skip(FormCollection collection)
        {
            long reviewId =-1;
            long organizationId =-1;
            int page =-1;
            ParseAndSave(collection, out reviewId, out organizationId, out page);
            return Take(reviewId,page+1,organizationId);
        }

        [HttpPost]
        public ActionResult Take(FormCollection collection)
        {
            long reviewId =-1;
            long organizationId =-1;
            int page =-1;

            if (ParseAndSave(collection, out reviewId, out organizationId, out page))
            {
                return Take(reviewId, page + 1, organizationId);
            }
            TempData["Message"] = DisplayNameStrings.remainingQuestions;
            ViewBag.Incomplete = true;
            return Take(reviewId, page, organizationId);
        }

        private Boolean ParseAndSave(FormCollection collection, out long reviewId, out long organizationId, out int currentPage)
        {
            try
            {
                reviewId = long.Parse(collection["reviewId"]);
                organizationId = long.Parse(collection["organizationId"]);
                currentPage = int.Parse(collection["page"]);

                var user = GetOneUserOrganization(organizationId);
                var allComplete = true;

                var values = collection.AllKeys.Select(k => collection[k]).ToList();

                foreach (var k in collection.AllKeys)
                {
                    var args = k.Split('_');
                    if (args[0] == "question")
                    {
                        var questionId = long.Parse(args[2]);
                        switch (args[1].Parse<QuestionType>())
                        {
                            case QuestionType.Slider:               allComplete = allComplete && _ReviewAccessor.UpdateSliderAnswer(user, questionId, decimal.Parse(collection[k]) / 100.0m); break;
                            case QuestionType.Thumbs:               allComplete = allComplete && _ReviewAccessor.UpdateThumbsAnswer(user, questionId, collection[k].Parse<ThumbsType>()); break;
                            case QuestionType.Feedback:             allComplete = allComplete && _ReviewAccessor.UpdateFeedbackAnswer(user, questionId, collection[k]); break;
                            case QuestionType.RelativeComparison:   allComplete = allComplete && _ReviewAccessor.UpdateRelativeComparisonAnswer(user, questionId, collection[k].Parse<RelativeComparisonType>()); break;
                            default: throw new Exception();
                        }
                    }
                }
                return allComplete;
            }
            catch (Exception)
            {
                throw new PermissionsException();
            }
        }

        [HttpGet]
        public ActionResult Take(long id, int? page, long? organizationId)
        {
            page = page ?? 1;
            var user = GetOneUserOrganization(organizationId);
            var review = _ReviewAccessor.GetReviewForUser(user, id);
            ViewBag.ReviewId = id;
            ViewBag.OrganizationId = user.Organization.Id;
            ViewBag.Page = page;


            //Required followed by not required
            switch (page)
            {
                case 1://Sliders and thumbs
                    {
                        var sliderQuestions = review.Answers.Where(x => (x is SliderAnswer || x is ThumbsAnswer));
                        if (sliderQuestions.Any())
                            return View("Slider", sliderQuestions.ToList());
                        goto case 2;
                    }
                case 2://Feedback
                    {
                        var feedback = review.Answers.Where(x => x is FeedbackAnswer);
                        //if (feedback.Any(x=>x.Required == required))
                        if (feedback.Any())
                            return View("Feedback", feedback.Cast<FeedbackAnswer>().ToList());
                        goto case 3;
                    }
                case 3://Relative comparisons
                    {
                        var relativeComparisonRequired = review.Answers.Where(x => x is RelativeComparisonAnswer && !x.Complete && x.Required).Shuffle();
                        if (relativeComparisonRequired.Any())
                            return View("RelativeComparison", relativeComparisonRequired.Cast<RelativeComparisonAnswer>().FirstOrDefault());
                        var relativeComparisonNonRequired = review.Answers.Where(x => x is RelativeComparisonAnswer && !x.Complete && !x.Required).Shuffle();
                        if (relativeComparisonNonRequired.Any())
                        {
                            ViewBag.Message = DisplayNameStrings.remainingIsOptional;
                            ViewBag.AlertType="alert-info";
                            ViewBag.AlertMessage = DisplayNameStrings.hey;
                            return View("RelativeComparison", relativeComparisonNonRequired.Cast<RelativeComparisonAnswer>().FirstOrDefault());
                        }
                        goto case 5;
                    }
                case 4: //Reroute to case three.. keep asking comparison questions
                    {
                        goto case 3;
                    }
                case 5:
                    {
                        TempData["Message"] = DisplayNameStrings.remainingQuestions;

                        if (review.Answers.Where(x => (x is SliderAnswer || x is ThumbsAnswer) && !x.Complete && x.Required).Any())
                            return Take(id, 1, organizationId);
                        if (review.Answers.Where(x => (x is FeedbackAnswer) && !x.Complete && x.Required).Any())
                            return Take(id, 2, organizationId);
                        if (review.Answers.Where(x => (x is RelativeComparisonAnswer) && !x.Complete && x.Required).Any())
                            return Take(id, 3, organizationId);
                        TempData["Message"] = DisplayNameStrings.youveCompletedThisReview;
                        return RedirectToAction("Index");
                    }
                default: return RedirectToAction("Index");
            }
            //TempData["Message"] = RadialReview.Properties.DisplayNameStrings.youveCompletedThisReview;
            /*
            foreach (var required in true.AsList(false))
            {
                //Sliders and thumbs not complete and required

                //Feedback not complete and required      
               

                //Relative Comparison not complete and required      
                ViewBag.Message = RadialReview.Properties.DisplayNameStrings.remainingIsOptional;
            }*/
        }

        [HttpGet]
        public ActionResult Create()
        {
            //TODO correct the time zone.
            var today = DateTime.UtcNow.ToLocalTime();
            var user = GetOneUserOrganization(null).Hydrate().ManagingUsers(subordinates: true).Organization().Execute();
            return PartialView(new IssueReviewViewModel() { Today = today, ForUsers = user.AllSubordinates });
        }

        [HttpPost]
        public JsonResult Create(String Date, String name)
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
                        var body = String.Format(EmailStrings.NewReview_Body,s.Name(), user.Name(), dueDate.ToShortDateString(), ProductStrings.BaseUrl + "n/" + guid, ProductStrings.BaseUrl + "n/" + guid, ProductStrings.ProductName);

                        Emailer.SendEmail(s.EmailAtOrganization, subject, body);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }




                return Json(JsonObject.Create(dueDate));
            }
            catch (Exception e)
            {
                return Json(new JsonObject(e));
            }
        }

    }
}