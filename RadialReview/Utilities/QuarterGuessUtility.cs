using RadialReview.Models.Askables;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace RadialReview.Utilities {
	public class QuarterGuessUtility {
		public class QuarterDateConfidence {
			public QuarterDateConfidence(double confidence, DateTime date) {
				Confidence = confidence;
				Date = date;
			}
			public double Confidence { get; set; }
			public DateTime Date { get; set; }
		}

		public static List<RockModel> GuessRocksFromLastQuarter() {

			return null;
		}

		public static List<QuarterDateConfidence> RemoveOutliers(List<DateTime> dates) {
			var dd = new DefaultDictionary<DateTime, double>(x => 0);
			dates.Add(new DateTime(2013, 11, 1));
			var rate = Math.Pow(1000, 1 / dates.Count);
			return RemoveOutliers(dates, 14, dd, rate, 1);
		}

		protected static List<QuarterDateConfidence> RemoveOutliers(List<DateTime> dates, double allowBelowDays, DefaultDictionary<DateTime, double> cumulativeScore, double rate, double compound) {


			var allDates = dates.OrderBy(x => x).ToList();

			if (!allDates.Any())
				return new List<QuarterDateConfidence>() { /*new QuarterDateConfidence(0, DateTime.UtcNow)*/ };

			if (allDates.Count == 1)
				return new List<QuarterDateConfidence>() { new QuarterDateConfidence(0, allDates.First()) };

			var sqDiffs = new List<Tuple<DateTime, double>>();

			for (var i = 1; i < allDates.Count; i++) {
				var diff = Math.Pow((allDates[i] - allDates[i - 1]).TotalDays, 2);
				sqDiffs.Add(Tuple.Create(allDates[i], diff));
			}

			var ordered = sqDiffs.OrderByDescending(x => x.Item2).ToList();
			var filtered = ordered.Select((x, i) => new { Date = x.Item1, Score = x.Item2, Index = i })
				.Where(x => x.Score > allowBelowDays * allowBelowDays)
				.ToList();

			var outlier = filtered.LastOrDefault();

			if (outlier != null) {
				ordered.RemoveAt(outlier.Index);

				foreach (var o in ordered)
					cumulativeScore[o.Item1] += o.Item2 * compound;

				return RemoveOutliers(ordered.Select(x => x.Item1).ToList(), allowBelowDays, cumulativeScore, rate, compound * rate);
			} else {
				return cumulativeScore.Select(x => new QuarterDateConfidence(x.Value, x.Key)).OrderByDescending(x => x.Confidence).ToList();
			}
		}

		public static List<QuarterDateConfidence> GuessQuarterStart(List<RockModel> rocks) {

			var allDates = rocks.Select(x => x.CreateTime).OrderBy(x => x).ToList();






			return null;
		}
	}
}