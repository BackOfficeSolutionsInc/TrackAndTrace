using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class JavascriptExtensions
    {

        public static DateTime ToDateTime(this long timeSinceEpoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timeSinceEpoch);
        }

        public static long ToJavascriptMilliseconds(this DateTime time)
        {
            return (long)time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}