using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ReviewController : BaseController
    {
        protected static TeamAccessor _TeamAccessor = new TeamAccessor();
        protected static NexusAccessor _NexusAccessor = new NexusAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        protected static CategoryAccessor _CategoryAccessor = new CategoryAccessor();
        protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();


        //
        // GET: /Review/
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index(String id)
        {
            var user = GetUser();
            var reviews = _ReviewAccessor.GetReviewsForUser(user, user);
            var output = new ReviewsListViewModel() { ForUser = user, Reviews = reviews };
            return View(output);
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Skip(FormCollection collection)
        {
            long reviewId = -1;
            int page = -1;
            ParseAndSave(collection, out reviewId, out page);
            return Take(reviewId, page + 1);
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Take(FormCollection collection)
        {
            long reviewId = -1;
            int page = -1;


            if (ParseAndSave(collection, out reviewId, out page))
            {
                return RedirectToAction("Take", new { id = reviewId, page = page + 1 });
            }
            TempData["Message"] = DisplayNameStrings.remainingQuestions;
            ViewBag.Incomplete = true;
            return RedirectToAction("Take", new { id = reviewId, page = page + 1 });
        }

        private Boolean ParseAndSave(FormCollection collection, out long reviewId, out int currentPage)
        {
            try
            {
                reviewId = long.Parse(collection["reviewId"]);
                currentPage = int.Parse(collection["page"]);

                if (collection.AllKeys.Contains("back"))
                {
                    currentPage = currentPage - 2;
                }

                var user = GetUser();

                var review=_ReviewAccessor.GetReview(user, reviewId);

                if (review.ForUserId != user.Id)
                    throw new PermissionsException("You cannot take this review.");

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
                            case QuestionType.Slider:
                                {
                                    decimal value = 0;
                                    decimal? output = null;
                                    if (decimal.TryParse(collection[k], out value))
                                        output = value / 100.0m;
                                    allComplete = allComplete && _ReviewAccessor.UpdateSliderAnswer(user, questionId, output);
                                } break;
                            case QuestionType.Thumbs: allComplete = allComplete && _ReviewAccessor.UpdateThumbsAnswer(user, questionId, collection[k].Parse<ThumbsType>()); break;
                            case QuestionType.Feedback: allComplete = allComplete && _ReviewAccessor.UpdateFeedbackAnswer(user, questionId, collection[k]); break;
                            case QuestionType.RelativeComparison: allComplete = allComplete && _ReviewAccessor.UpdateRelativeComparisonAnswer(user, questionId, collection[k].Parse<RelativeComparisonType>()); break;
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

        public class TakeViewModel
        {
            public List<AnswerModel> Answers { get; set; }
            public UserOrganizationModel ForUser { get; set; }

        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult SetNotes(long id,string notes)
        {
            _ReviewAccessor.UpdateNotes(GetUser(), id, notes);
            return Json(ResultObject.Success);
        }

        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult AdditionalReview(long id,long page)
        {
            var organizationUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id);
            var review = _ReviewAccessor.GetReview(GetUser(), id);

            var permittedUsers = organizationUsers.Where(x => !review.Answers.Any(y => y.AboutUserId == x.Id));

            var selectList = permittedUsers.Select(x => new SelectListItem() { Text = x.GetNameAndTitle(), Value = "" + x.Id }).ToList();

            var model = new AdditionalReviewViewModel()
            {
                Id=id,
                Possible=selectList,
                Page=page
            };


            return View(model);
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult AdditionalReview(AdditionalReviewViewModel model)
        {
            _ReviewAccessor.AddToReview(GetUser(),GetUser().Id, model.Id,model.User);
            return RedirectToAction("Take", new { id = model.Id, page = model.Page });
        }

        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Take(long id, int? page)
        {
            var user = GetUser();
            var review = _ReviewAccessor.GetReview(user, id);

            if (review.ForUserId != user.Id)
                throw new PermissionsException("You cannot take this review.");

            ViewBag.ReviewId = id;
            ViewBag.OrganizationId = user.Organization.Id;
            ViewBag.Page = page;

            var pages = review.Answers.GroupBy(x => x.AboutUserId).ToList();

            try
            {
                var p = pages[page ?? 0].ToList();
                if (p.Any(x => x.Complete) && p.Any(x => !x.Complete && x.Required))
                {
                    TempData["Message"] = DisplayNameStrings.remainingQuestions;
                    ViewBag.Incomplete = true;
                }

                var model = new TakeViewModel()
                {
                    Answers = p,
                    ForUser = p.FirstOrDefault().NotNull(x => x.AboutUser)
                };


                return View(model);
            }
            catch (ArgumentOutOfRangeException)
            {
                //Session["Page"] = page;
                return RedirectToAction("AdditionalReview",new { id=id,page=page });
            }
            #region Comment
            /*
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
                            ViewBag.AlertType = "alert-info";
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
                            return Take(id, 1);
                        if (review.Answers.Where(x => (x is FeedbackAnswer) && !x.Complete && x.Required).Any())
                            return Take(id, 2);
                        if (review.Answers.Where(x => (x is RelativeComparisonAnswer) && !x.Complete && x.Required).Any())
                            return Take(id, 3);
                        TempData["Message"] = DisplayNameStrings.youveCompletedThisReview;
                        return RedirectToAction("Index");
                    }
                default: return RedirectToAction("Index");
            }*/
            //TempData["Message"] = RadialReview.Properties.DisplayNameStrings.youveCompletedThisReview;
            /*
            foreach (var required in true.AsList(false))
            {
                //Sliders and thumbs not complete and required

                //Feedback not complete and required      
               

                //Relative Comparison not complete and required      
                ViewBag.Message = RadialReview.Properties.DisplayNameStrings.remainingIsOptional;
            }*/
            #endregion
        }

        [HttpGet]
        [Access(AccessLevel.Manager)]
        public ActionResult Create()
        {
            //TODO correct the time zone.
            var today = DateTime.UtcNow.ToLocalTime();
            var user = GetUser().Hydrate().ManagingUsers(subordinates: true).Organization().Execute();

            var teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), user.Id).ToSelectList(x => x.Name, x => x.Id).ToList();

            // teams.Add(new SelectListItem() { Text = "Subordinates", Value = "-5" });

            return PartialView(new IssueReviewViewModel()
            {
                Today = today,
                ForUsers = user.AllSubordinates,
                PotentialTeams = teams
            });
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult Create(IssueReviewViewModel model)
        {
            try
            {
                var dueDate = DateTime.Parse(model.Date);
                //var caller = GetUser().Hydrate().ManagingUsers(subordinates: true).Organization().Execute();

                /*if (!caller.ManagingOrganization)
                    throw new PermissionsException();*/

                var userId = GetUserModel().UserName;

                //TODO HERE
                new Task(() =>
                {
                    var result = _ReviewAccessor.CreateCompleteReview(GetUser(), model.ForTeamId, dueDate, model.Name, model.Emails);
                    Thread.Sleep(4000);
                    var hub=GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                    hub.Clients.User(userId).jsonAlert(ResultObject.Create(false, "Finished creating review \"" + model.Name + "\"."), true);
                    hub.Clients.User(userId).unhide("#ManageNotification");
                }).Start();

                return Json(ResultObject.Create(false,"Creating review. This will may take a few minutes."));
            }
            catch (Exception e)
            {
                return Json(new ResultObject(e));
            }
        }

        public class ReviewDetailsViewModel
        {
            public ReviewModel Review { get; set; }
            public List<SelectListItem> Axis { get; set; }
            public long xAxis { get; set; }
            public long yAxis { get; set; }
            public List<AnswerModel> AnswersAbout { get; set; }
            public Dictionary<long, String> Categories { get; set; }

        }

        ReviewDetailsViewModel GetReviewDetails(ReviewModel review)
        {
            var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id).OrderByDescending(x=>x.Id);
            var answers = _ReviewAccessor.GetAnswersForUserReview(GetUser(), review.ForUserId, review.ForReviewsId);
            var model = new ReviewDetailsViewModel()
            {
                Review = review,
                Axis = categories.ToSelectList(x => x.Category.Translate(), x => x.Id),
                xAxis = ((long?)Session["lastXAxis"]) ?? categories.FirstOrDefault().NotNull(x => x.Id),
                yAxis = ((long?)Session["lastYAxis"]) ?? categories.Skip(1).FirstOrDefault().NotNull(x => x.Id),
                AnswersAbout = answers,
                Categories = categories.ToDictionary(x => x.Id, x => x.Category.Translate()),
            };
            return model;
        }


        [Access(AccessLevel.UserOrganization)]
        public ActionResult Details(long id)
        {
            var review = _ReviewAccessor.GetReview(GetUser(), id);

            //Clients View
            if (GetUser().Id == review.ForUserId && !GetUser().ManagingOrganization)
            {
                return RedirectToAction("ClientDetails", new { id = id });

            }
            //Managers View
            else
            {
                _PermissionsAccessor.Permitted(GetUser(), x => x.ManagesUserOrganization(review.ForUserId));
                var model = GetReviewDetails(review);
                return View(model);
            }
        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult ClientDetails(long id)
        {
            var review = _ReviewAccessor.GetReview(GetUser(), id);
            var managesUser = _PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagesUserOrganization(review.ForUserId));
            if (review.ClientReview.Visible || managesUser || GetUser().ManagingOrganization)
            {
                var model = GetReviewDetails(review);
                return View(model);
            }
            else
            {
                throw new PermissionsException("This review is not visible at this time. If you feel this is in error, please contact your reviewing manager.");
            }
        }

        [Access(AccessLevel.Manager)]
        public JsonResult SetFeedback(long feedbackId, long reviewId, bool on)
        {
            if (on) _ReviewAccessor.AddFeedbackToReview(GetUser(), reviewId, feedbackId);
            else _ReviewAccessor.RemoveFeedbackFromReview(GetUser(), reviewId, feedbackId);
            return Json(ResultObject.Create(new { FeedbackId = feedbackId, On = on }), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult AddChart(long x, long y, long reviewId)
        {
            var chartId = _ReviewAccessor.AddChartToReview(GetUser(), reviewId, x, y);
            var xTitle = _CategoryAccessor.Get(GetUser(), x).Category.Translate();
            var yTitle = _CategoryAccessor.Get(GetUser(), y).Category.Translate();

            return Json(ResultObject.Create(new { XTitle = xTitle, YTitle = yTitle, ChartId = chartId }), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult RemoveChart(long chartId, long reviewId)
        {
            _ReviewAccessor.RemoveChartFromReview(GetUser(), reviewId, chartId);
            return Json(ResultObject.Create(new { ChartId = chartId }), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult Authorize(bool authorized, long reviewId)
        {
            _ReviewAccessor.Authorize(GetUser(), reviewId, authorized);
            return Json(ResultObject.Create(new { Authorized = authorized }, authorized ? "Authorized" : "Unauthorized"), JsonRequestBehavior.AllowGet);
        }

    }
}