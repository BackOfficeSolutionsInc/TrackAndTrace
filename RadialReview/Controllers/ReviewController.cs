using RadialReview.Accessors;
using RadialReview.Exceptions;
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
            long organizationId = -1;
            int page = -1;
            ParseAndSave(collection, out reviewId, out page);
            return Take(reviewId, page + 1);
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Take(FormCollection collection)
        {
            long reviewId = -1;
            long organizationId = -1;
            int page = -1;

            if (ParseAndSave(collection, out reviewId, out page))
            {
                return Take(reviewId, page + 1);
            }
            TempData["Message"] = DisplayNameStrings.remainingQuestions;
            ViewBag.Incomplete = true;
            return Take(reviewId, page);
        }

        private Boolean ParseAndSave(FormCollection collection, out long reviewId, out int currentPage)
        {
            try
            {
                reviewId = long.Parse(collection["reviewId"]);
                currentPage = int.Parse(collection["page"]);

                var user = GetUser();
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
                            case QuestionType.Slider: allComplete = allComplete && _ReviewAccessor.UpdateSliderAnswer(user, questionId, decimal.Parse(collection[k]) / 100.0m); break;
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

        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Take(long id, int? page)
        {
            var user = GetUser();
            var review = _ReviewAccessor.GetReview(user, id);
            ViewBag.ReviewId = id;
            ViewBag.OrganizationId = user.Organization.Id;
            ViewBag.Page = page;

            var pages = review.Answers.GroupBy(x => x.AboutUserId).ToList();

            var p=pages[page??0].ToList();

            return View(p);



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
        }

        [HttpGet]
        [Access(AccessLevel.Manager)]
        public ActionResult Create()
        {
            //TODO correct the time zone.
            var today = DateTime.UtcNow.ToLocalTime();
            var user = GetUser().Hydrate().ManagingUsers(subordinates: true).Organization().Execute();

            var teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), user.Id).ToSelectList(x => x.Name, x => x.Id).ToList();

            return PartialView(new IssueReviewViewModel() { Today = today, ForUsers = user.AllSubordinates, PotentialTeams = teams });
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult Create(IssueReviewViewModel model)
        {
            try
            {
                var dueDate = DateTime.Parse(model.Date);
                var caller = GetUser().Hydrate().ManagingUsers(subordinates: true).Organization().Execute();

                /*if (!caller.ManagingOrganization)
                    throw new PermissionsException();*/

                var organization = caller.Organization;

                var usersToReview = _TeamAccessor.GetTeamMembers(GetUser(), model.ForTeamId).ToListAlive();

                var reviewContainer = new ReviewsModel()
                {
                    DateCreated = DateTime.UtcNow,
                    DueDate = dueDate,
                    ReviewName = model.Name,
                    CreatedById = caller.Id,
                };
                _ReviewAccessor.CreateReviewContainer(caller, reviewContainer);

                List<Exception> exceptions = new List<Exception>();
                int sent = 0;
                int errors = 0;
                foreach (var beingReviewed in usersToReview)
                {
                    var beingReviewedUser = beingReviewed.User;
                    try
                    {
                        /*about self
                        var askable = new List<AskableAbout>();
                        askable.AddRange(responsibilities.Select(x => new AskableAbout() { Askable = x, AboutUserId = beingReviewed.Id, AboutType = AboutType.Self }));
                        askable.AddRange(questions.Select(x => new AskableAbout() { Askable = x, AboutUserId = beingReviewed.Id, AboutType = AboutType.Self }));
                        */

                        //Generate Askables
                        var askables = GetAskables(GetUser(), beingReviewedUser);
                        //Create the Review
                        var review = _QuestionAccessor.GenerateReviewForUser(caller, beingReviewedUser, reviewContainer, askables);
                        //Generate Review Nexus
                        Guid guid = Guid.NewGuid();
                        NexusModel nexus = new NexusModel(guid) {
                                                                    ForUserId = beingReviewed.Id,
                                                                    ActionCode = NexusActions.TakeReview
                                                                };
                        _NexusAccessor.Put(nexus);
                        //Send email
                        var subject = String.Format(RadialReview.Properties.EmailStrings.NewReview_Subject, organization.Name.Translate());
                        var body = String.Format(EmailStrings.NewReview_Body, beingReviewedUser.GetName(), caller.GetName(), dueDate.ToShortDateString(), ProductStrings.BaseUrl + "n/" + guid, ProductStrings.BaseUrl + "n/" + guid, ProductStrings.ProductName);
                        Emailer.SendEmail(beingReviewedUser.EmailAtOrganization, subject, body);
                        sent++;

                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        errors++;
                    }
                }
                return Json(JsonObject.Create(new { due = dueDate, sent = sent, errors = errors }));
            }
            catch (Exception e)
            {
                return Json(new JsonObject(e));
            }
        }

        private List<AskableAbout> GetAskables(UserOrganizationModel caller, UserOrganizationModel beingReviewed)
        {
            #region comment
            /** Old questions way to do things.
            var review = _QuestionAccessor.GenerateReviewForUser(user, s, reviewContainer);
            //review.ForReviewsId = reviewContainer.Id;
            //review.DueDate = reviewContainer.DueDate;
            //review.Name = reviewContainer.ReviewName;
            //_ReviewAccessor.UpdateIndividualReview(user, review);
            */
            #endregion

            var responsibilityGroups = _ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(GetUser(), beingReviewed.Id);

            var askable = new AskableUtility();

            // Personal Responsibilities 
            {
                var responsibilities = responsibilityGroups.SelectMany(x => x.Responsibilities).ToList();
                var questions = _QuestionAccessor.GetQuestionsForUser(GetUser(), beingReviewed.Id);

                askable.AddUnique(responsibilities, AboutType.Self, beingReviewed.Id);
                askable.AddUnique(questions, AboutType.Self, beingReviewed.Id);
            }
            // Team members 
            {
                var teams = responsibilityGroups.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>().Where(x => x.InterReview).ToList();

                foreach (var team in teams)
                {
                    var teamMembers = _TeamAccessor.GetTeamMembers(GetUser(), team.Id).ToListAlive();
                    foreach (var teammember in teamMembers)
                    {
                        var teamMemberResponsibilities = _ResponsibilitiesAccessor
                                                            .GetResponsibilityGroupsForUser(GetUser(), teammember.Id)
                                                            .SelectMany(x => x.Responsibilities)
                                                            .ToList();
                        askable.AddUnique(teamMemberResponsibilities, AboutType.Teammate, teammember.User.Id);
                    }
                }
            }
            // Peers
            {
                var peers = _UserAccessor.GetPeers(GetUser(), beingReviewed.Id);
                foreach (var peer in peers)
                {
                    var peerResponsibilities = _ResponsibilitiesAccessor
                                                        .GetResponsibilityGroupsForUser(GetUser(), beingReviewed.Id)
                                                        .SelectMany(x => x.Responsibilities)
                                                        .ToList();
                    askable.AddUnique(peerResponsibilities, AboutType.Peer, peer.Id);
                }
            }
            // Managers
            {
                var managers = _UserAccessor.GetManagers(GetUser(), beingReviewed.Id);
                foreach (var manager in managers)
                {
                    var managerResponsibilities = _ResponsibilitiesAccessor
                                                        .GetResponsibilityGroupsForUser(GetUser(), beingReviewed.Id)
                                                        .SelectMany(x => x.Responsibilities)
                                                        .ToList();
                    askable.AddUnique(managerResponsibilities, AboutType.Manager, manager.Id);
                }
            }
            // Subordinates
            {
                var subordinates = _UserAccessor.GetSubordinates(GetUser(), beingReviewed.Id);
                foreach (var subordinate in subordinates)
                {
                    var subordinateResponsibilities = _ResponsibilitiesAccessor
                                                        .GetResponsibilityGroupsForUser(GetUser(), beingReviewed.Id)
                                                        .SelectMany(x => x.Responsibilities)
                                                        .ToList();
                    askable.AddUnique(subordinateResponsibilities, AboutType.Subordinate, subordinate.Id);
                }
            }

            return askable.Askables;
        }


        [Access(AccessLevel.UserOrganization)]
        public ActionResult Details(long id)
        {
            var model = _ReviewAccessor.GetReview(GetUser(), id);

            return View(model);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Data(long id, long category1Id, long category2Id)
        {
            var model = _ReviewAccessor.GetReview(GetUser(), id);

            //HeatMapBinner.Bin(model.Answers
            //model.

            return null;
        }



    }
}