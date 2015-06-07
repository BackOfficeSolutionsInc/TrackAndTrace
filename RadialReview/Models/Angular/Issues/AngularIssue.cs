using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Issues;

namespace RadialReview.Models.Angular.Issues
{
	public class AngularIssue : BaseAngular
	{
		public AngularIssue(IssueModel.IssueModel_Recurrence recurrenceIssue) : base(recurrenceIssue.Id)
		{
			var issue = recurrenceIssue.Issue;
			Name = issue.Message;
			Details = issue.Description;
			CompleteTime = recurrenceIssue.CloseTime;
			CreateTime = recurrenceIssue.CreateTime;
			Children = recurrenceIssue._ChildIssues.NotNull(x => 
				x.Select(y => new AngularIssue(y)).ToList()
			)?? new List<AngularIssue>();
			Complete = recurrenceIssue.CloseTime != null;
			if (recurrenceIssue.Owner!=null)
				Owner = new AngularUser(recurrenceIssue.Owner);
		}
		public AngularIssue()
		{
			
		}

		public AngularUser Owner { get; set; }
		public String Name { get; set; }
		public String Details { get; set; }
		public List<AngularIssue> Children { get; set; }
		public DateTime? CompleteTime { get; set; }
		public DateTime? CreateTime { get; set; }
		public bool? Complete { get; set; }

	}
}