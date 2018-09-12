using System;
using System.Linq;
using System.Collections.Generic;
using ParserUtilities.Utilities.LogFile;
using ParserUtilities.Utilities.DataTypes;

namespace ParserUtilities.Utilities.OutputFile {

	public class Matrix {
		public static PreMatrix<LINE, XTYPE, YTYPE> Create<LINE, XTYPE, YTYPE>(LogFile<LINE> file, Func<LINE, XTYPE> xs, Func<LINE, YTYPE> ys) where LINE : ILogLine {
			return new PreMatrix<LINE, XTYPE, YTYPE>(file, xs, ys);
		}

	}

	public class PreMatrix<LINE, XTYPE, YTYPE> where LINE : ILogLine {
		public PreMatrix(LogFile<LINE> file, Func<LINE, XTYPE> xSelector, Func<LINE, YTYPE> ySelector) {
			File = file;
			XSelector = xSelector;
			YSelector = ySelector;
		}

		protected LogFile<LINE> File { get; set; }
		protected Func<LINE, XTYPE> XSelector { get; set; }
		protected Func<LINE, YTYPE> YSelector { get; set; }

		protected Func<LINE, IComparable> XOrdering { get; set; }
		protected Func<LINE, IComparable> YOrdering { get; set; }

		public PreMatrix<LINE, XTYPE, YTYPE> XOrder(Func<LINE, IComparable> order) {
			XOrdering = (x => order(x));
			return this;
		}
		public PreMatrix<LINE, XTYPE, YTYPE> YOrder(Func<LINE, IComparable> order) {
			YOrdering = (x => order(x));
			return this;
		}


		public class Input {
			public XTYPE X { get; set; }
			public YTYPE Y { get; set; }
			public LINE Line { get; set; }
		}

		public Matrix<LINE, T> AggregateOn<T>(Func<Input, T, T> aggregate) {
			return new Matrix<LINE, T>(File, a => XSelector(a), a => YSelector(a), (input, prev) => {
				var transform = new Input {
					Line = input.Line,
					X = (XTYPE)input.X,
					Y = (YTYPE)input.Y,
				};
				return aggregate(transform, prev);
			},XOrdering,YOrdering);
		}

		public Matrix<LINE, int> IncrementIf(Func<Input, bool> predicte) {
			return AggregateOn<int>((x, prev) => (predicte(x) ? 1 : 0) + prev);
		}
	}

	public class Matrix<LINE, T> where LINE : ILogLine {

		public class Input {
			public object X { get; set; }
			public object Y { get; set; }
			public LINE Line { get; set; }
		}

		public Matrix(LogFile<LINE> file, Func<LINE, object> xSelector, Func<LINE, object> ySelector, Func<Input, T, T> aggregator, Func<LINE, IComparable> xOrder, Func<LINE, IComparable> yOrder) {
			File = file;
			XSelector = xSelector;
			YSelector = ySelector;
			Aggregator = aggregator;
			XOrder = xOrder ?? (a => (xSelector(a) as IComparable)??0);
			YOrder = yOrder ?? (a => (ySelector(a) as IComparable)??0);
		}

		protected LogFile<LINE> File { get; set; }
		protected Func<LINE, object> XSelector { get; set; }
		protected Func<LINE, object> YSelector { get; set; }
		protected Func<LINE, object> XOrder { get; set; }
		protected Func<LINE, object> YOrder { get; set; }
		protected Func<Input, T, T> Aggregator { get; set; }

		protected class MatrixResult {
			public object X { get; set; }
			public object Y { get; set; }
			public double Value { get; set; }
		}

		public Csv ToCsv() {
			var lines = File.GetFilteredLines();
			var xs = lines.OrderBy(a => XOrder(a)).Select(x => XSelector(x)).Distinct().ToArray();
			var ys = lines.OrderBy(a => YOrder(a)).Select(x => YSelector(x)).Distinct().ToArray();

			var dict = new DefaultDictionary<object, DefaultDictionary<object, T>>(x => new DefaultDictionary<object, T>(y => default(T)));

			var results = new List<MatrixResult>();
			foreach (var x in xs) {
				foreach (var y in ys) {
					//var clone = File.Clone();
					foreach (var line in lines) {
						/*var myX = XSelector(line);
						var myY = YSelector(line);
						if ((myX == x && myY==y) || (myX!=null && myY!=null && myX.Equals(x) && myY.Equals(y))) {*/
						var input = new Input {
							Line = line,
							X = x,
							Y = y
						};
						dict[x][y] = Aggregator(input, dict[x][y]);
						//}
					}
				}
			}

			var csv = new Csv();
			foreach (var x in xs) {
				foreach (var y in ys) {
					csv.Add("" + y, "" + x, "" + dict[x][y]);
				}
			}
			return csv;
		}

