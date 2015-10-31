using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.Json;
using RadialReview.Models.Permissions;
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

		public class MeetingVm{
			public long id { get; set; }
			public string name { get; set; }
		}
	    [Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateTodoRecurrence()
	    {
			var model = new TodoVM(GetUser().Id){
				ByUserId = GetUser().Id,
				AccountabilityId = GetUser().Id,
				MeetingId = -1
			};
			ViewBag.PossibleMeetings = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id, false)
				.Select(x => new MeetingVm { name = x.Recurrence.Name, id = x.Recurrence.Id })
				.ToList();
			return PartialView("CreateTodoRecurrence", model);
	    }
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateTodoRecurrence(TodoVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId,x=>x.AccountabilityId);
			if (model.MeetingId != -1)
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
				AccountableUserId = model.AccountabilityId,
				DueDate = model.DueDate
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

	    [Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateTodo(long recurrence, long meeting = -1, string todo = null, long? modelId = null, string modelType = null)
		{
			if (meeting!=-1)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var model = new TodoVM(recur.DefaultTodoOwner)
			{
				ForModelId = modelId,
				ForModelType = modelType,
				Message = todo,
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
			if (model.MeetingId!=-1)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));
		

			TodoAccessor.CreateTodo(GetUser(), model.RecurrenceId, new TodoModel()
			{
				CreatedById = GetUser().Id,
				ForRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message ?? "",
				Details = model.Details ?? "",
				ForModel = model.ForModelType??"TodoModel",
				ForModelId = model.ForModelId??-1,
				Organization = GetUser().Organization,
				AccountableUserId = model.AccountabilityId,
				DueDate = model.DueDate
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateScorecardTodo(long meeting, long recurrence, long measurable, long score, long? accountable = null)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = ScorecardAccessor.GetScoreInMeeting(GetUser(), score, recurrence);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var model = new ScoreCardTodoVM(recur.DefaultTodoOwner)
			{
				ByUserId = GetUser().Id,
				Message = s.NotNull(x=>x.GetTodoMessage()),
				Details = s.NotNull(x=>x.GetTodoDetails()),
				MeasurableId = measurable,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				AccountabilityId = accountable ?? recur.DefaultTodoOwner,
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
				AccountableUserId = model.AccountabilityId,
				DueDate = model.DueDate
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
			//return PartialView("ScorecardIssueModal", model);
		}
		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateRockTodo(long meeting, long recurrence, long rock, long? accountable = null)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = RockAccessor.GetRockInMeeting(GetUser(), rock, meeting);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var model = new RockTodoVM(recur.DefaultTodoOwner)
			{
				ByUserId = GetUser().Id,
				Message = s.NotNull(x=>x.GetTodoMessage()),
				Details = s.NotNull(x=>x.GetTodoDetails()),
				RockId = rock,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				AccountabilityId = accountable ?? recur.DefaultTodoOwner,
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
				AccountableUserId = model.AccountabilityId,
				DueDate = model.DueDate
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
			//return PartialView("ScorecardIssueModal", model);
		}


		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateTodoFromIssue(long issue, long recurrence, long? meeting = null)
		{
			if (meeting!=null)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting.Value));
			var i = IssuesAccessor.GetIssue(GetUser(), issue);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence,true);



			var model = new TodoFromIssueVM(recur.DefaultTodoOwner)
			{
				Message = i.NotNull(x=>x.GetTodoMessage()),
				Details = i.NotNull(x=>x.GetTodoDetails()),
				ByUserId = GetUser().Id,
				MeetingId = meeting??-1,
				IssueId = issue,
				RecurrenceId = recurrence,
				AccountabilityId = L10Accessor.GuessUserId(i, recur.DefaultTodoOwner),
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
			if (model.MeetingId!=-1)
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
				AccountableUserId = model.AccountabilityId,
				DueDate = model.DueDate
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

	    public class LinkExternalTodo
		{
			[Display(Name = "For User")]
			public long UserId { get; set; }
			[Display(Name = "Account Type")]
		    public ExternalTodoType Account { get; set; } 
			public List<AccountableUserVM> PossibleUsers { get; set; }
			public long RecurrenceId { get; set; }
	    }

	    [Access(AccessLevel.UserOrganization)]
        public PartialViewResult LinkToExternal(long recurrence, long user = 0)
		{
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
		    var model = new LinkExternalTodo(){
				RecurrenceId = recurrence,
				UserId = user,
			    PossibleUsers = recur._DefaultAttendees
				    .Select(x => x.User)
				    .Select(x => new AccountableUserVM(){
					    id = x.Id,
					    imageUrl = x.ImageUrl(true, ImageSize._32),
					    name = x.GetName()
				    }).ToList(),
		    };

			return PartialView(model);
	    }

	    [HttpPost]
	    [Access(AccessLevel.UserOrganization)]
		public JsonResult LinkToExternal(LinkExternalTodo model)
	    {
			ValidateValues(model,x=>x.RecurrenceId);
		    switch(model.Account){
			    case ExternalTodoType.Trello:
					return Json(ResultObject.SilentSuccess(TrelloAccessor.AuthUrl(GetUser(), model.RecurrenceId,model.UserId)));
			    case ExternalTodoType.Basecamp:
					return Json(ResultObject.SilentSuccess(BaseCampAccessor.AuthUrl(GetUser(), model.RecurrenceId, model.UserId)));

				
				default:
				    throw new ArgumentOutOfRangeException();
		    }
	    }
		[Access(AccessLevel.UserOrganization)]
		public JsonResult DetachLink(long id)
		{
			ExternalTodoAccessor.DetatchLink(GetUser(),id);
			return Json(ResultObject.SilentSuccess(true), JsonRequestBehavior.AllowGet);
		}


	    [Access(AccessLevel.UserOrganization)]
	    public ActionResult List(long? id=null)
	    {
			//Views\L10\Details\todo.cshtml
		   // 

		    return View(id??GetUser().Id);
	    }

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult AjaxList(long? id = null)
		{
			return PartialView("~/Views/Todo/Partial/list.cshtml",id ?? GetUser().Id);
		}
	}
}