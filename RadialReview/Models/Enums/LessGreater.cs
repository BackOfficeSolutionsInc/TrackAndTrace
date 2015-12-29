using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Enums
{
	[JsonConverter(typeof(StringEnumConverter))] 
	public enum LessGreater
	{
		[Display(Name = "<")]
		[Description("Less than")]
		//[EnumMember(Value = "Less than")]
		LessThan = -1,

		[Display(Name = "≥")]
		[Description("Greater than")]
		//[EnumMember(Value = "Greater than")]
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