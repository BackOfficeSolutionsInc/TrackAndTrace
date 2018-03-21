using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class ConsecutiveLateEnds : BaseL10EventAnalyzer {
		public override List<IEvent> EventsForRecurrence(long recurrenceId, IEventSettings settings) {
			var meetings = settings.Lookup(new SearchRealL10Meeting(recurrenceId));
			var pageTimes = settings.Lookup(new SearchPageTimerSettings(recurrenceId));
			var expectedDuration = 0m;
			foreach (var p in pageTimes) {
				expectedDuration += p.Minutes;
			}

			//var actualMeetingDurations =

			var evts = EventHelper.ToBinnedEvents(EventFrequency.Weekly, meetings, x => x.StartTime, bin => {
				var durations = bin.Where(x => x.CompleteTime != null)
								  .Select(x => (x.CompleteTime - x.StartTime).Value.Minutes - expectedDuration)
								  .Where(x => x < 90) //Remove all meetings where Failed to conclude.
								  .ToList();
				if (!durations.Any())
					return null;
				return new BaseEvent(durations.Max(), bin.Date);
			});
			return evts;
			//var pageTimes = settings.Lookup(new SearchPageTimerActualsForMeeting(recurrenceId));

		}

		public override IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.GreaterThan, 15);
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