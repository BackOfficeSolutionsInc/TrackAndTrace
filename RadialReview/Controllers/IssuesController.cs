using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Amazon.ElasticTranscoder.Model;
using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public class IssuesController : BaseController
	{
		#region From Todo
		[Access(AccessLevel.UserOrganization)]
		public ActionResult IssueFromTodo(long recurrence, long todo, long meeting)
		{
			//var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);
			//copyto = copyto ?? i.Recurrence.Id;
			_PermissionsAccessor.Permitted(GetUser(), x => 
				x.ViewL10Meeting(meeting)
				 .ViewL10Recurrence(recurrence)
			);

			var todoModel = TodoAccessor.GetTodo(GetUser(), todo);
			var model = new IssueVM()
			{
				//IssueId = i.Issue.Id,
				RecurrenceId = recurrence,
				Message = todoModel.GetIssueMessage(),
				Details = todoModel.GetIssueDetails(),
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				ForId = todo,
			};
			return PartialView("CreateIssueModal", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult IssueFromTodo(IssueVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId, x => x.ForId);
			_PermissionsAccessor.Permitted(GetUser(), x =>
				x.ViewL10Meeting(model.MeetingId)
				 .ViewL10Recurrence(model.RecurrenceId)
			);


			IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, new IssueModel(){
				CreatedById = GetUser().Id,
				//MeetingRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message ?? "",
				Description = model.Details ?? "",
				ForModel = "TodoModel",
				ForModelId = model.ForId,
				Organization = GetUser().Organization,
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}
		#endregion


		/// <summary>
		/// Copy an issue
		/// </summary>
		/// <param name="copyto"></param>
		/// <param name="recurrence_issue"></param>
		/// <returns></returns>
	    [Access(AccessLevel.UserOrganization)]
		public ActionResult CopyModal(long recurrence_issue, long? copyto=null)
	    {
			var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);

			copyto = copyto ?? i.Recurrence.Id;

			var model = new CopyIssueVM()
			{
				IssueId = i.Issue.Id,
				Message = i.Issue.Message,
				Details = i.Issue.Description,
				ParentIssue_RecurrenceId = i.Id,
				CopyIntoRecurrenceId = copyto.Value,
				PossibleRecurrences = L10Accessor.GetAllL10RecurrenceAtOrganization(GetUser(), GetUser().Organization.Id)
			};
			return PartialView("CopyIssueModal", model);
	    }
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CopyModal(CopyIssueVM model)
		{
			ValidateValues(model, x => x.ParentIssue_RecurrenceId,x=>x.IssueId);
			IssuesAccessor.CopyIssue(GetUser(), model.ParentIssue_RecurrenceId, model.CopyIntoRecurrenceId);
			model.PossibleRecurrences = L10Accessor.GetAllL10RecurrenceAtOrganization(GetUser(), GetUser().Organization.Id);
			
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

	    [Access(AccessLevel.UserOrganization)]
	    public ActionResult CreateIssue(long meeting, long recurrence)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));
			var model = new IssueVM(){
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				RecurrenceId = recurrence
		    };
			return PartialView("CreateIssueModal", model);
	    }

		[HttpPost]
	    [Access(AccessLevel.UserOrganization)]
		public ActionResult CreateIssue(IssueVM model)
	    {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x=>x.RecurrenceId,x=>x.ForId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, new IssueModel()
			{
				CreatedById = GetUser().Id,
				//MeetingRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message??"",
				Description = model.Details??"",
				ForModel = "IssueModel",
				ForModelId = -1,
				Organization = GetUser().Organization
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
	    }

	    [Access(AccessLevel.UserOrganization)]
		public ActionResult Modal(long meeting, long recurrence, long measurable,long score)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s =ScorecardAccessor.GetScoreInMeeting(GetUser(), score,recurrence);

			var model = new ScoreCardIssueVM()
			{
				ByUserId = GetUser().Id,
				Message = s.GetIssueMessage(),
				Details = s.GetIssueDetails(),
				MeasurableId = measurable,
				MeetingId = meeting,
				RecurrenceId = recurrence
			};
			return PartialView("ScorecardIssueModal", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Modal(ScoreCardIssueVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.MeasurableId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			IssuesAccessor.CreateIssue(GetUser(),model.RecurrenceId, new IssueModel(){
				CreatedById = GetUser().Id,
				//MeetingRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message =model.Message,
				Description = model.Details,
				ForModel = "MeasurableModel",
				ForModelId = model.MeasurableId,
				Organization = GetUser().Organization
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
			//return PartialView("ScorecardIssueModal", model);
		}

    }
}