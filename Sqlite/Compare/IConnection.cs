using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public interface IConnection
    {
        String GetDbType();
        String GetConnectionString();
        String AddTableSyntax(String table, params Column[] columns);
        String AddColumnSyntax(String table, Column column);
        String AlterColumnSyntax(String table, Column column);
        string ConvertType(ColumnType type);
        ColumnType ParseColumnType(String type);
    }
}
