using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.DataTypes {
	public class Rangeable {
		public double Min { get; private set; }
		public double Max { get; private set; }

		public Rangeable(double min, double max) {
			Min = min;
			Max = max;
		}

		public static bool IsRangeable(Type o) {
			if (o == typeof(TimeSpan) || o == typeof(DateTime) || o == typeof(double) || o == typeof(decimal) || o == typeof(int) || o == typeof(long) || o == typeof(float)) {
				return true;
			}

			if (o == typeof(TimeSpan?) || o == typeof(DateTime?) || o == typeof(double?) || o == typeof(decimal?) || o == typeof(int?) || o == typeof(long?) || o == typeof(float?)) {
				return true;
			}
			return false;
		}

		public static Rangeable GetRangeable(object[] o) {

			var sorted = o.Select(x => MapToDouble(x))
				.OrderBy(x => x)
				.Where(x => x.HasValue)
				.Select(x => x.Value)
				.ToList();

			if (sorted.Any()) {
				return new Rangeable(sorted.First(), sorted.Last());
			}

			return Rangeable.Invalid;
		}

		public double GetPercentage(object o) {
			var d = MapToDouble(o);
			if (d == null)
				return OnNull();
			d = Math.Max(Min, Math.Min(Max, d.Value));

			return (d.Value - Min) / (Max - Min);
		}

		private double OnNull() {
			return Min;
		}

		public static Rangeable Invalid = new Rangeable(double.MinValue, double.MaxValue);

		private static double? MapToDouble(object o) {
			if (o == null)
				return null;
			if (o is TimeSpan)
				return ((TimeSpan)o).Ticks;
			if (o is DateTime)
				return ((DateTime)o).Ticks;
			return o as double?;

		}

	}
}
