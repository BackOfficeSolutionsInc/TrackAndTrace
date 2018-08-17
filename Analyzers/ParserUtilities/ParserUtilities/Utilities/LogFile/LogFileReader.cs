using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {
	

	public class LogFileReader {
		
		public static LogFile<T> Read<T>(string path,int skipLines=0) where T : ILogLine, new() {
			using (var sr = new FileStream(path, FileMode.Open)) {
				return Read<T>(sr, path, skipLines);
			}
		}

		public static LogFile<T> Read<T>(Stream contents, string path, int skipLines = 0) where T : ILogLine,new() {

			Log.Info("Parsing log file");
			var file = new LogFile<T>() {
				Path = path,
			};
			int lineNum = -1;
			using (StreamReader sr = new StreamReader(contents)) {
				while (sr.Peek() >= 0) {
					var line = sr.ReadLine();
					lineNum += 1;
					if (lineNum < skipLines)
						continue;

					var parsed = (T)new T().ConstructFromLine(line);
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
