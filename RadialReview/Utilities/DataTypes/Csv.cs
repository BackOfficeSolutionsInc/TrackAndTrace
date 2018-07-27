using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
	public class Csv {
		private List<CsvItem> Items { get; set; }
		private List<String> Rows { get; set; }
		private List<String> Columns { get; set; }

		private DefaultDictionary<string, int?> RowsPositions { get; set; }
		private DefaultDictionary<string, int?> ColumnPositions { get; set; }

		private int RowLength = 0;
		private int ColLength = 0;

		public string Title { get; set; }
		public Csv(string title = null) {
			Items = new List<CsvItem>();
			Rows = new List<String>();
			Columns = new List<String>();
			RowsPositions = new DefaultDictionary<string, int?>(x => null);
			ColumnPositions = new DefaultDictionary<string, int?>(x => null);
			Title = title;
		}

		public void Add(String row, String column, String value) {

			row = row ?? "null";
			column = column ?? "null";

			if (RowsPositions[row] == null) {
				Rows.Add(row);
				RowsPositions[row] = RowLength;
				RowLength += 1;
			}
			if (ColumnPositions[column]==null) {
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

		public String ToCsv(bool showRowTitle = true) {
			var sb = new StringBuilder();
			var cols = Columns.ToList();
			var rows = Rows.ToList();

			var items = new String[RowLength][];

			for (var i = 0; i < rows.Count; i++)
				items[i] = new String[ColLength];

			foreach (var item in Items) {
				var col = ColumnPositions[item.Column].Value;
				var row = RowsPositions[item.Row].Value;
				items[row][col] = CsvQuote(item.Value);
			}
			var cc = new List<String>();
			if (showRowTitle)
				cc.Add(CsvQuote(Title));

			cc.AddRange(cols.Select(CsvQuote));
			sb.AppendLine(String.Join(",", cc));

			for (var i = 0; i < rows.Count; i++) {
				var rr = new List<String>();
				if (showRowTitle)
					rr.Add(CsvQuote(rows[i]));

				rr.AddRange(items[i]);
				sb.AppendLine(String.Join(",", rr));
			}
			return sb.ToString();
		}

		static public string CsvQuote(string cell) {
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
	}

	public class CsvItem {
		public String Column { get; set; }
		public String Row { get; set; }
		public String Value { get; set; }
	}
}