using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview
{
	public enum NumberFormat
	{
		English = 0,
		European1 = 1,
		European2 = 2
	}


	public static class NumberFormatExtensions
	{
		public static List<SelectListItem> Formats()
		{
			return Enum.GetValues(typeof(NumberFormat)).Cast<NumberFormat>().Select(x => new SelectListItem()
			{
				Text = x.Sample(),
				Value = ""+x
			}).ToList();
		}

		public static Dictionary<NumberFormat, string> angFormats = GetAngFormats();

		public static string Radix(this NumberFormat format)
		{
			switch (format)
			{
				case NumberFormat.English: return ".";
				case NumberFormat.European1: return ",";
				case NumberFormat.European2: return ",";
				default: throw new ArgumentOutOfRangeException("" + format);
					
			}
		}
		public static string GroupSeparator(this NumberFormat format)
		{
			switch (format)
			{
				case NumberFormat.English: return ",";
				case NumberFormat.European1: return " ";
				case NumberFormat.European2: return ".";
				default: throw new ArgumentOutOfRangeException(""+format);
			}
		}

		public static string Sample(this NumberFormat format)
		{
			var r = format.Radix();
			var g = format.GroupSeparator();
			return "1" + g + "234" + g + "567" + r + "90";
		}

		private static Dictionary<NumberFormat, string> GetAngFormats() {
			return Enum.GetValues(typeof(NumberFormat))
				.Cast<NumberFormat>()
				.ToDictionary(x => x, x =>
				   {
					   var r = x.Radix();
					   var g = x.GroupSeparator();
					   return "{radix:'" + r + "',group:'" + g + "'}";
				   });
		}

		public static string Angular(this NumberFormat format)
		{
			return angFormats[format];
		}
	}
}
