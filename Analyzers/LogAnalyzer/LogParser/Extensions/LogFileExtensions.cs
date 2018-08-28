using LogParser.Models;
using ParserUtilities.Utilities.LogFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser {
	public static class LogFileExtensions {

		public static IEnumerable<IGrouping<string,LogLine>> GroupByUsers(this IEnumerable<LogLine> lines,bool activeOnly) {
			return lines.Where(x=>!activeOnly || !x.csUriStem.Contains("/signalr/")).GroupBy(x=> x.csUsername);
		}

		public static void RemoveCachedPages(this LogFile<LogLine> file) {
			file.Filter(x => x.csUriStem, "/bundles/", "/Content/", "/styles/", "/{{user.ImageUrl}}");
		}

		public static void AutoFlag(this LogFile<LogLine> file) {
			var shortRequests = new[] { "healthcheck","favicon.ico","/Image/TodoCompletion" };

			var longRequest = new Func<LogLine, bool>(x => (shortRequests.Any(s => x.csUriStem.Contains(s)) && x.Duration > TimeSpan.FromSeconds(1)));
			file.Flag(longRequest, FlagType.UnusuallyLongRequest);

			var symptoms = new Func<LogLine,bool>(x => x.StatusCode >=500 || longRequest(x));
			var lines = file.GetFilteredLines().ToList();
			var unusuallyLongRequests = lines.Where(symptoms).ToList();


			foreach (var u in unusuallyLongRequests) {
				var interesting = file.Clone()
					.FilterRange(u.StartTime, u.EndTime.AddSeconds(1))
					.Filter(x => x.Duration < TimeSpan.FromSeconds(1))
					.Filter(x => x.csUriStem.Contains("signalr"))
					.GetFilteredLines().ToList();
				interesting.ForEach(x => x.Flag = FlagType.PotentialCauses);

				var cause = interesting.OrderByDescending(x => x.Duration).FirstOrDefault();
				if (cause != null)
					cause.Flag = FlagType.LikelyCause;
			}

			file.Flag(x => x.Duration > TimeSpan.FromSeconds(3) && !x.csUriStem.Contains("signalr"), FlagType.UnusuallyLongRequest);
			file.Flag(x => x.StatusCode >= 500, FlagType.HasError);
			
		}
		
		public static void RemoveInitialSignalR(this LogFile<LogLine> file) {
			var nonSignalR = file.GetFilteredLines().OrderBy(x => x.StartTime).First(x => !x.csUriStem.Contains("signalr"));
			file.Filter(x => x.csUriStem.Contains("signalr") && x.StartTime < nonSignalR.StartTime);
		}
	}
}
