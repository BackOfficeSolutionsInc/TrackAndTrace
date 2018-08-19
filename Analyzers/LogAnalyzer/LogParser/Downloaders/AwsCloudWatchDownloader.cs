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

namespace LogParser.Downloaders {

	public class Stat {
		
		public static Stat UnHealthyHostCount = new Stat("UnHealthyHostCount", "UnHealthyHostCount", "AWS/ELB", "Maximum",0,5);
		public static Stat HTTPCode_Backend_5XX = new Stat("HTTPCode_Backend_5XX", "HTTPCode_Backend_5XX", "AWS/ELB", "Sum",0);
		public static Stat HTTPCode_Backend_4XX = new Stat("HTTPCode_Backend_4XX", "HTTPCode_Backend_4XX", "AWS/ELB", "Sum",0);
		public static Stat MaximumLatency = new Stat("MaximumLatency", "Latency", "AWS/ELB", "Maximum",0);
		public static Stat TotalRequests = new Stat("TotalRequests", "RequestCount", "AWS/ELB", "Sum",0);
		public static Stat TotalNetworkIn = new Stat("TotalNetworkIn", "NetworkIn", "AWS/EC2", "Sum",0);
		public static Stat TotalNetworkOut = new Stat("TotalNetworkOut", "NetworkOut", "AWS/EC2", "Sum",0);
		public static Stat CpuUtilization = new Stat("CpuUtilization", "CPUUtilization", "AWS/EC2", "Maximum",0,100);

		public static Stat[] AllStats = new[] {
			/*ELB*/ UnHealthyHostCount,HTTPCode_Backend_5XX,HTTPCode_Backend_4XX,MaximumLatency,TotalRequests, 
			/*EC2*/ TotalNetworkIn,TotalNetworkOut,CpuUtilization	
		};
		
		public Stat(string name, string metric, string nameSpace, string statistic, double? min = null, double? max = null) {
			Name = name;
			Metric = metric;
			Namespace = nameSpace;
			Statistic = statistic;
			Min = min;
			Max = max;
			//Unit = unit;
		}
		public string Name { get; set; }
		public string Metric { get; set; }
		public string Namespace { get; set; }
		public string Statistic { get; set; }
		public double? Min { get; set; }
		public double? Max { get; set; }

		public Dimension GetDimension(AwsEnvironment env) {
			switch (Namespace) {
				case "AWS/ELB": return new Dimension() { Name = "LoadBalancerName", Value = env.LoadBalancerName };
				case "AWS/EC2": return new Dimension() { Name = "AutoScalingGroupName", Value = env.AutoScalingGroup };
			}
			throw new Exception("Namespace unhandled:" + Namespace);
		}

		public double? GetValue(Datapoint d) {
			switch (Statistic) {
				case "Average":
					return d.Average;
				case "Sum":
					return d.Sum;
				case "Maximum":
					return d.Maximum;
				case "Minimum":
					return d.Minimum;
				case "SampleCount":
					return d.SampleCount;
			}
			return null;
		}

		public Point ToPoint(Datapoint d) {
			return new Point(d.Timestamp, (decimal?)GetValue(d));
		}
	}

	public class DataChart {
		public string Name { get; set; }
		public List<Point> Datapoints { get; set; }
		public Stat Statistic { get; set; }
	}

	public class AwsCloudWatchDownloader {


		public static DataChart Download(Stat statistic, TimeRange range, AwsEnvironment env) {
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
			return new DataChart() {
				Datapoints = data,
				Name = statistic.Name,
				Statistic =statistic,
			};


		}
	}
}
