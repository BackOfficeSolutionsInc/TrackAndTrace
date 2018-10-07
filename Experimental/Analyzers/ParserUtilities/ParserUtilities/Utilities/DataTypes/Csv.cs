using ParserUtilities.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.DataTypes {
	public class Csv {
		private List<CsvItem> Items { get; set; }
		private List<string> Rows { get; set; }
		private List<string> Columns { get; set; }

		private DefaultDictionary<string, int?> RowsPositions { get; set; }
		private DefaultDictionary<string, int?> ColumnPositions { get; set; }

		private int RowLength = 0;
		private int ColLength = 0;

		public string Title { get; set; }
		public Csv(string title = null) {
			Items = new List<CsvItem>();
			Rows = new List<string>();
			Columns = new List<string>();
			RowsPositions = new DefaultDictionary<string, int?>(x => null);
			ColumnPositions = new DefaultDictionary<string, int?>(x => null);
			Title = title;
		}

		public void Add(string row, string column, string value) {

			row = row ?? "null";
			column = column ?? "null";

			if (RowsPositions[row] == null) {
				Rows.Add(row);
				RowsPositions[row] = RowLength;
				RowLength += 1;
			}
			if (ColumnPositions[column] == null) {
				Columns.Add(column);
				ColumnPositions[column] = ColLength;
				ColLength += 1;
			}


			Items.Add(new CsvItem() { Row = row, Column = column, Value = value });
		}

		public void SetTitle(string title) {
			Title = title;
		}

		public byte[] ToBytes() {
			return new UTF8Encoding().GetBytes(this.ToCsv());
		}

		public List<string> GetColumnsCopy() {
			return Columns.Select(x => x).ToList();
		}
		public List<string> GetRowsCopy() {
			return Columns.Select(x => x).ToList();
		}

		public string ToCsv(bool showRowTitle = true) {
			var sb = new StringBuilder();
			var cols = Columns.ToList();
			var rows = Rows.ToList();

			var items = new string[RowLength][];

			for (var i = 0; i < rows.Count; i++)
				items[i] = new string[ColLength];

			foreach (var item in Items) {
				var col = ColumnPositions[item.Column].Value;
				var row = RowsPositions[item.Row].Value;
				items[row][col] = CsvQuote(item.Value);
			}
			var cc = new List<string>();
			if (showRowTitle)
				cc.Add(CsvQuote(Title));

			cc.AddRange(cols.Select(CsvQuote));
			sb.AppendLine(string.Join(",", cc));

			for (var i = 0; i < rows.Count; i++) {
				var rr = new List<string>();
				if (showRowTitle)
					rr.Add(CsvQuote(rows[i]));

				rr.AddRange(items[i]);
				sb.AppendLine(string.Join(",", rr));
			}
			return sb.ToString();
		}

		public static string CsvQuote(string cell) {
			if (cell == null) {
				return string.Empty;
			}

			if (cell.StartsWith("-"))
				cell = " " + cell;

			var containsQuote = false;
			var containsComma = false;
			var containsReturn = false;
			var len = cell.Length;
			for (var i = 0; i < len && (containsComma == false || containsQuote == false); i++) {
				var ch = cell[i];
				if (ch == '"') {
					containsQuote = true;
				} else if (ch == ',') {
					containsComma = true;
				} else if (ch == '\n') {
					containsReturn = true;
				}
			}

			var mustQuote = containsComma || containsQuote || containsReturn;

			if (containsQuote) {
				cell = cell.Replace("\"", "\"\"");
			}

			if (mustQuote) {
				return "\"" + cell + "\"";  // Quote the cell and replace embedded quotes with double-quote
			} else {
				return cell;
			}
		}

		public string Get(int i, int j) {
			var row = Rows[i];
			var col = Columns[j];

			return Items.FirstOrDefault(x => x.Column == col && x.Row == row).NotNull(x => x.Value);
		}


		public static string[] LineToCells(string line) {
			var CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
			return CSVParser.Split(line);
		}

		public void Save(string file) {

			using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write)) {
				var byteArray = ToBytes();
				fs.Write(byteArray, 0, byteArray.Length);
			}
		}

	}

	public class CsvItem {
		public string Column { get; set; }
		public string Row { get; set; }
		public string Value { get; set; }
	}
}
