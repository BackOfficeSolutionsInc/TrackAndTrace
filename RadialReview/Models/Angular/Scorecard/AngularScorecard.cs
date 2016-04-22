using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Application;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Models.Angular.Scorecard
{
	public class AngularScorecard : BaseAngular
	{
        //public AngularScorecard(long id, DayOfWeek weekstart,int timezoneOffset, IEnumerable<AngularMeetingMeasurable> measurables, List<ScoreModel> scores,DateTime? currentWeek,ScorecardPeriod scorecardPeriod,YearStart yearStart) 
        //    : this(id,weekstart,timezoneOffset,)
        //{

        //}
        public AngularScorecard(long id): base(id)
        {

        }
		public AngularScorecard(long id, DayOfWeek weekstart,int timezoneOffset, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores,DateTime? currentWeek,ScorecardPeriod scorecardPeriod,YearStart yearStart) : base(id)
		{
			Weeks = TimingUtility.GetPeriods(weekstart, timezoneOffset, DateTime.UtcNow, currentWeek.NotNull(x => x.Value.AddDays(7)), /*scores,*/ true, scorecardPeriod,yearStart)
                .Select(x => new AngularWeek(x)).ToList();
			Measurables = measurables.ToList();
			Scores = scores.Select(x => new AngularScore(x,false)).ToList();

			DateFormat1 = TimingUtility.ScorecardFormat1(scorecardPeriod);
			DateFormat2 = TimingUtility.ScorecardFormat2(scorecardPeriod);

			foreach (var s in Scores){
				var found = Measurables.FirstOrDefault(x => x.Id == s.Measurable.Id);
				if (found != null){
					s.Measurable.Ordering = found.Ordering;
					s.Measurable.RecurrenceId = found.RecurrenceId;
				}

			}
		}

		public AngularScorecard()
		{
		}

		public IEnumerable<AngularMeasurable> Measurables { get; set; }
        public IEnumerable<AngularScore> Scores { get; set; }
        public IEnumerable<AngularWeek> Weeks { get; set; }

		public string DateFormat1 { get; set; }
		public string DateFormat2 { get; set; }
	}

}