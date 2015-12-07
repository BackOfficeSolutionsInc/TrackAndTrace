using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Application;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public partial class MeetingController : BaseController
    {
		// GET: Meeting
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            return View();
        }

		[Access(AccessLevel.UserOrganization)]
	    public ActionResult Attend(long id){
		    return View(id);
		}

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult Template(string id)
	    {
		    var approved = new []{"segue","scorecard","rocks","headlines","todo","ids","conclusion"};
		    if (approved.Contains(id.ToLower()))
			    return PartialView("Templates/" + id);
			throw new PermissionsException("Template does not exist.");

	    }

		[Access(AccessLevel.UserOrganization)]
	    public JsonResult Data(long id)
	    {
		    var current = L10Accessor.GetCurrentL10Meeting(GetUser(), id, false, true, true);
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), id, true);
		    if (current != null){
				var model = new AngularMeeting(id);
				model.AgendaItems.Add(new AngularAgendaItem_Segue(-1));
				//Scorecard 
			    var scorecard = new AngularAgendaItem_Scorecard(-2);
			    var scores =L10Accessor.GetScoresForRecurrence(GetUser(), id);
				scorecard.Measurables = current._MeetingMeasurables.Select(x => new AngularMeetingMeasurable(x)).ToList();


				var sow = GetUser().Organization.Settings.WeekStart;
				var offset = GetUser().Organization.GetTimezoneOffset();
			    var period = GetUser().Organization.Settings.ScorecardPeriod;


				scorecard.Weeks = TimingUtility.GetPeriods(sow, offset, DateTime.UtcNow, current.StartTime, scores, true, period, new YearStart(GetUser().Organization)).Select(x => new AngularWeek(x)).ToList();
			    scorecard.Scores = scores.Select(x => new AngularScore(x)).ToList();

				model.AgendaItems.Add(scorecard);


				//Rocks
			    var rockPage = new AngularAgendaItem_Rocks(-3, GetUser().Organization.Settings.RockName + " Review");
			    rockPage.Rocks = current._MeetingRocks.Select(x => new AngularMeetingRock(x)).ToList();
				model.AgendaItems.Add(rockPage);

				model.AgendaItems.Add(new AngularAgendaItem_Headlines(-4));
				model.AgendaItems.Add(new AngularAgendaItem_Todos(-5));
				model.AgendaItems.Add(new AngularAgendaItem_IDS(-6));
				model.AgendaItems.Add(new AngularAgendaItem_Conclusion(-7));

			    model.Notes = recurrence._MeetingNotes.Select(x => new AngularMeetingNotes(x)).ToList();

			    model.Start = current.StartTime;
				model.Attendees = current._MeetingAttendees.Select(x => AngularUser.CreateUser(x.User)).ToList();
			    model.Name = recurrence.Name;
			    model.MeetingId = current.Id;
				model.Leader = AngularUser.CreateUser(current.MeetingLeader);
			    model.CurrentPage = current._MeetingLeaderCurrentPage;

				return Json(model,JsonRequestBehavior.AllowGet);
		    }
			throw new PermissionsException("Meeting has not started");
	    }
    }
}