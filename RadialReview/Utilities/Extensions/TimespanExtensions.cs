using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace RadialReview
{
    public static class TimespanExtensions
    { 
        public static TimeSpan OneMonth()
        {
            return TimeSpan.FromDays(30).Add(TimeSpan.FromMilliseconds(1));
        }

        public static DateTime AddTimespan(this DateTime self, TimeSpan span)
        {
            if (span.TotalMilliseconds == OneMonth().TotalMilliseconds)
                return self.AddMonths(1);
            return self.Add(span);

        }

		public static string ToPrettyFormat(this TimeSpan span) {

			if (span == TimeSpan.Zero)
				return "Moments ago";

			var sb = new StringBuilder();
			if (span.TotalDays >= 1)
				return sb.AppendFormat("{0} day{1} ", (int)span.TotalDays, span.TotalDays != 1 ? "s" : String.Empty).ToString();
			if (span.TotalHours >= 1)
				return sb.AppendFormat("{0} hour{1} ", (int)span.TotalHours, span.TotalHours != 1 ? "s" : String.Empty).ToString();
			if (span.TotalMinutes >= 1)
				return sb.AppendFormat("{0} minute{1} ", (int)span.TotalMinutes, span.TotalMinutes != 1 ? "s" : String.Empty).ToString();
			if (span.TotalSeconds >= 1)
				return sb.AppendFormat("{0} second{1} ", (int)span.TotalSeconds, span.TotalSeconds != 1 ? "s" : String.Empty).ToString();

			return "Moments ago";


		}
	}
}