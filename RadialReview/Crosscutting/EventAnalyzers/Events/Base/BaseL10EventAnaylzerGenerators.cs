using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Events.Base {
	public abstract class BaseL10EventAnaylzerGenerators : IEventAnalyzerGenerator {

		protected bool LeadershipTeamOnly { get; set; }
		protected bool IncludeHistoricalMembers { get; set; }

		public BaseL10EventAnaylzerGenerators(bool leadershipTeamOnly, bool includeHistoricalMembers) {
			LeadershipTeamOnly = leadershipTeamOnly;
			IncludeHistoricalMembers = includeHistoricalMembers;
		}

		public virtual BaseSearch<List<long>> GetRecurrenceIdSearcher() {
			if (LeadershipTeamOnly)
				return new SearchLeadershipL10RecurrenceIds();
			else
				return new SearchL10RecurrenceIds();
		}

		//public virtual BaseSearch<List<IHistoricalImpl>> GetAttendeeSearcher(long recurrenceId) {
		//	return new SearchHisoricalRecurrenceAttendees(recurrenceId);
		//}


		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {

			var results = new List<IEventAnalyzer>();
			var ltRecurrences = await settings.Lookup(GetRecurrenceIdSearcher());
			foreach (var rid in ltRecurrences) {
				var recurrenceAttendees = await settings.Lookup(new SearchHisoricalRecurrenceAttendees(rid));
				foreach (var attendee in recurrenceAttendees.Where(x=>IncludeHistoricalMembers || x.DeleteTime ==null)) {
					results.Add(EventAnalyzerConstructor(rid,IHistoricalImpl.From(attendee)));
				}
			}
			return results;
		}
		public abstract IEventAnalyzer EventAnalyzerConstructor(long recurrenceId, IHistoricalImpl attendee);
	}
}