using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Enums
{
	[JsonConverter(typeof(StringEnumConverter))] 
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

		public static string Format(this UnitType type, string value)
		{
			switch (type)
			{
				case UnitType.None: return string.Format("{0}", value);
				case UnitType.Dollar: return string.Format("${0}", value);
				case UnitType.Percent: return string.Format("{0}%", value);
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static string Format(this UnitType type, decimal value)
		{
			return Format(type, string.Format("{0:#,##0.####}", value));
			/*
			switch (type)
			{
				case UnitType.None: return ;
				case UnitType.Dollar: return string.Format("${0:#,##0.####}", value);
				case UnitType.Percent: return string.Format("{0:#,##0.####}%", value);
				default:
					throw new ArgumentOutOfRangeException("type");
			}*/

		}
	}
}
