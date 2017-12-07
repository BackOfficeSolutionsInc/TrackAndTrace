using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RadialReview.Models.Todo;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Models.Issues
{
	public class ScoreCardTodoVM : TodoVM
	{
		[Obsolete("Use other constructor",false)]
		public ScoreCardTodoVM(){
			
		}
		public ScoreCardTodoVM(long accountableUserId, TimeSettings timeSettings) : base(accountableUserId, timeSettings) {
		}

		[Required]
		public long MeasurableId { get; set; }

		/*[Required]
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