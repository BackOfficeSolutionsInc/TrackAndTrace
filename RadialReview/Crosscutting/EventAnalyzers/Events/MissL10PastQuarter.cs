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
            var meetings = settings.Lookup(new SearchRealL10Meeting(recurrenceId));
            var meetings = settings.Lookup(new SearchRealL10Meeting(recurrenceId));
            var ids = meetings.Where(x => x.CreateTime > settings.LastCheck.AddDays(-13 * 7)).Select(x=>x.Id).ToList();
            if (!ids.Any())
                return new List<IEvent>();
            var minId = ids.Min();
            attendees.Where(x => x.Id >= ids);



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