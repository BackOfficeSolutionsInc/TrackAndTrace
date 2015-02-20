using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public partial class L10Controller : BaseController
    {
		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public ActionResult UpdateScore(long id, long s, long w, long m, string value, string dom)
		{
			var recurrenceId = id;
			var scoreId = s;
			var week = w.ToDateTime();
			var measurableId = m;
			decimal measured;
			decimal? val = null;
			string output = null;
			if (decimal.TryParse(value, out measured))
			{
				val = measured;
				output = value;
			}
			ScorecardAccessor.UpdateScoreInMeeting(GetUser(), recurrenceId, scoreId, week, measurableId, val, dom);


			return Json(ResultObject.SilentSuccess(output), JsonRequestBehavior.AllowGet);
		}

	    public class AddMeasurableVm
		{
			public long RecurrenceId { get; set; }
			public List<SelectListItem> AvailableMeasurables  { get; set; }
			public long SelectedMeasurable { get; set; }
			public List<SelectListItem> AvailableMembers { get; set; }
			public long SelectedAccountableMember { get; set; }
			public long SelectedAdminMember { get; set; }

	    }
		
		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public ActionResult AddMeasurable(long id)
		{
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
				
			var allMeasurables = ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var addableMeasurables = allMeasurables.Except(recurrence._DefaultMeasurables.Select(x=>x.Measurable),x=>x.Id);

			var am = new AddMeasurableVm(){
				AvailableMeasurables = addableMeasurables.ToSelectList(x=>x.Title,x=>x.Id),
				AvailableMembers = allMembers.ToSelectList(x=>x.GetName(),x=>x.Id),
				RecurrenceId = recurrenceId,
			};

			return PartialView(am);
		}



    }
}