using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
    
    public class SearchHisoricalRecurrenceAttendees: BaseSearch<List<L10Recurrence.L10Recurrence_Attendee >> {
        public long RecurrenceId { get; set; }

        public SearchHisoricalRecurrenceAttendees(long recurrenceId) {
            RecurrenceId = recurrenceId;
        }

        public override List<L10Recurrence.L10Recurrence_Attendee> PerformSearch(IEventSettings settings) {
            return settings.Session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                                    .Where(x=>x.L10Recurrence.Id==RecurrenceId)
                                    .List().ToList();
        }

        protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
            yield return "" + RecurrenceId;
        }
    }
}