using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum LessGreater
	{
		[Display(Name ="Less than")]
		LessThan = -1,

		[Display(Name = "Greater than")]
		GreaterThan = 1,
	}
}