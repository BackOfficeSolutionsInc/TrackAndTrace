using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
    public class SearchMeetingAttendees : BaseSearch<List<L10Meeting.L10Meeting_Attendee>> {

        public long RecurrenceId { get; set; }

        public SearchMeetingAttendees(long recurrenceId) {
            RecurrenceId = recurrenceId;
        }
        
        public override List<L10Meeting.L10Meeting_Attendee> PerformSearch(IEventSettings settings) {
            L10Meeting alias = null;
            return settings.Session.QueryOver<L10Meeting.L10Meeting_Attendee>()
                                    .JoinAlias(x => x.L10Meeting, () => alias)
                                    .Where(x => x.DeleteTime == null && alias.L10RecurrenceId == RecurrenceId)
                                    .List().ToList();
        }

        protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
            yield return "" + RecurrenceId;
        }
    }
}