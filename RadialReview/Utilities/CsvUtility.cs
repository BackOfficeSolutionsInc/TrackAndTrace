using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class CsvUtility
    {
        public static List<List<String>> Load(Stream stream,bool trim=true)
        {
            stream.Seek(0, SeekOrigin.Begin);
            TextReader tr = new StreamReader(stream);
            //var csv = new CsvReader(tr);
            var csvData = new List<List<string>>();
            var lowest = 0;
            int fieldCount;
            using (var csv = new LumenWorks.Framework.IO.Csv.CsvReader(tr, false))
            {
                fieldCount = csv.FieldCount;
                while (csv.ReadNextRecord())
                {
                    var row = new List<String>();
                    for (int i = 0; i < fieldCount; i++)
                    {
                        row.Add(csv[i]);
                        if (!string.IsNullOrWhiteSpace(csv[i]))
                            lowest = Math.Max(lowest, i);
                    }
                    csvData.Add(row);
                }
            }
            if (trim)
            {
                for (var i = csvData.Count - 1; i >= 0; i--)
                {
                    if (csvData[i].All(x => string.IsNullOrWhiteSpace(x)))
                        csvData.RemoveAt(i);
                    else
                        break;
                }
                if (lowest < fieldCount)
                {
                    for (var i=0;i<csvData.Count;i++)
                    {
                        var row = csvData[i].Where((x,k)=> k<=lowest).ToList();
                        csvData[i] = row;
                    }
                }
            }

            return csvData;
        }


    }

}