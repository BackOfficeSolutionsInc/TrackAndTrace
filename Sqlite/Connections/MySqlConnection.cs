using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public class MySql_Connection : IConnection
    {
        public String ConnectionString { get; set; }
        public MySql_Connection(String connectionString)
        {
            ConnectionString = connectionString;
        }
        public string GetDbType()
        {
            return "mysql";
        }

        public string GetConnectionString()
        {
            return ConnectionString;
        }

        public string AddTableSyntax(String tableName, params Column[] columns)
        {
            var columnsText=String.Join(",",columns.Select(x=>ColumnSyntax(x)));
            var primary=columns.FirstOrDefault(x => x.Primary);

            var query = "CREATE TABLE " + tableName + " (" + columnsText;

            if (primary!=null)
                query+=", PRIMARY KEY ("+primary.Name+")";
            query+= ");";
            return query;
        }

        public String ColumnSyntax(Column x)
        {
            var text = x.Name + " " + ConvertType(x.ColumnType);
            if (x.IsNull)
                text += " NULL";
            else
                text += " NOT NULL";

            if (x.AutoIncrement)
                text += " AUTO_INCREMENT";
            text += " DEFAULT "+(String.IsNullOrWhiteSpace(x.Default)?"NULL":x.Default);
            return text;
        }

        public string AddColumnSyntax(string table, Column column)
        {
            return "ALTER TABLE "+table+" ADD "+ColumnSyntax(column)+";";
        }


        public string AlterColumnSyntax(string table, Column column)
        {
            return "ALTER TABLE "+table+" MODIFY "+column.Name+" "+ConvertType(column.ColumnType)+";";
        }

        public string ConvertType(ColumnType type)
        {
            switch (type)
            {
                case ColumnType.bigint: return "bigint";
                case ColumnType.tinyint: return "tinyint";
                case ColumnType.datetime: return "datetime";
                case ColumnType.@decimal: return "decimal";
                case ColumnType.@int: return "int";
                case ColumnType.text: return "text";
                case ColumnType.varchar: return "varchar(65000)";
                case ColumnType.intQ: return "bigint";
                case ColumnType.@double: return "double";
                default: throw new Exception(type.ToString());
            }
        }

        public static ColumnType ParseColumnDataType(string type)
        {
            switch (type)
            {
                case "tinyint": return ColumnType.tinyint;
                case "bigint": return ColumnType.bigint;
                case "datetime": return ColumnType.datetime;
                case "int": return ColumnType.@int;
                case "text": return ColumnType.text;
                case "varchar": return ColumnType.varchar;
                case "decimal": return ColumnType.@decimal;
                case "double": return ColumnType.@double;
                default: throw new Exception(type);
            }
        }

        public ColumnType ParseColumnType(string type)
        {
            return MySql_Connection.ParseColumnDataType(type);
        }
    }
}
