using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum UnitType
	{
		[Display(Name = "No units")]
		[Description("No units")]
		None = 0,
		[Display(Name = "Dollars")]
		[Description("Dollars")]
		Dollar = 1,
		[Display(Name = "Percent")]
		[Description("Percent")]
		Percent =2,

	}
}