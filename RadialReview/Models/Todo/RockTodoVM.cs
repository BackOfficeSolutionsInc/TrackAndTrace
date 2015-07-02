using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Ajax.Utilities;

namespace RadialReview.Models.Todo
{
	public class RockTodoVM : TodoVM
	{
		[Obsolete("Use other constructor",false)]
		public RockTodoVM(){
			
		}

		public RockTodoVM(long accountableUserId) : base(accountableUserId)
		{
		}

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