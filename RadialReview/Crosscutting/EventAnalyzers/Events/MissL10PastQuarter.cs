using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
    public class MissL10PastQuarter : BaseLeadershipTeamL10EventAnalyzer {
        public override List<IEvent> EventsForRecurrence(long recurrenceId, IEventSettings settings) {
            var attendees = settings.Lookup(new SearchMeetingAttendees(recurrenceId));
            


        }

        public override EventFrequency GetExecutionFrequency() {
            return EventFrequency.Weekly;
        }

        public override IThreshold GetFireThreshold(IEventSettings settings) {
            return new EventThreshold(LessGreater.GreaterThan, 3);
        }

        public override int GetNumberOfFailsToTrigger(IEventSettings settings) {
            return 1;
        }

        public override int GetNumberOfPassesToReset(IEventSettings settings) {
            return 3;
        }

        public override bool IsEnabled(IEventSettings settings) {
            return false;
        }
    }
}