using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RadialReview {
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

        public static List<string> ReadLines(this MemoryStream stream)
        {
            stream.Position = 0; // Rewind!
            List<string> rows = new List<string>();
            using (var reader = new StreamReader(stream)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    rows.Add(line);
                }
            }
            return rows;
        }

	}
}