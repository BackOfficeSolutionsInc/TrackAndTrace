using Newtonsoft.Json.Linq;
using ParserUtilities;
using ParserUtilities.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static LogParser.Downloaders.AwsChartDownloader;

namespace LogParser.Downloaders {

	[DebuggerDisplay("({X}, {Y})")]
	[Serializable]
	public class Point  {
		/// <summary>
		/// Use utc time
		/// </summary>
		public DateTime X { get; set; }
		public decimal? Y { get; set; }

		public Point() {
		}

		public Point(DateTime x, decimal? y) {
			X = x;
			Y = y;
		}
		public Point(long utc, decimal? y) : this(utc.ToDateTime(), y) {
		}
		
	}
	[Serializable]
	public class XYSeries {
		public string Name { get; set; }
		public List<Point> Points { get; set; }
	}

	[Serializable]
	public class Chart {
		public string Name { get; set; }
		public List<XYSeries> Series { get; set; }
	}

	public class AwsConstants {
		public class Environments {
			public static AwsChartDownloader.AwsEnvironment Env3 = new AwsChartDownloader.AwsEnvironment("app-tractiontools-env-3", "awseb-e-j-AWSEBLoa-1SFZNUPGVL28D", "us-west-2", "awseb-e-jpqjr2mgak-stack-AWSEBAutoScalingGroup-MLHNOA0L8LN2");
			public static AwsChartDownloader.AwsEnvironment Env4 = new AwsChartDownloader.AwsEnvironment("app-tractiontools-env-4", "awseb-e-d-AWSEBLoa-N797H3PA6H8", "us-west-2", "awseb-e-de9akqpmac-stack-AWSEBAutoScalingGroup-J8AUCDX80SAK");
		}

		public class Charts {
			public static ChartDesc AverageLatency = new ChartDesc("AverageLatency", ChartDesc.MetricDimension.LoadBalancer, "Latency", "AWS%2FELB", "Average");  /* https://us-west-2.console.aws.amazon.com/elasticbeanstalk/service/cloudwatch/stats/2018-07-10T06:52:00.735Z/2018-07-13T06:52:00.735Z?dimension.LoadBalancerName=awseb-e-d-AWSEBLoa-N797H3PA6H8&metricName=Latency&namespace=AWS%2FELB&period=300&region=us-west-2&statistics=Average */
			public static ChartDesc TotalRequests = new ChartDesc("TotalRequests", ChartDesc.MetricDimension.LoadBalancer, "RequestCount", "AWS%2FELB", "Sum");        /* https://us-west-2.console.aws.amazon.com/elasticbeanstalk/service/cloudwatch/stats/2018-07-10T06:53:43.404Z/2018-07-13T06:53:43.404Z?dimension.LoadBalancerName=awseb-e-d-AWSEBLoa-N797H3PA6H8&metricName=RequestCount&namespace=AWS%2FELB&period=300&region=us-west-2&statistics=Sum */
			public static ChartDesc TotalNetworkIn = new ChartDesc("TotalNetworkIn", ChartDesc.MetricDimension.AutoScalingGroup, "NetworkIn", "AWS%2FELB", "Sum");     /* https://us-west-2.console.aws.amazon.com/elasticbeanstalk/service/cloudwatch/stats/2018-07-06T20:17:15.053Z/2018-07-13T08:55:53.392Z?dimension.AutoScalingGroupName=awseb-e-jpqjr2mgak-stack-AWSEBAutoScalingGroup-MLHNOA0L8LN2&metricName=NetworkIn&namespace=AWS%2FEC2&period=900&region=us-west-2&statistics=Sum */
			public static ChartDesc TotalNetworkOut = new ChartDesc("TotalNetworkOut", ChartDesc.MetricDimension.AutoScalingGroup, "NetworkOut", "AWS%2FELB", "Sum");  /* https://us-west-2.console.aws.amazon.com/elasticbeanstalk/service/cloudwatch/stats/2018-07-06T20:17:15.053Z/2018-07-13T08:55:53.392Z?dimension.AutoScalingGroupName=awseb-e-jpqjr2mgak-stack-AWSEBAutoScalingGroup-MLHNOA0L8LN2&metricName=NetworkIn&namespace=AWS%2FEC2&period=900&region=us-west-2&statistics=Sum */
		}
	}


	public class AwsChartDownloader {


		public static void GetJSession() {
			var url = "https://resources.console.aws.amazon.com/r/auth?state=hashArgs";

			var res = CookieReader.ExecuteGetRequest(url, r => {
				r.Accept = "*/*";
				r.Host = new Uri(url).Host;
				r.Headers.Add("Accept-Encoding: gzip, deflate, br");
				r.Headers.Add("Accept-Language: en-US,en;q=0.9,en-GB;q=0.8");
				r.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
				r.Referer = "https://us-west-2.console.aws.amazon.com/elasticbeanstalk/home?region=us-west-2";
			});
			int a = 0;
		}

