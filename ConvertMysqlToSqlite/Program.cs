using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertMysqlToSqlite
{
    public class Program
    {
        static void Main(string[] args)
        {
            var date = DateTime.UtcNow.Ticks;
            date = 635305914062551517;

            var mysqlFile = @"c:\bu\" + date + ".sql";
            var sqliteFile = @"c:\bu\" + date + ".db";

            //Convert.PullDatabase(mysqlFile);
            Convert.ConvertMysqlToSqlite(mysqlFile, sqliteFile);

        }

    }
}
