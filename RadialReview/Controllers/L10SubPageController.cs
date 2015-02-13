using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RestSharp.Validation;

namespace RadialReview.Controllers
{
	public partial class L10Controller : BaseController
	{

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Load(long id, string page = null)
		{
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId);
			var model = new L10MeetingVM() { Recurrence = recurrence };
			try{
				model.Meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId);

				switch (page.ToLower())
				{
					case "scorecard":
						return ScoreCard(model);
					case "segue":
						return Segue(model);
					default: throw new MeetingException("Page doesn't exist",MeetingExceptionType.Error);
				}
			}catch (MeetingException e){
				if (e.MeetingExceptionType == MeetingExceptionType.Unstarted){
					ViewBag.Message = "You must start the meeting first.";
					return StartMeeting(model,false);
				}

				if (e.MeetingExceptionType == MeetingExceptionType.Error)
					return Error(e);
			}

			return null;
		}

		private PartialViewResult Error(MeetingException e)
		{
			return PartialView("Error", e);
		}

		private PartialViewResult Segue(L10MeetingVM model)
		{
			return PartialView("Segue", model);
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult StartMeeting(L10MeetingVM model,bool start)
		{
			return PartialView("StartMeeting", model);
		}


		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult StartMeeting(L10MeetingVM model)
		{
			ValidateValues(model, x => x.Recurrence.Id);
			
			if (model.Attendees == null || model.Attendees.Count() == 0){
				ModelState.AddModelError("Attendees","At least one attendee is required.");
			}

			if (ModelState.IsValid){

				var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

				var attendees =allMembers.Where(x => model.Attendees.Contains(x.Id)).ToList();
				L10Accessor.StartMeeting(GetUser(), model.Recurrence.Id, attendees);
				return RedirectToAction("Load", new {id = model.Recurrence.Id, page = "Segue"});
			}
			
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), model.Recurrence.Id);
			model.Recurrence._DefaultAttendees = recurrence._DefaultAttendees;

			return StartMeeting(model, false);
		}


		private PartialViewResult ScoreCard(L10MeetingVM model)
		{
			model.Scores = L10Accessor.GetScoresForRecurrence(GetUser(), model.Recurrence.Id);
			var ordered = model.Scores.Select(x => x.DateDue).OrderBy(x => x);
			model.StartDate = ordered.FirstOrDefault().NotNull(x => DateTime.UtcNow);
			model.EndDate = ordered.LastOrDefault().NotNull(x => DateTime.UtcNow).AddDays(7);

			var s = model.StartDate.StartOfWeek(GetUser().Organization.Settings.WeekStart).AddDays(-7*4);

			if (model.StartDate >= model.EndDate)
				throw new PermissionsException("Date ordering incorrect");
			while (true)
			{
				model.DateRanges.Add(Tuple.Create(s, s.AddDays(7)));
				s = s.AddDays(7);
				if (s >= model.EndDate)
					break;
			}

			return PartialView("Scorecard", model);
		}

	}
}