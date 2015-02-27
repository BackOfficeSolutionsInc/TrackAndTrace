using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Amazon.ElasticTranscoder.Model;
using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;
using RadialReview.Models.Issues;

namespace RadialReview.Controllers
{
    public class IssuesController : BaseController
    {
        // GET: Issues
		//public ActionResult Index()
		//{
		//	return View();
		//}


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
				ForModel = "measurablemodel",
				ForModelId = model.MeasurableId,
				Organization = GetUser().Organization
			});

		
			return PartialView("ScorecardIssueModal", model);
		}

    }
}