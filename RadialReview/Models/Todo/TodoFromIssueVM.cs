using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.Todo
{
	public class TodoFromIssueVM : TodoVM
	{
		[Obsolete("Use other constructor",false)]
		public TodoFromIssueVM()
		{
			
		}
		public TodoFromIssueVM(long accountableUserId) : base(accountableUserId)
		{
		}

		[Required]
		public long IssueId { get; set; }
		/*
		[Required]
		public long MeetingId { get; set; }
		[Required]
		public long ByUserId { get; set; }
		[Required]
		[Display(Name = "To-do")]
		public String Message { get; set; }

		[Display(Name = "To-do Details")]
		public string Details { get; set; }

		public long RecurrenceId { get; set; }
	
		[Required]
		[Display(Name = "Who's Accountable")]
		public long AccountabilityId { get; set; }
		public List<AccountableUserVM> PossibleUsers { get; set; }*/
	}
}