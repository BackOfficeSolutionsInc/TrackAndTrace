using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using RadialReview.Models.L10;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Enums;
using System.Threading.Tasks;
using RadialReview.Models.Frontend;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class DaysWithoutL10 : IEventAnalyzer,IEventAnalyzerGenerator, IRecurrenceEventAnalyerGenerator {
		public DaysWithoutL10(long recurrenceId) {
			RecurrenceId = recurrenceId;
			Days = 15;
		}

		[Display(Name = "Meeting")]
		public long RecurrenceId { get; set; }
		public string EventType { get { return "DaysWithoutL10"; } }
		public int Days { get; set; }

		public EventFrequency GetExecutionFrequency() {
			return EventFrequency.Weekly;
		}
		

		public int GetNumberOfFailsToTrigger(IEventSettings settings) {
			return Days; // Days
		}
		

		public int GetNumberOfPassesToReset(IEventSettings settings) {
			return 1; //Days
		}

		public bool IsEnabled(IEventSettings settings) {
			return true;
		}

		public IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.LessThan, 1);
		}
		
		//public async Task<List<IEvent>> EventsForRecurrence(long recurrenceId, IEventSettings settings) {	
		//}

		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var l10Meetings = await settings.Lookup(new SearchRealL10Meeting(RecurrenceId));
			return EventHelper.ToHistogram(EventFrequency.Daily, l10Meetings, x => x.StartTime);
		}

		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
			return new[] { this };
		}

		public async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			//todo EditorField
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.Days)
			};
		}

		public string GetFriendlyName() {
			return "Days without a Level 10";
		}
	}
}