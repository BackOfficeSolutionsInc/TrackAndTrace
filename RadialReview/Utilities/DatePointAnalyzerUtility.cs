using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities {

	public class DatePoint {
		public DatePoint(DateTime date, decimal value) {
			Date = date;
			Value = value;
		}

		public DateTime Date { get;private set; }
		public decimal Value { get; private set; }
	}

	public class DatePointAnalyzerUtility {

		public class WindowAnalysis {
			public Ratio GrowthPercentageFromBeginning { get { return new Ratio(EndValue,StartValue); } }
			public Ratio PercentageFromWindowMax { get { return new Ratio(EndValue, MaxValue); } }
			public decimal StartValue { get; set; }
			public decimal EndValue { get; set; }
			public decimal MaxValue { get; set; }
			public bool Valid { get; set; }
		}

		private static int GetIndexInclusive(List<DatePoint> data, DateTime date) {
			var found = data.Where(x => x.Date <= date  ).Select((x, i) => i).LastOrDefault();
			var foundLessOne = found - 1;
			return foundLessOne;

		}

		public static WindowAnalysis AnalyzeWindow(List<DatePoint> data, DateTime windowStart, DateTime windowEnd) {
			var result = new WindowAnalysis();			

			var windowed = data.Where(x => windowStart <= x.Date && x.Date <= windowEnd).ToList();
			
			var startIdx = GetIndexInclusive(data, windowStart);
			var endIdx = GetIndexInclusive(data, windowEnd);
			
			if (startIdx >= 0) {
				result.StartValue = data[startIdx].Value;
				windowed.Insert(0,new DatePoint(windowStart, result.StartValue));
			}
			if (endIdx >= 0) {
				result.EndValue = data[endIdx].Value;
				windowed.Add(new DatePoint(windowEnd, result.EndValue));
			}

			if (startIdx >= 0 || endIdx >= 0) {
				result.MaxValue = windowed.Max(x => x.Value);
				result.Valid = true;
			}


			return result;
		}


	}
}