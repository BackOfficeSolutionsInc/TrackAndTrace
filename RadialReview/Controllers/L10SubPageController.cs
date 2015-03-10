﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RestSharp.Validation;

namespace RadialReview.Controllers
{
	public partial class L10Controller : BaseController
	{

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Load(long id, string connection, string page = null)
		{
			var recurrenceId = id;
			page = page.ToLower();
			if (!String.IsNullOrEmpty(page))
				L10Accessor.UpdatePage(GetUser(), GetUser().Id, recurrenceId, page, connection);

			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
			var model = new L10MeetingVM(){Recurrence = recurrence};

			//Dont need the meeting 
			switch(page){
				case "stats":
					return MeetingStats(recurrenceId);
				case "startmeeting":
					return StartMeeting(model, true);
				default:
					break; //fall through
			}

			//Do need the meeting
			try{
				model.Meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, load: true);

				switch (page)
				{
					case "rocks":
						return Rocks(model);
					case "todo":
						return Todo(model);
					case "scorecard":
						return ScoreCard(model);
					case "segue":
						return Segue(model);
					case "conclusion":
						return Conclusion(model, null, true);
					case "ids":
						return IDS(model);
					case "stats":
						throw new Exception("Handled above");
					case "startmeeting":
						throw new Exception("Handled above");
					case "":{
						var meetingPage = L10Accessor.GetCurrentL10MeetingLeaderPage(GetUser(), model.Meeting.Id);
						if (String.IsNullOrEmpty(meetingPage))
							return RedirectToAction("Load", new{id = id, page = "segue"});
						return RedirectToAction("Load", new{id = id, page = meetingPage});
					}
					default:
						throw new MeetingException("Page doesn't exist", MeetingExceptionType.Error);
				}
			}
			catch (MeetingException e){
				if (e.MeetingExceptionType == MeetingExceptionType.Unstarted){
					if (page != "startmeeting"){
						ViewBag.Message = "You must start the meeting first.";
					}
					return StartMeeting(model, false);
				}

				if (e.MeetingExceptionType == MeetingExceptionType.Error)
					return Error(e);
			}

			return null;
			
		}

		#region StartMeeting
		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult StartMeeting(L10MeetingVM model, bool start)
		{
			return PartialView("StartMeeting", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult StartMeeting(L10MeetingVM model)
		{
			ValidateValues(model, x => x.Recurrence.Id);

			if (model.Attendees == null || model.Attendees.Count() == 0)
			{
				ModelState.AddModelError("Attendees", "At least one attendee is required.");
			}

			if (ModelState.IsValid)
			{

				var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

				var attendees = allMembers.Where(x => model.Attendees.Contains(x.Id)).ToList();
				L10Accessor.StartMeeting(GetUser(), GetUser(), model.Recurrence.Id, attendees);
				return RedirectToAction("Load", new { id = model.Recurrence.Id, page = "Segue" });
			}

			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), model.Recurrence.Id, true);
			model.Recurrence._DefaultAttendees = recurrence._DefaultAttendees;

			return StartMeeting(model, false);
		}
		#endregion

		#region Segue
		private PartialViewResult Segue(L10MeetingVM model)
		{
			return PartialView("Segue", model);
		}
		#endregion

