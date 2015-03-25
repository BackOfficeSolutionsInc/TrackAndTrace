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
			var min = d1.Ticks;
			foreach (var d in dates)
			{
				min = Math.Min(min, d.Ticks);
			}
			return new DateTime(min);
		}
		public static DateTime Max(DateTime d1, params DateTime[] dates)
		{
			var min = d1.Ticks;
			foreach (var d in dates)
			{
				min = Math.Min(min, d.Ticks);
			}
			return new DateTime(min);
		}


		public static double Coerce(double val, double low, double high)
		{
			return Math.Max(Math.Min(high, val), low);
		}
	}

}