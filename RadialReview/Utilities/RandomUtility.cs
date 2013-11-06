using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class RandomUtility
    {
        public static Random GlobalRandom = new Random();

        public static long LongRandom(long min, long max, Random rand)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }
        public static long LongRandom()
        {
            return LongRandom(long.MinValue, long.MaxValue, GlobalRandom);
        }

        

    }
}