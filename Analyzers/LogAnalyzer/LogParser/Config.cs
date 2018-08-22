using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser {
	/// <summary>
	/// Always end directories with '\'
	/// </summary>
	public class Config {
		private static string BaseDirectory = @"C:\Users\Lynnea\Desktop\Diagnosis\";
		private static string Session = null;

		public static void SetSession(string session) {
			Session = session.Trim('\\');
		}

		public static String GetSession() {
			if (string.IsNullOrWhiteSpace(Session))
				throw new Exception("Session name was not set");
			return Session;
		}

		/// <summary>
		/// Get the full path given just the file name
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static String GetFile(string file) {
			return GetDirectory() + file;
		}

		/// <summary>
		/// Get the full path given just the file name
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static String BaseDirectoryFile(string file) {
			return GetBaseDirectory() + file;
		}

		public static String GetDirectory() {
			var session = GetSession();
			var dir = GetBaseDirectory() + session + "\\";
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir;
		}
		public static String GetBaseDirectory() {
			return BaseDirectory.Trim('\\') + "\\";
		}

		public static string GetCacheDirectory() {
			return Config.GetBaseDirectory() + "Cache";
		}
		
		public static string ChangePath(string path, string addSuffix, string changeFileType = null) {
			var last = path.LastIndexOf(".");
			if (last == -1)
				last = path.Length - 1;
			changeFileType = "." + (changeFileType ?? path.Substring(last + 1));
			return path.Substring(0, last - 1) + addSuffix + changeFileType;
		}
	}
}
