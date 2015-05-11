using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Angular;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.L10VM;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class UnitTestController : BaseController
    {
        //
        // GET: /UnitTest/
        [Access(AccessLevel.Radial)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Radial)]
        public String DbType()
        {
	        return ""+Config.GetEnv();
        }

        [Access(AccessLevel.Radial)]
        public void Status(string text="Test Status")
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
            var hubUsers = hub.Clients.User(GetUserModel().UserName);
            hubUsers.status(text);
        }



		[Access(AccessLevel.Radial)]
		public ActionResult Tristate(Tristate state = Models.Enums.Tristate.Indeterminate)
		{
			return View(state);
		}

		[Access(AccessLevel.Radial)]
		public JsonResult TestRequest(bool error = false, string message = "TestMessage")
		{

			var res = new ResultObject(error,message);
			if (!error){
				res.Status = StatusType.Success;
			}
			return Json(res, JsonRequestBehavior.AllowGet);
		}

	    [Access(AccessLevel.Radial)]
	    public JsonResult Agenda(long id=1)
	    {
		    var recurrenceId = id;
			var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, load: true);
			var rocks = L10Accessor.GetRocksForRecurrence(GetUser(),  recurrenceId, meeting.Id);
			var model = new AngularMeeting(recurrenceId){
				MeetingId = meeting.Id
			};

			model.Name = meeting.L10Recurrence.Name;
			model.Start = meeting.StartTime.Value;

			model.Attendees= meeting._MeetingAttendees.Select(x=>new AngularUser(x.User)).ToList();
			var aRocks = rocks.Select(x => new AngularMeetingRock(x)).ToList();
		    var rockPage = new AngularAgendaItem_Rocks(1,"Rock Review");
		    rockPage.Rocks = aRocks;

			model.AgendaItems = rockPage.AsList().Cast<AngularAgendaItem>().ToList();
			/*
		    var rVm =rocks.Select(x => new AgendaItem_Rocks.Rock(){
			    Completion = x.Completion,
				DueDate = DateTime.UtcNow,
				Owner = x.ForRock.AccountableUser.GetName(),
				Title = x.ForRock.Rock,
				Id = x.ForRock.Id,
		    }).ToList();

		    var agendaItemVm=new AgendaItem_Rocks(){
			    Rocks = rVm,
			    Duration = 5,
			    Name = "Rock Review"
		    };
		    var agenda = new List<AgendaItem>();
			
			agenda.Add(agendaItemVm);

		    var model = new MeetingVM(){
			    Name = "An L10 Meeting",
				MeetingId = meeting.Id,
				RecurrenceId = recurrenceId,
				AgendaItems = agenda
		    };*/

		    return Json(model,JsonRequestBehavior.AllowGet);

	    }

		[Access(AccessLevel.Radial)]
	    public ActionResult SinglePageL10()
		{
			return View();
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Update(long id=1)
		{
			var recurrenceId = id;
			var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, load: true);
			var rocks = L10Accessor.GetRocksForRecurrence(GetUser(), recurrenceId, meeting.Id);
			
			var aRocks = rocks.Select(x => new AngularMeetingRock(x)).ToList();
			
			
			var rock =aRocks.First();

			rock.Rock.Name = "What?!";
			rock.Rock.Owner= null;

			var updates = new AngularUpdate();

			updates.Add(rock);

			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
			group.update(updates);

			return Json(updates, JsonRequestBehavior.AllowGet);
		}


	    [Access(AccessLevel.Radial)]
	    public ActionResult UpdateName(long id = 1,string name="NEW_NAME",long user=604)
	    {
			var recurrenceId = id;
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

		    var updates = new AngularUpdate(){
			    new AngularUser(user){Name = name}
		    };

			group.update(updates);
			return Json(updates, JsonRequestBehavior.AllowGet);
	    }




    }
}