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
		[Display(Name = "To-do")]
		public String Message { get; set; }

		[Display(Name = "To-do Details")]
		public string Details { get; set; }

		public long RecurrenceId { get; set; }

		[Required]

		[Display(Name = "Who's Accountable")]
		public long AccountabilityId { get; set; }

		public List<AccountableUserVM> PossibleUsers { get; set; }

		public DateTime DueDate { get; set; }

		public TodoVM()
		{
			DueDate = DateTime.UtcNow.AddDays(7);
		}

		public TodoVM(long accountableUserId) : this()
		{
			AccountabilityId = accountableUserId;

		}
	}

	public class AccountableUserVM
	{
		public long id { get; set; }
		public string name { get; set; }
		public string imageUrl { get; set; }
	}
}