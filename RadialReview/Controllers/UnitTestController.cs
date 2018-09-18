using System.Threading.Tasks;
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
using RadialReview.Models.Tests;
using RadialReview.Models.VideoConference;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.ViewModels;
using RadialReview.Exceptions;
using Hangfire;
using RadialReview.Crosscutting.Schedulers;
using PdfSharp.Drawing;
using RadialReview.Utilities.Pdf;
using PdfSharp.Pdf;
using MigraDoc.DocumentObjectModel;
using System.Drawing;
using RadialReview.Utilities.Constants;

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
		public ActionResult Sync(long id)
		{
			var recurrenceId = id;
			return View(recurrenceId);
		}


		[Access(AccessLevel.Radial)]
		public ActionResult SelectExistingOrCreate(bool create=false) {

			var obj = UserAccessor.BuildCreateUserVM(GetUser(), ViewBag);

			var settings = SelectExistingOrCreateUtility.Create<CreateUserOrganizationViewModel>("/User/Search", "CreateUserOrganizationViewModel",obj, create);

			return PartialView(settings);
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
			var rocks = L10Accessor.GetRocksForMeeting(GetUser(),  recurrenceId, meeting.Id);
			var model = new AngularMeeting(recurrenceId){
				MeetingId = meeting.Id
			};

			model.Name = meeting.L10Recurrence.Name;
			model.Start = meeting.StartTime.Value;

			model.Attendees= meeting._MeetingAttendees.Select(x=>AngularUser.CreateUser(x.User)).ToList();
			var aRocks = rocks.Select(x => new AngularMeetingRock(x.Rock)).ToList();
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
	    public async Task<ActionResult> AllPages()
	    {
		    var results = await new TestAccessor().RunAllUrlTests(GetUserModel().Id);
			return View(results);
	    }

	    [Access(AccessLevel.Radial)]
	    public ActionResult EditPage(long id=0)
	    {
			if (id==0)
				return View(new TestUrl(){
					Active = true,
					AsUserId = GetUser().Id,
					ExpectedCode = 200,
				});
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					return View(s.Get<TestUrl>(id));
				}
			}
	    }
		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult EditPage(TestUrl model)
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					s.SaveOrUpdate(model);
					tx.Commit();
					s.Flush();
				}
			}
			ViewBag.Message = "Added!";
			return View(model);
		}

	    [Access(AccessLevel.Radial)]
		public ActionResult Update(long id=1)
		{
			var recurrenceId = id;
			var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, load: true);
			var rocks = L10Accessor.GetRocksForMeeting(GetUser(), recurrenceId, meeting.Id);
			
			var aRocks = rocks.Select(x => new AngularMeetingRock(x.Rock)).ToList();
			
			
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
		public ActionResult Video(string room=null)
		{
			if (room == null)
				return RedirectToAction("Video", new{room = Guid.NewGuid().ToString()});
			var m = new VideoConferenceVM(){
				RoomId = room
			};
			return View(m);
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

		[Access(AccessLevel.UserOrganization)]
		public ActionResult VideoChat(long id)
		{
			return View(id);
		}

        [Access(AccessLevel.Radial)]
        public ActionResult SpeechRecog(long id) {
            return View(id);
        }

        [Access(AccessLevel.Radial)]
        public JsonResult SyncException(long? id=null) {
            throw new SyncException(id);
        }

		[Access(AccessLevel.Radial)]
		public JsonResult Hangfire(int seconds = 10) {
			Scheduler.Enqueue(() => Task.Delay(seconds*1000));
			return Json(new { status = "Started" }, JsonRequestBehavior.AllowGet);
		}


		private PdfDocumentAndStats CreateRectPage(int x, int y, int w, int h, double scale) {
			var doc = new PdfDocument();
			var p = doc.AddPage();
			var gfx = XGraphics.FromPdfPage(p);
			var ux = Unit.FromInch(x);
			var uy = Unit.FromInch(y);
			var uw = Unit.FromInch(w * scale);
			var uh = Unit.FromInch(h * scale);
			gfx.DrawString("asdb", new XFont("arial", 1 * scale), Brushes.Blue, new XPoint(ux, uy));
			var rect = new XRect(ux, uy, uw, uh);
			gfx.DrawRectangle(Brushes.Red, rect);
			return new PdfDocumentAndStats(doc, new DocStats(rect, scale));
		}

		[Access(AccessLevel.Radial)]
		public JsonResult LayoutOptimizer() {
			var docs = new[] {
				CreateRectPage(1, 1, 1, 1, 1),
				CreateRectPage(2, 1, 2, 1, 1),
				CreateRectPage(3, 1, 3, 1, 1),
				CreateRectPage(1, 1, 3, 2, 1),
				CreateRectPage(1, 2, 2, 3, 1),
				CreateRectPage(1, 3, 1, 3, 1),
				CreateRectPage(2, 2, 1, 2, 1)
			};
            var timeout = TimeSpan.FromSeconds(6);
			var layout = MultipageLayoutOptimizer.GetBestLayout(docs,new MultiPageDocument.Settings(new XSize(8.5, 11)), timeout);
			return Json(layout, JsonRequestBehavior.AllowGet);
		}

        [Access(AccessLevel.Radial)]
        public JsonResult TempCred() {
            if (Config.IsLocal()) {
                return Json(KeyManager.ProductionDatabaseCredentials, JsonRequestBehavior.AllowGet);
            }
            return null;
        }
	}
}