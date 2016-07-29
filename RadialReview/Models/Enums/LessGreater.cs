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
	public enum LessGreater {
        [Display(Name = "≤")]
        [Description("Less than or equal to")]
        //[EnumMember(Value = "Less than")]
        LessThanOrEqual = -2,

		[Display(Name = "<")]
		[Description("Less than")]
		//[EnumMember(Value = "Less than")]
        LessThan = -1,

        [Display(Name = "≥")]
        [Description("Greater than or equal to")]
        //[EnumMember(Value = "Greater than")]
        GreaterThan = 1,

        [Display(Name = "=")]
        [Description("Equal to")]
        //[EnumMember(Value = "Greater than")]
        EqualTo = 0,

		[Display(Name = ">")]
		[Description("Greater than")]
		//[EnumMember(Value = "Greater than")]
        GreaterThanNotEqual = 2,
	}

	
}

namespace RadialReview
{
	public static class LessGreaterExtensions
	{
        public static bool MeetGoal(this LessGreater self,decimal goal, decimal? measured)
        {
            if (measured == null)
                return false;

            switch (self) {
                case LessGreater.LessThan:
                    return measured < (goal);
                case LessGreater.GreaterThan:
                    return measured >= (goal);
                case LessGreater.EqualTo:
                    return measured == (goal);
                case LessGreater.GreaterThanNotEqual:
                    return measured > (goal);
                case LessGreater.LessThanOrEqual:
                    return measured <= (goal);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

		public static string ToSymbol(this LessGreater self)
		{
			switch (self)
			{
				case LessGreater.LessThan:
                    return "<";
                case LessGreater.GreaterThan:
                    return "≥";
                case LessGreater.EqualTo:
                    return "=";
                case LessGreater.GreaterThanNotEqual:
                    return ">";
                case LessGreater.LessThanOrEqual:
                    return "≤";
				default:                    
					throw new ArgumentOutOfRangeException();
			}
		}

        /////////////  Also update Upload_ScorecardController!!!  /////////////
	}
}