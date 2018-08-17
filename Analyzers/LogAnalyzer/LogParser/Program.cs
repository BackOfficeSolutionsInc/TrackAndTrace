using ChartVisualizer;
using LogParser.Downloaders;
using LogParser.Models;
using LogParser.Output;
using ParserUtilities;
using ParserUtilities.Utilities;
using ParserUtilities.Utilities.CacheFile;
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

		public static string Session = "20180816";

		public static void Main(string[] args) {
			Config.SetSession(Session);

			//RunAwsCharts();

			//var end = new DateTime(2018, 8, 16, 8, 06, 0);


			var early = new TimeRange(new DateTime(2018, 8, 16, 6, 26, 0), new DateTime(2018, 8, 16, 6, 34, 0), DateTimeKind.Local);
			var later = new TimeRange(20, new DateTime(2018, 8, 16, 8, 06, 0), DateTimeKind.Local);
			var fivehundred2 = new TimeRange(new DateTime(2018, 8, 16, 7, 53, 0), new DateTime(2018, 8, 16, 7, 59, 0), DateTimeKind.Local);

			var imageChange1 = TimeRange.Around(10, new DateTime(2018, 8, 16, 9, 35, 0), DateTimeKind.Local);
			var imageChange2 = TimeRange.Around(6,  new DateTime(2018, 8, 16, 14, 47, 0), DateTimeKind.Utc);
			var imageChange3 = TimeRange.Around(6, new DateTime(2018, 8, 16, 14, 35, 0), DateTimeKind.Utc);
			var imageChange4 = TimeRange.Around(10, new DateTime(2018, 8, 16, 16, 35, 0), DateTimeKind.Utc);

			RunRemoteLogFile(fivehundred2);

			//RunLocalLogFile(Config.GetFile("combine.txt"));
			//Process.Start(@"C:\Users\Clay\Desktop\Diagnosis\log.xlsx");
			//FlattenFiles(@"C:\Users\Clay\Desktop\Diagnosis\20180728\logs");

		}

		//public static void RunRemoteLogFile(double durationMins, DateTime endTime, DateTimeKind kind) {
		//	RunRemoteLogFile(endTime - TimeSpan.FromMinutes(durationMins), endTime, kind);
		//}

		public static void RunRemoteLogFile(TimeRange range) {
			var logs = AwsLogFileDownloader.DownloadAccessLogs(range);
			SaveFiltered(logs);
		}

		public static void SaveFiltered(LogFile<LogLine> logFile) {
			//logFile.Filter(x => x.csUriStem, "healthcheck");//system requests
			//logFile.Filter(x => x.csUriStem, "healthcheck", "hangfire"); //system requests
			//logFile.Filter(x => x.csUriStem, "/styles/", "signalr", "bundle", "/content/"); //Irrelavent requests
			//logFile.Filter(x => x.csUriStem, "/TileData/", "/Image/TodoCompletion", "/styles/", "/bundle/", "/content/", "/dropdown/"); //Noisy requests
			{
				//Your custom filters here:
				//logFile.FilterExact(x => x.csUriStem, "/People"); //Noisy requests
				//logFile.Filter(x => x.csUriStem, "UpdateAng", "Dashboard","chargeaccount");
				//logFile.Filter(x => x.csUriStem.Contains("hangfire") && x.Duration < TimeSpan.FromSeconds(2));
				//logFile.Filter(x => x.Duration < TimeSpan.FromSeconds(.1));

				logFile.Filter(x => x.csUriStem,"/signalr/"); //Irrelavent requests
				//logFile.FilterRange(1534406069398.ToDateTime(DateTimeKind.Local), 1534406208111.ToDateTime(DateTimeKind.Local));

				//logFile.FilterRange(1534406131031.ToDateTime(DateTimeKind.Local), 1534406154581.ToDateTime(DateTimeKind.Local));
				//logFile.Filter(x => x.csUsername != "robert.cain@treeprosaz.com");

				//logFile.FilterRange(1534405938328.ToDateTime(DateTimeKind.Local), 1534406160521.ToDateTime(DateTimeKind.Local),DateRangeFilterType.CompletelyInLowerRange);
				//logFile.FilterRange(1534406130193.ToDateTime(DateTimeKind.Local), 1534406159601.ToDateTime(DateTimeKind.Local));
				//logFile.FilterRange(1534406101584.ToDateTime(DateTimeKind.Local),1534406234430.ToDateTime(DateTimeKind.Local));
				//logFile.Filter(x => !x.csUsername.Contains("@treeprosaz.com"));
				//logFile.Flag(x => x.AnyFieldContains("7922"));
				//logFile.Filter(x => !x.csUriStem.Contains("1d4254c8-cd20-4099-bb93-a60f011f9538"));
				//9c5061c9-3c77-4fb0-a54c-a93e01114bec
				//logFile.Filter(x => !x.csMethod.Contains("POST"));
			}
			logFile.SetOrdering(x => x.StartTime);
			logFile.SetGrouping(x => x.csUsername);

			logFile.Save(Config.GetBaseDirectory() + "log.txt", " ");
			DurationChart.SaveDurationChart(Config.GetBaseDirectory() + "chart.html", logFile, x => x.csUsername);


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
