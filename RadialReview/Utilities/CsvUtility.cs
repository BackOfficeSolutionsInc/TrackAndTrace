using RadialReview.Utilities.DataTypes;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Web;
using System.Text.RegularExpressions;

namespace RadialReview.Utilities {
    public class CsvUtility {
        public static List<List<String>> Load(Stream stream, bool trim = true) {
            stream.Seek(0, SeekOrigin.Begin);
            TextReader tr = new StreamReader(stream);
            //var csv = new CsvReader(tr);
            var csvData = new List<List<string>>();
            var lowest = 0;
            int fieldCount;
            using (var csv = new LumenWorks.Framework.IO.Csv.CsvReader(tr, false)) {
                fieldCount = csv.FieldCount;
                while (csv.ReadNextRecord()) {
                    var row = new List<String>();
                    for (int i = 0; i < fieldCount; i++) {
                        row.Add(csv[i]);
                        if (!string.IsNullOrWhiteSpace(csv[i]))
                            lowest = Math.Max(lowest, i);
                    }
                    csvData.Add(row);
                }
            }
            if (trim) {
                for (var i = csvData.Count - 1; i >= 0; i--) {
                    if (csvData[i].All(x => string.IsNullOrWhiteSpace(x)))
                        csvData.RemoveAt(i);
                    else
                        break;
                }
                if (lowest < fieldCount) {
                    for (var i = 0; i < csvData.Count; i++) {
                        var row = csvData[i].Where((x, k) => k <= lowest).ToList();
                        csvData[i] = row;
                    }
                }
            }

            return csvData;
        }

        public static SLDocument ToXls(params Csv[] file) {
            return ToXls(file.ToList());
        }


        public static SLDocument ToXls(List<Csv> files) {
            SLDocument sl = new SLDocument();
            var first = true;
            var existingNames = new DefaultDictionary<string, int>(x => 0);
            string firstName = null;
            foreach (var csv in files) {
                var sheetname = csv.Title ?? "Sheet ";
                var dup = existingNames[sheetname] + 1;
                existingNames[sheetname] = dup;
                if (dup > 1) {
                    sheetname += dup;
                }

                //Naming
                if (first) {
                    firstName = sheetname;
                    sl.RenameWorksheet(SLDocument.DefaultFirstSheetName, sheetname);
                } else {
                    sl.AddWorksheet(sheetname);
                }


                //Copy data
                for (var j = 0; j < csv.Columns.Count; j++) {
                    SetCell(sl, csv.Columns[j], 0, j + 1);
                }
                for (var i = 0; i < csv.Rows.Count; i++) {
                    SetCell(sl, csv.Rows[i], i + 1, 0);
                    for (var j = 0; j < csv.Columns.Count; j++) {
                        var cell = csv.Get(i, j);
                        SetCell(sl, cell, i+1, j+1);
                    }
                }

                first = false;
            }
            if (!first && firstName != null) {
                sl.SelectWorksheet(firstName);
            }

            return sl;
        }

        private static Regex LongTester = new Regex(@"^\d+$");

        /// <summary>
        /// Takes zero indexed rows and columns, automatically converts cell to long if possible
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cell"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        private static void SetCell(SLDocument document, string cell, int row, int col) {

            if (cell == null) {
                return;
            }
            try {
                if (LongTester.IsMatch(cell)) {
                    document.SetCellValue(row + 1, col + 1, cell.ToLong());
                    return;
                }
            } catch (Exception) {
                //Eat it.
            }

            //default is to use string.
            document.SetCellValue(row + 1, col + 1, cell);
            return;
        }
    }
}