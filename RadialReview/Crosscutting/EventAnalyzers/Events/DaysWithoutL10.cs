using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using RadialReview.Models.L10;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Enums;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class DaysWithoutL10 : BaseLeadershipTeamL10EventAnalyzer {
		
		public override EventFrequency GetExecutionFrequency() {
			return EventFrequency.Weekly;
		}
		

		public override int GetNumberOfFailsToTrigger(IEventSettings settings) {
			return 15; // Days
		}
		

		public override int GetNumberOfPassesToReset(IEventSettings settings) {
			return 1; //Days
		}

		public override bool IsEnabled(IEventSettings settings) {
			return true;
		}

		public override IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.LessThan, 1);
		}
		
		public override List<IEvent> EventsForRecurrence(long recurrenceId, IEventSettings settings) {			
			var l10Meetings = settings.Lookup(new SearchRealL10Meeting(recurrenceId));			
			return EventHelper.ToHistogram(EventFrequency.Daily, l10Meetings, x => x.StartTime);			
		}
	}
}