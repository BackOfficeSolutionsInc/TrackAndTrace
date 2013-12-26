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
        public HashSet<String> Rows { get; set; }
        public HashSet<String> Columns { get; set; }

        public Csv ()
	    {
            Items = new List<CsvItem>();
            Rows = new HashSet<String>();
            Columns = new HashSet<String>();
	    }

        public void Add( String row,String column, String value)
        {
            Rows.Add(column);
            Columns.Add(row);
            Items.Add(new CsvItem() { Row = row, Column = column, Value = value });
        }

       

        public String ToCsv()
        {
            StringBuilder sb = new StringBuilder();
            var cols=Columns.ToList();
            var rows=Rows.ToList();

            String[][] items = new String[rows.Count][];
            
            for(int i=0;i<rows.Count;i++)
                items[i]=new String[cols.Count];

            foreach (var item in Items)
            {
                var col = cols.IndexOf(item.Column);
                var row = rows.IndexOf(item.Row);
                items[row][col] = item.Value;
            }

            sb.AppendLine(String.Join(",", "", cols));

            for(int i=0;i<rows.Count;i++)
            {
                sb.AppendLine(String.Join(",",rows[i],items[i]));
            }
            return sb.ToString();
        }
    }

    public class CsvItem
    {
        public String Column { get; set; }
        public String Row { get; set; }
        public String Value { get; set; }
    }
}