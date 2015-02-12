using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Helpers;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Accessors;
using RadialReview.Models.Scorecard;

namespace RadialReview.Controllers
{
    public class L10Controller : BaseController
    {
	    public class L10Listing
		{
			public List<L10VM> Recurrences { get; set; }
			public List<L10Meeting> Meetings { get; set; }

		    public L10Listing()
			{
				Recurrences = new List<L10VM>();
				Meetings = new List<L10Meeting>();
		    }
	    }
        // GET: L10
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
		{
			var recurrences = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id,true);
			var model = new L10Listing(){
				Recurrences = recurrences,
			};
			return View(model);
        }


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Meeting(long id)
		{
			var recurrenceId = id;
			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Edit(long id)
		{
			var recurrenceId = id;
			var r = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId);

			var allMeasurables = ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var model = new L10Edit()
			{
				Recurrence = r,

				PossibleMeasurables = allMeasurables,
				PossibleMembers = allMembers,

				SelectedMeasurables = r._DefaultMeasurables.Select(x => x.Measurable.Id).ToArray(),
				SelectedMembers = r._DefaultAttendees.Select(x => x.User.Id).ToArray(),

			};
			return View("Edit", model);
		}
		
	    public class L10Edit
	    {
		    public L10Recurrence Recurrence { get; set; }
			public List<UserOrganizationModel> PossibleMembers { get; set; }
			public List<MeasurableModel> PossibleMeasurables { get; set; }
			[MinLength(1)]
			public long[] SelectedMembers { get; set; }
			public long[] SelectedMeasurables { get; set; }

		    public L10Edit()
			{
				SelectedMembers = new long[0] { };
				SelectedMeasurables = new long[0] { };
		    }
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Create()
		{
			var m = new L10Recurrence();
			
			var allMeasurables = ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var model = new L10Edit(){
				Recurrence = new L10Recurrence(){
					CreateTime = DateTime.UtcNow,
				},
				PossibleMeasurables = allMeasurables,
				PossibleMembers = allMembers,
				SelectedMeasurables = new long[0],
				SelectedMembers = new long[0],
			};

			return View("Edit",model);
		}

		[HttpPost]
	    [Access(AccessLevel.UserOrganization)]
	    public ActionResult Edit(L10Edit model)
	    {

			ValidateValues(model,x=>x.Recurrence.Id,x=>x.Recurrence.CreateTime);


			var allMeasurables = ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
			if (ModelState.IsValid){
				model.Recurrence.OrganizationId = GetUser().Organization.Id;
				model.Recurrence._DefaultAttendees = allMembers.Where(x => model.SelectedMembers.Any(y => y == x.Id))
				.Select(x=>new L10Recurrence.L10Recurrence_Attendee(){
					L10Recurrence = model.Recurrence,
					User = x
				}).ToList();
				model.Recurrence._DefaultMeasurables = allMeasurables.Where(x => model.SelectedMeasurables.Any(y => y == x.Id))
				.Select(x => new L10Recurrence.L10Recurrence_Measurable(){
					L10Recurrence = model.Recurrence,
					Measurable = x
				}).ToList();

				L10Accessor.EditL10Recurrence(GetUser(),model.Recurrence);

				return RedirectToAction("Index");
			}

			model.PossibleMeasurables = allMeasurables;
			model.PossibleMembers = allMembers;
			model.SelectedMeasurables = model.SelectedMeasurables ?? new long[0];
			model.SelectedMembers = model.SelectedMembers ?? new long[0];

			return View("Edit",model);
		}
    }
}