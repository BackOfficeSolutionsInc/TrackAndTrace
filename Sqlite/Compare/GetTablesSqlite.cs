using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public class GetTablesSqLite
    {
        public static List<Column> AllColumns(string connectionString)
        {
            var output = new List<Column>();
            foreach (var table in GetAllTables(connectionString))
            {
                output.AddRange(GetColumns(connectionString, table));
            }
            return output;
        }

        public static List<Column> GetColumns(string connectionString, String tablename)
        {
            var output = new List<Column>();
            var autoColumnsTable = new List<String>();
            using (SQLiteConnection connect = new SQLiteConnection(connectionString))
            {
                connect.Open();
                using (SQLiteCommand fmd = connect.CreateCommand())
                {
                    fmd.CommandText = "Pragma table_info(" + tablename + ")";
                    fmd.CommandType = CommandType.Text;
                    SQLiteDataReader r = fmd.ExecuteReader();
                    while (r.Read())
                    {
                        try
                        {
                            output.Add(ParseColumn(r, tablename));
                        }
                        catch (IndexOutOfRangeException)
                        {
                        }
                    }
                }

                using (SQLiteCommand fmd = connect.CreateCommand())
                {
                    fmd.CommandText = @"SELECT tbl_name FROM sqlite_master WHERE sql LIKE ""%AUTOINCREMENT%""";
                    fmd.CommandType = CommandType.Text;
                    SQLiteDataReader r = fmd.ExecuteReader();

                    while (r.Read())
                    {
                        autoColumnsTable.Add(r["tbl_name"] as String);
                    }
                }

            }

            foreach (var act in autoColumnsTable)
            {
                foreach (var o in output)
                {
                    if (o.TableName == act && o.Primary)
                    {
                        o.AutoIncrement = true;
                    }
                }
            }


            return output;

        }

        public static Column ParseColumn(SQLiteDataReader record, string tableName)
        {
            if (tableName == "sqlite_sequence")
                throw new IndexOutOfRangeException();

            return new Column()
            {
                DbType = "sqlite",
                TableName = tableName,
                Default = record["dflt_value"] as string,
                Primary = (record["pk"] as long? ?? 0) == 1,
                IsNull = (record["notnull"] as long? ?? 0) == 0,
                Name = record["name"] as string,
                ColumnType = SqliteConnection.ParseColumnDataType(record["type"] as string)
            };
        }



        public static List<String> GetAllTables(string connectionString)
        {
            var output = new List<string>();
            using (SQLiteConnection connect = new SQLiteConnection(connectionString))
            {
                connect.Open();
                using (SQLiteCommand fmd = connect.CreateCommand())
                {
                    fmd.CommandText = @"SELECT name FROM sqlite_master WHERE type = ""table""";
                    fmd.CommandType = CommandType.Text;
                    SQLiteDataReader r = fmd.ExecuteReader();
                    while (r.Read())
                    {
                        output.Add(GetName(r));

                    }
                }
            }

            return output;
        }

        private static String GetName(SQLiteDataReader record)
        {
            return (string)record["name"];
        }

    }
}
