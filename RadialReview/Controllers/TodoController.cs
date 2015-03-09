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

    }
}