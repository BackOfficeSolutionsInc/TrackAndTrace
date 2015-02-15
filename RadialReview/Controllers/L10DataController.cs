using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Hubs;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public partial class L10Controller : BaseController
    {
		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
        public ActionResult UpdateScore(long id,long s,long w,long m,string value,string dom)
		{
			var recurrenceId = id;
			var scoreId = s;
			var week = w.ToDateTime();
			var measurableId = m;
			decimal measured;
			decimal? val = null;
			string output = null;
			if (decimal.TryParse(value, out measured)){
				val = measured;
				output = value;
			}
			ScorecardAccessor.UpdateScoreInMeeting(GetUser(), recurrenceId, scoreId, week, measurableId, val,dom);


			return Json(ResultObject.SilentSuccess(output), JsonRequestBehavior.AllowGet);
		}
    }
}