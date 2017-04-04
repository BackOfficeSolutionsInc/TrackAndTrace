﻿using System;
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
        [HttpPut]
        public async Task<bool> CreateIssue(long recurrenceId, long ownerId, [FromBody]IssueModel issueModel)
        {
            return await IssuesAccessor.CreateIssue(GetUser(), recurrenceId, ownerId, issueModel);
        }

        // GET: api/Issue/5
        [Route("issue/{id}")]
        [HttpGet]
        public AngularIssue Get(long id)
        {
            IssueModel.IssueModel_Recurrence model = new IssueModel.IssueModel_Recurrence();
            model.Issue = IssuesAccessor.GetIssue(GetUser(), id);
            return new Models.Angular.Issues.AngularIssue(model);
        }

        // GET: api/Issue/mine
        [Route("issue/mine")]
        public IEnumerable<AngularIssue> GetMineIssues()
        {
            return IssuesAccessor.GetMyIssues(GetUser(), GetUser().Id).Select(x => new AngularIssue(x));
        }

        // GET: api/Issue/mine
        [Route("issue/user/{userId}/{recurrenceId}")]
        public IEnumerable<AngularIssue> GetUserIssues(long userId, long recurrenceId)
        {
            return IssuesAccessor.GetUserIssues(GetUser(), userId, recurrenceId).Select(x => new AngularIssue(x));
        }

        // GET: api/Issue/mine
        [Route("issue/user/{id}")]
        public IEnumerable<IssueModel.IssueModel_Recurrence> GetRecurrenceIssues(long id)
        {
            return L10Accessor.GetIssuesForRecurrence(GetUser(), id, false);
        }

        // PUT: api/Todo/5
        [Route("issue/{id}")]
        [HttpPut]
        public void EditIssue(long id, [FromBody]string message, [FromBody]DateTime dueDate)
        {
            L10Accessor.UpdateIssue(GetUser(), id, dueDate, message);
        }
    }
}