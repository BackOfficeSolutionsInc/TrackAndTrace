using ParserUtilities.Utilities;
using ParserUtilities.Utilities.LogFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Models {
	public class LogLine : ILogLine{
		public string Guid { get; set; }
		public string date { get; private set; }
		public string time { get; private set; }
		public string sIp { get; private set; }
		public string csMethod { get; private set; }
		public string csUriStem { get; private set; }
		public string csUriQuery { get; private set; }
		public string sPort { get; private set; }
		public string csUsername { get; private set; }
		public string cIp { get; private set; }
		public string csUserAgent { get; private set; }
		public string csReferer { get; private set; }
		public string scStatus { get; private set; }
		public string scSubstatus { get; private set; }
		public string scWin32Status { get; private set; }
		public string timeTaken { get; private set; }


		public FlagType Flag { get; set; }
		public int GroupNumber { get; set; }
		public DateTime StartTime { get; private set; }
		public DateTime EndTime { get; private set; }
		public TimeSpan Duration { get; private set; }
        public string InstanceName { get; set; }

		public int? StatusCode {
			get {
				int status = 0;
				return int.TryParse(scStatus, out status) ? (int?)status : null;
			}
		}


		public ILogLine ConstructFromLine(string line) {
			try {
				var parts = line.Split(' ');

				LogLine ll = null;
				switch (parts.Length) {
					case 14:
						ll = ParseCloudwatchLine(parts);
						break;
					case 15:
						ll = ParseLocalLine(parts);
						break;
					default:
						return null;
				}

				ll.EndTime = DateTime.Parse(ll.date + " " + ll.time);
				ll.StartTime = ll.EndTime.AddMilliseconds(-int.Parse(ll.timeTaken));
				ll.Duration = ll.EndTime - ll.StartTime;
				ll.Guid = Sha256.Hash(line);

				return ll;
			} catch (Exception) {
				return null;
			}
		}

		private static LogLine ParseLocalLine(string[] parts) {
			return new LogLine {
				date = parts[0],
				time = parts[1],
				sIp = parts[2],
				csMethod = parts[3],
				csUriStem = parts[4],
				csUriQuery = parts[5],
				sPort = parts[6],
				csUsername = parts[7],
				cIp = parts[8],
				csUserAgent = parts[9],
				csReferer = parts[10],
				scStatus = parts[11],
				scSubstatus = parts[12],
				scWin32Status = parts[13],
				timeTaken = parts[14],
			};
		}
		private static LogLine ParseCloudwatchLine(string[] parts) {
			var dateTime = parts[0].Split('T');

			return new LogLine {
				date = dateTime[0],
				time = dateTime[1],
				sIp = parts[1],
				csMethod = parts[2],
				csUriStem = parts[3],
				csUriQuery = parts[4],
				sPort = parts[5],
				csUsername = parts[6],
				cIp = parts[7],
				csUserAgent = parts[8],
				csReferer = parts[9],
				scStatus = parts[10],
				scSubstatus = parts[11],
				scWin32Status = parts[12],
				timeTaken = parts[13],
			};
		}

		public string[] GetHeaders() {
			var items = new[]{
					"date"          ,
					"time"          ,
					"sIp"           ,
					"csMethod"      ,
					"csUriStem"     ,
					"csUriQuery"    ,
					"sPort"            ,
					"csUsername"    ,
					"cIp"           ,
					"csUserAgent"   ,
					"csReferer"     ,
					"scStatus"      ,
					"scSubstatus"   ,
					"scWin32Status" ,
					"timeTaken"     ,
					"StartDate","StartTime",
					"EndDate","EndTime",
					"RelativeStart",
					"Duration",
					"LocalStartDate","LocalStartTime",
				};
			return items;
			//return string.Join(" ", items);

		}

		public bool AnyFieldContains(string lookup) {
			lookup = lookup.ToLower();
			foreach (var f in GetLine(DateTime.MinValue))
				if (f.ToLower().Contains(lookup))
					return true;
			return false;
		}

		public string[] GetLine(DateTime startRange) {
			var items = new[]{
					date          ,
					time          ,
					sIp           ,
					csMethod      ,
					csUriStem     ,
					csUriQuery    ,
					sPort         ,
					csUsername    ,
					cIp           ,
					csUserAgent   ,
					csReferer     ,
					scStatus      ,
					scSubstatus   ,
					scWin32Status ,
					timeTaken     ,
					StartTime.ToString("MM-dd-yyyy HH:mm:ss.FFFF"),
					EndTime.ToString("MM-dd-yyyy HH:mm:ss.FFFF"),
					(StartTime-startRange).ToString("hh':'mm':'ss'.'fff"),
					(Duration).ToString("hh':'mm':'ss'.'fff"),
					(StartTime.ToLocalTime()).ToString("MM-dd-yyyy HH:mm:ss.FFFF"),
				};
			return items;
			//return string.Join(" ", items);
		}
	}
}
