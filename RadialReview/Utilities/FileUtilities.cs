using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
	public class FileUtilities
	{
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