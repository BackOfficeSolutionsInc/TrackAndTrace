using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static string SubstringAfter(this string value, string search, int? length = null, int startIndex = 0)
        {
            if (length != null && length < 0)
                throw new ArgumentOutOfRangeException("Length must be greater than zero.");

            if (value == null)
                return null;
            var loc = value.IndexOf(search, startIndex);
            if (loc == -1)
                return null;
            //throw new ArgumentOutOfRangeException("search","Search term was not found");
            if (length == null) {
                return value.Substring(loc + search.Length);
            } else {
                return value.Substring(loc + search.Length, length.Value);
            }
        }
        public static string SubstringBefore(this string value, string search, int? length = null, int startIndex = 0)
        {
            if (length != null && length < 0)
                throw new ArgumentOutOfRangeException("Length must be greater than zero.");
            if (value == null)
                return null;
            var loc = value.IndexOf(search, startIndex);
            if (loc == -1)
                return null;
            //throw new ArgumentOutOfRangeException("search","Search term was not found");
            if (length == null) {
                return value.Substring(0,loc);
            } else {
                //return value.Substring(loc + search.Length, length.Value);
                return value.Substring(loc-length.Value, loc);
            }
        }

		public static string GetString(this byte[] bytes)
		{
			var chars = new char[bytes.Length / sizeof(char)];
			System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
			return new string(chars);
		}

        public static string ToTitleCase(this string str)
        {
            var myTI = new CultureInfo("en-US", false).TextInfo;
            return myTI.ToTitleCase(str);
        }

		//*********************************************************************************************************
		// © 2013 jakemdrew.com. All rights reserved. 
		// This source code is licensed under The GNU General Public License (GPLv3):  
		// http://opensource.org/licenses/gpl-3.0.html
		//*********************************************************************************************************

		//*********************************************************************************************************
		//makeNgrams - Example n-gram creator.
		//Created By - Jake Drew 
		//Version -    1.0, 04/22/2013
		//*********************************************************************************************************
		public static IEnumerable<string> GetNGrams(string text, int nGramSize) {
			if (nGramSize == 0)
				throw new Exception("nGram size was not set");

			StringBuilder nGram = new StringBuilder();
			Queue<int> wordLengths = new Queue<int>();

			int wordCount = 0;
			int lastWordLen = 0;

			//append the first character, if valid.
			//avoids if statement for each for loop to check i==0 for before and after vars.
			if (text != "" && char.IsLetterOrDigit(text[0])) {
				nGram.Append(text[0]);
				lastWordLen++;
			}

			//generate ngrams
			for (int i = 1; i < text.Length - 1; i++) {
				char before = text[i - 1];
				char after = text[i + 1];

				if (char.IsLetterOrDigit(text[i])
						||
						//keep all punctuation that is surrounded by letters or numbers on both sides.
						(text[i] != ' '
						&& (char.IsSeparator(text[i]) || char.IsPunctuation(text[i]))
						&& (char.IsLetterOrDigit(before) && char.IsLetterOrDigit(after))
						)
					) {
					nGram.Append(text[i]);
					lastWordLen++;
				} else {
					if (lastWordLen > 0) {
						wordLengths.Enqueue(lastWordLen);
						lastWordLen = 0;
						wordCount++;

						if (wordCount >= nGramSize) {
							yield return nGram.ToString();
							nGram.Remove(0, wordLengths.Dequeue() + 1);
							wordCount -= 1;
						}

						nGram.Append(" ");
					}
				}
			}
			nGram.Append(text.Last());
			yield return nGram.ToString();
		}
	}
}