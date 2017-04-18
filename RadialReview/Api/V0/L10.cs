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

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class L10_Controller : BaseApiController
    {
        // PUT: api/L10
        [Route("L10/Create")]
        [HttpPut]
        public long CreateL10([FromBody]string name)
        {
            var _recurrence = L10Accessor.CreateBlankRecurrence(GetUser(), GetUser().Organization.Id);

            L10Accessor.UpdateRecurrence(GetUser(), _recurrence.Id, name);

            return _recurrence.Id;
        }

        [Route("L10/{recurrenceId}/edit")]
        [HttpPut]
        public void EditL10(long recurrenceId, [FromBody]string name)
        {
            L10Accessor.UpdateRecurrence(GetUser(), recurrenceId, name);
        }

        [Route("L10/{recurrenceId}/attachmeasurable/{measurableId}")]
        [HttpPut]
        public void AttachMeasurableL10(long recurrenceId, long measurableId)
        {
            L10Accessor.AttachMeasurable(GetUser(), recurrenceId, measurableId);
        }

        [Route("L10/{recurrenceId}/measurable/{measurableId}")]
        [HttpDelete]
        public void RemoveMeasurableL10(long recurrenceId, long measurableId)
        {
            L10Accessor.Remove(GetUser(), new AngularMeasurable() { Id = measurableId }, recurrenceId, null);
        }

        [Route("L10/{recurrenceId}/attachrock/{rockId}")]
        [HttpPut]
        public void AttachRockMeetingL10(long recurrenceId, long rockId)
        {
            L10Accessor.AttachRock(GetUser(), recurrenceId, rockId);
        }

        [Route("L10/{recurrenceId}/rock/{rockId}")]
        [HttpDelete]
        public void RemoveRockL10(long recurrenceId, long rockId)
        {
            L10Accessor.Remove(GetUser(), new AngularRock() { Id = rockId }, recurrenceId, null);
        }

        [Route("L10/{recurrenceId}")]
        [HttpDelete]
        public void RemoveL10(long recurrenceId)
        {
            L10Accessor.DeleteL10(GetUser(), recurrenceId);
        }

        [Route("L10/attachtodo/{recurrenceId}")]
        [HttpPut]
        public async Task<bool> AttachTodoL10(long recurrenceId, [FromBody]string name, [FromBody]long? ownerId = null, [FromBody]DateTime? duedate = null)
        {
            if (!duedate.HasValue)
            {
                duedate = DateTime.Now.AddDays(7);
            }

            var model = new TodoModel()
            {
                Message = name,
                DueDate = duedate.Value,
                AccountableUserId = ownerId ?? GetUser().Id,
                ForRecurrenceId = recurrenceId
            };

            return await TodoAccessor.CreateTodo(GetUser(), recurrenceId, model);
        }

        [Route("L10/attachissue/{recurrenceId}")]
        [HttpPut]
        public async Task<bool> AttachIssueL10(long recurrenceId, [FromBody]string name, [FromBody]long? ownerId = null, [FromBody]string details = null)
        {
            return await IssuesAccessor.CreateIssue(GetUser(), recurrenceId, ownerId ?? GetUser().Id, new IssueModel() { Message = name, Description = details });
        }

        [Route("L10/{recurrenceId}/issue/{issueId}")]
        [HttpDelete]
        public void RemoveIssueL10(long recurrenceId, long issueId)
        {
            L10Accessor.Remove(GetUser(), new AngularIssue() { Id = issueId }, recurrenceId, null);
        }

        [Route("L10/attachheadline/{recurrenceId}")]
        [HttpPut]
        public async Task<bool> AttachHeadlineL10(long recurrenceId, [FromBody]string name = null, [FromBody]long? OwnerId = null, [FromBody]string Details = null)
        {
            if (OwnerId == null)
            {
                OwnerId = GetUser().Id;
            }

            return await HeadlineAccessor.CreateHeadline(GetUser(), new PeopleHeadline() { Message = name, OwnerId = OwnerId.Value, OrganizationId = GetUser().Organization.Id, _Details = Details, RecurrenceId = recurrenceId });
        }

        [Route("L10/{recurrenceId}/headline/{headlineId}")]
        [HttpDelete]
        public void RemoveHeadlineL10(long recurrenceId, long headlineId)
        {
            L10Accessor.Remove(GetUser(), new AngularHeadline() { Id = headlineId }, recurrenceId, null);
        }

        [Route("L10/{recurrenceId}")]
        [HttpGet]
        public AngularRecurrence GetL10(long recurrenceId)
        {
            return L10Accessor.GetAngularRecurrence(GetUser(), recurrenceId);
        }

        [Route("L10/attendees/{recurrenceId}")]
        [HttpPut]
        public IEnumerable<Models.UserOrganizationModel> GetL10Attendees(long recurrenceId)
        {
            return L10Accessor.GetAttendees(GetUser(), recurrenceId);
        }

        [Route("L10/list")]
        [HttpGet]
        public IEnumerable<Models.Application.NameId> GetL10List()
        {
            return L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id);
        }
    }
}