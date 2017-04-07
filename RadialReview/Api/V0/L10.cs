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

            // call updateRecurrence and pass name
            L10Accessor.UpdateRecurrence(GetUser(), _recurrence.Id, name);

            return _recurrence.Id;
        }

        [Route("L10/{recurrenceId}/edit")]
        [HttpPut]
        public void EditL10(long recurrenceId, [FromBody]L10Recurrence L10) // string name can be sent too if only name need to be updated
        {
            L10Accessor.UpdateRecurrence(GetUser(), recurrenceId, L10.Name); // updateRecurrence
        }

        [Route("L10/{recurrenceId}/attachmeasurable/{measurableId}")]
        [HttpPut]
        public void AttachMeasurableL10(long recurrenceId, long measurableId) // Attaching measurable with L10
        {
            L10Accessor.AttachMeasurable(GetUser(), recurrenceId, measurableId);
        }

        [Route("L10/{recurrenceId}/removemeasurable/{measurableId}")]
        [HttpPut]
        public void RemoveMeasurableL10(long recurrenceId, long measurableId)
        {
            L10Accessor.Remove(GetUser(), new AngularMeasurable() { Id = measurableId }, recurrenceId, null);
        }

        [Route("L10/{recurrenceId}/rock/{rockId}")]
        [HttpPut]
        public void AttachRockMeetingL10(long recurrenceId, long rockId)
        {
            L10Accessor.AttachRock(GetUser(), recurrenceId, rockId);
        }

        [Route("L10/{recurrenceId}/removerock/{rockId}")]
        [HttpPut]
        public void RemoveRockL10(long recurrenceId, long rockId) // Attaching measurable with L10
        {
            L10Accessor.Remove(GetUser(), new AngularRock() { Id = rockId }, recurrenceId, null);
        }

        [Route("L10/{recurrenceId}")]
        [HttpDelete]
        public void RemoveL10(long recurrenceId)
        {
            L10Accessor.DeleteL10(GetUser(), recurrenceId);
            // pass null for connectionId
            // DeleteL10 with RecurrenceId
        }

        [Route("L10/attachtodo/{recurrenceId}")]
        [HttpPut]
        public async Task<bool> AttachtodoMeetingL10(long recurrenceId, [FromBody]TodoModel model)
        {
            bool result = await TodoAccessor.CreateTodo(GetUser(), recurrenceId, model); // need to ask this.
            return result;
        }

        [Route("L10/{recurrenceId}/removetodo/{todoId}")]
        [HttpPut]
        public void RemoveTodoL10(long recurrenceId, long todoId)
        {
            L10Accessor.Remove(GetUser(), new AngularTodo() { Id = todoId }, recurrenceId, null);
        }

        [Route("L10/attachissue/{recurrenceId}")]
        [HttpPut]
        public async Task<bool> AttachIssueMeetingL10(long recurrenceId, [FromBody]IssueModel model)
        {
            bool result = await IssuesAccessor.CreateIssue(GetUser(), recurrenceId, GetUser().Id, model); // need to ask this.
            return result;
        }

        [Route("L10/{recurrenceId}/removeissue/{issueId}")]
        [HttpPut]
        public void RemoveIssueL10(long recurrenceId, long issueId)
        {
            L10Accessor.Remove(GetUser(), new AngularIssue() { Id = issueId }, recurrenceId, null);
        }


        [Route("L10/attachheadline/{recurrenceId}")]
        [HttpPut]
        public void AttachHeadlineMeetingL10(long recurrenceId, long headlineId)
        {
            L10Accessor.AttachHeadline(GetUser(), recurrenceId, headlineId); // need to ask this.            
        }

        [Route("L10/{recurrenceId}/removeheadline/{headlineId}")]
        [HttpPut]
        public void RemoveHeadlineL10(long recurrenceId, long headlineId)
        {
            L10Accessor.Remove(GetUser(), new AngularHeadline() { Id = headlineId }, recurrenceId, null);
        }

        [Route("get/{recurrenceId}")]
        [HttpGet]
        public AngularRecurrence GetL10(long recurrenceId) // GetAngularRecurrence
        {
            return L10Accessor.GetAngularRecurrence(GetUser(), recurrenceId);
        }

        [Route("L10/meetingattendees/{recurrenceId}")]
        [HttpPut]
        public IEnumerable<Models.UserOrganizationModel> GetL10MeetingAttendees(long recurrenceId)
        {
            return L10Accessor.GetAttendees(GetUser(), recurrenceId);
        }

        // Get list of L10 methods for API
        // Get GetVisibleL10Meetings_Tiny

        [Route("L10/getlist")]
        [HttpPut]
        public IEnumerable<Models.Application.NameId> GetList()
        {
            return L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id);            
        }      
    }
}