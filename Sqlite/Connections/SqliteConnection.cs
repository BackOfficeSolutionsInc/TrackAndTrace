using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public class SqliteConnection : IConnection
    {
        public String ConnectionString { get; set; }
        public SqliteConnection(String connectionString)
        {
            ConnectionString = connectionString;
        }
        public string GetDbType()
        {
            return "sqlite";
        }

        public string GetConnectionString()
        {
            return ConnectionString;
        }
        public string AddTableSyntax(string table, params Column[] columns)
        {
            throw new NotImplementedException();
        }

        public string AddColumnSyntax(string table, Column column)
        {
            throw new NotImplementedException();
        }

        public string AlterColumnSyntax(string table, Column column)
        {
            throw new NotImplementedException();
        }

        public string ConvertType(ColumnType type)
        {
            switch (type)
            {
                case  ColumnType.tinyint: return "BOOL";
                case  ColumnType.bigint:  return "BIGINT";
                case  ColumnType.datetime:return "DATETIME";
                case ColumnType.varchar: return "TEXT";
                case ColumnType.@decimal: return "NUMERIC";
                case ColumnType.@int: return "INT";
                case ColumnType.text: return "TEXT";
                case ColumnType.@double: return "DOUBLE";
                default: throw new Exception(type.ToString());
            }
        }

        public static ColumnType ParseColumnDataType(string type)
        {
            switch (type)
            {
                case "BOOL": return ColumnType.tinyint;
                case "BIGINT": return ColumnType.bigint;
                case "DATETIME": return ColumnType.datetime;
                case "TEXT": return ColumnType.varchar;
                case "integer": return ColumnType.bigint;
                case "INT": return ColumnType.bigint;
                case "NUMERIC": return ColumnType.@decimal;
                case "UNIQUEIDENTIFIER": return ColumnType.intQ;
                case "DOUBLE": return ColumnType.@double;
                default: throw new Exception(type);
            }
        }

        public ColumnType ParseColumnType(string type)
        {
            return SqliteConnection.ParseColumnDataType(type);
        }
    }
}
