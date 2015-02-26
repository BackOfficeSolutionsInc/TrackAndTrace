using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
	public static class RandomUtil
	{
		public static double GaussianNormal(double mean=0, double stdDev=1)
		{
			return (new Random()).NextNormal(mean, stdDev);
		}
		public static double NextNormal(this Random rand, double mean=0, double stdDev=1)
		{
			var u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
			var u2 = rand.NextDouble();
			var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
			return mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
		}
	}
}