using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.Todo
{
	public class TodoVM
	{
		[Required]
		public long MeetingId { get; set; }
		[Required]
		public long ByUserId { get; set; }
		[Required]
		[Display(Name = "Todo")]
		public String Message { get; set; }

		[Display(Name = "Todo Details")]
		public string Details { get; set; }

		public long RecurrenceId { get; set; }
		[Required]

		[Display(Name = "Who's Accountable")]
		public long AccountabilityId { get; set; }
		public List<AccountableUserVM> PossibleUsers { get; set; }
	}

	public class AccountableUserVM
	{
		public long id { get; set; }
		public string name { get; set; }
		public string imageUrl { get; set; }
	}
}