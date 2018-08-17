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


	}
}
