using System.IO;

namespace RadialReview.Utilities.Extensions {
	public static class StreamExtension {

		public static string ReadToEnd(this Stream stream) {
			return new StreamReader(stream).ReadToEnd();
		}

		public static Stream ToStream(this string s)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}

	}
}