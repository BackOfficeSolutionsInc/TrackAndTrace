using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.S3;
using Amazon.S3.Model;
using LogParser.Models;
using ParserUtilities;
using ParserUtilities.Utilities.CacheFile;
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

namespace LogParser.Downloaders {
	public class AwsLogFileDownloader {

		public static String DownloadBucket = "radial-log";
		public static LogStream TTAccessLogs = new LogStream("TractionTools.log.group", "TractionTools.instance.log");

		public class LogStream {
			public LogStream(string logGroupName, string logStreamName) {
				LogStreamName = logStreamName;
				LogGroupName = logGroupName;
			}
			public string LogStreamName { get; set; }
			public string LogGroupName { get; set; }
		}
		public static LogFile<LogLine> DownloadAccessLogsAround(DateTime time, DateTimeKind kind, double rangeMinutes, bool ignoreRangeWarning = false) {
			return DownloadAccessLogsAround(time, kind, TimeSpan.FromMinutes(rangeMinutes), ignoreRangeWarning);
		}

		public static LogFile<LogLine> DownloadAccessLogsAround(DateTime time, DateTimeKind kind, TimeSpan? range = null, bool ignoreRangeWarning = false) {
			var r = range ?? TimeSpan.FromMinutes(30);
			
			var rr = TimeRange.Around(r, time, kind);
			return Download(TTAccessLogs,  rr, ignoreRangeWarning);
		}

		public static LogFile<LogLine> DownloadAccessLogs(TimeRange range, bool ignoreRangeWarning = false) {
			return Download(TTAccessLogs, range, ignoreRangeWarning);
		}
		//}
		//public static LogFile<LogLine> DownloadAccessLogs(DateTime startTime, TimeSpan duration, DateTimeKind kind, bool ignoreRangeWarning = false) {
		//	return Download(TTAccessLogs, startTime, startTime + duration, kind, ignoreRangeWarning);
		//}
		//public static LogFile<LogLine> DownloadAccessLogs(TimeSpan duration, DateTime endTime, DateTimeKind kind, bool ignoreRangeWarning = false) {
		//	return Download(TTAccessLogs, endTime - duration, endTime, kind, ignoreRangeWarning);
		//}
		//public static LogFile<LogLine> DownloadAccessLogs(double durationMinutes, DateTime endTime, DateTimeKind kind, bool ignoreRangeWarning = false) {
		//	return Download(TTAccessLogs, endTime - TimeSpan.FromMinutes(durationMinutes), endTime, kind, ignoreRangeWarning);
		//}
		//public static LogFile<LogLine> DownloadAccessLogs(DateTime startTime, double durationMinutes, DateTimeKind kind, bool ignoreRangeWarning = false) {
		//	return Download(TTAccessLogs, startTime, startTime + TimeSpan.FromMinutes(durationMinutes), kind, ignoreRangeWarning);
		//}

		public static LogFile<LogLine> Download(LogStream stream, TimeRange range, bool ignoreRangeWarning = false) {
			var nowMs = DateTime.UtcNow.ToJsMs();
			var startTime = range.Start;
			var endTime = range.End;
			var kind = range.Kind;

			if (endTime.ToJsMs() == nowMs || startTime.ToJsMs() == nowMs) {
				Log.Warn("Using UtcNow as input will disable caching",false);
			}

			startTime = DateTime.SpecifyKind(startTime, kind);
			endTime = DateTime.SpecifyKind(endTime, kind);

			var name = "log-file\\" + startTime.ToJsMs() + "-" + endTime.ToJsMs() + ".log";


			var sw = Stopwatch.StartNew();
			var file = FileCache.GetOrAddCachedFile(Config.GetCacheDirectory(),name, outFile => {
				Log.Info("Exporting logs");
				var details = StartExport(stream, startTime, endTime, ignoreRangeWarning);
				//Wait until finished
				WaitUntilExportComplete(details);
				//All done.
				details.ExecutionElapsedMs = sw.ElapsedMilliseconds;
				Log.Info("...export complete. (" + details.ExecutionElapsedMs + "ms)");
				Log.Info("Downloading logs");
				if (!details.Success)
					throw new Exception("Stream export did not succeed");

				var files = AwsS3Downloader.DownloadFromPrefix(details.Bucket, details.Prefix);
				Log.Info("...download complete. (" + (sw.ElapsedMilliseconds - details.ExecutionElapsedMs) + "ms)");
				Log.Info("Decompressing...");
				StringBuilder sb = new StringBuilder();
				foreach (var f in files) {
					sb.AppendLine(FileUtility.Decompress(f));

				}
				Log.Info("Writing...");
				File.WriteAllText(outFile, sb.ToString());
				Log.Info("...written.");
			});

			return LogFileReader.Read<LogLine>(file);
		}

		private class ExportDetails {
			public string TaskId { get; set; }
			public string Bucket { get; set; }
			public DateTime StartTime { get; set; }
			public DateTime EndTime { get; set; }
			public string Prefix { get; set; }
			public bool Finished { get; set; }
			public bool Success { get; set; }
			public long ExecutionElapsedMs { get; set; }
		}

		private static ExportDetails StartExport(LogStream stream, DateTime startTime, DateTime endTime, bool ignoreRangeWarning) {
			if (endTime < startTime) {
				var e = endTime;
				endTime = startTime;
				startTime = e;
			}

			if (!ignoreRangeWarning && endTime - startTime > TimeSpan.FromDays(1)) {
				throw new Exception("Download range is too wide. Turn off warning with optional flag.");
			}
			var bucketPrfix = "LogParser";
			using (var client = new AmazonCloudWatchLogsClient(RegionEndpoint.USWest2)) {
				var start = startTime.ToUniversalTime().ToJsMs();
				var end = endTime.ToUniversalTime().ToJsMs();
				var request = new CreateExportTaskRequest() {
					Destination = "radial-log",
					DestinationPrefix = bucketPrfix,
					From = start,
					To = end,
					TaskName = "ExportLogs",
					LogGroupName = stream.LogGroupName,
					LogStreamNamePrefix = stream.LogStreamName,
				};
				//Start export
				var task = client.CreateExportTask(request);
				var details = new ExportDetails {
					Bucket = DownloadBucket,
					Prefix = bucketPrfix + "/" + task.TaskId + "/" + stream.LogStreamName,
					TaskId = task.TaskId,
					StartTime = startTime,
					EndTime = endTime,
				};


				return details;
			}
		}

		private static ExportDetails WaitUntilExportComplete(ExportDetails details) {
			using (var client = new AmazonCloudWatchLogsClient(RegionEndpoint.USWest2)) {
				ExportTask task = null;
				while (true) {
					var describeRequest = new DescribeExportTasksRequest() {
						TaskId = details.TaskId,
					};
					var result = client.DescribeExportTasks(describeRequest);

					task = result.ExportTasks.Single();

					if (task.Status.Code != ExportTaskStatusCode.RUNNING && task.Status.Code != ExportTaskStatusCode.PENDING) {
						break;
					}
				}
				details.Finished = true;
				if (task.Status.Code == ExportTaskStatusCode.COMPLETED)
					details.Success = true;
				return details;
			}
		}





	}
}
