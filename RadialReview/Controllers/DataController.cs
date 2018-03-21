using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using RadialReview.Engines;
using RadialReview.Utilities;
using RadialReview.Controllers.AbstractController;

namespace RadialReview.Controllers {
	public class DataController : BaseController {
		public class BurnDownData {
			public MetricGraphic RockCompletion { get; set; }
			public MetricGraphic Issues { get; set; }
			public MetricGraphic Todos { get; set; }
			public MetricGraphic Employees { get; set; }
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult BurnDown() {
			var rockBD = StatsAccessor.GetOrganizationRockCompletionBurndown(GetUser(), GetUser().Organization.Id);
			var issueBD = StatsAccessor.GetOrganizationIssueBurndown(GetUser(), GetUser().Organization.Id);
			var todoBD = StatsAccessor.GetOrganizationTodoBurndown(GetUser(), GetUser().Organization.Id);
			var employeeBD = StatsAccessor.GetOrganizationMemberBurndown(GetUser(), GetUser().Organization.Id);
			return Json(new BurnDownData {
				RockCompletion = rockBD,
				Issues = issueBD,
				Todos = todoBD,
				Employees = employeeBD

			}, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Dashboard() {
			return View();
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult ForceDirected() {
			var map = DeepAccessor.GetOrganizationMap(GetUser(), GetUser().Organization.Id).Where(x => x.Child.UserId != null && x.Parent.UserId != null);
			var allMembers = map.Select(x => x.Child.UserId.Value).Union(map.Select(x => x.Parent.UserId.Value)).Distinct().ToList();
			var dict = new Dictionary<long, int>();

			var membersDict = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false).ToDictionary(x => x.Id, x => x.GetName());

			var nodes = allMembers.Select((x, i) => {
				dict.Add(x, i);
				return new { name = membersDict[x], group = 1 };
			}).ToArray();

			var links = map.Select(x => new {
				source = dict[x.Parent.UserId.Value],
				target = dict[x.Child.UserId.Value],
				value = 1
			}).ToArray();

			return Json(new { nodes = nodes, links = links }, JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult ReviewsData(long id) {
			var reviewContainerId = id;
			var output = _ReviewAccessor.GetReviewStats(GetUser(), reviewContainerId);
			return Json(ResultObject.Create(output).ForceSilent(), JsonRequestBehavior.AllowGet);

		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Scatter(long id, long reviewId) {
			var chartTuple = _ReviewAccessor.GetChartTuple(GetUser(), reviewId, id);

			var reviewContainer = _ReviewAccessor.GetReviewContainerByReviewId(GetUser(), reviewId);
			var review = _ReviewAccessor.GetReview(GetUser(), reviewId);

			var title = _ChartsEngine.GetChartTitle(GetUser(), id);

			var options = new ChartOptions() {
				Id = id,
				ChartName = title,
				DeleteTime = chartTuple.DeleteTime,
				DimensionIds = "category-" + chartTuple.Item1 + ",category-" + chartTuple.Item2,
				Filters = chartTuple.Filters,
				ForUserId = review.ReviewerUserId,
				GroupBy = chartTuple.Groups,
				Options = "" + reviewContainer.Id,
				Source = ChartDataSource.Review,
			};
			var scatter = _ChartsEngine.ScatterFromOptions(GetUser(), options, false);
			return Json(ResultObject.Create(scatter).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult ScorecardData(long id) {
			var measurableId = id;
			var model = _ChartsEngine.ScorecardChart(GetUser(), measurableId);
			return Json(ResultObject.Create(model).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Line(long userId, long reviewId, string categories) {
			return null;
		}

		[Access(AccessLevel.Manager)]
		public JsonResult AggregateReviewScatter(long id, bool admin = false) {
			var reviewContainerId = id;
			var newScatter = _ChartsEngine.AggregateReviewScatter(GetUser(), id, admin);
			return Json(ResultObject.Create(newScatter).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult ReviewScatter(long id, long reviewsId) {
			var newScatter = _ChartsEngine.ReviewScatter(GetUser(), id, reviewsId, true);
			return Json(ResultObject.Create(newScatter).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult ReviewScatter2(long id, long reviewsId, string groupBy, bool client = false, bool includePrevious = false) {
			var newScatter = _ChartsEngine.ReviewScatter2(GetUser(), id, reviewsId, groupBy, !client, includePrevious);
			return Json(ResultObject.Create(newScatter).ForceSilent(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult OrganizationAccountability(long id) {
			var orgId = id;
			var tree = _OrganizationAccessor.GetOrganizationTree(GetUser(), orgId, includeRoles: true);
			return Json(tree, JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult OrganizationHierarchy(long id) {
			var tree = _OrganizationAccessor.GetOrganizationTree(GetUser(), id);
			return Json(tree, JsonRequestBehavior.AllowGet);
		}

		//[Access(AccessLevel.UserOrganization)]
		//public JsonResult OrganizationHierarchies(long id)
		//{
		//    var tree = _OrganizationAccessor.GetOrganizationTree(GetUser(), id);
		//    return Json(tree.AsList(), JsonRequestBehavior.AllowGet);
		//}

		public class Merger {
			public Dictionary<long, List<decimal>> dictionary { get; set; }

			public AboutType About { get; set; }

			public Merger(List<SliderAnswer> merger) {
				dictionary = new Dictionary<long, List<decimal>>();
				foreach (var m in merger) {
					var catId = m.Askable.Category.Id;
					var found = new List<decimal>();
					if (dictionary.ContainsKey(catId))
						found = dictionary[catId];
					for (int i = 0; i < (int)m.Askable.Weight; i++) {
						found.Add(m.Percentage.Value * 200 - 100);
					}
					dictionary[catId] = found;
					About = m.AboutType;
				}
			}

			public String ToCsv(List<QuestionCategoryModel> categories) {
				var list = categories.Select(c => {
					var catId = c.Id;
					if (dictionary.ContainsKey(catId))
						return "" + dictionary[catId].Average();
					return "0";
				}).ToList();

				var about = About.GetFlags().OrderBy(x => x).LastOrDefault();

				list.Insert(0, Convert(about));

				return String.Join(",", list);
			}

			private String Convert(Enum e) {
				var str = e.ToString();

				if (str == AboutType.Subordinate.ToString())
					return AboutType.Manager.ToString();

				if (str == AboutType.Manager.ToString())
					return AboutType.Subordinate.ToString();

				return e.ToString();
			}
		}


		[Access(AccessLevel.UserOrganization)]
		public FileContentResult ReviewData(long id, long reviewsId) {
			var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);

			var review = _ReviewAccessor.GetAnswersForUserReview(GetUser(), id, reviewsId);
			var completeSliders = review.Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>();

			var titles = categories.Select(x => "" + x.Id).ToList();
			titles.Insert(0, "about");

			var lines = completeSliders.GroupBy(x => x.ReviewerUserId).Select(x => new Merger(x.ToList()).ToCsv(categories)).ToList();
			lines.Insert(0, String.Join(",", titles));


			var csv = String.Join("\n", lines);
			return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "Report.csv");
		}

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult OrganizationReviewData(long id, long reviewsId) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditUserOrganization(id));

			var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);

			var reviewAnswers = _ReviewAccessor.GetReviewContainerAnswers(GetUser(), reviewsId);
			var completedSliders = reviewAnswers.Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>();

			var categoryIds = categories.Select(x => x.Id).ToList();

			var sb = new StringBuilder();

			//Header row
			sb.AppendLine("about," + String.Join(",", categoryIds));

			var sbMiddle = new StringBuilder();
			var sbEnd = new StringBuilder();

			foreach (var c in completedSliders.GroupBy(x => x.RevieweeUserId)) //answers about each user
			{
				var dictionary = new Multimap<long, decimal>();

				foreach (var answer in c.ToList())
					dictionary.AddNTimes(answer.Askable.Category.Id, answer.Percentage.Value * 200 - 100, (int)answer.Askable.Weight);

				var cols = new String[categoryIds.Count];

				for (int i = 0; i < categoryIds.Count; i++) {
					var datapts = dictionary.Get(categoryIds[i]);
					var average = 0m;
					if (datapts.Count > 0)
						average = datapts.Average();
					cols[i] = "" + average;
				}
				var row = String.Join(",", cols);

				sb.AppendLine("Employee," + row);

				if (c.First().RevieweeUser is UserOrganizationModel &&
					((UserOrganizationModel)(c.First().RevieweeUser)).IsManager()
					) {
					sbMiddle.AppendLine("Management," + row);
				}

				if (c.First().RevieweeUserId == id)
					sbEnd.AppendLine("You," + row);
			}
			var managers = sbMiddle.ToString();
			var you = sbEnd.ToString();

			if (!String.IsNullOrWhiteSpace(managers))
				sb.Append(managers);
			if (!String.IsNullOrWhiteSpace(you))
				sb.Append(you);




			var csv = sb.ToString();

			return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "Report.csv");

		}

	}
}
