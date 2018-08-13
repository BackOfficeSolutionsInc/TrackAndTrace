using ChartVisualizer;
using LogParser.Downloaders;
using LogParser.Models;
using ParserUtilities.Utilities;
using ParserUtilities.Utilities.LogFile;
using ParserUtilities.Utilities.OutputFile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser {
	public class Program {

		public static string Session = "20180728";

		public static void Main(string[] args) {
			Config.SetSession("20180728");

			//RunAwsCharts();

			var end = new DateTime(2018, 7, 28,17,20,0);


			RunLocalLogFile(Config.GetFile("combine.txt"));
			//RunRemoteLogFile(40, end, DateTimeKind.Local);

			//int a = 0;
			//Process.Start(@"C:\Users\Clay\Desktop\Diagnosis\log.xlsx");

			//FlattenFiles(@"C:\Users\Clay\Desktop\Diagnosis\20180728\logs");

		}

		public static void RunRemoteLogFile(double durationMins, DateTime endTime, DateTimeKind kind) {
			RunRemoteLogFile(TimeSpan.FromMinutes(durationMins), endTime, kind);
		}

		public static void RunRemoteLogFile(TimeSpan duration, DateTime endTime, DateTimeKind kind) {
			var logs = AwsLogFileDownloader.DownloadAccessLogs(duration, endTime, kind);
			SaveFiltered(logs);
		}

		public static void SaveFiltered(LogFile logFile) {
			logFile.Filter(x => x.csUriStem, "healthcheck");//system requests
			//logFile.Filter(x => x.csUriStem, "healthcheck", "hangfire"); //system requests
			//logFile.Filter(x => x.csUriStem, "/styles/", "signalr", "bundle", "/content/"); //Irrelavent requests
			//logFile.Filter(x => x.csUriStem, "/TileData/", "/Image/TodoCompletion", "/styles/", "/bundle/", "/content/", "/dropdown/"); //Noisy requests
			{
				//Your custom filters here:
				//logFile.FilterExact(x => x.csUriStem, "/People"); //Noisy requests
				//logFile.Filter(x => x.csUriStem, "UpdateAng", "Dashboard","chargeaccount");
				//logFile.Filter(x => x.csUriStem.Contains("hangfire") && x.Duration < TimeSpan.FromSeconds(2));
				//logFile.Filter(x => x.Duration < TimeSpan.FromSeconds(.1));
				
				logFile.FilterRelativeRange(5.2*60,8.5*60);
			}
			logFile.SetOrdering(x => x.StartTime);
			logFile.Save(Config.GetBaseDirectory() + "log.txt"," ");
		}


		public static void RunLocalLogFile(string path) {
			var logFile = LogFileReader.Read<LogLine>(path);
			SaveFiltered(logFile);
		}

		public static void RunAwsCharts() {
			//var url = "https://us-west-2.console.aws.amazon.com/elasticbeanstalk/service/cloudwatch/stats/2018-07-13T01:37:08.751Z/2018-07-13T05:26:38.264Z?dimension.LoadBalancerName=awseb-e-d-AWSEBLoa-N797H3PA6H8&metricName=RequestCount&namespace=AWS%2FELB&period=300&region=us-west-2&statistics=Sum";
			var env = AwsConstants.Environments.Env4;
			//var url = AwsConstants.Charts.AverageLatency.ToUrl(env,DateTime.UtcNow.AddDays(-1),DateTime.UtcNow);

			//var cs = CookieReader.ReadCookies(url);
			//foreach (var c in cs) {
			//	Log.Info(c.Name+"~"+c.Value+"~"+c.Domain+"~"+c.Expires);
			//}

			//// AwsChartDownloader.GetJSession();
			//var res = AwsChartDownloader.ExecuteGetRequest(url);

			////var r = CookieReader.CreateRequest("https://us-west-2.console.aws.amazon.com/elasticbeanstalk/home?region=us-west-2#/environment/dashboard?applicationName=app-tractiontools&environmentId=e-de9akqpmac");


			var dur = TimeSpan.FromDays(2);

			var charts = new[] {
				//AwsConstants.Charts.AverageLatency,
				AwsConstants.Charts.TotalRequests,
				//AwsConstants.Charts.TotalNetworkIn,
				//AwsConstants.Charts.TotalNetworkOut,
			};

			var series = new List<XYSeries>();

			foreach (var c in charts)
				series.Add(AwsChartDownloader.DownloadSeries(c, env, dur));


			var chart = new Chart() {
				Series = series
			};



			int a = 0;
		}

		public static void FlattenFiles(string dir) {
			var files = Directory.GetFiles(dir);
			FileUtility.CombineMultipleFiles(files, dir + "\\..\\combine.txt");
		}
	}
}
