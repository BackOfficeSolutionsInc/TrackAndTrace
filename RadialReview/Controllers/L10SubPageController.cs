using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Models.Json;
using RadialReview.Models.L10.VM;
using RestSharp.Validation;

namespace RadialReview.Controllers
{
	public partial class L10Controller : BaseController
	{

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Load(long id, string page = null)
		{
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
			var model = new L10MeetingVM() { Recurrence = recurrence };
			try{
				model.Meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, load:true);

				switch (page.ToLower())
				{
					case "":			goto case "segue";
					case "scorecard":	return ScoreCard(model);
					case "segue":		return Segue(model);
					case "conclusion":  return Conclusion(model,null,true);
					default:throw new MeetingException("Page doesn't exist",MeetingExceptionType.Error);
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

		#region StartMeeting
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
			
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), model.Recurrence.Id,true);
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

			var s = model.StartDate.StartOfWeek(GetUser().Organization.Settings.WeekStart).AddDays(-7*4);
			var e = model.EndDate.StartOfWeek(GetUser().Organization.Settings.WeekStart).AddDays(7*4);
			if (model.StartDate >= model.EndDate)
				throw new PermissionsException("Date ordering incorrect");
			while (true){
				var currWeek = false;
				var next = s.AddDays(7);
				var s1 = s;
				if (model.Meeting.StartTime.NotNull(x=>s1<=x.Value && x.Value<next))
					currWeek = true;


				var sow = model.Recurrence.Organization.Settings.WeekStart;

				model.Weeks.Add(new L10MeetingVM.WeekVM(){
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

		#region Conclusion
		private PartialViewResult Conclusion(L10MeetingVM model, FormCollection form, bool start)
		{
			return PartialView("Conclusion", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Conclusion(L10MeetingVM model,FormCollection form)
		{
			ValidateValues(model, x => x.Recurrence.Id);

			var ratingValues = new List<Tuple<long, int?>>();

			if (ModelState.IsValid){
				var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
				//var attendees = allMembers.Where(x => model.Attendees.Contains(x.Id)).ToList();

				var ratingKeys = form.AllKeys.Where(x => x.StartsWith("rating_"));
				var ratingIds = ratingKeys.Select(x => long.Parse(x.Replace("rating_", ""))).ToList();

				ratingValues = ratingIds.Select(x => Tuple.Create(x, form["rating_" + x].TryParse())).ToList();
				allMembers.Select(x => x.Id).EnsureContainsAll(ratingIds);

				foreach (var r in ratingValues){
					if (r.Item2 < 1 || r.Item2 > 10)
					{
						ModelState.AddModelError("rating_" + r.Item1, "Value must be between 1 and 10.");
					}
				}

				if (ratingValues.All(x => x.Item2 == null)){
					foreach (var r in ratingValues)
						ModelState.AddModelError("rating_" + r.Item1, "Ratings must be filled out.");
				}



				if (ModelState.IsValid){
					L10Accessor.ConcludeMeeting(GetUser(), model.Recurrence.Id, ratingValues);

					return MeetingStats(model);

				}
			}

			var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), model.Recurrence.Id,false, true);
			model.Meeting = meeting;

			foreach (var r in model.Meeting._MeetingAttendees){
				r.Rating = ratingValues.FirstOrDefault(x => x.Item1 == r.User.Id).NotNull(x => x.Item2);
			}

			return Conclusion(model,form, false);
		}
		#endregion

		#region Meeting Stats


		private PartialViewResult MeetingStats(L10MeetingVM model)
		{



			return PartialView("MeetingStats", model);
		}


		#endregion

	}
}