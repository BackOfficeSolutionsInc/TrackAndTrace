using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public class DbAccess
    {
        public static List<Column> GetColumns(IConnection connection)
        {
            if (connection is MySql_Connection)
                return GetTablesMySql.AllColumns(connection.GetConnectionString());
            else if (connection is SqliteConnection)
                return GetTablesSqLite.AllColumns(connection.GetConnectionString());
            throw new Exception();
        }

    }
}
