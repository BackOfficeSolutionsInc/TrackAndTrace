using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Askables;
using RadialReview.Models.Issues;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using System.Runtime.Serialization;
using RadialReview.Utilities;

namespace RadialReview.Models.L10.VM
{
	public class L10MeetingVM
	{
		public L10Recurrence Recurrence { get; set; }
		public L10Meeting Meeting { get; set; }
		public List<ScoreModel> Scores { get; set; }
		public List<IssueModel.IssueModel_Recurrence> Issues { get; set; }
		public List<TodoModel> Todos { get; set; }
		public List<L10Meeting.L10Meeting_Rock> Rocks { get; set; }

		public ScorecardPeriod ScorecardType { get; set; }

		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }

		public bool CanEdit { get; set; }
		public bool CanAdmin { get; set; }

        public long VtoId { get;set;}

		[DataContract]
		public class WeekVM{

			public DateTime DisplayDate { get; set; }
			[DataMember(Name = "EndDate")]
			public DateTime DataContract_EndDate { get { return StartDate.AddDays(7); } }
			[DataMember(Name = "ForWeek")]
			public long DataContract_Weeks { get { return TimingUtility.GetWeekSinceEpoch(ForWeek); } }

			[DataMember(Name="StartDate")]
            public DateTime StartDate { get; set; }
			public DateTime ForWeek { get; set; }
			public bool IsCurrentWeek { get; set; }
			public int NumPeriods { get; set; }

			public WeekVM()
			{
				NumPeriods = 1;
			}

        }

		public List<WeekVM> Weeks { get; set; }

		public DateTime? MeetingStart
		{
			get
			{
				if (Meeting != null){
					if (Meeting.StartTime != null)
						return Meeting.StartTime.Value; //.AddMinutes(Recurrence.Organization.Settings.TimeZoneOffsetMinutes);
				}
				return null;
			}
		}

		public string HeadlinesId { get; set; }

		public long[] Attendees { get; set; }
		public bool SendEmail { get; set; }

        public bool ShowAdmin { get; set; }
        public bool ShowScorecardChart { get; set; }

		public bool EnableTranscript { get; set; }

		public List<MeetingTranscriptVM> CurrentTranscript { get; set; }

		public L10MeetingVM()
		{
			StartDate = DateTime.UtcNow;
			EndDate = DateTime.UtcNow;
			Weeks = new List<WeekVM>();
			CurrentTranscript=new List<MeetingTranscriptVM>();
		}

		//public bool AutoPrioritize { get; set; }
	}

	public class MeetingTranscriptVM
	{
		public long Id { get; set; }
		public string Message { get; set; }
		public string Owner { get; set; }

		public long Order { get; set; }
	}
}