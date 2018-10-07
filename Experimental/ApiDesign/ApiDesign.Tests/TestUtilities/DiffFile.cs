using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ApiDesign.Tests.TestUtilities {
	public class Hash {

		public static string File(string file) {
			var reader = new StreamReader(file, Encoding.UTF8);
			return String(reader.ReadToEnd());
		}

		public static string String(string contents) {
			var text = Encoding.UTF8.GetBytes(contents);
			using (SHA1Managed sha1 = new SHA1Managed()) {
				byte[] hash = sha1.ComputeHash(text);
				StringBuilder formatted = new StringBuilder(2 * hash.Length);
				foreach (byte b in hash) {
					formatted.AppendFormat("{0:X2}", b);
				}
				return formatted.ToString();
			}
		}

		public static bool FilesAreDifferent(string hash, string file) {
			return HashsAreDifferent(hash,File(file));
		}
		public static bool HashsAreDifferent(string hash1, string hash2) {
			return hash1 != hash2;
		}


	}
}
