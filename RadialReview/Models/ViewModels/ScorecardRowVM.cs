using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.ViewModels
{
	public class ScorecardRowVM
	{
		public long MeetingId { get; set; }
		public long RecurrenceId { get; set; }
		public List<L10MeetingVM.WeekVM> Weeks { get; set; }
		public List<ScoreModel> Scores { get; set; }
		public L10Meeting.L10Meeting_Measurable MeetingMeasurable { get; set; }
		
		public bool IsDivider { get; set; }
        public bool ShowAdmin { get;set; }
        public bool ShowScorecardChart { get; set; }
    }
}