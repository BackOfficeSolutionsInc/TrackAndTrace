using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class ShortenerUtility
    {
        private static char[] BaseChars = new char[] { 
            'Q','B','a','A','4','v','5','x','j','T',
            'U','z','K','k','o','F','Y','p','M','c',
            'W','E','g','q','R','S','P','1','i','t',
            '7','m','I','w','9','h','C','s','X','D',
            'f','L','u','O','n','b','2','l','N','G',
            'J','H','d','Z','0','8','V','3','e','6',
            'r'
        };

        public static String Shorten(long number)
        {
            return LongToStringFast(number, BaseChars);
        }

        public static long Expand(String val)
        {
            return StringToLong(val,BaseChars);
        }

        private static long StringToLong(String str,char[] baseChars)
        {
            string parsed = str;
            int targetBase = baseChars.Length;
            long value = 0;
            
            foreach(var c in parsed.ToCharArray())
            {
                value = value * targetBase;
                value+=Array.IndexOf(baseChars, c);
            }
            return value;
        }

        /// <summary>
        /// An optimized method using an array as buffer instead of 
        /// string concatenation. This is faster for return values having 
        /// a length > 1.
        /// </summary>
        private static string LongToStringFast(long value, char[] baseChars)
        {
            // 32 is the worst cast buffer size for base 2 and int.MaxValue
            int i = 32;
            char[] buffer = new char[i];
            int targetBase = baseChars.Length;
            do{
                buffer[--i] = baseChars[value % targetBase];
                value = value / targetBase;
            }while (value > 0);

            char[] result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);

            return new string(result);
        }
    }
}