using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Ajax.Utilities;

namespace RadialReview
{
    public static class JavascriptExtensions
    {

        public static string ToJavascript(this bool self)
        {
            return self ? "true" : "false";
        }

        public static DateTime ToDateTime(this long timeSinceEpoch)
        {
	        try{
		        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timeSinceEpoch);
	        }
	        catch (Exception){
		        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	        }
        }

        public static long ToJavascriptMilliseconds(this DateTime time)
        {
            return (long)time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}