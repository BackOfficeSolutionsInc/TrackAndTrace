using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.Issues
{
	public class IssueVM {
		public long IssueId { get; set; }
		public long IssueRecurrenceId { get; set; }

		[Required]
		public long MeetingId { get; set; }
		[Required]
		public long ByUserId { get; set; }
		[Required]
		[Display(Name = "Issue")]
        [AllowHtml]
		public String Message { get; set; }

		[Display(Name = "Issue Details")]
        [AllowHtml]
		public string Details { get; set; }

		public long RecurrenceId { get; set; }
		public long ForId { get; set; }

		public long? ForModelId { get; set; }
		public string ForModelType { get; set; }

		[Display(Name = "Issue Owner")]
		public long OwnerId { get; set; }
		public List<AccountableUserVM> PossibleUsers { get; set; }

        public int Priority { get; set; }
		public class AccountableUserVM
		{
			public long id { get; set; }
			public string name { get; set; }
			public string imageUrl { get; set; }
		}

        public bool ShowPriority { get; set; }
    }
}