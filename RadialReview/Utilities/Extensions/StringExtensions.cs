using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace RadialReview
{
    public static class StringExtensions
    {
        public static string IntToStringFast(this int value, char[] baseChars)
        {
            // 32 is the worst cast buffer size for base 2 and int.MaxValue
            int i = 32;
            char[] buffer = new char[i];
            int targetBase = baseChars.Length;

            do
            {
                buffer[--i] = baseChars[value % targetBase];
                value = value / targetBase;
            }
            while (value > 0);

            char[] result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);

            return new string(result);
        }

        public static string ToLetter(this int self){
            return self.IntToStringFast(new[] { 'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z' });
        }

        public static bool EqualsInvariant(this String self, string other)
        {
            if (self == null && other == null)
                return true;
            if (self == null || other == null)
                return false;
            return self.ToLower().Equals(other.ToLower());
        }
		
        public static string Surround(this String self, string left, string right)
        {
            if (String.IsNullOrWhiteSpace(self))
                return self;
            else
                return left + self + right;
        }

        public static String Pluralize(this String self, double count, String plural = null)
        {
            if (count == 1) return self;
            else return plural ?? (self + "s");
        }
        public static String Possessive(this String self)
        {
            return self + "'s";
        }

	    public static String EscapeHtml(this string self)
	    {
		    if (self == null)
			    return null;

			return self.Replace("'", "&#39;").Replace("\"", "&#34;");//.Replace("\n", "&#13;");
	    }

		public static string EscapeJSONString(this string value)
		{
			const char BACK_SLASH = '\\';
			const char SLASH = '/';
			const char DBL_QUOTE = '"';

			var output = new StringBuilder(value.Length);
			foreach (char c in value)
			{
				switch (c)
				{
					case SLASH:
						output.AppendFormat("{0}{1}", BACK_SLASH, SLASH);
						break;

					case BACK_SLASH:
						output.AppendFormat("{0}{0}", BACK_SLASH);
						break;

					case DBL_QUOTE:
						output.AppendFormat("{0}{1}", BACK_SLASH, DBL_QUOTE);
						break;

					default:
						output.Append(c);
						break;
				}
			}

			return output.ToString();
		}

		public static byte[] GetBytes(this string str)
		{
			var bytes = new byte[str.Length * sizeof(char)];
			System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
		}

		public static string GetString(this byte[] bytes)
		{
			var chars = new char[bytes.Length / sizeof(char)];
			System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
			return new string(chars);
		}
    }
}