using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class DateTimeExtensions
    {
        public static bool IsAfter(this DateTime self, DateTime other)
        {
            return self > other;
        }
        public static bool IsBefore(this DateTime self, DateTime other)
        {
            return self < other;
        }

		public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
		{
			var diff = dt.DayOfWeek - startOfWeek;
			if (diff < 0)
			{
				diff += 7;
			}
			if (dt == DateTime.MinValue)
				return dt;

			try{
				return dt.AddDays(-1*diff).Date;
			}
			catch (ArgumentOutOfRangeException){
				return dt;
			}
		}

	    public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startOfWeek)
	    {
		    return dt.StartOfWeek(startOfWeek).AddDays(6).Date;
	    }

	    public static DateTime SafeSubtract(this DateTime dt, TimeSpan ts)
	    {
		    return Math2.Max(dt, new DateTime(ts.Ticks)).Subtract(ts);
	    }

    }
}