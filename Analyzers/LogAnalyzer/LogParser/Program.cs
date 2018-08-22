﻿using ChartVisualizer;
using LogParser.Downloaders;
using LogParser.Models;
using LogParser.Output;
using ParserUtilities;
using ParserUtilities.Utilities;
using ParserUtilities.Utilities.CacheFile;
using ParserUtilities.Utilities.Colors;
using ParserUtilities.Utilities.DataTypes;
using ParserUtilities.Utilities.LogFile;
using ParserUtilities.Utilities.OutputFile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LogParser.Downloaders.AwsChartDownloader;
using static LogParser.Downloaders.AwsCloudWatchDownloader;

namespace LogParser {
	public class Program {

		public static string Session = "20180816";
        public static AwsEnvironment CurrentEnv = AwsConstants.Environments.Env3;

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

			var o20180817 = new TimeRange(new DateTime(2018, 8, 17, 7, 26, 0), new DateTime(2018, 8, 17, 8, 26, 0), DateTimeKind.Local);
			var o20180817_2132 = new TimeRange(new DateTime(2018, 8, 17, 21, 12, 0), new DateTime(2018, 8, 17, 22, 37, 0), DateTimeKind.Local);
			var o20180818_1700 = new TimeRange(new DateTime(2018, 8, 18, 17, 00, 0), new DateTime(2018, 8, 18, 17, 16, 0), DateTimeKind.Local);


			var o20180816_0037 = new TimeRange(new DateTime(2018, 8, 16, 0, 37, 0), new DateTime(2018, 8, 16, 1, 32, 0), DateTimeKind.Local);
            var o20180816_0627 = TimeRange.Around(30, new DateTime(2018, 8, 16, 6, 27, 0), DateTimeKind.Local);

            //Unhealthy server
            var o20180817_0914 = new TimeRange(new DateTime(2018, 8, 17, 9, 0, 0), new DateTime(2018, 8, 17, 9, 40, 0), DateTimeKind.Local);

            // var today = TimeRange.Around(30, new DateTime(2018, 8, 20, 7, 29, 0), DateTimeKind.Local);
           // var today = new TimeRange(new DateTime(2018, 8, 21, 7, 53, 0), new DateTime(2018, 8, 21, 8, 10, 0), DateTimeKind.Local);
            var today = new TimeRange(new DateTime(2018, 8, 21, 16, 18, 0), new DateTime(2018, 8, 21, 17, 17, 0), DateTimeKind.Local);
            /*HEY.. Checky our Env matches*/
            RunRemoteLogFile(today, true);

			//RunLocalLogFile(Config.GetFile("combine.txt"));
			//Process.Start(@"C:\Users\Clay\Desktop\Diagnosis\log.xlsx");
			//FlattenFiles(@"C:\Users\Clay\Desktop\Diagnosis\20180728\logs");

		}

		//public static void RunRemoteLogFile(double durationMins, DateTime endTime, DateTimeKind kind) {
		//	RunRemoteLogFile(endTime - TimeSpan.FromMinutes(durationMins), endTime, kind);
		//}

		public static void RunRemoteLogFile(TimeRange range,bool downloadCharts) {
			var logs = AwsLogFileDownloader.DownloadAccessLogs(range);
			var charts = new DataChartModel[0];
            if (downloadCharts) {
                charts = Stats.AllStats.Select(x => AwsCloudWatchDownloader.Download(x, range, CurrentEnv)).ToArray();
                MetricChart.SaveOverlay(Config.GetBaseDirectory() + "metrics.html", charts);
            }
			SaveFiltered(logs, charts);
		}

