using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularWeek : BaseAngular
	{
		public AngularWeek(L10MeetingVM.WeekVM week):base(week.ForWeek.Ticks)
		{
			ForWeek = week.ForWeek;
			ForWeekNumber = TimingUtility.GetWeekSinceEpoch(week.ForWeek);
			DisplayDate = week.DisplayDate;
			IsCurrentWeek = week.IsCurrentWeek;
		}
		public long ForWeekNumber { get; set; }
		public DateTime ForWeek { get; set; }
		public DateTime DisplayDate { get; set; }
		public bool IsCurrentWeek { get; set; }

	}
}