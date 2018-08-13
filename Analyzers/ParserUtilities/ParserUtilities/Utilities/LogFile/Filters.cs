using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {
	public enum FilterType {
		Exclude,
		Include
	}
	public interface IFilter {
		bool Include(ILogLine line);
		bool Conflit(IFilter f);
	}
	public class RegexFilter : IFilter {
		public RegexFilter(Regex filterRegex, FilterType type, ILogLineField field) {
			FilterRegex = filterRegex;
			Type = type;
			Field = field;
		}

		public ILogLineField Field { get; set; }
		public Regex FilterRegex { get; set; }
		public FilterType Type { get; set; }

		public bool Conflit(IFilter f) {
			return false;
		}

		public bool Include(ILogLine line) {
			var f = Field(line);

			var match = FilterRegex.IsMatch(f);
			switch (Type) {
				case FilterType.Exclude:
					return !match;
				case FilterType.Include:
					return match;
				default:
					throw new ArgumentOutOfRangeException("" + Type);
			}
		}
	}

	public class CustomFilter : IFilter {
		public CustomFilter(Func<ILogLine, bool> predicate, FilterType type) {
			Predicate = predicate;
			Type = type;
		}

		public FilterType Type { get; set; }
		public Func<ILogLine, bool> Predicate { get; set; }

		public bool Conflit(IFilter f) {
			return false;
		}

		public bool Include(ILogLine line) {
			if (Type == FilterType.Include)
				return Predicate(line);
			else if (Type == FilterType.Exclude)
				return !Predicate(line);
			throw new ArgumentOutOfRangeException("" + Type);
		}
	}


	public class StringFilter : IFilter {
		public StringFilter(string substring, FilterType type, ILogLineField field, bool exact = false) {
			Substring = substring;
			if (!exact)
				Substring = Substring.ToLower();
			Type = type;
			Field = field;
			Exact = exact;
		}

		public static StringFilter Exclude(string substring, ILogLineField field) {
			return new StringFilter(substring, FilterType.Exclude, field);
		}

		public ILogLineField Field { get; set; }
		public FilterType Type { get; set; }
		public string Substring { get; set; }
		public bool Exact { get; set; }

		public bool Include(ILogLine line) {
			if (Substring == "")
				return true;

			var f = Field(line);
			bool match;
			if (Exact) {
				match = f == Substring;
			} else {
				match = f.ToLower().Contains(Substring);
			}
			switch (Type) {
				case FilterType.Exclude:
					return !match;
				case FilterType.Include:
					return match;
				default:
					throw new ArgumentOutOfRangeException("" + Type);
			}
		}

		public bool Conflit(IFilter f) {
			return false;
		}

	}

	public enum DateFilterType {
		Before,
		After
	}
	public class DateFilter : IFilter {

		private DateFilterType SwapIf(DateFilterType type, bool shouldSwap) {
			if (shouldSwap) {
				if (type == DateFilterType.After)
					return DateFilterType.Before;
				return DateFilterType.After;
			} else {
				return type;
			}
		}

		public DateFilter(DateTime time, DateFilterType when, FilterType type, ILogLineDateField field) {
			FilterOn = time;
			IncludeWhen = SwapIf(when, type == FilterType.Exclude);
			Field = field;
		}

		public static DateFilter ExcludeIfBefore(DateTime time, ILogLineDateField field) {
			return new DateFilter(time, DateFilterType.Before, FilterType.Exclude, field);
		}
		public static DateFilter ExcludeIfAfter(DateTime time, ILogLineDateField field) {
			return new DateFilter(time, DateFilterType.After, FilterType.Exclude, field);
		}

		public ILogLineDateField Field { get; set; }
		public DateFilterType IncludeWhen { get; set; }

		public DateTime FilterOn { get; set; }

		public bool Include(ILogLine line) {
			var f = Field(line);
			var isBefore = f < FilterOn;
			if (IncludeWhen == DateFilterType.Before)
				return isBefore;
			else if (IncludeWhen == DateFilterType.After)
				return !isBefore;
			throw new ArgumentOutOfRangeException("" + IncludeWhen);
		}

		public bool Conflit(IFilter f) {
			if (f is DateFilter) {
				var other = f as DateFilter;
				if (IncludeWhen == DateFilterType.Before && other.IncludeWhen == DateFilterType.After) {
					if (this.FilterOn <= other.FilterOn)
						throw new Exception("Date filters eliminate all items");
				} else if (other.IncludeWhen == DateFilterType.Before && IncludeWhen == DateFilterType.After) {
					if (other.FilterOn <= this.FilterOn)
						throw new Exception("Date filters eliminate all items");
				}
			}
			return false;
		}
	}

	public enum DateRangeFilterType {
		PartlyInRange,
		CompletelyInRange
	}

	public class DateRangeFilter : IFilter {

		public DateRangeFilter(DateTime startRange, DateTime endRange, DateRangeFilterType when, ILogLineDateField startTimeField, ILogLineDateField endTimeField) {
			var beforeSelector = when == DateRangeFilterType.PartlyInRange ? startTimeField : endTimeField;
			var afterSelector = when == DateRangeFilterType.PartlyInRange ? endTimeField : startTimeField;
			if (when == DateRangeFilterType.PartlyInRange) {
				Before = DateFilter.ExcludeIfAfter(endRange, startTimeField);
				After = DateFilter.ExcludeIfBefore(startRange, endTimeField);
			} else if (when == DateRangeFilterType.CompletelyInRange) {
				Before = DateFilter.ExcludeIfAfter(endRange, endTimeField);
				After = DateFilter.ExcludeIfBefore(startRange, startTimeField);
			} else {
				throw new ArgumentOutOfRangeException("" + when);
			}
		}

		private DateFilter Before { get; set; }
		private DateFilter After { get; set; }

		public bool Conflit(IFilter f) {
			return Before.Conflit(f) || After.Conflit(f);
		}

		public bool Include(ILogLine line) {
			return Before.Include(line) && After.Include(line);
		}
	}
}
