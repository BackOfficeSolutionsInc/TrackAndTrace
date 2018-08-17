using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.DataTypes {
	public class TimeRange {
		public DateTime Start { get; private set; }
		public DateTime End { get; private set; }
		public DateTimeKind Kind { get; set; }

		public TimeRange(DateTime start, DateTime end, DateTimeKind kind) {
			Start	= new DateTime(Math.Min(start.Ticks,end.Ticks));
			End		= new DateTime(Math.Max(start.Ticks, end.Ticks));
			Kind	= kind;
		}

		public TimeRange(double minutesBefore, DateTime end, DateTimeKind kind) : this(end - TimeSpan.FromMinutes(minutesBefore), end, kind) { }

		public TimeRange(DateTime start, double minutesAfter, DateTimeKind kind) : this(start, start + TimeSpan.FromMinutes(minutesAfter), kind) { }

		public static TimeRange Around(DateTime time, TimeSpan totalSpan, DateTimeKind kind) {
			var r = new TimeSpan(totalSpan.Ticks / 2);
			return new TimeRange(time - r, time + r, kind);
		}
	}
}
