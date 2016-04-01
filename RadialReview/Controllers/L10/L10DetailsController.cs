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

            
            return View(id);

			//switch (id.ToLower())
			//{
			//	case "todo": return DetailsTodo(complete);
			//	case "issues": return DetailsIssues();
			//	case "scorecard": return DetailsScorecard();
			//	case "recent": return DetailsRecent();
			//	default:throw new PermissionsException("Page does not exist");
			//}
		}

	    [Access(AccessLevel.UserOrganization)]
	    public JsonResult DetailsData(long id)
	    {
            var model=L10Accessor.GetAngularRecurrence(GetUser(), id);
            model.Name=null;
		    return Json(model, JsonRequestBehavior.AllowGet);
	    }
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularRock(AngularRock model, string connectionId = null)
		{
			L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularIssue(AngularIssue model, string connectionId = null)
		{
			L10Accessor.Update(GetUser(),model,connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularTodo(AngularTodo model, string connectionId = null)
		{
			L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularScore(AngularScore model, string connectionId = null)
		{
			L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularMeetingNotes(AngularMeetingNotes model, string connectionId = null)
		{
			L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		/*
	    private PartialViewResult DetailsTodo(long id,bool complete)
	    {
		    L10Accessor.GetVisibleTodos(GetUser(), new []{GetUser().Id}, complete);
		    return null;
	    }

		private PartialViewResult DetailsIssues()
		{
			return null;
		}
		private PartialViewResult DetailsScorecard()
		{
			return null;
		}
		private PartialViewResult DetailsRecent()
		{
			return null;
		}*/

    }
}