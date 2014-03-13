using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public class GetTablesMySql
    {
        public static List<Column> AllColumns(string connectionString)
        {
            MySqlConnection conn = null;
            var output = new List<Column>();
            using (conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string stm = "select Table_Name,Column_Name,Data_type,Column_default,Is_Nullable,Column_key,Extra from information_schema.columns where table_schema = 'radial';";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    output.Add(ParseColumn(reader));
                }
            }
            return output;  
        }

        public static Column ParseColumn(MySqlDataReader r)
        {
            return new Column()
            {
                DbType="mysql",
                AutoIncrement = (r["Extra"] as String ?? "").Contains("auto_increment"),
                Primary = (r["Column_key"] as string).Contains("PRI"),
                Default = r["Column_default"] as string,
                IsNull = (r["Is_Nullable"] as string).Contains("YES"),
                Name = r["Column_Name"] as string,
                TableName = r["Table_Name"] as string,
                ColumnType = MySql_Connection.ParseColumnDataType(r["Data_type"] as string),
            };
        }


        
    }

}
