using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities {
	public static class DateTimeExtensions {
		public static long ToJsMs(this DateTime time) {
			return (long)(time - new DateTime(1970, 01, 01)).TotalMilliseconds;
		}
		public static DateTime ToDateTime(this long jsMsUtc, DateTimeKind kind = DateTimeKind.Unspecified) {
			return new DateTime(1970, 01, 01,0,0,0,kind).AddMilliseconds(jsMsUtc);
			
		}
	}
}
