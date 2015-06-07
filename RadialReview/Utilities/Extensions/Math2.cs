using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
	public class Math2
	{

		public static DateTime Min(DateTime d1, params DateTime[] dates)
		{
			var min = dates.Select(d => d.Ticks).Concat(new[]{d1.Ticks}).Min();
			return new DateTime(min);
		}
		public static DateTime Max(DateTime d1, params DateTime[] dates)
		{
			var max = dates.Select(d => d.Ticks).Concat(new[]{d1.Ticks}).Max();
			return new DateTime(max);
		}


		public static double Coerce(double val, double low, double high)
		{
			return Math.Max(Math.Min(high, val), low);
		}
	}

}