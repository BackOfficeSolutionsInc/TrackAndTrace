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
	
	}
}