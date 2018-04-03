using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	public static class DateTimeExtensions {

		public static bool IsBetween(this DateTime self, DateTime start, DateTime end) {
			return self.IsAfter(start) && self.IsBefore(end);
		}

		public static bool IsAfter(this DateTime self, DateTime other) {
			return self > other;
		}
		public static bool IsBefore(this DateTime self, DateTime other) {
			return self < other;
		}

		public static DateTime StartOfPeriod(this DateTime dt, EventFrequency period) {
			switch (period) {
				//    case EventFrequency.Minutly:    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
				case EventFrequency.Hourly:
					return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
				case EventFrequency.Daily:
					return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
				case EventFrequency.Weekly:
					return StartOfWeek(dt, DayOfWeek.Sunday);
				case EventFrequency.Biweekly:
					return TimingUtility.GetDateSinceEpoch((int)(TimingUtility.GetWeekSinceEpoch(dt) / 2) * 2);
				case EventFrequency.Monthly:
					return new DateTime(dt.Year, dt.Month, 1);
				case EventFrequency.Yearly:
					return new DateTime(dt.Year, 1, 1);
				default:
					throw new ArgumentOutOfRangeException("period", "" + period);
			}
		}

		public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek) {
			var diff = dt.DayOfWeek - startOfWeek;
			if (diff < 0) {
				diff += 7;
			}
			if (dt == DateTime.MinValue)
				return dt;

			try {
				return dt.AddDays(-1 * diff).Date;
			} catch (ArgumentOutOfRangeException) {
				return dt;
			}
		}

		public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startOfWeek) {
			return dt.StartOfWeek(startOfWeek).AddDays(6).Date;
		}

		[Obsolete("I dont think this works...")]
		public static DateTime SafeSubtract(this DateTime dt, TimeSpan ts) {
			return Math2.Max(dt, new DateTime(ts.Ticks)).Subtract(ts);
		}
	}
}
