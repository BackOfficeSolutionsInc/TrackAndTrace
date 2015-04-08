using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using RadialReview.Models.Json;
using RadialReview.Models.Todo;

namespace RadialReview.Controllers
{
    public class TodoController : BaseController
	{
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Previous(long id)
		{
			var recurrenceId = id;
			var model = L10Accessor.GetPreviousTodos(GetUser(), recurrenceId);

			/*var model = new List<TodoVM()
			{
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				PossibleUsers = recur._DefaultAttendees
					.Select(x => x.User)
					.Select(x => new AccountableUserVM()
					{
						id = x.Id,
						imageUrl = x.ImageUrl(true, ImageSize._32),
						name = x.GetName()
					}).ToList(),
			};*/
			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult CreateTodo(long meeting, long recurrence)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
			
			var model = new TodoVM()
			{
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				PossibleUsers = recur._DefaultAttendees
					.Select(x=>x.User)
					.Select(x=>new AccountableUserVM(){
						id=x.Id,
						imageUrl = x.ImageUrl(true,ImageSize._32),
						name = x.GetName()
					}).ToList(),
			};
			return PartialView("CreateTodo", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateTodo(TodoVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			TodoAccessor.CreateTodo(GetUser(), model.RecurrenceId, new TodoModel()
			{
				CreatedById = GetUser().Id,
				ForRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message ?? "",
				Details = model.Details ?? "",
				ForModel = "TodoModel",
				ForModelId = -1,
				Organization = GetUser().Organization,
				AccountableUserId = model.AccountabilityId
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult CreateScorecardTodo(long meeting, long recurrence, long measurable, long score, long? accountable = null)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = ScorecardAccessor.GetScoreInMeeting(GetUser(), score, recurrence);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var model = new ScoreCardTodoVM()
			{
				ByUserId = GetUser().Id,
				Message = s.GetTodoMessage(),
				Details = s.GetTodoDetails(),
				MeasurableId = measurable,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				AccountabilityId = accountable ?? 0,
				PossibleUsers = recur._DefaultAttendees
					.Select(x => x.User)
					.Select(x => new AccountableUserVM()
					{
						id = x.Id,
						imageUrl = x.ImageUrl(true, ImageSize._32),
						name = x.GetName()
					}).ToList(),
			};
			return PartialView("ScorecardTodoModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateScorecardTodo(ScoreCardTodoVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.MeasurableId,x=>x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			TodoAccessor.CreateTodo(GetUser(), model.RecurrenceId, new TodoModel()
			{
				CreatedById = GetUser().Id,
				ForRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message ?? "",
				Details = model.Details ?? "",
				ForModel = "MeasurableModel",
				ForModelId = model.MeasurableId,
				Organization = GetUser().Organization,
				AccountableUserId = model.AccountabilityId
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
			//return PartialView("ScorecardIssueModal", model);
		}
		[Access(AccessLevel.UserOrganization)]
		public ActionResult CreateRockTodo(long meeting, long recurrence, long rock, long? accountable = null)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = RockAccessor.GetRockInMeeting(GetUser(), rock, meeting);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var model = new RockTodoVM()
			{
				ByUserId = GetUser().Id,
				Message = s.GetTodoMessage(),
				Details = s.GetTodoDetails(),
				RockId = rock,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				AccountabilityId = accountable ?? 0,
				PossibleUsers = recur._DefaultAttendees
					.Select(x => x.User)
					.Select(x => new AccountableUserVM()
					{
						id = x.Id,
						imageUrl = x.ImageUrl(true, ImageSize._32),
						name = x.GetName()
					}).ToList(),
			};
			return PartialView("RockTodoModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateRockTodo(RockTodoVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RockId,x=>x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			TodoAccessor.CreateTodo(GetUser(), model.RecurrenceId, new TodoModel()
			{
				CreatedById = GetUser().Id,
				ForRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message ?? "",
				Details = model.Details ?? "",
				ForModel = "RockModel",
				ForModelId = model.RockId,
				Organization = GetUser().Organization,
				AccountableUserId = model.AccountabilityId
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
			//return PartialView("ScorecardIssueModal", model);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult CreateTodoFromIssue(long meeting, long recurrence,long issue)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));
			var i = IssuesAccessor.GetIssue(GetUser(), issue);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence,true);

			

			var model = new TodoFromIssueVM()
			{
				Message = i.GetTodoMessage(),
				Details = i.GetTodoDetails(),
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				IssueId = issue,
				RecurrenceId = recurrence,
				AccountabilityId = L10Accessor.GuessUserId(i),
				PossibleUsers = recur._DefaultAttendees
					.Select(x => x.User)
					.Select(x => new AccountableUserVM()
					{
						id = x.Id,
						imageUrl = x.ImageUrl(true, ImageSize._32),
						name = x.GetName()
					}).ToList(),
			};
			return PartialView("CreateTodoFromIssue", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateTodoFromIssue(TodoFromIssueVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId,x=>x.IssueId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			TodoAccessor.CreateTodo(GetUser(), model.RecurrenceId, new TodoModel()
			{
				CreatedById = GetUser().Id,
				ForRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message ?? "",
				Details = model.Details ?? "",
				ForModel = "IssueModel",
				ForModelId = model.IssueId,
				Organization = GetUser().Organization,
				AccountableUserId = model.AccountabilityId
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

    }
}