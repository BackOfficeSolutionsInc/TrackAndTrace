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

namespace RadialReview.Controllers
{
    public partial class L10Controller : BaseController
    {

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Details(long id,bool complete=false)
		{
			ViewBag.NumberOfWeeks = (int)Math.Ceiling(TimingUtility.ApproxDurationOfPeriod(GetUser().Organization.Settings.ScorecardPeriod).TotalDays)*13;
			
            var recur = L10Accessor.GetL10Recurrence(GetUser(),id,false);

            ViewBag.VtoId = recur.VtoId;
            ViewBag.IncludeHeadlines = recur.HeadlineType;
            ViewBag.ShowPriority = (/*recur.Prioritization == Models.L10.PrioritizationType.Invalid||*/recur.Prioritization == Models.L10.PrioritizationType.Priority);

            
            return View(id);
		}

	    [Access(AccessLevel.UserOrganization)]
        [OutputCache(NoStore = true, Duration = 0)]
	    public JsonResult DetailsData(long id,bool scores = true,bool historical=true,long start=0, long end= long.MaxValue,bool fullScorecard = false)
        {
            var startRange = start.ToDateTime();
            var endRange   = end.ToDateTime();

            var model = L10Accessor.GetAngularRecurrence(GetUser(), id, scores, historical, fullScorecard: fullScorecard,range:new DateRange(startRange, endRange));
            //model.Name=null;
		    return Json(model, JsonRequestBehavior.AllowGet);
	    }       		

    }
}