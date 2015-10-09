using System.Collections;
using System.Diagnostics;
using System.Web.UI;
using Microsoft.AspNet.SignalR;
using NHibernate.Util;
using RadialReview.Accessors;
using RadialReview.Engines;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Periods;
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
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Controllers
{
	public class ReviewController : BaseController
	{


		//
		// GET: /Review/
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index(String id, int page = 0)
		{
			var user = GetUser();
			var reviewCount = _ReviewAccessor.GetNumberOfReviewsForUser(user, user);
			var reviews = _ReviewAccessor.GetReviewsForUser(user, user.Id, page, user.CountPerPage, DateTime.UtcNow);
			var output = new ReviewsListViewModel()
			{
				ForUser = user,
				Reviews = reviews,
				Page = page,
				NumPages = reviewCount / (double)user.CountPerPage
			};
			ViewBag.Page = "View";
			return View(output);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Skip(FormCollection collection)
		{
			long reviewId = -1;
			int page = -1;
			DateTime dueDate;
			ParseAndSave(collection, out reviewId, out page, out dueDate);
			return Take(reviewId, page + 1);
		}

		private List<IGrouping<long, AnswerModel>> GetPages(ReviewModel review)
		{
			return review.Answers.GroupBy(x => x.AboutUserId).ToList();
		}
		private List<IGrouping<long, AnswerModel>> GetPages(long callerId, IEnumerable<ReviewModel> reviews)
		{
			return reviews.SelectMany(x => x.Answers).GroupBy(x => x.AboutUserId).OrderByDescending(x => x.Key == callerId).ToList();
		}



		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public ActionResult Take(long id, int? page)
		{
			var now = DateTime.UtcNow;
			var user = GetUser();
			var reviewContainer = _ReviewAccessor.GetReviewContainer(user, id, false, false);

			var reviews = _ReviewAccessor.GetReviewForUser(GetUser(), GetUser().Id, id);

			if (!reviews.Any())
				throw new PermissionsException("Review does not exist.");

			if (!reviews.All(x => user.UserIds.Any(y => y == x.ForUserId)))
				throw new PermissionsException("You cannot take this review.");

			ViewBag.ReviewId = id;
			ViewBag.OrganizationId = user.Organization.Id;
			ViewBag.Page = page;

			var pages = GetPages(GetUser().Id, reviews);
			
			try
			{
				var pageConcrete = page ?? 0;
				var p = pages[pageConcrete].ToListAlive();

				//_ReviewAccessor.UpdateStarted(GetUser(),p,now);

				var forUser = p.FirstOrDefault().NotNull(x => x.AboutUser);
				//ViewBag.Subheading = "Only " + forUser.GetFirstName().Possessive() + " manager will see your answers.";

				var model = new TakeViewModel(p)
				{
					Anonymous = reviewContainer.AnonymousByDefault,
					FirstPageHint = !reviews.Any(x=>x.SeenHints),
					Id = id,
					StartTime = now,
					Page = pageConcrete,
					//Answers = p,
					Editable = reviews.Any(x => x.DueDate > now),
					ForUser = forUser,
					OrderedPeople = pages.Select(x =>
						Tuple.Create(
							x.First().AboutUser.GetNameExtended(),
							x.All(y => !y.Required || y.Complete),
							x.Count(y => y.Required && y.Complete)/(decimal)x.Count(y=>y.Required)*100
						)).ToList()
				};

				if (model.Editable && p.Any(x => x.Complete) && p.Any(x => !x.Complete && x.Required))
				{
					//TempData["Message"] = DisplayNameStrings.remainingQuestions;
					ViewBag.Incomplete = true;
				}

				if (model.FirstPageHint && pageConcrete == 0 && model.Editable){
					using (var s = HibernateSession.GetCurrentSession())
					{
						using (var tx = s.BeginTransaction())
						{
							foreach (var r in reviews){
								r.SeenHints = true;
								s.Update(r);
							}
							tx.Commit();
							s.Flush();
						}
					}
				}

				return View(model);
			}
			catch (ArgumentOutOfRangeException)
			{
				//Session["Page"] = page;
				return RedirectToAction("AdditionalReview", new { id = id, page = page });
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

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Save(FormCollection collection)
		{
			long reviewId;
			int page;
			bool incomplete;
			Save(collection, out reviewId, out page, out incomplete);

			ViewBag.Incomplete = incomplete;

			return Json(new{incomplete, reviewId, page}, JsonRequestBehavior.AllowGet);
		}

		private void Save(FormCollection collection,out long reviewId,out int pageOut,out bool incomplete)
		{
			//reviewId = -1;
			var page = -1;
			DateTime dueDate;
			incomplete = false;

			if (ParseAndSave(collection, out reviewId, out page, out dueDate)){
				pageOut = page + 1;
				return;
			}

			if (dueDate > DateTime.UtcNow){
				//TempData["Message"] = DisplayNameStrings.remainingQuestions;
				incomplete = true;
			}
			pageOut = page + 1;
			return;
		} 

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Take(FormCollection collection)
		{
			long reviewId;
			int page;
			bool incomplete;
			Save(collection, out reviewId, out page, out incomplete);

			ViewBag.Incomplete = incomplete;

			return RedirectToAction("Take", new { id = reviewId, page });
		}

		private Boolean ParseAndSave(FormCollection collection, out long reviewId, out int currentPage, out DateTime dueDate)
		{
			try{
				var now = DateTime.UtcNow;
				reviewId = long.Parse(collection["reviewId"]);
				try{
					currentPage = int.Parse(collection["page"]);
				}catch (FormatException){
					currentPage = 0;
				}

				if (collection.AllKeys.Contains("back")){
					currentPage = currentPage - 2;
				}

				var sw = Stopwatch.StartNew();
				var o =_ReviewAccessor.UpdateAnswers(GetUser(),reviewId, collection, now, out dueDate);
				var end = sw.Elapsed;
				return o;


			}catch (PermissionsException e){
				throw e;
			}catch (Exception e){
				throw new PermissionsException();
			}
		}

		public class AnswerVM
		{
			public string Identifier { get; set; }
			public List<AnswerModel> FromAnswers { get; set; }
			public AnswerModel BaseAnswer { get; set; }
		}

		public class TakeViewModel
		{
			public long Id { get; set; }
			public long Page { get; set; }
			public bool Editable { get; set; }
			public DateTime StartTime { get; set; }
			public List<AnswerVM> Answers { get; set; }
			public ResponsibilityGroupModel ForUser { get; set; }
			public List<Tuple<String, bool,decimal>> OrderedPeople { get; set; }
			public bool FirstPageHint { get; set; }
			public bool Anonymous { get; set; }

			public TakeViewModel(List<AnswerModel> answers)
			{
				Answers = answers.GroupBy(x => Tuple.Create(x.Askable.Id, x.AboutUserId)).Select(x => new AnswerVM()
				{
					BaseAnswer = x.First(),
					FromAnswers = x.ToList()
				}).ToList();
			}

		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult SetNotes(long id, string notes)
		{
			_ReviewAccessor.UpdateNotes(GetUser(), id, notes);
			return Json(ResultObject.Success("Added notes.").ForceSilent());
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult AdditionalReview(long id, long page = 0)
		{
			var organizationUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
			//var review = _ReviewAccessor.GetReview(GetUser(), id);


			var existingUsers = _ReviewAccessor.GetReviewForUser(GetUser(), GetUser().Id, id).SelectMany(x => x.Answers.Select(y => y.AboutUserId).Distinct()).Distinct();



			var permittedUsers = organizationUsers.Where(orgUser => existingUsers.All(existingUserId => existingUserId != orgUser.Id));

			var selectList = permittedUsers.OrderBy(x => x.GetName()).Select(x => new SelectListItem() { Text = x.GetNameAndTitle(), Value = "" + x.Id }).ToList();

			var model = new AdditionalReviewViewModel()
			{
				Id = id,
				Possible = selectList,
				Page = page
			};


			return View(model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult AdditionalReview(AdditionalReviewViewModel model)
		{
			if (model.Users == null){
				return RedirectToAction("Index","Home");
			}

			foreach (var userId in model.Users)
			{
				_ReviewAccessor.AddToReview(GetUser(), GetUser().Id, model.Id, userId);
			}

			var reviews = _ReviewAccessor.GetReviewForUser(GetUser(), GetUser().Id, model.Id);
			var pages = GetPages(GetUser().Id, reviews);
			var found = pages.Select((x, i) => Tuple.Create(x.Key, i)).FirstOrDefault(x => x.Item1 == model.Users.FirstOrDefault());
			var pageNum = (pages.Count - 1);
			if (found != null)
				pageNum = found.Item2;
			return RedirectToAction("Take", new { id = model.Id, page = pageNum });
		}

		[HttpGet]
		[Access(AccessLevel.Manager)]
		public ActionResult Create()
		{
			throw new PermissionsException("depricated");
			/*
			//TODO correct the time zone.
			var today = DateTime.UtcNow.ToLocalTime();
			var user = GetUser().Hydrate().ManagingUsers(subordinates: true).Organization().Execute();

			var teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), user.Id).ToSelectList(x => x.Name, x => x.Id).ToList();

			// teams.Add(new SelectListItem() { Text = "Subordinates", Value = "-5" });

			return PartialView(new IssueReviewViewModel() {
				Today = today,
				ForUsers = user.AllSubordinates,
				PotentialTeams = teams
			});*/
		}


		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public ActionResult ClientDetails(long id, bool print = false, bool reviewing = false)
		{
			var review = _ReviewAccessor.GetReview(GetUser(), id);
			var managesUser = _PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagesUserOrganization(review.ForUserId, false));
			if (managesUser)
				ViewBag.Reviewing = true;
			ViewBag.ReviewId = id;

			if (review.ClientReview.Visible || managesUser || GetUser().ManagingOrganization)
			{
				var model = GetReviewDetails(review);
				ViewBag.Reviewing = reviewing;
				ViewBag.ReviewId = id;
				if (print)
					return View("ClientDetailsPrint", model);
				return View(model);
			}
			else
			{
				throw new PermissionsException("This review is not visible at this time. If you feel this is in error, please contact your reviewing manager.");
			}
		}


		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeScatter(long reviewId, bool on)
		{
			_ReviewAccessor.SetIncludeScatter(GetUser(), reviewId, on);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeTimeline(long id, bool on)
		{
			var reviewId = id;
			_ReviewAccessor.SetIncludeTimeline(GetUser(), reviewId, on);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.Manager)]
		public JsonResult EditRockCompletion(long id, Tristate val)
		{
			_ReviewAccessor.UpdateRockCompletionOverride(GetUser(), id, val);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.Manager)]
		public JsonResult SetRockCompletionComment(long id, String val)
		{
			_ReviewAccessor.UpdateRockCompletionNotes(GetUser(), id, val);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}




		[Access(AccessLevel.Manager)]
		public JsonResult SetFeedback(long feedbackId, long reviewId, bool on)
		{
			if (on)
				_ReviewAccessor.AddAnswerToReview(GetUser(), reviewId, feedbackId);
			else
				_ReviewAccessor.RemoveAnswerFromReview(GetUser(), reviewId, feedbackId);
			return Json(ResultObject.Create(new { FeedbackId = feedbackId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult UpdateScatterChart(
				long reviewId,
				string aggregateBy = null,
				string filterBy = null,
				string title = null,
				long? xAxis = null,
				long? yAxis = null,
				long? startTime = null,
				long? endTime = null,
				bool? include = null,
				bool? includePrevious = null
			)
		{

			_ReviewAccessor.UpdateScatterChart(GetUser(), reviewId, aggregateBy, filterBy, title, xAxis, yAxis, startTime.NotNull(x => x.Value.ToDateTime()), endTime.NotNull(x => x.Value.ToDateTime()), include, includePrevious);
			return Json(ResultObject.Create(new { ReviewId = reviewId }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}



		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeGWCFeedback(long answerId, bool on, string gwc)
		{
			_ReviewAccessor.SetIncludeGWCFeedback(GetUser(), on, answerId, gwc);
			return Json(ResultObject.Create(new { On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeValueFeedback(long answerId, bool on)
		{
			_ReviewAccessor.SetIncludeValueFeedback(GetUser(), on, answerId);
			return Json(ResultObject.Create(new { On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeTable(long reviewId, bool on)
		{
			_ReviewAccessor.SetIncludeQuestionTable(GetUser(), reviewId, on);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeManagerAnswers(long reviewId, bool on)
		{
			_ReviewAccessor.SetIncludeManagerAnswers(GetUser(), reviewId, on);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeSelfAnswers(long reviewId, bool on)
		{
			_ReviewAccessor.SetIncludeSelfAnswers(GetUser(), reviewId, on);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeNotes(long reviewId, bool on)
		{
			_ReviewAccessor.SetIncludeNotes(GetUser(), reviewId, on);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult SetIncludeEvaluation(long reviewId, bool on)
		{
			_ReviewAccessor.SetIncludeEvaluation(GetUser(), reviewId, on);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = on }).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult Authorize(bool authorized, long reviewId)
		{
			var result = _ReviewAccessor.Authorize(GetUser(), reviewId, authorized);
			result.Object = new { Authorized = authorized };
			return Json(result.ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		public class ReviewDetailsViewModel
		{
			public ReviewsModel ReviewContainer { get; set; }
			public ReviewModel Review { get; set; }
			public long xAxis { get; set; }
			public long yAxis { get; set; }
			public String JobDescription { get; set; }
			public List<SelectListItem> Axis { get; set; }
			public List<AnswerModel> AnswersAbout { get; set; }
			public Dictionary<long, String> Categories { get; set; }
			public List<String> Responsibilities { get; set; }
			public List<Askable> Questions { get; set; }
			public List<UserOrganizationModel> Supervisers { get; set; }
			public List<Askable> ActiveQuestions { get; set; }
			public List<ChartType> ChartTypes { get; set; }

			public PeriodModel Period { get; set; }
			public PeriodModel NextPeriod { get; set; }

			public List<RockModel> NextRocks { get; set; }


			public DateTime CurrentTime { get; set; }

			/*
			public Table EvaluationTable
			{
				get { return CompanyValuesTable; }
			}
			*
			*public Table RolesTable
			{
				get
				{
					var answers = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.GWC).Cast<GetWantCapacityAnswer>();
					var dictionary = new DefaultDictionary<String, decimal[]>(x => new decimal[]{0,0});
					var table = new TableData();
					foreach (var x in answers){
						var clz = String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason";
						
						if (x.GetIt != Tristate.Indeterminate){
							dictionary[x.Askable.GetQuestion() + " (Get It)"][0] += (x.GetIt == Tristate.True) ? 1 : 0;
							dictionary[x.Askable.GetQuestion() + " (Get It)"][1] += 1;
						}
						if (x.WantIt != Tristate.Indeterminate){
							dictionary[x.Askable.GetQuestion() + " (Want It)"][0] += (x.WantIt == Tristate.True) ? 1 : 0;
							dictionary[x.Askable.GetQuestion() + " (Want It)"][1] += 1;
						}
						if (x.HasCapacity != Tristate.Indeterminate){
							dictionary[x.Askable.GetQuestion() + " (Capacity To Do It)"][0] += (x.HasCapacity == Tristate.True) ? 1 : 0;
							dictionary[x.Askable.GetQuestion() + " (Capacity To Do It)"][1] += 1;
						}

						table.Set(x.ByUser.GetName(),x.Askable.GetQuestion()+" (Get It)",new HtmlString("<span class='fill roles " + clz + " " + x.GetIt + "' title='" + x.Reason + "'></span>"));
						table.Set(x.ByUser.GetName(),x.Askable.GetQuestion()+" (Want It)",new HtmlString("<span class='fill roles " + clz + " " + x.WantIt+ "' title='" + x.Reason + "'></span>"));
						table.Set(x.ByUser.GetName(),x.Askable.GetQuestion()+" (Capacity To Do It)",new HtmlString("<span class='fill roles " + clz + " " + x.HasCapacity + "' title='" + x.Reason + "'></span>"));
					}
					
					foreach (var kv in dictionary)
					{
						var ex = Tristate.True;
						var v = kv.Value;
						var reason = "";
						if (v[0] == 0){
							ex = Tristate.Indeterminate;
						}else if (v[0] != v[1]){
							reason = "Aiming for 0 No answers.";
							ex = Tristate.False;
						}

						var clz = String.IsNullOrWhiteSpace(reason) ? "" : "hasReason";
						table.Set("Score", kv.Key, new HtmlString("<span class='fill score " + clz + " " + ex + "' title='" + reason + "'></span>"));
					}
					table.Rows.Add("Score");

					return new Table(table);
				}
			}**
			public Table RolesTable
			{
				get
				{
					var answers = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.GWC).Cast<GetWantCapacityAnswer>();




					var dictionary = new DefaultDictionary<String, decimal[]>(x => new decimal[] { 0, 0 });
					var table = new TableData();
					foreach (var x in answers)
					{
						table.Update(x.Askable.GetQuestion(),"Get It",()=>);



						var clz = String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason";

						if (x.GetIt != Tristate.Indeterminate)
						{
							dictionary[x.Askable.GetQuestion() + " (Get It)"][0] += (x.GetIt == Tristate.True) ? 1 : 0;
							dictionary[x.Askable.GetQuestion() + " (Get It)"][1] += 1;
						}
						if (x.WantIt != Tristate.Indeterminate)
						{
							dictionary[x.Askable.GetQuestion() + " (Want It)"][0] += (x.WantIt == Tristate.True) ? 1 : 0;
							dictionary[x.Askable.GetQuestion() + " (Want It)"][1] += 1;
						}
						if (x.HasCapacity != Tristate.Indeterminate)
						{
							dictionary[x.Askable.GetQuestion() + " (Capacity To Do It)"][0] += (x.HasCapacity == Tristate.True) ? 1 : 0;
							dictionary[x.Askable.GetQuestion() + " (Capacity To Do It)"][1] += 1;
						}

						table.Set(x.ByUser.GetName(), x.Askable.GetQuestion() + " (Get It)", new HtmlString("<span class='fill roles " + clz + " " + x.GetIt + "' title='" + x.Reason + "'></span>"));
						table.Set(x.ByUser.GetName(), x.Askable.GetQuestion() + " (Want It)", new HtmlString("<span class='fill roles " + clz + " " + x.WantIt + "' title='" + x.Reason + "'></span>"));
						table.Set(x.ByUser.GetName(), x.Askable.GetQuestion() + " (Capacity To Do It)", new HtmlString("<span class='fill roles " + clz + " " + x.HasCapacity + "' title='" + x.Reason + "'></span>"));
					}

					foreach (var kv in dictionary)
					{
						var ex = Tristate.True;
						var v = kv.Value;
						var reason = "";
						if (v[0] == 0)
						{
							ex = Tristate.Indeterminate;
						}
						else if (v[0] != v[1])
						{
							reason = "Aiming for 0 No answers.";
							ex = Tristate.False;
						}

						var clz = String.IsNullOrWhiteSpace(reason) ? "" : "hasReason";
						table.Set("Score", kv.Key, new HtmlString("<span class='fill score " + clz + " " + ex + "' title='" + reason + "'></span>"));
					}
					table.Rows.Add("Score");

					return new Table(table);
				}
			}*/

			public static int NEUTRAL_CUTOFF = 2;

			public Table RockTable(long reviewId)
			{
				var rocks = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.Rock).Cast<RockAnswer>();
				var o = rocks.Select(x => Tuple.Create(x, 1)).Union(rocks.Select(x => Tuple.Create(x, 2)));


				return Table.Create(
					o,
					x => x.Item1.Askable.GetQuestion(),
					x => x.Item2 == 1 ? x.Item1.ByUser.GetName() : "Override",
					xx =>
					{
						var x = xx.Item1;
						if (xx.Item2 == 1)
						{
							return new SpanCell
							{
								Class = "fill rocks " + (String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason") +/* " " + x.Finished +*/ " " + x.Completion,
								Title = x.Reason,
								Data = new Dictionary<string, string>() { { "reviewId", "" + reviewId }, { "rockId", "" + x.Askable.Id }, { "byuserid", "" + x.ByUserId } },
							}.ToHtmlString();
						}
						else
						{
							return new SpanCell
							{
								Class = "fill rocks override " + (String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason") + /*" " + x.Finished +*/ " " + x.Completion,
								Title = x.Reason,
								Data = new Dictionary<string, string>() { { "reviewId", "" + reviewId }, { "rockId", "" + x.Askable.Id }, { "byuserid", "" + x.ByUserId } },
							}.ToHtmlString();

						}

					},
					"rocks");
			}

			private void AddCompanyValueRow(string row, TableData table, IEnumerable<IGrouping<long, CompanyValueAnswer>> values, Func<decimal, decimal, decimal, HtmlString> content, String clazz)
			{
				foreach (var x in values)
				{
					var pos = x.Count(y => y.Exhibits == PositiveNegativeNeutral.Positive);
					var neg = x.Count(y => y.Exhibits == PositiveNegativeNeutral.Negative);
					var neut = x.Count(y => y.Exhibits == PositiveNegativeNeutral.Neutral);
					var tot = x.Count(y => y.Exhibits != PositiveNegativeNeutral.Indeterminate);

					PositiveNegativeNeutral ex;

					if (neg > 0 || neut >= NEUTRAL_CUTOFF)
						ex = PositiveNegativeNeutral.Negative;
					else if (pos == tot && tot > 0)
						ex = PositiveNegativeNeutral.Positive;
					else if (tot > 0)
						ex = PositiveNegativeNeutral.Neutral;
					else
						ex = PositiveNegativeNeutral.Indeterminate;

					//var perc = Math.Round((pos + (neut/2m))/tot*100m);

					var html = new SpanCell()
					{
						Class = "fill companyValues " + clazz + " " + ex,
						Contents = content(pos, neg, neut)
					}.ToHtmlString();
					table.Set(row, x.First().Askable.GetQuestion(), html);
				}

			}


			public Table CompanyValuesScore
			{
				get
				{
					var values = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.CompanyValue).Cast<CompanyValueAnswer>().ToList();

					var data = new TableData();

					//Pull self
					var selfAns = values.Where(x => x.AboutType.HasFlag(AboutType.Self)).GroupBy(x => x.Askable.Id);
					AddCompanyValueRow(Review.ForUser.GetName(), data, selfAns, (x, y, z) => new HtmlString(""), "");
					//Pull manager
					var managers = values.Where(x => x.AboutType.HasFlag(AboutType.Subordinate)).GroupBy(x => x.ByUser.GetName());
					foreach (var m in managers)
					{
						var mAnswers = m.GroupBy(x => x.Askable.Id);
						AddCompanyValueRow(m.First().ByUser.GetName(), data, mAnswers, (x, y, z) => new HtmlString(""), "");
					}
					//Pull peer answers
					var otherAns = values.Where(x => !x.AboutType.HasFlag(AboutType.Self) && !x.AboutType.HasFlag(AboutType.Subordinate)).GroupBy(x => x.Askable.Id);
					AddCompanyValueRow("Others", data, otherAns, (pos, neg, neut) =>
					{
						var tot = pos + neg + neut;
						if (tot == 0)
							return new HtmlString("");
						return new HtmlString("" + Math.Round((pos + (neut / 2m)) / (tot) * 100m) + "<span class='percent'>%</span>");
					}, "companyValues companyValues-score");

					return new Table(data) { TableClass = "companyValues companyValues-client" };
				}
			}

			public Table CompanyValuesTable(long reviewId)
			{
				var dictionary = new DefaultDictionary<String, decimal[]>(x => new decimal[] { 0, 0, 0 });

				var values = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.CompanyValue).Cast<CompanyValueAnswer>();


				var dictionaryPerson = new DefaultDictionary<string, decimal>(x => 0);


				var table = Table.Create(
					values,
					x => x.ByUser.GetName(),
					x => x.Askable.GetQuestion(),
					x =>
					{
						var d = dictionary[x.Askable.GetQuestion()];

						dictionaryPerson[x.ByUser.GetName()] += x.Exhibits.Score2();

						d[0] += x.Exhibits.Score();
						if (x.Complete)
							d[1] += 1;
						if (x.Exhibits == PositiveNegativeNeutral.Negative)
							d[2] += 1;

						return new SpanCell
						{
							Class = "fill companyValues " + (String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason") + " " + x.Exhibits,
							Title = x.Reason,
							Data = new Dictionary<string, string>() { { "reviewId", "" + reviewId }, { "valueId", "" + x.Askable.Id }, { "byuserid", "" + x.ByUserId } },
						}.ToHtmlString();
					},
					"companyValues");

				foreach (var kv in dictionary)
				{
					PositiveNegativeNeutral ex;
					var v = kv.Value;
					var reason = "";
					if (v[1] == 0)
						ex = PositiveNegativeNeutral.Indeterminate;
					else if (v[2] > 0)
					{
						reason = "Aiming for 0 negatives.";
						ex = PositiveNegativeNeutral.Negative;
					}
					else if (v[0] == v[1]) //Num == Denom
						ex = PositiveNegativeNeutral.Positive;
					else
					{
						if ((v[1] - v[0]) * 2 >= NEUTRAL_CUTOFF)
						{
							ex = PositiveNegativeNeutral.Negative;
							reason = "Several ratings of +/- may indicate an area of improvement.";
						}
						else
						{
							ex = PositiveNegativeNeutral.Neutral;
							reason = "One +/- may indicate an area of improvement.";
						}
					}
					var clz = "";//String.IsNullOrWhiteSpace(reason) ? "" : "hasReason";
					table.Data.Set("Score", kv.Key, new HtmlString("<span class='fill score " + clz + " " + ex + "' title='" + reason.Replace("'", "&#39;").Replace("\"", "&quot;") + "'></span>"));
				}
				table.Rows = table.Rows.OrderByDescending(x =>dictionaryPerson[x]).ToList();
				table.Rows.Add("Score");


				return table;
			}

			public class ChartType
			{
				public String Title { get; set; }
				public String ImageUrl { get; set; }
				public bool Checked { get; set; }

			}

			public ReviewDetailsViewModel()
			{
				Axis = new List<SelectListItem>();
				AnswersAbout = new List<AnswerModel>();
				Categories = new Dictionary<long, string>();
				Responsibilities = new List<string>();
				Questions = new List<Askable>();
				Supervisers = new List<UserOrganizationModel>();
				ActiveQuestions = new List<Askable>();
				CurrentTime = DateTime.UtcNow;
			}

		}

		private ReviewDetailsViewModel GetReviewDetails(ReviewModel review)
		{
			var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id).OrderByDescending(x => x.Id);
			var answers = _ReviewAccessor.GetAnswersForUserReview(GetUser(), review.ForUserId, review.ForReviewsId).Alive().ToList();
			var managers = _UserAccessor.GetManagers(GetUser(), review.ForUserId);

			var user = _UserAccessor.GetUserOrganization(GetUser(), review.ForUserId, false, false);


			foreach (var c in review.ClientReview.Charts.ToListAlive())
			{
				c.Title = _ChartsEngine.GetChartTitle(GetUser(), c.Id);
			}

			var reviewContainer = _ReviewAccessor.GetReviewContainer(GetUser(), review.ForReviewsId, false, false, false);

			var questions = _AskableAccessor.GetAskablesForUser(GetUser(), review.ForUserId, review.PeriodId,new DateRange(reviewContainer.DateCreated,DateTime.UtcNow));
			var activeQuestions = questions.Where(x => answers.Any(y => y.Askable.Id == x.Id)).ToList();

			var chartTypes = new List<ReviewDetailsViewModel.ChartType>();
			chartTypes.Add(new ReviewDetailsViewModel.ChartType() { Checked = false, Title = "Aggregate All", ImageUrl = "https://s3.amazonaws.com/Radial/base/Charts/AggAll.png" });
			chartTypes.Add(new ReviewDetailsViewModel.ChartType() { Checked = false, Title = "Aggregate By Relationship", ImageUrl = "https://s3.amazonaws.com/Radial/base/Charts/AggByRelation.png" });
			chartTypes.Add(new ReviewDetailsViewModel.ChartType() { Checked = false, Title = "Show All", ImageUrl = "https://s3.amazonaws.com/Radial/base/Charts/All.png" });
			chartTypes.Add(new ReviewDetailsViewModel.ChartType() { Checked = false, Title = "Show All (Uncolored)", ImageUrl = "https://s3.amazonaws.com/Radial/base/Charts/AllGray.png" });

			var now = DateTime.UtcNow;
			var nextRocks = _RockAccessor.GetRocks(GetUser(), review.ForUserId, reviewContainer.NextPeriodId).ToList();

			var model = new ReviewDetailsViewModel()
			{
				ReviewContainer = reviewContainer,
				Review = review,
				Axis = categories.ToSelectList(x => x.Category.Translate(), x => x.Id),
				xAxis = ((long?)Session["lastXAxis"]) ?? categories.FirstOrDefault().NotNull(x => x.Id),
				yAxis = ((long?)Session["lastYAxis"]) ?? categories.Skip(1).FirstOrDefault().NotNull(x => x.Id),
				AnswersAbout = answers,
				Categories = categories.ToDictionary(x => x.Id, x => x.Category.Translate()),
				Supervisers = managers,
				Questions = questions,
				ActiveQuestions = activeQuestions,
				JobDescription = user.JobDescription,
				ChartTypes = chartTypes,
				NextRocks = nextRocks,
				Period = reviewContainer.Period,
				NextPeriod = reviewContainer.NextPeriod,
			};
			return model;
		}


		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public ActionResult Details(long id)
		{
			var review = _ReviewAccessor.GetReview(GetUser(), id);
			ViewBag.ReviewId = id;
			//Clients View
			if (GetUser().Id == review.ForUserId && !GetUser().ManagingOrganization)
			{
				return RedirectToAction("ClientDetails", new { id = id });

			}
			//Managers View
			else
			{
				ViewBag.RoleDetails = true;
				_PermissionsAccessor.Permitted(GetUser(), x => x.ManagesUserOrganization(review.ForUserId, false));
				var model = GetReviewDetails(review);
				//model.Supervisors = model.AnswersAbout.Where(x => x.ByUserId == GetUser().Id).ToList();
				return View(model);
			}
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult GWCDetails(long reviewId, long roleId, string gwc)
		{
			var review = _ReviewAccessor.GetReview(GetUser(), reviewId);
			var model = GetReviewDetails(review);
			ViewBag.RoleId = roleId;
			ViewBag.GWC = gwc;


			return PartialView(model);
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult ValueDetails(long reviewId, long valueId, long userId)
		{
			var review = _ReviewAccessor.GetReview(GetUser(), reviewId);
			var model = GetReviewDetails(review);
			ViewBag.ValueId = valueId;
			ViewBag.ByUserId = userId;


			return PartialView(model);
		}
		[Access(AccessLevel.Manager)]
		public PartialViewResult RockDetails(long reviewId, long rockId, long userId)
		{
			var review = _ReviewAccessor.GetReview(GetUser(), reviewId);
			var model = GetReviewDetails(review);
			ViewBag.RockId = rockId;
			ViewBag.ByUserId = userId;


			return PartialView(model);
		}

		//[HttpPost]
		//[Access(AccessLevel.Manager)]
		//public async Task<JsonResult> Create(IssueReviewViewModel model) {
		//	var userId = GetUserModel().UserName;
		//	try {
		//		var dueDate = DateTime.Parse(model.Date);
		//		//var caller = GetUser().Hydrate().ManagingUsers(subordinates: true).Organization().Execute();

		//		/*if (!caller.ManagingOrganization)
		//			throw new PermissionsException();*/

		//		var user = GetUser();

		//		// try
		//		// {
		//		//throw new Exception("Todo");
		//		var result = await Task.Run(() => {
		//			return _ReviewAccessor.CreateCompleteReview(user, model.ForTeamId, dueDate,
		//				model.Name, model.Emails, model.ReviewSelf, model.ReviewManagers, model.ReviewSubordinates,
		//				model.ReviewTeammates, model.ReviewPeers);
		//		});
		//		new Thread(() => {
		//			Thread.Sleep(4000);
		//			var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
		//			var hubUsers =  hub.Clients.User(userId);
		//			//var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
		//			hubUsers.jsonAlert(ResultObject.Create(false, "Finished creating review \"" + model.Name + "\"."), true);
		//			hubUsers.unhide("#ManageNotification");
		//		}).Start();
		//		//return true;
		//		/* }
		//			catch (Exception e)
		//			{
		//				//var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
		//				// hub.Clients.User(userId).jsonAlert(new ResultObject(e));
		//				//hub.Clients.User(userId).unhide("#ManageNotification");
		//				log.Error(e);
		//				throw e;
		//				// return false;
		//			}*/

		//	}
		//	catch (Exception e) {
		//		log.Error(e);
		//		new Thread(() => {
		//			var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
		//			hub.Clients.User(userId).jsonAlert(new ResultObject(e));
		//			hub.Clients.User(userId).unhide("#ManageNotification");
		//		}).Start();
		//	}
		//	return Json(ResultObject.SilentSuccess());
		//}
		/*[Access(AccessLevel.Manager)]
		public JsonResult SetScatterChart(long reviewId, string on) {
			var aggregateBy = on;
			_ReviewAccessor.SetAggregateBy(GetUser(), reviewId, aggregateBy);
			return Json(ResultObject.Create(new { ReviewId = reviewId, On = aggregateBy }), JsonRequestBehavior.AllowGet);
		}*
		 *[Access(AccessLevel.Manager)]
		public JsonResult AddChart(long x, long y, long reviewId, String groups, String filters, long start, long end) {
			var chartId = _ReviewAccessor.AddChartToReview(GetUser(), reviewId, x, y, groups, filters, start.ToDateTime(), end.ToDateTime());

			var xTitle = _CategoryAccessor.Get(GetUser(), x).Category.Translate();
			var yTitle = _CategoryAccessor.Get(GetUser(), y).Category.Translate();
			var title = _ChartsEngine.GetChartTitle(GetUser(), chartId);

			return Json(ResultObject.Create(new {
				XTitle = xTitle,
				YTitle = yTitle,
				ChartId = chartId,
				Grouped = groups,
				Filters = filters,
				Title = title,
				Start = start,
				End = end,
			}), JsonRequestBehavior.AllowGet);
		}*
		 *[Access(AccessLevel.Manager)]
		public JsonResult RemoveChart(long chartId, long reviewId) {
			_ReviewAccessor.RemoveChartFromReview(GetUser(), reviewId, chartId);
			return Json(ResultObject.Create(new { ChartId = chartId }), JsonRequestBehavior.AllowGet);
		}
		*/

	}
}