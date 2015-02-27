using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Issues;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.L10.VM
{
	public class L10MeetingVM
	{
		public L10Recurrence Recurrence { get; set; }
		public L10Meeting Meeting { get; set; }
		public List<ScoreModel> Scores { get; set; }
		public List<IssueModel> Issues { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }

		public class WeekVM
		{
			public DateTime DisplayDate { get; set; }
			public DateTime ForWeek { get; set; }
			public bool IsCurrentWeek { get; set; }
		}

		public List<WeekVM> Weeks { get; set; }

		public DateTime? MeetingStart
		{
			get
			{
				if (Meeting != null)
				{
					if (Meeting.StartTime != null)
						return Meeting.StartTime.Value;//.AddMinutes(Recurrence.Organization.Settings.TimeZoneOffsetMinutes);
				}
				return null;
			}
		}

		public long[] Attendees { get; set; }


		public L10MeetingVM()
		{
			StartDate = DateTime.UtcNow;
			EndDate = DateTime.UtcNow;
			Weeks = new List<WeekVM>();
		}


	}
}