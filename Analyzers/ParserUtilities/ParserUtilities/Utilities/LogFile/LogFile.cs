using ParserUtilities.Utilities.DataTypes;
using ParserUtilities.Utilities.OutputFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {

	public delegate string ILogLineField<LINE>(LINE line) where LINE : ILogLine;
	public delegate DateTime ILogLineDateField<LINE>(LINE line) where LINE : ILogLine;

	public class LogFile<LINE> where LINE : ILogLine {
		public string Path { get; set; }
		public DateTime ParseTime { get; set; }
		public DateTime StartRange { get; set; }
		public DateTime EndRange { get; set; }

		protected Func<LINE, object> Ordering { get; set; }
		protected List<LINE> Lines { get; set; }
		protected List<IFilter<LINE>> Filters { get; set; }
		protected List<IFilter<LINE>> RelativeRangeFilter { get; set; }
		protected Func<LINE, object> Grouping { get; set; }
		protected int SkipLines { get; set; }
        protected List<Action<LINE>> ForEachs { get; set; }

        protected List<Tuple<Func<LINE, bool>,FlagType>> Flags { get; set; }
		protected bool FlagsToTop { get; private set; }
        protected List<TimeSlice> Slices { get; set; }


		public LogFile<LINE> Clone() {
            return new LogFile<LINE>() {
                Path = Path,
                ParseTime = ParseTime,
                StartRange = StartRange,
                EndRange = EndRange,
                Ordering = Ordering,
                Lines = Lines.ToList(),
                Filters = Filters.ToList(),
                RelativeRangeFilter = RelativeRangeFilter.ToList(),
                Flags = Flags.ToList(),
                SkipLines = SkipLines,
                Grouping = Grouping,
                FlagsToTop = FlagsToTop,
                ForEachs = ForEachs.ToList(),
                Slices = Slices.ToList(),
            };
		}

		public LogFile() {
			Lines = new List<LINE>();
			ParseTime = DateTime.UtcNow;
			Filters = new List<IFilter<LINE>>();
			StartRange = DateTime.MaxValue;
			EndRange = DateTime.MinValue;
			RelativeRangeFilter = new List<IFilter<LINE>>();
			Flags = new List<Tuple<Func<LINE, bool>, FlagType>>();
            ForEachs = new List<Action<LINE>>();
            Slices = new List<TimeSlice>();
		}

		public LINE AddLine(LINE line) {
			StartRange = new DateTime(Math.Min(line.StartTime.Ticks, StartRange.Ticks));
			EndRange = new DateTime(Math.Max(line.EndTime.Ticks, EndRange.Ticks));
			Lines.Add(line);
			return line;
		}
		public void SetGrouping<T>(Func<LINE, T> groupBy) {
			Grouping = x => groupBy(x);
		}

		public void SetOrdering<T>(Func<LINE, T> order) {
			Ordering = x=>order(x);
		}
						
		public void AddFilters(params IFilter<LINE>[] filters) {
			foreach (var filter in filters) {
				TestFilterForConflits(filter);
				Filters.Add(filter);
			}
		}

		private void TestFilterForConflits(IFilter<LINE> filter) {
			var anyConfilts = Filters.Where(x => x.Conflit(filter));
			if (anyConfilts.Any())
				throw new Exception("This filter:\n\t" + filter.ToString() + "\nconflicts with the following filters:\n" + string.Join("\n", anyConfilts.Select(x => "\t" + x.ToString())));
		}

		public void Skip(int lines) {
			SkipLines = lines;
		}

		public void Flag(Func<LINE, bool> condition,FlagType flagType = FlagType.UserFlag) {
			Flags.Add(Tuple.Create(condition, flagType));
		}

		internal void FilterExact(ILogLineField<LINE> field, params string[] exclude) {
			AddFilters(exclude.Select(x => new StringFilter<LINE>(x, FilterType.Exclude, field, true)).ToArray());
		}

		public LogFile<LINE> Filter(ILogLineField<LINE> field, params string[] exclude) {
			AddFilters(exclude.Select(x=>new StringFilter<LINE>(x, FilterType.Exclude, field)).ToArray());
			return this;
		}
		public LogFile<LINE> Filter(Func<LINE, bool> predicate, FilterType type = FilterType.Exclude) {
			AddFilters(new CustomFilter<LINE>(predicate, type));
			return this;
        }
        public LogFile<LINE> FilterRange(TimeRange range, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
            return FilterRange(range.Start, range.End, type);
        }
        public LogFile<LINE> FilterRange(TimeRange range, double expandBy, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
            return FilterRange(range.Start, range.End, expandBy, type);
        }
        public LogFile<LINE> FilterRange(DateTime start, DateTime end, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
			AddFilters(new DateRangeFilter<LINE>(start, end, type, x => x.StartTime, x => x.EndTime));
			return this;
		}
		public LogFile<LINE> FilterRange(DateTime start, DateTime end,double expandByMinutes, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
			AddFilters(new DateRangeFilter<LINE>(start.AddMinutes(-expandByMinutes/2), end.AddMinutes(expandByMinutes/2), type, x => x.StartTime, x => x.EndTime));
			return this;
		}

		[Obsolete("Must be just before save")]
		public LogFile<LINE> FilterRelativeRange(double startMinutes, double? endMinutes=null, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
			FilterRelativeRange(TimeSpan.FromMinutes(startMinutes), TimeSpan.FromMinutes(endMinutes??1000000), type);
			return this;
		}

		[Obsolete("Must be just before save")]
		public LogFile<LINE> FilterRelativeRange(TimeSpan start, TimeSpan? end=null, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
			var first = GetFilteredLines().First();
			var s = new DateTime(Math.Min(first.StartTime.Ticks, first.EndTime.Ticks));
			var d = first.EndTime - first.StartTime;
			var filter = new DateRangeFilter<LINE>(s + start, s + d + (end ?? TimeSpan.FromMinutes(1000000)), type, x => x.StartTime, x => x.EndTime);
			TestFilterForConflits(filter);
			RelativeRangeFilter.Add(filter);
			return this;
		}

		public int Count() {
			return GetFilteredLines().Count();
		}

		public IEnumerable<LINE> GetFilteredLines() {
			//Apply filters
			var f= Lines.Where(line => Filters.All(filter => filter.Include(line)));			
			//apply orderings
			if (Ordering != null)
				f = f.OrderBy(Ordering);
			//Apply relative filter
			f = f.Where(line => RelativeRangeFilter.All(filter=>filter.Include(line)));


			//Handle Grouping
			if (Grouping != null) {
				var i = 0;
				var groups = f.GroupBy(Grouping);
				f= groups.SelectMany(x => {
					x.ToList().ForEach(y => { y.GroupNumber = i; });
					i += 1;
					return x.ToList();
				});
				var groupingsLookup = groups.ToDictionary(x => x.Key, x => x.ToList());
				if (Ordering != null) {
					f = f.OrderByDescending(x=>groupingsLookup[Grouping(x)].Count()).ThenBy(x=> groupingsLookup[Grouping(x)].First().StartTime).ThenBy(Grouping).ThenBy(Ordering);
				} else {
					f = f.OrderByDescending(x => groupingsLookup[Grouping(x)].Count()).ThenBy(x => groupingsLookup[Grouping(x)].First().StartTime).ThenBy(Grouping);
				}
				f = f.ToList();
				var j = -1;
				var prevGroup = -1;
				foreach (var item in f) {
					var ign = item.GroupNumber;
					if (ign != prevGroup) {
						j += 1;
						prevGroup = item.GroupNumber;
					}
					item.GroupNumber = j;
				}

			}

			//Apply flags
			foreach (var h in Flags) {
				foreach (var line in f.ToList()) {
					var flag = h.Item1(line);
					if (flag) {
						line.Flag |= h.Item2;
					}
				}
			}

			//Skip Lines
			f = f.Skip(SkipLines);

			//Add flags to top
			f = f.ToList();
			if (FlagsToTop) {
				var orderedFlags = f.Where(x => x.Flag != FlagType.None).OrderByDescending(x => x.Flag).ToList();
				orderedFlags.AddRange(f.ToList());
				f = orderedFlags;
			}

            foreach (var line in f) {
                foreach (var each in ForEachs) {
                    each(line);
                }
            }

			return f;
		}

        public List<TimeSlice> GetSlices() {
            return Slices.ToList();
        }

        public void ForEach(Action<LINE> action) {
            ForEachs.Add(action);
        }

        public IEnumerable<string> ToStringLines(string separator) {
			var f = GetFilteredLines();
			var first = f.FirstOrDefault();
			var date = StartRange;
			var title ="";
			if (first != null) {
				date = first.StartTime;
				title = string.Join(separator, first.GetHeaders() );
			}
			var lines = new List<string> { title };
			lines.AddRange(f.Select(x => string.Join(separator, x.GetLine(date))));
			return lines;
		}
		public void Save(string path, string record_separator) {
			var lines = ToStringLines(record_separator);
			File.WriteAllLines(path, lines);
			if (!lines.Any()) {
				Log.Warn("No lines.",true);
			}
		}

		public PivotTable<LINE> ToPivotTable(ILogLineField<LINE> x, ILogLineField<LINE> y,Func<List<LINE>,string> cell) {
			return new PivotTable<LINE>(this, x, y, cell);
		}

		public PreMatrix<LINE,XTYPE,YTYPE> ToMatrixBuilder<XTYPE,YTYPE>(Func<LINE, XTYPE> xs, Func<LINE, YTYPE> ys) {
			return Matrix.Create(this, xs, ys);
		}

		public void FlagsAtTop() {
			FlagsToTop = true;
		}

        public void AddSlice(TimeRange range, string name) {
            Slices.Add(new TimeSlice(range, name));
        }

        public void AddSlice(DateTime time, DateTimeKind kind, string name) {
            Slices.Add(new TimeSlice(time, kind, name));
        }

        public void AddSlice(DateTime start, DateTime endTime, string name="") {
            Slices.Add(new TimeSlice(new TimeRange(start, endTime,start.Kind), name));
        }

        /*public Matrix<XTYPE,YTYPE,LINE, RESULT> ToMatrix<XTYPE,YTYPE,RESULT>(Func<LINE, XTYPE> xs, Func<LINE, YTYPE> ys,Func<Matrix<XTYPE, YTYPE, LINE, RESULT>.MatrixInput, RESULT> cellSelector, Func<Matrix<XTYPE, YTYPE, LINE, RESULT>.MatrixResult, Matrix<XTYPE, YTYPE, LINE, RESULT>.MatrixResult, Matrix<XTYPE, YTYPE, LINE, RESULT>.MatrixResult> aggregator) {
			return new Matrix<XTYPE, YTYPE, LINE, RESULT>(this, new XTYPE[0], new YTYPE[0], cellSelector, aggregator);
		}*/
        /*public Matrix<LINE, RESULT> ToMatrix<RESULT>(Func<MatrixInput<LINE>, RESULT> cellSelector, Func<Matrix<LINE, RESULT>.MatrixResult, Matrix<LINE, RESULT>.MatrixResult, Matrix<LINE, RESULT>.MatrixResult> aggregator, IEnumerable<string> xs, IEnumerable<string> ys) {
			return new Matrix<LINE, RESULT>(this, xs.ToArray(), ys.ToArray(), cellSelector, aggregator);
		}
		public Matrix<LINE, RESULT> ToMatrix<RESULT>(Func<MatrixInput<LINE>, RESULT> cellSelector, Func<Matrix<LINE, RESULT>.MatrixResult, Matrix<LINE, RESULT>.MatrixResult, Matrix<LINE, RESULT>.MatrixResult> aggregator, IEnumerable<string> xs, ILogLineField<LINE> ys) {
			return new Matrix<LINE, RESULT>(this, xs.ToArray(), ys, cellSelector, aggregator);
		}
		public Matrix<LINE, RESULT> ToMatrix<RESULT>(Func<MatrixInput<LINE>, RESULT> cellSelector, Func<Matrix<LINE, RESULT>.MatrixResult, Matrix<LINE, RESULT>.MatrixResult, Matrix<LINE, RESULT>.MatrixResult> aggregator, ILogLineField<LINE> xs, IEnumerable<string> ys) {
			return new Matrix<LINE, RESULT>(this, xs, ys.ToArray(), cellSelector, aggregator);
		}*/
    }
}
