using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Issues;
using RadialReview.Utilities;

namespace RadialReview.Models.Angular.Issues
{
	public class AngularIssue : BaseAngular
	{
		public AngularIssue(IssueModel.IssueModel_Recurrence recurrenceIssue) : base(recurrenceIssue.Id)
		{
			var issue = recurrenceIssue.Issue;
			DetailsUrl = Config.NotesUrl() + "p/" + issue.PadId + "?showControls=true&showChat=false";
			Name = issue.Message;
			Details = issue.Description;
			CompleteTime = recurrenceIssue.CloseTime;
			CreateTime = recurrenceIssue.CreateTime;
			Children = recurrenceIssue._ChildIssues.NotNull(x => 
				x.Select(y => new AngularIssue(y)).ToList()
			)?? new List<AngularIssue>();
			Complete = recurrenceIssue.CloseTime != null;
			if (recurrenceIssue.Owner!=null)
				Owner = AngularUser.CreateUser(recurrenceIssue.Owner);
		}
		public AngularIssue()
		{
			
		}

		public string DetailsUrl { get; set; }
		public AngularUser Owner { get; set; }
		public String Name { get; set; }
		public String Details { get; set; }
		public List<AngularIssue> Children { get; set; }
		public DateTime? CompleteTime { get; set; }
		public DateTime? CreateTime { get; set; }
		public bool? Complete { get; set; }

	}
}