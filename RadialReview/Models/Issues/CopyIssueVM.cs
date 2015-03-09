using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;

namespace RadialReview.Models.Issues
{
	public class CopyIssueVM
	{
		[Required]
		public long ParentIssue_RecurrenceId { get; set; }
		[Required]
		public long IssueId { get; set; }
		[Required]

		[Display(Name = "Copy into")]
		public long CopyIntoRecurrenceId { get; set; }

		public List<L10Recurrence> PossibleRecurrences { get; set; }

		public String Message { get; set; }
		public string Details { get; set; }
	}
}