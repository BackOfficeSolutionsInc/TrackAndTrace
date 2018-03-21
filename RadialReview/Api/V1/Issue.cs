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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Api.V1 {
	[RoutePrefix("api/v1")]
	public class IssuesController : BaseApiController {

		public class CreateIssueModel {
			///<summary>
			///Level 10 meeting ID
			///</summary>
			[Required]
			public long meetingId { get; set; }
			///<summary>
			///Title for the issue
			///</summary>
			[Required]
			public string title { get; set; }
			///<summary>
			///Owner's user ID (Default: you)
			///</summary>
			public long? ownerId { get; set; }
			///<summary>
			///Optional issue notes (Default: none)
			///</summary>
			public string notes { get; set; }
		}


		/// <summary>
		/// Create a new issue in for a Level 10
		/// </summary>
		/// <returns>The created issue</returns>
		// Put: api/Issue/mine
		[Route("issues/create")]
		[HttpPost]
		public async Task<AngularIssue> CreateIssue([FromBody]CreateIssueModel body) {
			body.ownerId = body.ownerId ?? GetUser().Id;
			//var issue = new IssueModel() { Message = body.title, Description = body.details };
			var creation = IssueCreation.CreateL10Issue(body.title, body.notes, body.ownerId, body.meetingId);
			var success = await IssuesAccessor.CreateIssue(GetUser(), creation);// body.meetingId, body.ownerId.Value, issue);
			return new AngularIssue(success.IssueRecurrenceModel);
		}
		/// <summary>
		/// Get a specific issue
		/// </summary>
		/// <param name="ISSUE_ID">Issue ID</param>
		/// <returns>The specified issue</returns>
		// GET: api/Issue/5
		[Route("issues/{ISSUE_ID}")]
		[HttpGet]
		public AngularIssue Get(long ISSUE_ID) {
			var model = IssuesAccessor.GetIssue_Recurrence(GetUser(), ISSUE_ID);
			return new AngularIssue(model);
		}

		/// <summary>
		/// Get all issues you own.
		/// </summary>
		/// <returns>List of your issues</returns>
		[Route("issues/users/mine")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetMineIssues() {
			List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), GetUser().Id);
			return list.Select(x => new AngularIssue(x));
		}
		/// <summary>
		/// Get all issues owned by a user.
		/// </summary>
		/// <param name="USER_ID"></param>
		/// <returns>List of the user's issues</returns>
		[Route("issues/users/{USER_ID:long}")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetUserIssues(long USER_ID) {
			List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), USER_ID);
			return list.Select(x => new AngularIssue(x));
		}

		public class UpdateIssueModel {
			///<summary>
			///Title for the issue
			///</summary>
			public string title { get; set; }
			///<summary>
			///Owner's user ID
			///</summary>
			public long? ownerId { get; set; }
		}

		/// <summary>
		/// Update an issue
		/// </summary>
		/// <param name="ISSUE_ID">Issue ID</param>
		/// <returns></returns>
		[Route("issues/{ISSUE_ID}")]
		[HttpPut]
		public async Task EditIssue(long ISSUE_ID, [FromBody]UpdateIssueModel body) {
			await IssuesAccessor.EditIssue(GetUser(), ISSUE_ID, message: body.title, owner: body.ownerId);
		}
	}
}
