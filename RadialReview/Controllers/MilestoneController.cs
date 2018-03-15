﻿using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Models.Rocks;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class MilestoneController : BaseController {
		// GET: Milestone
		public ActionResult Index() {
			return View();
		}


		//[Access(AccessLevel.UserOrganization)]
		//public async Task<ActionResult> Pad(long id) {
		//	try {
		//		var rock = RockAccessor.GetRock(GetUser(), id);
		//		var padId = rock.PadId;
		//		if (!_PermissionsAccessor.IsPermitted(GetUser(), x => x.EditRock(id))) {
		//			padId = await PadAccessor.GetReadonlyPad(rock.PadId);
		//		}
		//		return Redirect(Config.NotesUrl("p/" + padId + "?showControls=true&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + Url.Encode(GetUser().GetName())));
		//	} catch (Exception e) {
		//		return RedirectToAction("Index", "Error");
		//	}
		//}
		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult EditModal(long id) {
			var ms = RockAccessor.GetMilestone(GetUser(), id);
			return PartialView(ms);
		}
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult EditModal(Milestone model) {
			RockAccessor.EditMilestone(GetUser(),model.Id,model.Name );
			return Json(ResultObject.SilentSuccess());
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult Add(DateTime dueDate, string milestone, long rockId) {

			var d = GetUser().GetTimeSettings().ConvertFromServerTime(dueDate);
			d = d.Date.AddDays(1).AddMilliseconds(-1);
			d = GetUser().GetTimeSettings().ConvertToServerTime(d);


			RockAccessor.AddMilestone(GetUser(), rockId, milestone, d);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult Edit(Milestone milestone) {
			RockAccessor.EditMilestone(GetUser(), milestone.Id, milestone.Name, milestone.DueDate, milestone.Required, milestone.Status);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Delete(long id) {
			RockAccessor.DeleteMilestone(GetUser(), id);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}

	}
}