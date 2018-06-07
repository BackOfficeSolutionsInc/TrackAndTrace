using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Angular;
using RadialReview.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.People.Controllers
{
    public class PeopleAnalyzerController : BaseController
    {
        // GET: People/PeopleAnalyzer
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index(bool noheading = false, long? recurrenceId = null) {
			ViewBag.NoTitleBar = noheading;
			//var pa = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), GetUser().Id);
			ViewBag.RecurrenceId = recurrenceId;
			return View();// pa);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Data(long? userId = null, long? recurrenceId = null) {
			AngularPeopleAnalyzer pa;
			if (recurrenceId!=null)
				pa = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(GetUser(), userId ?? GetUser().Id, recurrenceId.Value);
			else
				pa = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), userId ?? GetUser().Id);
			return Json(pa, JsonRequestBehavior.AllowGet);
		}

	}
}