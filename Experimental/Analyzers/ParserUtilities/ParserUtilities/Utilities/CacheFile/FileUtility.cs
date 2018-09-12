using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.CacheFile {
	public class FileUtility {

		public static string Filename(string defaultDirectory, string fileName) {
			if (fileName.Contains(":")) {
				var dir = Path.GetDirectoryName(fileName);
				if (!Directory.Exists(dir)) {
					Directory.CreateDirectory(dir);
				}
				return fileName;
			} else {
				var path = defaultDirectory + "\\" + fileName;
				return Filename(path);
			}
		}

		public static string Filename(string filePath) {
			if (!filePath.Contains(":")) {
				throw new Exception("Expecting an absolute file path");
			}
			var dir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}
			return filePath;
		}

		public static string Decompress(string file) {
			var bytes = DecompressToBytes(file);
			return Encoding.UTF8.GetString(bytes);
		}

		private static byte[] DecompressToBytes(string file) {
			byte[] gzip = File.ReadAllBytes(file);
			// Create a GZIP stream with decompression mode.
			// ... Then create a buffer and write into while reading from the GZIP stream.
			using (var stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress)) {
				const int size = 4096;
				byte[] buffer = new byte[size];
				using (MemoryStream memory = new MemoryStream()) {
					int count = 0;
					do {
						count = stream.Read(buffer, 0, size);
						if (count > 0) {
							memory.Write(buffer, 0, count);
						}
					}while (count > 0);

					return memory.ToArray();
				}
			}

		}

		public static void CombineMultipleFiles(IEnumerable<string> inputPaths, string outputFilePath) {
			using (var outputStream = File.Create(outputFilePath)) {
				foreach (var inputFilePath in inputPaths) {
					using (var inputStream = File.OpenRead(inputFilePath)) {
						inputStream.CopyTo(outputStream);
					}
				}
			}
		}
	}
}
