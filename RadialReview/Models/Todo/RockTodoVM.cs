using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Todo
{
	public class RockTodoVM : TodoVM
	{
		[Required]
		public long RockId { get; set; }
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

		[Display(Name = "Who's Accountable")]
		public long AccountabilityId { get; set; }

		public List<AccountableUserVM> PossibleUsers { get; set; }*/
	}
}