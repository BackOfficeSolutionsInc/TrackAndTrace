using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using RadialReview.Models.Frontend;
using System.ComponentModel.DataAnnotations;
using NHibernate;

namespace RadialReview.Crosscutting.EventAnalyzers.Events.Base {
	public abstract class BaseL10EventAnaylzerGenerators : IEventAnalyzerGenerator, IRecurrenceEventAnalyerGenerator {

		protected bool IncludeHistoricalMembers { get; set; }

		[Display(Name = "Meeting")]
		public long RecurrenceId { get; private set; }

		public BaseL10EventAnaylzerGenerators(long recurrenceId, bool includeHistoricalMembers) {
			IncludeHistoricalMembers = includeHistoricalMembers;
			RecurrenceId = recurrenceId;
		}

		//public virtual BaseSearch<List<IHistoricalImpl>> GetAttendeeSearcher(long recurrenceId) {
		//	return new SearchHisoricalRecurrenceAttendees(recurrenceId);
		//}


		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {

			var results = new List<IEventAnalyzer>();
			//var ltRecurrences = await settings.Lookup(GetRecurrenceIdSearcher());
			//foreach (var rid in ltRecurrences) {
			var rid = RecurrenceId;
			var recurrenceAttendees = await settings.Lookup(new SearchHisoricalRecurrenceAttendees(rid));
			foreach (var attendee in recurrenceAttendees.Where(x => IncludeHistoricalMembers || x.DeleteTime == null)) {
				results.Add(EventAnalyzerConstructor(rid, IHistoricalImpl.From(attendee)));
			}
			//}
			return results;
		}
		public abstract IEventAnalyzer EventAnalyzerConstructor(long recurrenceId, IHistoricalImpl attendee);

		public abstract Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings);

		public abstract string Name { get; }

		public abstract string Description { get; }

		public abstract string EventType { get; }

		public abstract Task PreSaveOrUpdate(ISession s);
		
	}
}