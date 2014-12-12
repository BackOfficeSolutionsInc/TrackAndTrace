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

			return dt.AddDays(-1 * diff).Date;
		}

	    public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startOfWeek)
	    {
		    return dt.StartOfWeek(startOfWeek).AddDays(6).Date;
	    }

    }
}