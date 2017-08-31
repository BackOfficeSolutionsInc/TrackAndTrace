using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.L10;
using RadialReview.Controllers;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Issues;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Rocks;
using System.Net;

namespace RadialReview.Api.V0 {
	[RoutePrefix("api/v0")]
	public class L10_Controller : BaseApiController {
		// PUT: api/L10
		[Route("L10/Create")]
		[HttpPost]
		public async Task<long> CreateL10([FromBody]string name) {
			var _recurrence = await L10Accessor.CreateBlankRecurrence(GetUser(), GetUser().Organization.Id);
			await L10Accessor.UpdateRecurrence(GetUser(), _recurrence.Id, name);
			return _recurrence.Id;
		}

		[Route("L10/{recurrenceId}")]
		[HttpPost]
		public async Task EditL10(long recurrenceId, [FromBody]string name) {
			await L10Accessor.UpdateRecurrence(GetUser(), recurrenceId, name);
		}

		[Route("L10/{recurrenceId}/measurable/{measurableId}")]
		[HttpPost]
		public async Task AttachMeasurableL10(long recurrenceId, long measurableId) {
			await L10Accessor.AttachMeasurable(GetUser(), recurrenceId, measurableId);
		}

		[Route("L10/{recurrenceId}/measurable/{measurableId}")]
		[HttpDelete]
		public async Task RemoveMeasurableL10(long recurrenceId, long measurableId) {
			await L10Accessor.Remove(GetUser(), new AngularMeasurable() { Id = measurableId }, recurrenceId, null);
		}

		[Route("L10/{recurrenceId}/rock/{rockId}")]
		[HttpPost]
		public async Task AttachRockMeetingL10(long recurrenceId, long rockId) {
			await L10Accessor.AttachRock(GetUser(), recurrenceId, rockId);
		}

		[Route("L10/{recurrenceId}/rock/{rockId}")]
		[HttpDelete]
		public async Task RemoveRockL10(long recurrenceId, long rockId) {
			await L10Accessor.Remove(GetUser(), new AngularRock() { Id = rockId }, recurrenceId, null);
		}

		[Route("L10/{recurrenceId}")]
		[HttpDelete]
		public async Task RemoveL10(long recurrenceId) {
			await L10Accessor.DeleteL10Recurrence(GetUser(), recurrenceId);
		}

		[Route("L10/{recurrenceId}/todo")]
		[HttpPost]
		public async Task<bool> AttachTodoL10(long recurrenceId, [FromBody]string name, [FromBody]long? ownerId = null, [FromBody]DateTime? duedate = null) {
			if (!duedate.HasValue) {
				duedate = DateTime.Now.AddDays(7);
			}

			var model = new TodoModel() {
				Message = name,
				DueDate = duedate.Value,
				AccountableUserId = ownerId ?? GetUser().Id,
				ForRecurrenceId = recurrenceId
			};

			return await TodoAccessor.CreateTodo(GetUser(), recurrenceId, model);
		}

		[Route("L10/{recurrenceId}/issue")]
		[HttpPost]
		public async Task<AngularIssue> CreateIssueL10(long recurrenceId, [FromBody]string name, [FromBody]long? ownerId = null, [FromBody]string details = null) {
			ownerId = ownerId ?? GetUser().Id;
			var issue = new IssueModel() { Message = name, Description = details };
			var success = await IssuesAccessor.CreateIssue(GetUser(), recurrenceId, ownerId.Value, issue);
			return new AngularIssue(success.IssueRecurrenceModel);

		}

		[Route("L10/{recurrenceId}/issue/{issueId}")]
		[HttpDelete]
		public async Task RemoveIssueL10(long recurrenceId, long issueId) {
			await L10Accessor.Remove(GetUser(), new AngularIssue() { Id = issueId }, recurrenceId, null);
		}

		[Route("L10/{recurrenceId}/headline")]
		[HttpPost]
		public async Task<AngularHeadline> CreateHeadlineL10(long recurrenceId, [FromBody]string name = null, [FromBody]long? OwnerId = null, [FromBody]string Details = null) {
			OwnerId = OwnerId ?? GetUser().Id;

			var headline = new PeopleHeadline() {
				Message = name,
				OwnerId = OwnerId.Value,
				_Details = Details,
				RecurrenceId = recurrenceId,
				OrganizationId = GetUser().Organization.Id
			};
			var success = await HeadlineAccessor.CreateHeadline(GetUser(), headline);
			if (!success)
				throw new HttpResponseException(HttpStatusCode.BadRequest);

			return new AngularHeadline(headline);
		}

		[Route("L10/{recurrenceId}/headline/{headlineId}")]
		[HttpDelete]
		public async Task RemoveHeadlineL10(long recurrenceId, long headlineId) {
			await L10Accessor.Remove(GetUser(), new AngularHeadline() { Id = headlineId }, recurrenceId, null);
		}

		[Route("L10/{recurrenceId}")]
		[HttpGet]
		public AngularRecurrence GetL10(long recurrenceId) {
			return L10Accessor.GetAngularRecurrence(GetUser(), recurrenceId);
		}

		[Route("L10/{recurrenceId}/attendee")]
		[HttpGet]
		public IEnumerable<Models.UserOrganizationModel> GetL10Attendees(long recurrenceId) {
			return L10Accessor.GetAttendees(GetUser(), recurrenceId);
		}

		[Route("L10/list")]
		[HttpGet]
		public IEnumerable<Models.Application.NameId> GetL10List() {
			return L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id);
		}


		[Route("l10/{id}/issue")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetRecurrenceIssues(long id) {
			return L10Accessor.GetIssuesForRecurrence(GetUser(), id, false).Select(x => new AngularIssue(x));
		}

		[Route("l10/{id}/user/{userId}/issue")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetUserIssues(long userId, long id) {
			return IssuesAccessor.GetUserIssues(GetUser(), userId, id).Select(x => new AngularIssue(x));
		}

		[Route("l10/{recurrenceId}/todo")]
		[HttpPost]
		public async Task<bool> CreateTodo(long recurrenceId, [FromBody]string message, [FromBody]DateTime dueDate) {
			return await TodoAccessor.CreateTodo(GetUser(), recurrenceId, new TodoModel() { Message = message, DueDate = dueDate });
		}
	}
}
