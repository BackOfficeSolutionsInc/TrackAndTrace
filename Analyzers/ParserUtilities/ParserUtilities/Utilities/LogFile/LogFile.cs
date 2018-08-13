using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {

	public delegate string ILogLineField(dynamic line);
	public delegate DateTime ILogLineDateField(dynamic line);

	public class LogFile {
		public string Path { get; set; }
		public DateTime ParseTime { get; set; }
		public DateTime StartRange { get; set; }
		public DateTime EndRange { get; set; }

		protected Func<ILogLine, object> Ordering { get; set; }
		protected List<ILogLine> Lines { get; set; }
		protected List<IFilter> Filters { get; set; }
		protected List<IFilter> RelativeRangeFilter { get; set; }


		public LogFile() {
			Lines = new List<ILogLine>();
			ParseTime = DateTime.UtcNow;
			Filters = new List<IFilter>();
			StartRange = DateTime.MaxValue;
			EndRange = DateTime.MinValue;
			RelativeRangeFilter = new List<IFilter>();
		}

		public ILogLine AddLine(ILogLine line) {
			StartRange = new DateTime(Math.Min(line.StartTime.Ticks, StartRange.Ticks));
			EndRange = new DateTime(Math.Max(line.EndTime.Ticks, EndRange.Ticks));
			Lines.Add(line);
			return line;
		}


		public void SetOrdering<T>(Func<ILogLine, T> order) {
			Ordering = x=>order(x);
		}
						
		public void AddFilters(params IFilter[] filters) {
			foreach (var filter in filters) {
				TestFilterForConflits(filter);
				Filters.Add(filter);
			}
		}

		private void TestFilterForConflits(IFilter filter) {
			var anyConfilts = Filters.Where(x => x.Conflit(filter));
			if (anyConfilts.Any())
				throw new Exception("This filter:\n\t" + filter.ToString() + "\nconflicts with the following filters:\n" + string.Join("\n", anyConfilts.Select(x => "\t" + x.ToString())));
		}

		internal void FilterExact(ILogLineField field, params string[] exclude) {
			AddFilters(exclude.Select(x => new StringFilter(x, FilterType.Exclude, field, true)).ToArray());
		}

		public void Filter(ILogLineField field, params string[] exclude) {
			AddFilters(exclude.Select(x=>new StringFilter(x, FilterType.Exclude, field)).ToArray());
		}
		public void Filter(Func<ILogLine, bool> predicate, FilterType type = FilterType.Exclude) {
			AddFilters(new CustomFilter(predicate, type));
		}
		public void FilterRange(DateTime start, DateTime end, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
			AddFilters(new DateRangeFilter(start, end, type, x => x.StartTime, x => x.EndTime));
		}

		[Obsolete("Must be just before save")]
		public void FilterRelativeRange(double startMinutes, double? endMinutes=null, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
			FilterRelativeRange(TimeSpan.FromMinutes(startMinutes), TimeSpan.FromMinutes(endMinutes??1000000), type);
		}
		[Obsolete("Must be just before save")]
		public void FilterRelativeRange(TimeSpan start, TimeSpan? end=null, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
			var first = GetFilteredLines().First();
			var s = new DateTime(Math.Min(first.StartTime.Ticks, first.EndTime.Ticks));
			var d = first.EndTime - first.StartTime;
			var filter = new DateRangeFilter(s + start, s + d + (end ?? TimeSpan.FromMinutes(1000000)), type, x => x.StartTime, x => x.EndTime);
			TestFilterForConflits(filter);
			RelativeRangeFilter.Add(filter);
		}
		


		public IEnumerable<ILogLine> GetFilteredLines() {
			var f= Lines.Where(line => Filters.All(filter => filter.Include(line)));
			if (Ordering != null)
				f = f.OrderBy(Ordering);			

			f = f.Where(line => RelativeRangeFilter.All(filter=>filter.Include(line)));

			return f;
		}
		public IEnumerable<string> ToStringLines(string separator) {
			var f = GetFilteredLines();
			var first = f.FirstOrDefault();
			var date = StartRange;
			var title ="";
			if (first != null) {
				date = first.StartTime;
				title = string.Join(separator, first.ToTitle() );
			}
			var lines = new List<string> { title };
			lines.AddRange(f.Select(x => string.Join(separator, x.ToLine(date))));
			return lines;
		}
		public void Save(string path, string record_separator) {
			var lines = ToStringLines(record_separator);
			File.WriteAllLines(path, lines);
			if (!lines.Any()) {
				Log.Warn("No lines.",true);
			}
		}
		//public void Save() {
		//	Save(Config.GetBaseDirectory()+"log.txt");
		//}

	}


	

}
