using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using System.Threading.Tasks;
using RadialReview.Models.Frontend;
using System.ComponentModel.DataAnnotations;
using RadialReview.Models.L10;
using NHibernate;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class ConsecutiveLateEnds : IEventAnalyzer, IEventAnalyzerGenerator, IRecurrenceEventAnalyerGenerator {
		public ConsecutiveLateEnds(long recurrenceId) {
			RecurrenceId = recurrenceId;
			WeeksInARow = 2;
			MinutesOver = 15;
		}

		[Display(Name = "Meeting")]
		public long RecurrenceId { get; set; }
		public int WeeksInARow { get; set; }
		public decimal MinutesOver { get; set; }

		public string EventType { get { return "ConsecutiveLateEnds"; } }
		public IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.GreaterThan, MinutesOver);
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
			var meetings = await settings.Lookup(new SearchRealL10Meeting(RecurrenceId));
			var pageTimes = await settings.Lookup(new SearchPageTimerSettings(RecurrenceId));
			var expectedDuration = 0m;
			foreach (var p in pageTimes) {
				expectedDuration += p.Minutes;
			}

			//var actualMeetingDurations =

			var evts = EventHelper.ToBinnedEvents(EventFrequency.Weekly, meetings, x => x.StartTime, bin => {
				var durations = bin.Where(x => x.CompleteTime != null)
								  .Select(x => (x.CompleteTime - x.StartTime).Value.Minutes - expectedDuration)
								  .Where(x => x < 90) //Remove all meetings where Failed to conclude. only where overage less that 90 minutes
								  .ToList();
				if (!durations.Any())
					return null;
				return new BaseEvent(durations.Max(), bin.Date);
			});
			return evts;
			//var pageTimes = settings.Lookup(new SearchPageTimerActualsForMeeting(recurrenceId));

		}

		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
			return new[] { this };
		}

		public async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.MinutesOver),
				EditorField.FromProperty(this, x => x.WeeksInARow)
			};
		}

		private string _MeetingName { get; set; }
		public async Task PreSaveOrUpdate(ISession s) {
			_MeetingName = s.Get<L10Recurrence>(RecurrenceId).Name;
		}
		/*
		  ,_MeetingName.NotNull(x=>" for "+x)??"");
		 */

		public string Name {
			get {
				return "Consecutive late meeting ends";
			}
		}
		public string Description {
			get {
				return string.Format("{0} minutes for {1} weeks in a row{2}", LessGreater.GreaterThan.ToDescription(MinutesOver), WeeksInARow, _MeetingName.NotNull(x => " for " + x) ?? "");
			}
		}

		public override bool IsEnabled(IEventSettings settings) {
			return true;
		}
	}
}
