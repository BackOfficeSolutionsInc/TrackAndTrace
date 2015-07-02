using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Models.Angular.Scorecard
{
	public class AngularScorecard : BaseAngular
	{
		public AngularScorecard(DayOfWeek weekstart,int timezoneOffset, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores) : this()
		{
			Weeks = TimingUtility.GetWeeks(weekstart,timezoneOffset, DateTime.UtcNow, null, scores,true).Select(x => new AngularWeek(x)).ToList();
			Measurables = measurables.ToList();
			Scores = scores.Select(x => new AngularScore(x)).ToList();
		}

		public AngularScorecard() :base(-1)
		{
			
		}

		public List<AngularMeasurable> Measurables { get; set; }
		public List<AngularScore> Scores { get; set; }
		public List<AngularWeek> Weeks { get; set; }
	}

}