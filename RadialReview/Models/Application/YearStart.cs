using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Application
{
	public class YearStart
	{
		public Month Month { get; set; }
		public DateOffset Offset { get; set; }

		public YearStart()
		{
			
		}

		public YearStart(OrganizationModel org)
		{
			Month = org.Settings.StartOfYearMonth;
			Offset = org.Settings.StartOfYearOffset;
		}
	}
}