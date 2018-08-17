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
	public interface IFilter<LINE> where LINE : ILogLine {
		bool Include(LINE line);
		bool Conflit(IFilter<LINE> f);
	}
	public class RegexFilter<LINE> : IFilter<LINE> where LINE : ILogLine {
		public RegexFilter(Regex filterRegex, FilterType type, ILogLineField<LINE> field) {
			FilterRegex = filterRegex;
			Type = type;
			Field = field;
		}

		public ILogLineField<LINE> Field { get; set; }
		public Regex FilterRegex { get; set; }
		public FilterType Type { get; set; }

		public bool Conflit(IFilter<LINE> f) {
			return false;
		}

		public bool Include(LINE line) {
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

	public class CustomFilter<LINE> : IFilter<LINE> where LINE : ILogLine {
		public CustomFilter(Func<LINE, bool> predicate, FilterType type) {
			Predicate = predicate;
			Type = type;
		}

		public FilterType Type { get; set; }
		public Func<LINE, bool> Predicate { get; set; }

		public bool Conflit(IFilter<LINE> f) {
			return false;
		}

		public bool Include(LINE line) {
			if (Type == FilterType.Include)
				return Predicate(line);
			else if (Type == FilterType.Exclude)
				return !Predicate(line);
			throw new ArgumentOutOfRangeException("" + Type);
		}
	}


	public class StringFilter<LINE> : IFilter<LINE> where LINE : ILogLine {
		public StringFilter(string substring, FilterType type, ILogLineField<LINE> field, bool exact = false) {
			Substring = substring;
			if (!exact)
				Substring = Substring.ToLower();
			Type = type;
			Field = field;
			Exact = exact;
		}

		public static StringFilter<LINE> Exclude(string substring, ILogLineField<LINE> field) {
			return new StringFilter<LINE>(substring, FilterType.Exclude, field);
		}

		public ILogLineField<LINE> Field { get; set; }
		public FilterType Type { get; set; }
		public string Substring { get; set; }
		public bool Exact { get; set; }

		public bool Include(LINE line) {
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

		public bool Conflit(IFilter<LINE> f) {
			return false;
		}

	}

	public enum DateFilterType {
		Before,
		After
	}
	public class DateFilter<LINE> : IFilter<LINE> where LINE : ILogLine  {

		private DateFilterType SwapIf(DateFilterType type, bool shouldSwap) {
			if (shouldSwap) {
				if (type == DateFilterType.After)
					return DateFilterType.Before;
				return DateFilterType.After;
			} else {
				return type;
			}
		}

		public DateFilter(DateTime time, DateFilterType when, FilterType type, ILogLineDateField<LINE> field) {
			FilterOn = time;
			IncludeWhen = SwapIf(when, type == FilterType.Exclude);
			Field = field;
		}

		public static DateFilter<LINE> ExcludeIfBefore(DateTime time, ILogLineDateField<LINE> field) {
			return new DateFilter<LINE>(time, DateFilterType.Before, FilterType.Exclude, field);
		}
		public static DateFilter<LINE> ExcludeIfAfter(DateTime time, ILogLineDateField<LINE> field) {
			return new DateFilter<LINE>(time, DateFilterType.After, FilterType.Exclude, field);
		}

		public ILogLineDateField<LINE> Field { get; set; }
		public DateFilterType IncludeWhen { get; set; }

		public DateTime FilterOn { get; set; }

		public bool Include(LINE line) {
			var f = Field(line);
			var isBefore = f < FilterOn;
			if (IncludeWhen == DateFilterType.Before)
				return isBefore;
			else if (IncludeWhen == DateFilterType.After)
				return !isBefore;
			throw new ArgumentOutOfRangeException("" + IncludeWhen);
		}

		public bool Conflit(IFilter<LINE> f) {
			if (f is DateFilter<LINE>) {
				var other = f as DateFilter<LINE>;
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
		CompletelyInRange,
		CompletelyInLowerRange
	}

	public class DateRangeFilter<LINE> : IFilter<LINE> where LINE : ILogLine {

		public DateRangeFilter(DateTime startRange, DateTime endRange, DateRangeFilterType when, ILogLineDateField<LINE> startTimeField, ILogLineDateField<LINE> endTimeField) {
			var beforeSelector = when == DateRangeFilterType.PartlyInRange ? startTimeField : endTimeField;
			var afterSelector = when == DateRangeFilterType.PartlyInRange ? endTimeField : startTimeField;
			if (when == DateRangeFilterType.PartlyInRange) {
				Before = DateFilter<LINE>.ExcludeIfAfter(endRange, startTimeField);
				After = DateFilter<LINE>.ExcludeIfBefore(startRange, endTimeField);
			} else if (when == DateRangeFilterType.CompletelyInRange) {
				Before = DateFilter<LINE>.ExcludeIfAfter(endRange, endTimeField);
				After = DateFilter<LINE>.ExcludeIfBefore(startRange, startTimeField);
			} else if (when == DateRangeFilterType.CompletelyInLowerRange) {
				Before = DateFilter<LINE>.ExcludeIfAfter(endRange, startTimeField);
				After = DateFilter<LINE>.ExcludeIfBefore(startRange, startTimeField);
			}else {
				throw new ArgumentOutOfRangeException("" + when);
			}
		}

		private DateFilter<LINE> Before { get; set; }
		private DateFilter<LINE> After { get; set; }

		public bool Conflit(IFilter<LINE> f) {
			return Before.Conflit(f) || After.Conflit(f);
		}

		public bool Include(LINE line) {
			return Before.Include(line) && After.Include(line);
		}
	}
}