		public static void SaveFiltered(LogFile<LogLine> logFile,IEnumerable<DataChartModel> charts) {
			logFile.Filter(line => line.csUriStem, "healthcheck");//system requests
			//logFile.Filter(x => x.csUriStem, "healthcheck", "hangfire"); //system requests
			logFile.Filter(line => line.csUriStem, "/styles/", "signalr", "bundle", "/content/"); //Irrelavent requests
            //	logFile.Filter(x => x.csUriStem, "/TileData/", "/Image/TodoCompletion", "/styles/", "/bundle/", "/content/", "/dropdown/"); //Noisy requests
            #region comments
            {
				//Your custom filters here:
				//logFile.FilterExact(x => x.csUriStem, "/People"); //Noisy requests
				//logFile.Filter(x => x.csUriStem, "UpdateAng", "Dashboard","chargeaccount");
				//logFile.Filter(x => x.csUriStem.Contains("hangfire") && x.Duration < TimeSpan.FromSeconds(2));
				//logFile.Filter(x => x.Duration < TimeSpan.FromSeconds(.1));

				//logFile.Filter(x => x.csUriStem,"/signalr/"); //Irrelavent requests
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
            #endregion

            //logFile.FilterRange(1534497199656.ToDateTime(DateTimeKind.Local), 1534497359273.ToDateTime(DateTimeKind.Local));
            //logFile.SetOrdering(x => -x.Duration);
            logFile.SetGrouping(x => x.StatusCode);
            //logFile.SetGrouping(x => x.csUsername);
            logFile.Filter(line => line.csUriStem, "TileData");
           //logFile.Filter(x => x.csUsername != "-");

			logFile.AutoFlag();
			logFile.FlagsAtTop();
            //logFile.RemoveInitialSignalR();

            //logFile.FilterRange(1534400864571.ToDateTime(DateTimeKind.Local), 1534401036887.ToDateTime(DateTimeKind.Local));

            logFile.Flag(x => x.StatusCode == 409);

            //logFile.Filter(x => !x.csUriStem.Contains("healthcheck"));
            //logFile.Filter(x => !x.csUserAgent.Contains("ELB-HealthChecker/1.0"));

            logFile.SetOrdering(x => x.StartTime);

            //logFile.SetGrouping(x => x.sIp);
            //logFile.SetGrouping(x => x.csUsername);

            //logFile.Filter(x => x.StatusCode < 400);
            //logFile.Filter(x => x.StatusCode ==409);
            //logFile.FilterRange(1534401062253.ToDateTime(DateTimeKind.Local), 1534401099718.ToDateTime(DateTimeKind.Local));
            // logFile.FilterRange(1534400938421.ToDateTime(DateTimeKind.Local), 1534401021502.ToDateTime(DateTimeKind.Local),10);
            //logFile.RemoveCachedPages();
            logFile.Flag(x => x.Guid == "73aff0657e2f5442b262c0064d1fda3558f13b4b324648c681f280036000eb43", FlagType.ByGuid);
            //Add Measurable error (o20180816_0627)
            logFile.Flag(x => x.Guid == "ef22e42204fabb5013cb05ac9a865039ca762a28b34ba6f15a40173beb8f0306", FlagType.Fixed);
            logFile.Flag(x => x.Guid == "f62543ff758f7f18a52286b6e3ea8188ba8c023b3ba3290a5390166c5ea94a9e", FlagType.Fixed);
            logFile.Flag(x => x.Guid == "70d77689dd7593d7826ddea423b598fe03c1aa34096637d15b72468f987a70ce", FlagType.Fixed);
            logFile.Flag(x => x.Guid == "8cf7c9b8641a8afff477f8f7d644212789690438ef9d0fe9e2b829f63fe25dcc", FlagType.Fixed);
            logFile.Flag(x => x.Guid == "f6eb024e1c1d7d8d7b3e408e93f42dd6fd94ac5e9ddeaca1631b29fff9ef9a67", FlagType.Fixed);
            logFile.Flag(x => x.Guid == "d15dfdaf1c872c5cbe75506c1020ba531594f2835603110a5d3e838afd7efc91", FlagType.Fixed);
            //logFile.FilterRange(1534838488440.ToDateTime(DateTimeKind.Local), 1534838585699.ToDateTime(DateTimeKind.Local),2);
            //logFile.FilterRange(1534497171457.ToDateTime(DateTimeKind.Local), 1534497541126.ToDateTime(DateTimeKind.Local));
            //logFile.Filter(x => x.StatusCode < 500);
            //logFile.FilterRange(1534400819745.ToDateTime(DateTimeKind.Local), 1534401062766.ToDateTime(DateTimeKind.Local));
            //logFile.FilterRange(1534401458448.ToDateTime(DateTimeKind.Local), 1534401556914.ToDateTime(DateTimeKind.Local));
            //logFile.FilterRange(1534401062028.ToDateTime(DateTimeKind.Local), 1534401071307.ToDateTime(DateTimeKind.Local),3);
            //logFile.FilterRange(1534401643662.ToDateTime(DateTimeKind.Local), 1534401695374.ToDateTime(DateTimeKind.Local));
            logFile.Save(Config.GetBaseDirectory() + "log.txt", " ");



			DurationChart.SaveDurationChart(Config.GetBaseDirectory() + "chart.html", logFile, x => x.sIp, Pallets.Stratified, charts);
		}


		public static void RunLocalLogFile(string path) {
			var logFile = LogFileReader.Read<LogLine>(path);
			SaveFiltered(logFile,null);
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
