using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
	public class StreamUtil
	{
		public static MemoryStream ReadIntoStream(Stream input)
		{
			var memoryStream = new MemoryStream();
			var buffer = new byte[32 * 1024]; // 32K buffer for example
			int bytesRead;
			while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0){
				memoryStream.Write(buffer, 0, bytesRead);
			}
			memoryStream.Position = 0;
			return memoryStream;
		}
	}
}