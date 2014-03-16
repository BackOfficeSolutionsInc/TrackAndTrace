using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConvertMysqlToSqlite
{
    public class Convert
    {
        public static void ConvertMysqlToSqlite(String mysqlFile,String outputDbFile)
        {
            var sqlFile = ConvertSqlScript(mysqlFile);

            var allLines = File.ReadAllLines(sqlFile).ToList();
            var defs = ExtractDefinitions.ExtractAll(allLines);


            var lines = defs.SelectMany(x => x.Lines);
            //var lines=defs.Where(x=>x.DefinitionType!=DefinitionType.Data).SelectMany(x => x.Lines);
            var scriptFile=sqlFile + ".new.sql";
            File.WriteAllText(scriptFile, String.Join("\n", lines));
            //File.WriteAllText(fileName + ".data.sql", String.Join("\n", defs.Where(x => x.DefinitionType == DefinitionType.Data).SelectMany(x => x.Lines)));
            ExecuteSqlScript(scriptFile, outputDbFile);
        }

        private static String Win2Cygwin(string file)
        {
            return file.ToLower().Replace("c:\\", "/cygdrive/c/").Replace("\\", "/");
        }

        public static string ConvertSqlScript(string mysqlFile)
        {
            Process p = new Process();
            var exDir = Environment.CurrentDirectory;

            var converter = Win2Cygwin(exDir + "\\m2s.sh");
            var mysqlCygFile = Win2Cygwin(mysqlFile);

            /*Console.WriteLine("Please execute this line in cygwin before pressing enter:");
            Console.WriteLine("'" + converter + "' '" + mysqlCygFile + "'");
            Console.ReadLine();*/

            ProcessStartInfo procStartInfo = new ProcessStartInfo(@"C:\cygwin\bin\bash.exe", "'" + converter + "' '" + mysqlCygFile + "'");
            p.StartInfo = procStartInfo;
            p.Start();
            p.WaitForExit();
            return mysqlFile + ".sqlite3.sql";
        }

        public static void ExecuteSqlScript(string sqlScriptFile, string sqlDatabaseFile)
        {
            Process p = new Process();
            var exDir = Environment.CurrentDirectory;

            sqlScriptFile = Win2Cygwin(sqlScriptFile);
            sqlDatabaseFile = Win2Cygwin(sqlDatabaseFile);

            ProcessStartInfo procStartInfo = new ProcessStartInfo(@"C:\cygwin\bin\bash.exe", "cat '" + sqlScriptFile + "' | sqlite3 '" + sqlDatabaseFile + "'");
            p.StartInfo = procStartInfo;
            p.Start();
            p.WaitForExit();
        }

        public static void PullDatabase(string filename)
        {
            ProcessStartInfo start = new ProcessStartInfo();

            start.FileName = @"mysqldump"; // Specify exe name.
            start.Arguments= @"-h radial.cvopqflzmczr.us-west-2.rds.amazonaws.com -P 3306 -u admin -p --compatible=ansi --skip-opt radial";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
                       

            //
            // Start the process.
            //
            using (var sw = new StreamWriter(filename))
            {
                using (Process process = Process.Start(start))
                {
                    //
                    // Read in all the text from the process with the StreamReader.
                    //
                    using (StreamReader reader = process.StandardOutput)
                    {
                        sw.Write(reader.ReadToEnd());
                    }
                    process.WaitForExit();
                }
            }
        }
    }
}
