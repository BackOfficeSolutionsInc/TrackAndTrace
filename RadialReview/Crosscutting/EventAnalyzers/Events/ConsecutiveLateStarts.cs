using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class ConsecutiveLateStarts : BaseL10EventAnalyzer {
		public override List<IEvent> EventsForRecurrence(long recurrenceId, IEventSettings settings) {
			var l10Meetings = settings.Lookup(new SearchRealL10Meeting(recurrenceId));

			var divisor = 15m;
			//var startTimes = l10Meetings.Where(x => x.StartTime.HasValue).Select(x => x.StartTime.Value.Minute).ToList();
			//if (startTimes.Any()) {
			//    //var hourAvg = startTimes.Select(x => ((x + 30) % 60) - 30).Average();
			//    //var thirtyAvg = startTimes.Select(x => ((x + 15) % 30) - 15).Average();
			//    var fifteenAvg = startTimes.Select(x => ((x + 7.5) % 15) - 7.5).Average();
			//    var tenAvg = startTimes.Select(x => ((x + 5) % 10) - 5).Average();

			//    if (Math.Abs(fifteenAvg) < Math.Abs(tenAvg)) {
			//        divisor = 15m;
			//    }
			//}

			var evts = EventHelper.ToBinnedEvents(
				EventFrequency.Weekly,
				l10Meetings,
				x => x.StartTime,
				x => {
					if (!x.Any())
						return null;
					var minutes = x.Max(y => ((y.StartTime.Value.Minute + divisor / 2.0m) % divisor) - divisor / 2.0m);
					return new BaseEvent(minutes, x.Date);
				}
			);

			return evts;
		}

		public override IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.GreaterThan, 5);
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

		public override bool IsEnabled(IEventSettings settings) {
			return true;
		}
	}
}