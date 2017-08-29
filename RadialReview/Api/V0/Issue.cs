using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Issues;
using System.Threading.Tasks;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class IssueController : BaseApiController
    {
        // Put: api/Issue/mine
        [Route("issue/create")]
        [HttpPost]
        public async Task<AngularIssue> CreateIssue(long recurrenceId, [FromBody]string name, [FromBody]long? ownerId = null, [FromBody]string details = null)
        {
			ownerId = ownerId ?? GetUser().Id;
			var issue = new IssueModel() { Message = name, Description = details };
			var success = await IssuesAccessor.CreateIssue(GetUser(), recurrenceId, ownerId.Value, issue);
			return new AngularIssue(success.IssueRecurrenceModel);
		}

        [Route("issue/{id}")]
        [HttpGet]
        public AngularIssue Get(long id)
        {
            //IssueModel.IssueModel_Recurrence model = new IssueModel.IssueModel_Recurrence();
            var model = IssuesAccessor.GetIssue_Recurrence(GetUser(), id);
            return new AngularIssue(model);
        }

        [Route("issue/user/mine")]
		[HttpGet]
        public IEnumerable<AngularIssue> GetMineIssues()
        {
            List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), GetUser().Id);
            return list.Select(x => new AngularIssue(x));
        }

        [Route("issue/user/{userId}")]
		[HttpGet]
        public IEnumerable<AngularIssue> GetUserIssues(long userId)
        {
            List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), userId);
            return list.Select(x => new AngularIssue(x));
        }

        [Route("issue/{id}")]
        [HttpPut]
        public async Task EditIssue(long id, [FromBody]string message)
        {
            await L10Accessor.UpdateIssue(GetUser(), id, DateTime.UtcNow, message);
        }
    }
}