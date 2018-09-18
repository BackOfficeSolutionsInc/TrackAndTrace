using ParserUtilities.Utilities.DataTypes;
using ParserUtilities.Utilities.LogFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.OutputFile {
	public class PivotTable<LINE> where LINE : ILogLine {

		protected LogFile<LINE> File { get; set; }
		protected ILogLineField<LINE> X { get; set; }
		protected ILogLineField<LINE> Y { get; set; }
		protected Func<List<LINE>, string> Cell { get; set; }

		public Func<LINE,object> XOrder { get; set; }
		public Func<LINE, object> YOrder { get; set; }


		public PivotTable(LogFile<LINE> file, ILogLineField<LINE> x, ILogLineField<LINE> y, Func<List<LINE>, string> cell) {
			File = file;
			X = x;
			Y = y;
			Cell = cell;
			XOrder = a => x(a);
			YOrder = a => y(a);
		}

		public Csv ToCsv() {

			var lines = File.GetFilteredLines().ToList();
			var xs = lines.OrderBy(a => XOrder(a)).Select(a => X(a)).Distinct().ToList();
			var ys = lines.OrderBy(a => YOrder(a)).Select(a => Y(a)).Distinct().ToList();

			var output = new DefaultDictionary<string, DefaultDictionary<string, List<LINE>>>(
									x => new DefaultDictionary<string, List<LINE>>(
									y => new List<LINE>()
								));

			foreach (var line in lines) {
				output[X(line)][Y(line)].Add(line);
			}

			var csv = new Csv();
			foreach (var x in xs) {
				foreach (var y in ys) {
					csv.Add(y, x, Cell(output[x][y]));
				}
			}
			return csv;

		}

		public void Save(string file) {
			ToCsv().Save(file);
		}

	}
}
