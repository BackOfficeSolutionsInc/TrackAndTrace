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
using RadialReview.Models.Frontend;
using NHibernate;
using RadialReview.Models.L10;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {

	public class MemberwiseTodoCompletionGenerator : BaseL10EventAnaylzerGenerators {

		public MemberwiseTodoCompletionGenerator(long recurrenceId) : base(recurrenceId, false) {
			Direction = LessGreater.LessThanOrEqual;
			Percentage = 50;
			WeeksInARow = 2;
		}

		public LessGreater Direction { get; set; }
		public decimal Percentage { get; set; }
		public int WeeksInARow { get; private set; }

		public override IEventAnalyzer EventAnalyzerConstructor(long recurrenceId, IHistoricalImpl attendee) {
			return new MemberwiseTodoCompletion(recurrenceId, attendee, Direction, Percentage,WeeksInARow);
		}
		private string _MeetingName { get; set; }
		public override async Task PreSaveOrUpdate(ISession s) {
			_MeetingName = s.Get<L10Recurrence>(RecurrenceId).Name;
		}
		
		public override string Name {
			get {
				return "Memberwise to-do completion percentage";
			}
		}

		public override string Description {
			get {
				return string.Format("{0} for {1} weeks in a row{2}", Direction.ToDescription(Percentage),WeeksInARow,_MeetingName.NotNull(x=>" for "+x)??"");
			}
		}

		public override async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			//todo EditorField
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.Direction),
				EditorField.FromProperty(this,x=>x.Percentage),
				EditorField.FromProperty(this,x=>x.WeeksInARow),
			};
		}

		public override string EventType { get { return "MemberwiseTodoCompletion"; } }
	}

	public class MemberwiseTodoCompletion : IEventAnalyzer, IRecurrenceEventAnalyerGenerator {
		public long RecurrenceId { get; set; }
		public IHistoricalImpl AttendeeUser { get; set; }
		public decimal Percentage { get; set; }
		public int WeeksInARow { get; private set; }
		public LessGreater Direction { get; set; }

		public MemberwiseTodoCompletion(long recurrenceId, IHistoricalImpl attendeeUser, LessGreater direction,decimal percentage,int weeksInARow) {
			RecurrenceId = recurrenceId;
			AttendeeUser = attendeeUser;
			Percentage = percentage;
			WeeksInARow = weeksInARow;
			Direction = direction;
		}


		/// <summary>
		/// Gives the todo completion % for meeting whose event time coinsides with the complete time.
		/// </summary>
		/// <param name="settings"></param>
		/// <returns></returns>
		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var todos = (await settings.Lookup(new SearchAllTodosForRecurrence(RecurrenceId)))
								.Where(x => x.AccountableUserId == AttendeeUser.Id && x.DeleteTime == null)
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
					results.Add(new BaseEvent(inRange.Count(x => x.CompleteTime != null) / inRange.Count(), end));
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
			return new EventThreshold(Direction, Percentage/100m);
		}

		public int GetNumberOfFailsToTrigger(IEventSettings settings) {
			return WeeksInARow;
		}

		public int GetNumberOfPassesToReset(IEventSettings settings) {
			return 1;
		}

		public bool IsEnabled(IEventSettings settings) {
			return true;
		}

		//public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
		//	return new[] { this };
		//}

		//public Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
		//	//todo EditorField
		//	throw new NotImplementedException();
		//}

	}
}