using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using RadialReview.Models.Askables;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.Angular.DataType;
using System.Threading.Tasks;

namespace RadialReview.Controllers {
    public partial class L10Controller : BaseController {

        [Access(AccessLevel.UserOrganization)]
        public ActionResult Details(long id, bool complete = false, long? start = null) {
            ViewBag.NumberOfWeeks = (int)Math.Ceiling(TimingUtility.ApproxDurationOfPeriod(GetUser().Organization.Settings.ScorecardPeriod).TotalDays) * 13;

            var recur = L10Accessor.GetL10Recurrence(GetUser(), id, false);

            ViewBag.VtoId = recur.VtoId;
            ViewBag.IncludeHeadlines = recur.HeadlineType;
            ViewBag.ShowPriority = (/*recur.Prioritization == Models.L10.PrioritizationType.Invalid||*/recur.Prioritization == Models.L10.PrioritizationType.Priority);
            ViewBag.StartDate = start.NotNull(x => x.Value);
            ViewBag.Title = recur.Name;

            return View(id);
        }

		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0)]
		public async Task<JsonResult> DetailsData(long id, bool scores = true, bool historical = true, long start = 0, long end = long.MaxValue, bool fullScorecard = false,bool removeWeeks = false) {
			var startRange = Math2.Min(start.ToDateTime(), end.ToDateTime());
			var endRange = Math2.Max(start.ToDateTime(), end.ToDateTime());

			var scorecardStart = startRange;
			var scorecardEnd = endRange;

			var period = GetUser().GetTimeSettings().Period;


			var range = new DateRange(startRange, endRange);
			var scorecardRange = new DateRange(scorecardStart, scorecardEnd);

			var model = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), id, scores, historical, fullScorecard: fullScorecard, range: range, scorecardRange: scorecardRange);
			//model.Name=null;

			if (start != 0 && end != long.MaxValue) {
				model.dateDataRange = new AngularDateRange(range);
			}

			if (scores) {

				if (false && model.Scorecard.Scores.Count() > 22 * 16 && period == ScorecardPeriod.Weekly) {
					var min = TimingUtility.GetDateSinceEpoch(model.Scorecard.Scores.Min(x => x.ForWeek)).ToJavascriptMilliseconds();
					var max = TimingUtility.GetDateSinceEpoch(model.Scorecard.Scores.Max(x => x.ForWeek)).ToJavascriptMilliseconds();
					if (max != min) {
						var mid = (max + min) / 2;

						model = new AngularRecurrence(id);
						model.LoadUrls = new List<AngularString>() { };

						if (start != mid)
							model.LoadUrls.Add(new AngularString((min / 13), $"/L10/DetailsData/{id}?scores={scores}&historical={historical}&start={start}&end={mid}&fullScorecard=false"));
						if (mid != end)
							model.LoadUrls.Add(new AngularString((min / 13) - 1, $"/L10/DetailsData/{id}?scores={scores}&historical={historical}&start={ mid }&end={end}&fullScorecard=false"));

					}
				}
			}

			if (removeWeeks) {
				model.Scorecard.Weeks = null;
			}

			return Json(model, JsonRequestBehavior.AllowGet);
		}
	}
}