using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Application;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;

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
		//public AngularScorecard(long id, DayOfWeek weekstart,int timezoneOffset, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores,DateTime? currentWeek,ScorecardPeriod scorecardPeriod,YearStart yearStart,DateRange range=null,bool includeNextWeek=true,DateTime? now=null) : base(id)
        public AngularScorecard(long id, TimeSettings settings, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores, DateTime? currentWeek, DateRange range = null, bool includeNextWeek = true, DateTime? now = null)
            : base(id)
		{
            var cur = currentWeek;
            if (cur != null)
                cur = cur.Value.AddDays(7);

            Weeks = TimingUtility.GetPeriods(settings, now ?? DateTime.UtcNow, cur, includeNextWeek, range: range)
                .Select(x => new AngularWeek(x)).ToList();
			Measurables = measurables.ToList();
			Scores = scores.Select(x => new AngularScore(x,false)).ToList();

            DateFormat1 = TimingUtility.ScorecardFormat1(settings.GetTimeSettings().Period);
            DateFormat2 = TimingUtility.ScorecardFormat2(settings.GetTimeSettings().Period);

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