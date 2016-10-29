using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.VideoConference;
using MathNet.Numerics.Distributions;
using RadialReview.Utilities;
using WebGrease.Css.Extensions;
using RadialReview.Models.UserModels;

namespace RadialReview.Controllers
{
	public partial class L10Controller : BaseController
	{

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Load(long id, string connection, string page = null)
		{
			var recurrenceId = id;
			page = page.NotNull(x=>x.ToLower());
			if (!String.IsNullOrEmpty(page) && page!="startmeeting")
				L10Accessor.UpdatePage(GetUser(), GetUser().Id, recurrenceId, page, connection);

			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
			var model = new L10MeetingVM(){
				Recurrence = recurrence,
				EnableTranscript = recurrence.EnableTranscription,
			};
            if (model != null && model.Recurrence != null)
			{
				model.CanAdmin = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, model.Recurrence.Id));
				model.CanEdit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanEdit(PermItem.ResourceType.L10Recurrence, model.Recurrence.Id));
                model.MemberPictures = model.Recurrence._DefaultAttendees.Select(x => new ProfilePictureVM {Initials = x.User.GetInitials(),Name = x.User.GetName(),UserId=x.User.Id,Url=x.User.ImageUrl(true,ImageSize._32) }).ToList();
				model.HeadlineType = recurrence.HeadlineType;
			}
			//Dont need the meeting 
			switch(page){
				case "stats":
					return MeetingStats(recurrenceId);
				case "startmeeting":
                    if (recurrence.MeetingInProgress == null){
                        return StartMeeting(model, true);
                    }else{
                        page = "";
                        break;
                    }
				default:
					break; //fall through
			}

			//Do need the meeting
			try{
				model.Meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, load: true);

				
				
				switch (page)
				{
					case "headlines":
						return Headlines(model);
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
						if (String.IsNullOrEmpty(meetingPage) || meetingPage == "startmeeting"){

							var p = L10Accessor.GetDefaultStartPage(recurrence);
							
							return RedirectToAction("Load", new{id = id, page = p});
						}
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
        public PartialViewResult StartMeeting(L10MeetingVM model, bool start)
		{
			return PartialView("StartMeeting", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult StartMeeting(L10MeetingVM model)
		{
			ValidateValues(model, x => x.Recurrence.Id);

			/*if (model.Attendees == null || model.Attendees.Count() == 0)
			{
				ModelState.AddModelError("Attendees", "At least one attendee is required.");
			}*/

			if (ModelState.IsValid)
			{

				var allMembers = OrganizationAccessor.GetMembers_Tiny(GetUser(), GetUser().Organization.Id).Select(x=>x.UserOrgId);

                List<long> attendees = new List<long>();
                if (model.Attendees!=null)
                    attendees=allMembers.Where(x => model.Attendees.Contains(x)).ToList();
				L10Accessor.StartMeeting(GetUser(), GetUser(), model.Recurrence.Id, attendees);
				var tempRecur = L10Accessor.GetL10Recurrence(GetUser(), model.Recurrence.Id, false);
				var p = L10Accessor.GetDefaultStartPage(tempRecur);
				return RedirectToAction("Load", new { id = model.Recurrence.Id, page = p });
			}

			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), model.Recurrence.Id, true);
			model.Recurrence._DefaultAttendees = recurrence._DefaultAttendees;

			return StartMeeting(model, false);
		}
		#endregion

		#region Segue
		private PartialViewResult Segue(L10MeetingVM model)
		{
			ViewBag.Segue_Subheading = CustomizeAccessor.GetSpecificCustomization(GetUser(), GetUser().Organization.Id, CUSTOMIZABLE.Segue_Subheading,"Share good news from the last 7 days.<br/> One personal and one professional.");
			
			return PartialView("Segue", model);
		}
		#endregion

		#region ScoreCard
		private PartialViewResult ScoreCard(L10MeetingVM model)
		{
			model.Scores = L10Accessor.GetScoresForRecurrence(GetUser(), model.Recurrence.Id);

			var sow = GetUser().Organization.Settings.WeekStart;
			var offset = GetUser().Organization.GetTimezoneOffset();

			var scorecardType = GetUser().Organization.Settings.ScorecardPeriod;
			model.ScorecardType = scorecardType;
			var timeSettings = GetUser().GetTimeSettings();
			timeSettings.WeekStart = model.Recurrence.StartOfWeekOverride ?? timeSettings.WeekStart;
			timeSettings.Descending = model.Recurrence.ReverseScorecard;

			model.Weeks = TimingUtility.GetPeriods(timeSettings, DateTime.UtcNow, model.MeetingStart, /*model.Scores,*/ true);
			return PartialView("Scorecard", model);
			/*model.StartDate = ordered.FirstOrDefault().NotNull(x => DateTime.UtcNow);
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
			 }*/
			
		}
		#endregion
		
		#region Rocks
		private PartialViewResult Rocks(L10MeetingVM model)
		{
			model.Rocks = L10Accessor.GetRocksForMeeting(GetUser(), model.Recurrence.Id, model.Meeting.Id);
			return PartialView("Rocks", model);
		}
		#endregion

		#region Headlines
		private PartialViewResult Headlines(L10MeetingVM model)
		{
            ViewBag.CEH_Subheading = CustomizeAccessor.GetSpecificCustomization(GetUser(), GetUser().Organization.Id, CUSTOMIZABLE.CustomerEmployeeHeadlines_Subheading, "Share headlines about customers/clients and people in the company.<br/> Good and bad. Drop down (to the issues list) anything that needs discussion.");
			model.HeadlinesId=model.Recurrence.HeadlinesId;

			model.Headlines = L10Accessor.GetHeadlinesForMeeting(GetUser(), model.Recurrence.Id);

			return PartialView("Headlines", model);
		}
		#endregion

		#region Todo
		private PartialViewResult Todo(L10MeetingVM model)
		{
			model.Todos = L10Accessor.GetTodosForRecurrence(GetUser(), model.Recurrence.Id,model.Meeting.Id);
            model.SeenTodoFireworks = model.Meeting._MeetingAttendees.NotNull(x => x.FirstOrDefault(yx => yx.User.Id == GetUser().Id).NotNull(z=>z.SeenTodoFireworks));

            //ViewBag.TodoCompletion = L10Accessor.GetTodosCompletion(GetUser(), model.Meeting.Id);
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
            model.SendEmail = true;
			model.CloseTodos = true;
			model.CloseHeadlines = true;

			//model.Meeting._MeetingAttendees.ForEach(x=>x.Rating=x.Rating??10);

			var stats=L10Accessor.GetStats(GetUser(), model.Recurrence.Id);
			ViewBag.TodosCreated = stats.AllTodos.Where(x=>x.CompleteTime == null).ToList();

			return PartialView("Conclusion", model);
		}

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<ActionResult> ForceConclude(long id)
        {
            await L10Accessor.ConcludeMeeting(GetUser(), id,new List<Tuple<long,decimal?>>(), false,false,false,null);
            return Content("Done");
        }

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Conclusion(L10MeetingVM model, FormCollection form,string connectionId=null)
		{
			ValidateValues(model, x => x.Recurrence.Id);

			var ratingValues = new List<Tuple<long, decimal?>>();

			if (ModelState.IsValid)
			{
				//var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
				//var attendees = allMembers.Where(x => model.Attendees.Contains(x.Id)).ToList();
                //var allMembers = _OrganizationAccessor.GetAllOrganizationMemberIdsAcrossTime(GetUser(), GetUser().Organization.Id);

				var ratingKeys = form.AllKeys.Where(x => x.StartsWith("rating_"));
				var ratingIds = ratingKeys.Where(x => form[x].TryParseDecimal()!=null).Select(x => long.Parse(x.Replace("rating_", ""))).ToList();

				ratingValues = ratingIds.Select(x => Tuple.Create(x, form["rating_" + x].TryParseDecimal())).ToList();
				//allMembers./*Select(x => x.Id).*/EnsureContainsAll(ratingIds);

                _OrganizationAccessor.EnsureAllAtOrganization(GetUser(), GetUser().Organization.Id, ratingIds);

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
					await L10Accessor.ConcludeMeeting(GetUser(), model.Recurrence.Id, ratingValues, model.SendEmail, model.CloseTodos, model.CloseHeadlines, connectionId);


					//var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					//hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(model.Recurrence.Id)).setHash("stats");
					 
					//return RedirectToAction("Load", new { id = model.Recurrence.Id, page = "stats" });
					//return Json(ResultObject.SilentSuccess().NoRefresh());
					return MeetingStats(model.Recurrence.Id);
                    //return Content("Please Wait");

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
			var model=L10Accessor.GetStats(GetUser(), recurrenceId);
			

			#region For Demo
			if (recurrenceId == 1){
				var latest =model.AllMeetings.Where(x => x.CompleteTime != null).OrderByDescending(x => x.CompleteTime.Value).FirstOrDefault();

				if (latest != null){

					var maxAvg = latest._MeetingAttendees.Average(x => x.Rating)??8;
					var count = latest._MeetingAttendees.Count();
					var past = 30;
					model.AllMeetings = Enumerable.Range(1, past).Select(x => new L10Meeting()
					{
						StartTime = latest.StartTime.Value.Date+TimeSpan.FromHours(9) + TimeSpan.FromMinutes(Normal.Sample(2.5, 4)) - TimeSpan.FromDays(x * 7),
						CompleteTime = latest.StartTime.Value.Date + TimeSpan.FromHours(9) + TimeSpan.FromMinutes(Normal.Sample(3, 4) + 90) - TimeSpan.FromDays(x * 7),
						_MeetingAttendees = Enumerable.Range(1,count).Select(y=>new L10Meeting.L10Meeting_Attendee(){
							Rating = (decimal)Math2.Coerce(
							(double)Math.Round(
								-1.0m*maxAvg/(past*past*1.7777m)*x*x+
								maxAvg+(decimal)Normal.Sample(0,(1-.35)/(past-1)*x+.35)
							),1,10)
						}).ToList()

					}).ToList();
					//model.AllMeetings.Add(latest);
				}
			}
			#endregion

			return PartialView("MeetingStats", model);
		}

		#endregion


		

	}
}