using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Enums
{
	public enum LessGreater
	{
		[Display(Name = "Less than")]
		[Description("Less than")]
		LessThan = -1,

		[Display(Name = "Greater than")]
		[Description("Greater than")]
		GreaterThan = 1,
	}

	
}

namespace RadialReview
{
	public static class LessGreaterExtensions
	{
		public static string ToSymbol(this LessGreater self)
		{
			switch (self)
			{
				case LessGreater.LessThan:
					return "<";
				case LessGreater.GreaterThan:
					return "≥";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}