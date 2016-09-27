using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Todo {
	public class HeadlineTodoVm : TodoVM {
		[Required]
		public long HeadlineId { get; set; }
		
	
		[Obsolete("Use other constructor", false)]
		public HeadlineTodoVm() {

		}


		public HeadlineTodoVm(long accountableUserId) : base(accountableUserId)
		{
		}
	}
}