		//public static string ExecuteJsonGetRequest(string url) {
		//	return CookieReader.ExecuteGetRequest(url, r => {
		//		r.Accept = "application/json, text/plain, */*";
		//		r.Host = new Uri(url).Host;
		//		r.Headers.Add("Accept-Encoding: gzip, deflate, br");
		//		r.Headers.Add("Accept-Language: en-US,en;q=0.9,en-GB;q=0.8");
		//		r.KeepAlive = true;
		//		r.Headers.Add("x-eb-xsrf-token: " + "b054am9GOUlpU004THlMY3BaeV9HLVJucmYySXRyWWFWWUdnUjhFaWdaOHwtNjIyMDA1MjExMDE5MDAyOTg4MnwxfDIwMTgtMDctMTNUMDU6MjY6MzkuODk4Wg==");
		//		r.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
		//		r.Referer = "https://us-west-2.console.aws.amazon.com/elasticbeanstalk/home?region=us-west-2";
		//		r.Headers.Add("X-ElasticBeanstalk-RequestId: ltspicsbdzg-wcs6hd11fl");
		//	});
		//}

		public static string ExecuteGetRequest(string url) {
			return CookieReader.ExecuteGetRequest(url, r => {
				r.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
				r.Headers.Add("Accept-Encoding: gzip, deflate, br");
				r.Headers.Add("Accept-Language: en-US,en;q=0.9,en-GB;q=0.8");
				r.Headers.Add("Cache-Control: max-age=0");
				r.KeepAlive = true;
				//r.Headers.Add("x-eb-xsrf-token: " + "b054am9GOUlpU004THlMY3BaeV9HLVJucmYySXRyWWFWWUdnUjhFaWdaOHwtNjIyMDA1MjExMDE5MDAyOTg4MnwxfDIwMTgtMDctMTNUMDU6MjY6MzkuODk4Wg==");
				r.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
				r.Headers.Add("Upgrade-Insecure-Requests: 1");
			},false);
		}

		public class AwsEnvironment {
			public AwsEnvironment(string name, string loadBalancerName, string region,string autoScalingGroup) {
				Name = name;
				LoadBalancerName = loadBalancerName;
				Region = region;
				AutoScalingGroup = autoScalingGroup;
			}

			public string Name { get; private set; }
			public string LoadBalancerName { get; private set; }
			public string Region { get; private set; }
			public string AutoScalingGroup { get;  set; }

			public override string ToString() {
				return Name;
			}
		}


		public class ChartDesc {
			public enum MetricDimension {
				LoadBalancer,
				AutoScalingGroup
			}

			public ChartDesc(string name , MetricDimension dimension, string metric, string @namespace, string statistics) {
				Name = name;
				Dimension = dimension;
				Metric = metric;
				Namespace = @namespace;
				Statistics = statistics;
			}

			public string Name { get; private set; }
			private MetricDimension Dimension { get; set; }
			private string Metric { get; set; }
			private string Namespace { get; set; }
			private string Statistics { get; set; }

			private string GetDimensionStr(AwsEnvironment env) {
				switch (Dimension) {
					case MetricDimension.LoadBalancer:
						return "dimension.LoadBalancerName=" + env.LoadBalancerName + "&namespace=AWS%2FELB";
					case MetricDimension.AutoScalingGroup:
						return "dimension.AutoScalingGroupName=" + env.AutoScalingGroup + "&namespace=AWS%2FEC2";
				}
				throw new ArgumentOutOfRangeException(""+Dimension);
			}

			public string ToUrl(AwsEnvironment app, DateTime start, DateTime end) {

				var period = (int)(end - start).TotalSeconds / 1440.0;

				return "https://" + app.Region + ".console.aws.amazon.com/elasticbeanstalk/service/cloudwatch/stats/" +
					start.ToString("yyyy-MM-ddThh:mm:ss.000Z") + "/" + //"2018-07-10T04:09:07.827Z" 
					end.ToString("yyyy-MM-ddThh:mm:ss.000Z") +//2018-07-13T02:52:34.954Z" +
					"?"+ GetDimensionStr(app) + //"awseb-e-d-AWSEBLoa-N797H3PA6H8" +
					"&metricName=" + Metric +
					"&period=" + period +
					"&region=" + app.Region +
					"&statistics=" + Statistics;
			}

		}

		public static XYSeries DownloadSeries(ChartDesc chart, AwsEnvironment app, TimeSpan span) {
			return DownloadSeries(chart, app, span, DateTime.UtcNow);
		}
		public static XYSeries DownloadSeries(ChartDesc chart, AwsEnvironment app, TimeSpan span, DateTime end) {
			return  DownloadSeries(chart, app, end.Subtract(span), end);
		}
		public static XYSeries DownloadSeries(ChartDesc chart, AwsEnvironment app, DateTime start, TimeSpan span) {
			return  DownloadSeries(chart, app, start, start.Add(span));
		}

		public static XYSeries DownloadSeries(ChartDesc chart, AwsEnvironment app, DateTime start, DateTime end) {
			var res =  ExecuteGetRequest(chart.ToUrl(app, start, end));
			dynamic dd = JArray.Parse(res);

			var XYs = new List<Point>();
			if (dd.Count > 0) {
				foreach (var x in dd[0].data) {
					XYs.Add(new Point((long)x[0], (decimal?)x[1]));
				}
			}

			return new XYSeries() {
				Name = chart.Name,
				Points = XYs
			};
		}

	}
}
