using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class AverageMeetingRatingBelowForWeeksInARow : BaseL10EventAnalyzer {

		public override bool IsEnabled(IEventSettings settings) {
			return true;
		}

		public override IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.LessThanOrEqual, 7);
		}

		public override EventFrequency GetExecutionFrequency() {
			return EventFrequency.Weekly;
		}

		public override int GetNumberOfFailsToTrigger(IEventSettings settings) {
			return 2;
		}

		public override int GetNumberOfPassesToReset(IEventSettings settings) {
			return 1;
		}

		public override List<IEvent> EventsForRecurrence(long recurrenceId, IEventSettings settings) {
			var meetings = settings.Lookup(new Searchers.SearchRealL10Meeting(recurrenceId));
			return EventHelper.ToBinnedEventsFromRatio(EventFrequency.Weekly, meetings, x => x.StartTime, x => x.AverageMeetingRating);

			//var bins = EventHelper.ToBins(EventFrequency.Weekly, meetings, x => x.StartTime, x=>x.AverageMeetingRating);

			//var events = bins.Select(x => {
			//    var ratio = x.Aggregate(new Ratio(), (i, r) => i.Add(r));
			//    if (!ratio.IsValid())
			//        return null;
			//    return (IEvent) new BaseEvent(ratio.GetValue(null), x.Date);
			//}).Where(x=>x!=null).ToList();

			//return events;
			//var byWeeks = meetings.Where(x => x.StartTime.HasValue && x.AverageMeetingRating!=null).GroupBy(x => TimingUtility.GetWeekSinceEpoch(x.StartTime.Value));

			//var weekRatings = new List<Ratio>();
			//foreach(var w in byWeeks) {
			//    var num = w.Sum(x => x.AverageMeetingRating.Numerator);
			//    var den = w.Sum(x => x.AverageMeetingRating.Denominator);
			//    weekRatings.Add(new Ratio(num,den));
			//}
			////var weekRatings = byWeeks.Select(x=>x.)


			//  return meetings.Select(x=>x.AverageMeetingRating.GetValue(null)).Where(x=>x!=null)

		}

	}
}
