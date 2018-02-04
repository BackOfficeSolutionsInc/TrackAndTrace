using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Accessors;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {

	public class LTZeroPercentTodoCompletionGenerator : BaseL10EventAnaylzerGenerators {

		public LTZeroPercentTodoCompletionGenerator() : base(true, false) { }

		public override IEventAnalyzer EventAnalyzerConstructor(long recurrenceId, IHistoricalImpl attendee) {
			return new LTZeroPercentTodoCompletion(recurrenceId, attendee);
		}
	}

	public class LTZeroPercentTodoCompletion : IEventAnalyzer {
		public long RecurrenceId { get; set; }
		public IHistoricalImpl AttendeeUser { get; set; }

		public LTZeroPercentTodoCompletion(long recurrenceId, IHistoricalImpl attendeeUser) {
			RecurrenceId = recurrenceId;
			AttendeeUser = attendeeUser;
		}


		/// <summary>
		/// Gives the todo completion % for meeting whose event time coinsides with the complete time.
		/// </summary>
		/// <param name="settings"></param>
		/// <returns></returns>
		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var todos = (await settings.Lookup(new SearchAllTodosForRecurrence(RecurrenceId)))
								.Where(x => x.AccountableUserId == AttendeeUser.Id && x.DeleteTime==null)
								.ToList();

			var meetingCompleteTimes = (await settings.Lookup(new SearchRealL10Meeting(RecurrenceId)))
								.Where(x => x.CompleteTime != null)
								.OrderBy(x => x.CompleteTime)
								.Select(x => x.CompleteTime.Value)
								.ToList();

			var results = new List<IEvent>(); 
			for (var i = 1; i < meetingCompleteTimes.Count; i++) {
				var start = meetingCompleteTimes[i - 1];
				var end = meetingCompleteTimes[i];

				var inRange = todos.Where(x => x.DueDate.IsBetween(start, end)).ToList();
				if (inRange.Any()) {
					results.Add(new BaseEvent(inRange.Count(x => x.CompleteTime != null)/inRange.Count(), end));
				} else {
					//add placeholder event, they were fine this week
					results.Add(new BaseEvent(1, end));
				}
			}
			return results;
		}

		public EventFrequency GetExecutionFrequency() {
			return EventFrequency.Weekly;
		}

		public IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.LessThanOrEqual, 0);
		}

		public int GetNumberOfFailsToTrigger(IEventSettings settings) {
			return 2;
		}

		public int GetNumberOfPassesToReset(IEventSettings settings) {
			return 1;
		}

		public bool IsEnabled(IEventSettings settings) {
			return true;
		}
	}
}