		#region ScoreCard
		private PartialViewResult ScoreCard(L10MeetingVM model)
		{
			model.Scores = L10Accessor.GetScoresForRecurrence(GetUser(), model.Recurrence.Id);
			var ordered = model.Scores.Select(x => x.DateDue).OrderBy(x => x);
			model.StartDate = ordered.FirstOrDefault().NotNull(x => DateTime.UtcNow);
			model.EndDate = ordered.LastOrDefault().NotNull(x => DateTime.UtcNow).AddDays(7);

			var s = model.StartDate.StartOfWeek(GetUser().Organization.Settings.WeekStart).AddDays(-7 * 4);
			var e = model.EndDate.StartOfWeek(GetUser().Organization.Settings.WeekStart).AddDays(7 * 4);
			e = Math2.Min(DateTime.UtcNow, e);
			if (model.StartDate >= model.EndDate)
				throw new PermissionsException("Date ordering incorrect");
			while (true)
			{
				var currWeek = false;
				var next = s.AddDays(7);
				var s1 = s;
				if (model.Meeting.StartTime.NotNull(x => s1 <= x.Value && x.Value < next))
					currWeek = true;


				var sow = model.Recurrence.Organization.Settings.WeekStart;

				model.Weeks.Add(new L10MeetingVM.WeekVM()
				{
					DisplayDate = s.StartOfWeek(sow),
					ForWeek = s.StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
			return PartialView("Scorecard", model);
		}
		#endregion
		
		#region Rocks
		private PartialViewResult Rocks(L10MeetingVM model)
		{
			model.Rocks = L10Accessor.GetRocksForRecurrence(GetUser(), model.Recurrence.Id, model.Meeting.Id);
			return PartialView("Rocks", model);
		}
		#endregion

		#region Todo
		private PartialViewResult Todo(L10MeetingVM model)
		{
			model.Todos = L10Accessor.GetTodosForRecurrence(GetUser(), model.Recurrence.Id,model.Meeting.Id);
			return PartialView("Todo", model);
		}
		#endregion

		#region IDS

		private PartialViewResult IDS(L10MeetingVM model)
		{
			var issues = L10Accessor.GetIssuesForRecurrence(GetUser(), model.Meeting.Id, true);
			model.Issues = issues;

			return PartialView("IDS", model);
		}
		#endregion

		#region Conclusion
		private PartialViewResult Conclusion(L10MeetingVM model, FormCollection form, bool start)
		{
			return PartialView("Conclusion", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Conclusion(L10MeetingVM model, FormCollection form)
		{
			ValidateValues(model, x => x.Recurrence.Id);

			var ratingValues = new List<Tuple<long, int?>>();

			if (ModelState.IsValid)
			{
				var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
				//var attendees = allMembers.Where(x => model.Attendees.Contains(x.Id)).ToList();

				var ratingKeys = form.AllKeys.Where(x => x.StartsWith("rating_"));
				var ratingIds = ratingKeys.Select(x => long.Parse(x.Replace("rating_", ""))).ToList();

				ratingValues = ratingIds.Select(x => Tuple.Create(x, form["rating_" + x].TryParse())).ToList();
				allMembers.Select(x => x.Id).EnsureContainsAll(ratingIds);

				foreach (var r in ratingValues)
				{
					if (r.Item2 < 1 || r.Item2 > 10)
					{
						ModelState.AddModelError("rating_" + r.Item1, "Value must be between 1 and 10.");
					}
				}

				if (ratingValues.All(x => x.Item2 == null))
				{
					foreach (var r in ratingValues)
						ModelState.AddModelError("rating_" + r.Item1, "Ratings must be filled out.");
				}



				if (ModelState.IsValid)
				{
					L10Accessor.ConcludeMeeting(GetUser(), model.Recurrence.Id, ratingValues);
					return RedirectToAction("Load", new { id = model.Recurrence.Id, page = "stats" });

					//return MeetingStats(model.Recurrence.Id);

				}
			}

			var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), model.Recurrence.Id, false, true);
			model.Meeting = meeting;

			foreach (var r in model.Meeting._MeetingAttendees)
			{
				r.Rating = ratingValues.FirstOrDefault(x => x.Item1 == r.User.Id).NotNull(x => x.Item2);
			}

			return Conclusion(model, form, false);
		}
		#endregion

		#region Meeting Stats

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult MeetingStats(long recurrenceId)
		{
			var meetings = L10Accessor.GetL10Meetings(GetUser(), recurrenceId, true);
			var model = new L10MeetingStatsVM()
			{
				AllMeetings = meetings
			};
			return PartialView("MeetingStats", model);
		}


		#endregion

		

	}
}