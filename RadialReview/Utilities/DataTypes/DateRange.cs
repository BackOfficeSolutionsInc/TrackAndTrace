using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
	public class DateRange
	{

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }

		public DateRange(DateTime start,DateTime end)
		{
			StartTime = start;
			EndTime = end;
		}

		public DateRange(){
			StartTime = DateTime.MinValue;
			EndTime = DateTime.MaxValue;
		}
	}
}