using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;
using RadialReview.Utilities;

namespace RadialReview.Models.Application
{
	public class YearStart
	{
		public Month Month { get; set; }
		public DateOffset Offset { get; set; }

        public DateTime GetDate(int year, int quarter = 0)
        {
            year = (int)(quarter / 4) +year;
            quarter = quarter % 4;
            return new DateTime(year, (int)Month, 1).AddMonths(quarter*3).AddDateOffset(Offset);
        }

		public YearStart()
		{
			
		}

        public YearStart(OrganizationModel org):this(org.Settings){
        }
        public YearStart(OrganizationModel.OrganizationSettings settings)
        {
            Month = settings.StartOfYearMonth;
            Offset = settings.StartOfYearOffset;
        }
	}
}