using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Enums
{
	public enum UnitType
	{
		[Display(Name = "No units")] [Description("No units")] None = 0,
		[Display(Name = "Dollars")] [Description("Dollars")] Dollar = 1,
		[Display(Name = "Percent")] [Description("Percent")] Percent = 2,

	}
}

namespace RadialReview
{
public static class UnitTypeExtensions
	{
		public static string ToTypeString(this UnitType type)
		{
			switch (type)
			{
				case UnitType.None: return "units";
				case UnitType.Dollar: return "dollars";
				case UnitType.Percent: return "%";
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}
	}
}
