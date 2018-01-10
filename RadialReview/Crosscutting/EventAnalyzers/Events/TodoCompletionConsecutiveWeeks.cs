using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
    public class TodoCompletionConsecutiveWeeks : BaseL10EventAnalyzer {
        public override List<IEvent> EventsForRecurrence(long recurrenceId, IEventSettings settings) {
            var l10Meetings = settings.Lookup(new SearchRealL10Meeting(recurrenceId));
            return EventHelper.ToBinnedEventsFromRatio(EventFrequency.Weekly, l10Meetings, x => x.StartTime,x=>x.TodoCompletion);

        }

        public override IThreshold GetFireThreshold(IEventSettings settings) {
            return new EventThreshold(LessGreater.LessThanOrEqual, 0.5m);
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