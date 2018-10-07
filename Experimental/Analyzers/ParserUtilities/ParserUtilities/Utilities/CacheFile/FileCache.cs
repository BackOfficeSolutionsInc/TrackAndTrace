using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.CacheFile {
	public class FileCache {

		private static bool ExistsInFileCache(string fileName) {
			return File.Exists(FileUtility.Filename(fileName));
		}
		

		public static string GetOrAddCachedFile(string cacheDirectory,string fileName, Action<string> generator, bool forceExecute = false) {
			var file = FileUtility.Filename(cacheDirectory/*Config.GetCacheDirectory()*/,fileName);
			Log.Info("Getting file:" + file);
			if (forceExecute || !ExistsInFileCache(file)) {
				Log.Info("\tcache-miss");
				Log.Info("\tGenerating");
				generator(file);
				Log.Info("\t...generated.");
			} else {
				Log.Info("\tcache-hit");
			}
			return file;
		}
		

	}
}
