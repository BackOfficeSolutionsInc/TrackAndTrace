using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {


	public class LTMissL10PastQuarterGenerator : BaseL10EventAnaylzerGenerators {
		public LTMissL10PastQuarterGenerator() : base(true,true) {}

		public override IEventAnalyzer EventAnalyzerConstructor(long recurrenceId, IHistoricalImpl attendee) {
			return new LTMissL10PastQuarter(IHistoricalImpl.From(attendee), recurrenceId);
		}
	}

	public class LTMissL10PastQuarter : IEventAnalyzer {

		public IHistoricalImpl AttendeeUser { get; set; }
		public long RecurrenceId { get; set; }
		public TimeSpan Range { get; set; }
		public int NumberMissed { get; set; }

		public LTMissL10PastQuarter(IHistoricalImpl attendeeUser,long recurrenceId) :base() {
			AttendeeUser = attendeeUser;
			RecurrenceId = recurrenceId;
			Range = TimeSpan.FromDays(13 * 7);
			NumberMissed = 3;
		}

        public EventFrequency GetExecutionFrequency() {
            return EventFrequency.Quarterly;
        }

        public IThreshold GetFireThreshold(IEventSettings settings) {
            return new EventThreshold(LessGreater.GreaterThan, NumberMissed);
        }

        public int GetNumberOfFailsToTrigger(IEventSettings settings) {
            return 1;
        }

        public int GetNumberOfPassesToReset(IEventSettings settings) {
            return 0;
        }

        public bool IsEnabled(IEventSettings settings) {
            return true;
        }

		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var meetings = (await settings.Lookup(new SearchRealL10Meeting(RecurrenceId)))
				.Where(x=>x.CompleteTime!=null && x.CompleteTime > settings.RunTime.Add(-Range))
				.Where(x=>x.StartTime !=null && AttendeeUser.AliveAt(x.StartTime.Value))
				.ToList();

			var meetingsAttended = (await settings.Lookup(new SearchAliveMeetingAttendees(RecurrenceId)))
				.Where(x=>x.UserId == AttendeeUser.Id)
				.ToList();

			return new[] { new BaseEvent(meetings.Count() - meetingsAttended.Count(), settings.RunTime) };
		}
	}
}