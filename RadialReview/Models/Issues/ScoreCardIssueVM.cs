using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Issues
{
	public class ScoreCardIssueVM : IssueVM
	{
		[Required]
		public long MeasurableId { get; set; }
		
		/*[Required]
		public long MeetingId { get; set; }
		[Required]
		public long ByUserId { get; set; }
		[Required]
		[Display(Name="Issue")]
		public String Message { get; set; }

		[Display(Name = "Issue Details")]
		public string Details { get; set; }

		public long RecurrenceId { get; set; }*/
	}
}