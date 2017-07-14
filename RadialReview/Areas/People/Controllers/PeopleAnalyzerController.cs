using RadialReview.Areas.People.Accessors;
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
        public ActionResult Index()
        {
			//var pa = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), GetUser().Id);
			return View();// pa);
        }
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Data(long? userId=null) {
			var pa = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), userId??GetUser().Id);
			return Json(pa, JsonRequestBehavior.AllowGet);
		}
    }
}