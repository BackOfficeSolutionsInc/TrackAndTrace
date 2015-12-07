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
		public AngularScorecard(DayOfWeek weekstart,int timezoneOffset, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores,DateTime? currentWeek,ScorecardPeriod scorecardPeriod,YearStart yearStart) : this()
		{
			Weeks = TimingUtility.GetPeriods(weekstart, timezoneOffset, DateTime.UtcNow, currentWeek.NotNull(x => x.Value.AddDays(7)), scores, true, scorecardPeriod,yearStart).Select(x => new AngularWeek(x)).ToList();
			Measurables = measurables.ToList();
			Scores = scores.Select(x => new AngularScore(x)).ToList();

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

		public AngularScorecard() :base(-1)
		{
			
		}

		public List<AngularMeasurable> Measurables { get; set; }
		public List<AngularScore> Scores { get; set; }
		public List<AngularWeek> Weeks { get; set; }

		public string DateFormat1 { get; set; }
		public string DateFormat2 { get; set; }
	}

}