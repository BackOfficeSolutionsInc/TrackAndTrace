using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    class Program
    {
        static void Main(string[] args)
        {
            var sqliteConnectionString = @"Data Source=C:\Users\Clay\Documents\Databases\RadialReview.db;Version=3;";
            var mysqlConnectionString = @"Server=radial.cvopqflzmczr.us-west-2.rds.amazonaws.com;Port=3306;Database=radial;Uid=admin;Pwd=Svf9tNSGLC;";

            var past = new MySql_Connection(mysqlConnectionString);
            var present = new SqliteConnection(sqliteConnectionString);

            var comparison = CompaireDbs.Compair(past, present,false)
                                        .OrderBy(x => x.ChangeType == ChangeType.Warning)
                                        .ThenBy(x => x.Column.TableName)
                                        .ThenBy(x => x.ObjectType)
                                        .ThenBy(x => x.ChangeType);

            File.WriteAllLines(@"C:\Users\Clay\Documents\Databases\Gen\allColumns.csv", comparison.Select(x=>x.CSV()));

            var commands = CompaireDbs.SuggestUpdateCommands(past, comparison.ToList());
            File.WriteAllLines(@"C:\Users\Clay\Documents\Databases\Gen\Commands\commands_" + DateTime.UtcNow.Ticks + ".txt", commands);

        }
    }
}
