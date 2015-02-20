using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Helpers;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Accessors;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;

namespace RadialReview.Controllers
{
    public partial class L10Controller : BaseController
    {
	    

	   

        // GET: L10
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
		{
			var recurrences = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id,true);
			var model = new L10ListingVM(){
				Recurrences = recurrences,
			};
			return View(model);
        }


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Meeting(long id)
		{
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId,true);
			var model = new L10MeetingVM(){
				Recurrence = recurrence,
				Meeting = L10Accessor.GetCurrentL10Meeting(GetUser(),recurrenceId,true)
			};

			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Edit(long id)
		{
			var recurrenceId = id;
			var r = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);

			var allMeasurables = ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var model = new L10EditVM()
			{
				Recurrence = r,

				PossibleMeasurables = allMeasurables,
				PossibleMembers = allMembers,

				SelectedMeasurables = r._DefaultMeasurables.Select(x => x.Measurable.Id).ToArray(),
				SelectedMembers = r._DefaultAttendees.Select(x => x.User.Id).ToArray(),

			};
			return View("Edit", model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Create()
		{
			var m = new L10Recurrence();
			
			var allMeasurables = ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var model = new L10EditVM(){
				Recurrence = new L10Recurrence(){
					CreateTime = DateTime.UtcNow,
					OrganizationId = GetUser().Organization.Id,
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
	    public ActionResult Edit(L10EditVM model)
	    {

			ValidateValues(model,x=>x.Recurrence.Id,x=>x.Recurrence.CreateTime,x=>x.Recurrence.OrganizationId);


			if (String.IsNullOrWhiteSpace(model.Recurrence.Name)){
				ModelState.AddModelError("Name","Meeting name is required");
			}


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


		#region Error
		[Access(AccessLevel.Any)]
		public PartialViewResult Error(MeetingException e)
		{
			return PartialView("Error", e);
		}
		[Access(AccessLevel.Any)]
		public PartialViewResult ErrorMessage(String message=null,MeetingExceptionType? type=null)
		{
			return PartialView("Error", new MeetingException(message ?? "An error has occurred.",type??MeetingExceptionType.Error));
		}
		#endregion
    }
}