using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using LogParser.Models;
using static LogParser.Downloaders.AwsChartDownloader;
using ParserUtilities.Utilities.CacheFile;
using System.IO;
using Newtonsoft.Json;
using Amazon;
using ParserUtilities.Utilities.DataTypes;

namespace LogParser.Downloaders {

	public class Stats {
		
		public static Stat UnHealthyHostCount = new Stat("UnHealthyHostCount", "UnHealthyHostCount", "AWS/ELB", "Maximum",0,5);
		public static Stat HTTPCode_Backend_5XX = new Stat("HTTPCode_Backend_5XX", "HTTPCode_Backend_5XX", "AWS/ELB", "Sum",0);
		public static Stat HTTPCode_Backend_4XX = new Stat("HTTPCode_Backend_4XX", "HTTPCode_Backend_4XX", "AWS/ELB", "Sum",0);
		public static Stat MaximumLatency = new Stat("MaximumLatency", "Latency", "AWS/ELB", "Maximum",0);
		public static Stat TotalRequests = new Stat("TotalRequests", "RequestCount", "AWS/ELB", "Sum",0);
		public static Stat TotalNetworkIn = new Stat("TotalNetworkIn", "NetworkIn", "AWS/EC2", "Sum",0);
		public static Stat TotalNetworkOut = new Stat("TotalNetworkOut", "NetworkOut", "AWS/EC2", "Sum",0);
        public static Stat CpuUtilization = new Stat("CpuUtilization", "CPUUtilization", "AWS/EC2", "Maximum", 0, 100);
        public static Stat RdsCpuUtilization = new Stat("RdsCpuUtilization", "CPUUtilization", "AWS/RDS", "Maximum", 0, 100);
        public static Stat DatabaseConnections = new Stat("DatabaseConnections", "DatabaseConnections", "AWS/RDS", "Maximum", 0);
        public static Stat ReadIOPS = new Stat("ReadIOPS", "ReadIOPS", "AWS/RDS", "Maximum", 0);
        public static Stat WriteIOPS = new Stat("WriteIOPS", "WriteIOPS", "AWS/RDS", "Maximum", 0);

        

        public static Stat[] AllStats = new[] {
			/*ELB*/ UnHealthyHostCount,HTTPCode_Backend_5XX,HTTPCode_Backend_4XX,MaximumLatency,TotalRequests, 
			/*EC2*/ TotalNetworkIn,TotalNetworkOut,CpuUtilization,
            /*RDS*/ RdsCpuUtilization,DatabaseConnections,ReadIOPS,WriteIOPS
        };		
	}


	public class AwsCloudWatchDownloader {


		public static DataChartModel Download(Stat statistic, TimeRange range, AwsEnvironment env) {
			var filename = FileCache.GetOrAddCachedFile(Config.GetCacheDirectory(), "charts/"+statistic.Name+"_"+range.ToString() + "_" + env.ToString(), outFile => {

				var dim = new Dimension() { };

				using (var cli = new AmazonCloudWatchClient(RegionEndpoint.USWest2)) {
					var resp = cli.GetMetricStatistics(new GetMetricStatisticsRequest() {
						StartTime = range.Start.ToUniversalTime().AddSeconds(-120),
						EndTime = range.End.ToUniversalTime().AddSeconds(120),
						Period = 60,
						Dimensions = new List<Dimension>() { statistic.GetDimension(env) },
						MetricName = statistic.Metric,
						Namespace = statistic.Namespace,
						//Unit = statistic.Unit,
						Statistics = new List<string> { statistic.Statistic }

					});

					resp.Datapoints.ForEach(x => x.Timestamp = DateTime.SpecifyKind(x.Timestamp, DateTimeKind.Utc));

					File.WriteAllText(outFile, JsonConvert.SerializeObject(resp.Datapoints.Select(x=>statistic.ToPoint(x))));					
				}
			});

			var contents = File.ReadAllText(filename);
			var data= JsonConvert.DeserializeObject<List<Point>>(contents);
			return new DataChartModel() {
				Datapoints = data,
				Name = statistic.Name,
				Statistic =statistic,
			};


		}
	}
}
