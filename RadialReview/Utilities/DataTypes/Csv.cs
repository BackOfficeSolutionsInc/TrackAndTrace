using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
    public class Csv
    {
        public List<CsvItem> Items { get; set; }
        public List<String> Rows { get; set; }
		public List<String> Columns { get; set; }
		public string Title { get; set; }
        public Csv ()
	    {
            Items = new List<CsvItem>();
			Rows = new List<String>();
			Columns = new List<String>();
	    }

        public void Add( String row,String column, String value)
        {
			if (!Rows.Contains(row))
				Rows.Add(row);
			if (!Columns.Contains(column))
				Columns.Add(column);


            Items.Add(new CsvItem() { Row = row, Column = column, Value = value });
        }

	    public void SetTitle(string title)
	    {
		    Title = title;
	    }
        public byte[] ToBytes()
        {
            return new System.Text.UTF8Encoding().GetBytes(this.ToCsv());
        }

        public String ToCsv(bool showRowTitle = true)
        {
            var sb = new StringBuilder();
            var cols=Columns.ToList();
            var rows=Rows.ToList();

            var items = new String[rows.Count][];
            
            for(var i=0;i<rows.Count;i++)
                items[i]=new String[cols.Count];

            foreach (var item in Items)
            {
                var col = cols.IndexOf(item.Column);
                var row = rows.IndexOf(item.Row);
				items[row][col] = CsvQuote(item.Value);
            }
			var cc = new List<String>();
	        if (showRowTitle)
				cc.Add(CsvQuote(Title));

	        cc.AddRange(cols.Select(CsvQuote));
			sb.AppendLine(String.Join(",", cc));

            for(var i=0;i<rows.Count;i++){
	            var rr = new List<String>();
				if (showRowTitle)
					rr.Add(CsvQuote(rows[i]));

	            rr.AddRange(items[i]);
                sb.AppendLine(String.Join(",",rr));
            }
            return sb.ToString();
        }

		static public string CsvQuote(string cell)
		{
			if (cell == null){
				return string.Empty;
			}

			var containsQuote = false;
			var containsComma = false;
			var containsReturn = false;
			var len = cell.Length;
			for (var i = 0; i < len && (containsComma == false || containsQuote == false); i++){
				var ch = cell[i];
				if (ch == '"'){
					containsQuote = true;
				}else if (ch == ','){
					containsComma = true;
				}else if (ch == '\n'){
					containsReturn = true;
				}
			}

			var mustQuote = containsComma || containsQuote || containsReturn;

			if (containsQuote){
				cell = cell.Replace("\"", "\"\"");
			}

			if (mustQuote){
				return "\"" + cell + "\"";  // Quote the cell and replace embedded quotes with double-quote
			}else{
				return cell;
			}
		}
    }

    public class CsvItem
    {
        public String Column { get; set; }
        public String Row { get; set; }
        public String Value { get; set; }
    }
}