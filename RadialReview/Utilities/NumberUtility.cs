using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
	public static class NumberUtility
	{

		public static string KiloFormat(this decimal num)
		{
			return KiloFormat((double)num);
		}
		public static string KiloFormat(this double num)
		{
			var strs = new[]{"", "k","M","B","T" };

			var n = 0;
			if (num !=0)
				n=(int)Math.Log10(Math.Abs(num));
			var t = Math.Min(4,(int)(n / 3));
			var t3 = t*3;
			var p = Math.Pow(10, t3);

			if (n <= -3)
				return num.ToString("0.##E+0");
			if (n <= 0)
				return num.ToString("0.###");
			if (n == 1 || n == 2){
				if (Math.Abs(num - (long) num) < double.Epsilon){
					return num.ToString("0.##");
				}else{
					return num.ToString("0.00");
				}
			}
			if (n == 3)
				return num.ToString("#,#.##");
			if (n == 4)
				return num.ToString("#,#");
			if (n >= 5)
			{
				if (n % 3 == 0)
					return (num / p).ToString("#,#.##") + strs[t];
				if (n % 3 == 1)
					return (num / p).ToString("#,#.##") + strs[t];
				if (n % 3 == 2)
					return (num / p).ToString("#,#.#") + strs[t];
			}
			return num.ToString("#.##");
			/*
			if (num >= 100.0 * 1E12)
				return (num / 1E12).ToString("#,0T");
			if (num >= 10.0 * 1E12)
				return (num / 1E12).ToString("0.#") + "T";

			if (num >= 100.0 * 1E9)
				return (num / 1E9).ToString("#,0B");
			if (num >= 10.0*1E9)
				return (num / 1E9).ToString("0.#") + "B";

			if (num >= 100.0 * 1E6)
				return (num / 1E6).ToString("#,0M");
			if (num >= 10.0*1E6)
				return (num / 1E6).ToString("0.#") + "M";

			if (num >= 100.0*1E3)
				return (num / 1E3).ToString("#,0K");
			if (num >= 10.0*1E3)
				return (num / 1E3).ToString("0.#") + "K";

			return num.ToString("#,0");*/
		} 
	}
}