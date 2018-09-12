using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities {
	public class Sha256 {
		public static string Hash(string str) {
			var crypt = new System.Security.Cryptography.SHA256Managed();
			var hash = new StringBuilder();
			byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(str));
			foreach (byte theByte in crypto) {
				hash.Append(theByte.ToString("x2"));
			}
			return hash.ToString();
		}
	}
}
