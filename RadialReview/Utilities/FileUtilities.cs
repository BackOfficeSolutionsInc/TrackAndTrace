using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
	public static class FileUtilities
	{
        public static bool IsFileLocked(this FileInfo file)
        {
            FileStream stream = null;

            try {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            } catch (IOException) {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            } finally {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

		public static string[] WriteSafeReadAllLines(String path)
		{
			using (var csv = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var sr = new StreamReader(csv))
			{
				var file = new List<string>();
				while (!sr.EndOfStream)
				{
					file.Add(sr.ReadLine());
				}

				return file.ToArray();
			}
		}
	}
}