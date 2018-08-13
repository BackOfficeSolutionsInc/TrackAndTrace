using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {
	

	public class LogFileReader {
		
		public static LogFile Read<T>(string path) where T : ILogLine, new() {
			using (var sr = new FileStream(path, FileMode.Open)) {
				return Read<T>(sr, path);
			}
		}

		public static LogFile Read<T>(Stream contents, string path) where T : ILogLine,new() {

			Log.Info("Parsing log file");
			var file = new LogFile() {
				Path = path,
			};
			using (StreamReader sr = new StreamReader(contents)) {
				while (sr.Peek() >= 0) {
					var line = sr.ReadLine();
					var parsed = new T().ConstructFromLine(line);
					if (parsed != null) {
						file.AddLine(parsed);
					}
				}
			}
			Log.Info("...parsed.");
			return file;
		}


	}
}
