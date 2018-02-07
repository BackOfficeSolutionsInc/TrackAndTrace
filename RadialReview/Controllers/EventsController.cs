using RadialReview.Accessors;
using RadialReview.Models.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class EventsController : BaseController
    {

		public class EventCreate {
			//public string T

		}

		// GET: Events
		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Index() {
			var subs = await EventAccessor.GetEventSubscriptions(GetUser(), GetUser().Id);
			return View(subs);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Create() {
			var form = await EventAccessor.CreateForm(GetUser(),EventAccessor.GetDefaultAvailableAnalyzers());
			return Json(ResultObject.SilentSuccess(form),JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Edit(long id) {
			var sub = await EventAccessor.GetEventSubscription(GetUser(), id);
			var gens = EventAccessor.BuildFromSubscription(sub);
			var form = await EventAccessor.CreateForm(GetUser(),new[] {gens });
			return Json(ResultObject.SilentSuccess(form), JsonRequestBehavior.AllowGet);
		}



		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> Edit(string body) {			
			var evt = EventAccessor.BuildFromJson(ReadBody());
			var r=await EventAccessor.SubscribeToEvent(GetUser(), GetUser().Id, evt);

			return Json(ResultObject.SilentSuccess(r));
		}



	}
}