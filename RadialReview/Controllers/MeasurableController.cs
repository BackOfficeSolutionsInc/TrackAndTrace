using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Json;
using RadialReview.Models.Scorecard;

namespace RadialReview.Controllers
{
    public class MeasurableController : BaseController
    {

		public class MeasurableVM
		{
			public long UserId { get; set; }
			public List<MeasurableModel> Measurables { get; set; }
			public DateTime CurrentTime = DateTime.UtcNow;

		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id)
		{
			var rocks = ScorecardAccessor.GetUserMeasurables(GetUser(), id);
			ViewBag.AllMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false).ToSelectList(x => x.GetNameAndTitle(), x => x.Id);

			return PartialView(new MeasurableController.MeasurableVM { Measurables = rocks, UserId = id });
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankEditorRow()
		{
			ViewBag.AllMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false).ToSelectList(x=>x.GetNameAndTitle(),x=>x.Id);

			return PartialView("_MeasurableRow", new MeasurableModel(){
				CreateTime = DateTime.UtcNow,
				NextGeneration = DateTime.UtcNow-TimeSpan.FromDays(7),
				DueDate = DayOfWeek.Friday,
				DueTime = TimeSpan.FromHours(12).Add(TimeSpan.FromMinutes(-1*GetUser().Organization.Settings.TimeZoneOffsetMinutes)),
			});
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(MeasurableController.MeasurableVM model)
		{
			var avail = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false).Select(x => x.Id).ToList();

			if (!avail.Contains(model.UserId))
				throw new PermissionsException();
			

			foreach (var r in model.Measurables){
				r.AccountableUserId = model.UserId;
				if (!avail.Contains(r.AdminUserId))
					throw new PermissionsException();
			}
			ScorecardAccessor.EditMeasurables(GetUser(), model.UserId, model.Measurables);
			return Json(ResultObject.SilentSuccess());
		} 
    }
}