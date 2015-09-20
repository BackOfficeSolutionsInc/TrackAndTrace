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
        public PartialViewResult IssueFromTodo(long recurrence, long todo, long meeting)
		{
			//var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);
			//copyto = copyto ?? i.Recurrence.Id;
			_PermissionsAccessor.Permitted(GetUser(), x => 
				x.ViewL10Meeting(meeting)
				 .ViewL10Recurrence(recurrence)
			);

			var todoModel = TodoAccessor.GetTodo(GetUser(), todo);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
			var possible = recur._DefaultAttendees
				.Select(x => x.User)
				.Select(x => new IssueVM.AccountableUserVM(){
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList();

			var model = new IssueVM()
			{
				//IssueId = i.Issue.Id,
				RecurrenceId = recurrence,
				Message = todoModel.NotNull(x=>x.GetIssueMessage()),
				Details = todoModel.NotNull(x=>x.GetIssueDetails()),
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				ForId = todo,
				PossibleUsers = possible,
				OwnerId = GetUser().Id,
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


			IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId,model.OwnerId, new IssueModel(){
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
        public PartialViewResult CopyModal(long recurrence_issue, long? copyto = null)
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
        public PartialViewResult CreateIssue(long recurrence, long meeting = -1)
		{
			if(meeting!=-1)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
			var possible = recur._DefaultAttendees
				.Select(x => x.User)
				.Select(x => new IssueVM.AccountableUserVM(){
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList();


			var model = new IssueVM(){
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				PossibleUsers = possible,
				OwnerId = GetUser().Id
		    };
			return PartialView("CreateIssueModal", model);
	    }

	    public class MeetingVm
	    {
		    public long id { get; set; }

			public string name { get; set; }
	    }

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateIssueRecurrence()
		{
			//if (meeting != -1)
			//	_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			ViewBag.PossibleMeetings = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id, false)
				.Select(x=>new MeetingVm{name=x.Recurrence.Name,id =x.Recurrence.Id})
				.ToList();
			
			var model = new IssueVM()
			{
				ByUserId = GetUser().Id,
				MeetingId = -1,
				PossibleUsers = null,
				OwnerId = GetUser().Id
			};
			return PartialView("CreateIssueRecurrenceModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateIssueRecurrence(IssueVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.OwnerId, x => x.ForId);
			IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, model.OwnerId, new IssueModel()
			{
				CreatedById = GetUser().Id,
				//MeetingRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message ?? "",
				Description = model.Details ?? "",
				ForModel = "IssueModel",
				ForModelId = -1,
				Organization = GetUser().Organization,

			});
			return Json(ResultObject.Success("Created issue").NoRefresh());
		}

		[HttpPost]
	    [Access(AccessLevel.UserOrganization)]
		public JsonResult CreateIssue(IssueVM model)
	    {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x=>x.RecurrenceId,x=>x.ForId);
			if (model.MeetingId!=-1)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId,model.OwnerId, new IssueModel()
			{
				CreatedById = GetUser().Id,
				//MeetingRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message??"",
				Description = model.Details??"",
				ForModel = "IssueModel",
				ForModelId = -1,
				Organization = GetUser().Organization,
				
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
	    }

	    [Access(AccessLevel.UserOrganization)]
        public PartialViewResult Modal(long meeting, long recurrence, long measurable, long score)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s =ScorecardAccessor.GetScoreInMeeting(GetUser(), score,recurrence);

			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
			var possible = recur._DefaultAttendees
				.Select(x => x.User)
				.Select(x => new IssueVM.AccountableUserVM()
				{
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList();

			var model = new ScoreCardIssueVM()
			{
				ByUserId = GetUser().Id,
				Message = s.NotNull(x=>x.GetIssueMessage()),
				Details = s.NotNull(x=>x.GetIssueDetails()),
				MeasurableId = measurable,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				PossibleUsers = possible,
				OwnerId = s.NotNull(x=>x.AccountableUserId)
			};
			return PartialView("ScorecardIssueModal", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Modal(ScoreCardIssueVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.MeasurableId,x=>x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			IssuesAccessor.CreateIssue(GetUser(),model.RecurrenceId,model.OwnerId, new IssueModel(){
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

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateRockIssue(long meeting, long rock)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = RockAccessor.GetRockInMeeting(GetUser(), rock, meeting);
			var recur = L10Accessor.GetCurrentL10RecurrenceFromMeeting(GetUser(), meeting);
			var possible = recur._DefaultAttendees
				.Select(x => x.User)
				.Select(x => new IssueVM.AccountableUserVM()
				{
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList();


			var model = new RockIssueVM()
			{
				ByUserId = GetUser().Id,
				Message = s.NotNull(x=>x.GetIssueMessage()),
				Details = s.NotNull(x=>x.GetIssueDetails()),
				MeetingId = meeting,
				RockId = rock,
				RecurrenceId =  s.ForRecurrence.Id,
				PossibleUsers = possible,
				OwnerId =s.ForRock.ForUserId
			};
			return PartialView("RockIssueModal", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateRockIssue(RockIssueVM model)
		{
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RockId,x=>x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId,model.OwnerId, new IssueModel()
			{
				CreatedById = GetUser().Id,
				//MeetingRecurrenceId = model.RecurrenceId,
				CreatedDuringMeetingId = model.MeetingId,
				Message = model.Message,
				Description = model.Details,
				ForModel = "RockModel",
				ForModelId = model.RockId,
				Organization = GetUser().Organization
			});
			return Json(ResultObject.SilentSuccess().NoRefresh());
			//return PartialView("ScorecardIssueModal", model);
		}
    }
}