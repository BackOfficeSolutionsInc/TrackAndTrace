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
using static RadialReview.Models.L10.L10Recurrence;

namespace RadialReview.Models.Angular.Scorecard {
	public class AngularScorecard : BaseAngular {
		//public AngularScorecard(long id, DayOfWeek weekstart,int timezoneOffset, IEnumerable<AngularMeetingMeasurable> measurables, List<ScoreModel> scores,DateTime? currentWeek,ScorecardPeriod scorecardPeriod,YearStart yearStart) 
		//    : this(id,weekstart,timezoneOffset,)
		//{

		//}
		public AngularScorecard(long id) : base(id) {

		}
		//public AngularScorecard(long id, DayOfWeek weekstart,int timezoneOffset, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores,DateTime? currentWeek,ScorecardPeriod scorecardPeriod,YearStart yearStart,DateRange range=null,bool includeNextWeek=true,DateTime? now=null) : base(id)
		//      public AngularScorecard(long id, TimeSettings settings, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores, DateTime? currentWeek, DateRange range = null, bool includeNextWeek = true, DateTime? now = null)
		//          : this(id,settings, new List<AngularMeasurableGroup>() { new AngularMeasurableGroup(id, measurables) },scores,currentWeek,range,includeNextWeek,now){
		//}

		public AngularScorecard(long id, TimeSettings settings, IEnumerable<AngularMeasurable> measurables, List<ScoreModel> scores, DateTime? currentWeek, DateRange range = null, bool includeNextWeek = true, DateTime? now = null, bool reverseScorecard = false) : base(id) {
			var cur = currentWeek;
			//if (cur != null)
			//    cur = cur.Value.AddDays(7);

			ScorecardWeekDay = settings.GetTimeSettings().WeekStart;

			Weeks = TimingUtility.GetPeriods(settings, now ?? DateTime.UtcNow, cur, includeNextWeek, true, range: range)
				.Select(x => new AngularWeek(x)).ToList();

			ReverseScorecard = reverseScorecard;
			Measurables = measurables;
			if (scores != null) {
				Scores = scores.Select(x => new AngularScore(x, null, false)).ToList();
			}

			DateFormat1 = TimingUtility.ScorecardFormat1(settings.GetTimeSettings().Period);
			DateFormat2 = TimingUtility.ScorecardFormat2(settings.GetTimeSettings().Period);

			//var allMeasurables = measurableGroups.SelectMany(x => x.Measurables);
			if (scores != null) {
				foreach (var s in Scores) {
					var measurable = Measurables.FirstOrDefault(x => x.Id == s.Measurable.Id);
					if (measurable != null) {
						s.Measurable.Ordering = measurable.Ordering;
						s.Measurable.RecurrenceId = measurable.RecurrenceId;
					}
				}
			}
			if (Measurables != null) {
				var mOrder = new List<AngularMeasurableOrder>();
				foreach (var m in Measurables) {
					if (m.Ordering != null) {
						mOrder.Add(new AngularMeasurableOrder(id, m.Id, m.Ordering.Value));
					}
				}
				MeasurableOrder = mOrder;
			}

			Period = "" + settings.GetTimeSettings().Period;
		}

		public static AngularScorecard Create(long id, TimeSettings settings, IEnumerable<L10Recurrence_Measurable> measurables, List<ScoreModel> scores, DateTime? currentWeek, DateRange range = null, bool includeNextWeek = true, DateTime? now = null, bool reverseScorecard = false) {
			var ms = measurables.Select(x => {
				if (x.IsDivider) {
					var m = AngularMeasurable.CreateDivider(x);
					m.RecurrenceId = x.L10Recurrence.Id;
					return m;
				} else {
					var m = new AngularMeasurable(x.Measurable, false);
					m.Ordering = x._Ordering;
					m.RecurrenceId = x.L10Recurrence.Id;
					return m;
				}
			}).ToList();

			return new AngularScorecard(id, settings, ms, scores, currentWeek, range, includeNextWeek, now, reverseScorecard);
		}

		public AngularScorecard() {
		}

		public IEnumerable<AngularMeasurableOrder> MeasurableOrder { get; set; }

		public IEnumerable<AngularMeasurable> Measurables { get; set; }
		public IEnumerable<AngularScore> Scores { get; set; }
		public IEnumerable<AngularWeek> Weeks { get; set; }

		public bool? ReverseScorecard { get; set; }
		public DayOfWeek? ScorecardWeekDay { get; set; }

		public string Period { get; set; }

		public string DateFormat1 { get; set; }
		public string DateFormat2 { get; set; }
	}

	public class AngularMeasurableOrder : BaseStringAngular {
		public long ScorecardId { get; set; }
		public long MeasurableId { get; set; }
		public int? Ordering { get; set; }
		public AngularMeasurableOrder(long scorecardId,long measurableId,int order) :base(scorecardId+"_"+measurableId) {
			ScorecardId = scorecardId;
			MeasurableId = measurableId;
			Ordering = order;
		}
	}
}