using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}