		public void Save(string file) {
			ToCsv().Save(file);
		}
	}

	/*
	public class Matrix<XTYPE,YTYPE,LINE, RESULT> where LINE : ILogLine {

		public class MatrixInput {
			public XTYPE X { get; set; }
			public YTYPE Y { get; set; }
			public LogFile<LINE> File { get; set; }

		}
		
		public class MatrixResult {
			public MatrixResult(XTYPE x, YTYPE y) {
				X = x;
				Y = y;
			}
			public XTYPE X { get; protected set; }
			public YTYPE Y { get; protected set; }
			public RESULT Result { get; set; }
		}


		protected Func<MatrixInput, RESULT> CellSelector;
		protected Func<MatrixResult, MatrixResult, MatrixResult> Aggregator;
		protected Func<XTYPE[]> Xs;
		protected Func<YTYPE[]> Ys;
		protected LogFile<LINE> File;

		protected Matrix(LogFile<LINE> file, Func<XTYPE[]> xs, Func<YTYPE[]> ys, Func<MatrixInput, RESULT> cellSelector, Func<MatrixResult, MatrixResult, MatrixResult> aggregator) {
			CellSelector = cellSelector;
			Aggregator = aggregator;
			Xs =  xs;
			Ys =  ys;
			File = file;
		}

		public Matrix(LogFile<LINE> file, XTYPE[] xs, YTYPE[] ys, Func<MatrixInput, RESULT> cellSelector, Func<MatrixResult, MatrixResult, MatrixResult> aggregator) :
			this(file,()=>xs,()=>ys,cellSelector,aggregator){
		}
		public Matrix(LogFile<LINE> file, Func<LINE,XTYPE> xs, Func<LINE, YTYPE> ys, Func<MatrixInput, RESULT> cellSelector, Func<MatrixResult, MatrixResult, MatrixResult> aggregator) :
		this(
			file,
			() => file.GetFilteredLines().Select(x => xs(x)).Distinct().ToArray(),
			() => file.GetFilteredLines().Select(x => ys(x)).Distinct().ToArray(),
			cellSelector,
			aggregator
		) {
		}

		public Matrix(LogFile<LINE> file, XTYPE[] xs, Func<LINE,YTYPE> ys, Func<MatrixInput, RESULT> cellSelector, Func<MatrixResult, MatrixResult, MatrixResult> aggregator) :
		this(
			file,
			() => xs,
			() => file.GetFilteredLines().Select(x => ys(x)).Distinct().ToArray(),
			cellSelector,
			aggregator
		) {
		}

		public Matrix(LogFile<LINE> file, Func<LINE, XTYPE> xs, YTYPE[] ys, Func<MatrixInput, RESULT> cellSelector, Func<MatrixResult, MatrixResult, MatrixResult> aggregator) :
		this(
			file,
			() => file.GetFilteredLines().Select(x => xs(x)).Distinct().ToArray(),
			() => ys,
			cellSelector,
			aggregator
		) {
		}


		public Csv ToCsv(Func<RESULT,string> toString=null) {

			toString = toString ?? (x=>"" + x);

			var results = new List<MatrixResult>();
			foreach (var x in Xs()) {
				foreach (var y in Ys()) {
					var clone = File.Clone();
					var input = new MatrixInput() { X = x, Y = y, File = clone };
					var res = CellSelector(input);

					results.Add(new MatrixResult(input.X, input.Y) {
						Result = res,
					});
				}
			}

			var cells = results.GroupBy(x => Tuple.Create(x.X, x.Y))
				.Select(x => x.Aggregate(new MatrixResult(x.Key.Item1, x.Key.Item2) {
					Result = default(RESULT),
				}, Aggregator));

			var csv = new Csv();
			foreach (var cell in cells) {
				csv.Add(""+cell.Y, ""+cell.X, toString(cell.Result));
			}

			return csv;
		}
	}*/
}
