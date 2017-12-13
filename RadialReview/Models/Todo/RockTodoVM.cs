using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Ajax.Utilities;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Models.Todo
{
	public class RockTodoVM : TodoVM
	{
		[Obsolete("Use other constructor",false)]
		public RockTodoVM(){
			
		}

		public RockTodoVM(long accountableUserId, TimeSettings timeSettings) : base(accountableUserId, timeSettings) {
		}

		[Required]
		public long RockId { get; set; }
	
	}
}