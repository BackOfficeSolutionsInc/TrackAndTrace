using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Utilities.DataTypes;
using System.Threading.Tasks;
using RadialReview.Models.Frontend;
using System.ComponentModel.DataAnnotations;
using NHibernate;
using RadialReview.Models.L10;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {

	public class TodoCompletionConsecutiveWeeks : IEventAnalyzer, IEventAnalyzerGenerator, IRecurrenceEventAnalyerGenerator {


		public TodoCompletionConsecutiveWeeks(long recurrenceId) {
			RecurrenceId = recurrenceId;
			Percentage = 50;
			Direction = LessGreater.LessThanOrEqual;
			WeeksInARow = 2;
		}

		//public override async Task<List<IEvent>> EventsForRecurrence(long recurrenceId, IEventSettings settings) {
		//}

		[Display(Name = "Meeting")]
		public long RecurrenceId { get; set; }

		public int WeeksInARow { get; set; }
		public decimal Percentage { get; set; }
		public LessGreater Direction { get; set; }

		public string EventType { get { return "TodoCompletionConsecutiveWeeks"; } }

		public IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(Direction, Percentage / 100.0m);
		}

		public EventFrequency GetExecutionFrequency() {
			return EventFrequency.Weekly;
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

		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var l10Meetings = await settings.Lookup(new SearchRealL10Meeting(RecurrenceId));
			return EventHelper.ToBinnedEventsFromRatio(EventFrequency.Weekly, l10Meetings, x => x.StartTime, x => x.TodoCompletion);
		}

		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
			return new[] { this };
		}

		public async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			//todo EditorField
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.Direction),
				EditorField.FromProperty(this,x=>x.Percentage),
				EditorField.FromProperty(this,x=>x.WeeksInARow),
			};
		}

		private string _MeetingName { get; set; }
		public async Task PreSaveOrUpdate(ISession s) {
			_MeetingName = s.Get<L10Recurrence>(RecurrenceId).Name;
		}

		public string Name {
			get {
				return "Aggregate to-do completion percentage";
			}
		}

		public string Description {
			get {
				return string.Format("{0} for {1} weeks in a row{2}", Direction.ToDescription(Percentage), WeeksInARow, _MeetingName.NotNull(x => " for " + x) ?? "");
			}
		}
	}
}
