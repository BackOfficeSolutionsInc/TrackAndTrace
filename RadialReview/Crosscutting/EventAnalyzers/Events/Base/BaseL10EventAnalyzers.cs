using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.L10;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.EventAnalyzers.Events.Base {
	//public abstract class BaseL10EventAnalyzer : IEventAnalyzer, IEventAnalyzerGenerator {

	//       public virtual BaseSearch<List<long>> GetRecurrenceIdSearcher() {
	//           return new SearchL10RecurrenceIds();
	//       }

	//       public abstract EventFrequency GetExecutionFrequency();

	//	public abstract int GetNumberOfFailsToTrigger(IEventSettings settings);

	//	public abstract int GetNumberOfPassesToReset(IEventSettings settings);

	//	public abstract IThreshold GetFireThreshold(IEventSettings settings);

	//	public abstract bool IsEnabled(IEventSettings settings);

	//	public abstract Task<List<IEvent>> EventsForRecurrence(long recurrenceId, IEventSettings settings);

	//	public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
	//		var s = settings.Session;
	//		var recurIds = await settings.DataSearch.Lookup(GetRecurrenceIdSearcher());

	//		var join = new List<IEvent>();
	//		foreach (var r in recurIds) {
	//			join.AddRange(await EventsForRecurrence(r, settings));
	//		}

	//		return join;
	//	}

	//	public virtual async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings orgId) {
	//		return new List<IEventAnalyzer>() { this }; 
	//	}
	//}

	//   public abstract class BaseLeadershipTeamL10EventAnalyzer: BaseL10EventAnalyzer {
	//       public override BaseSearch<List<long>> GetRecurrenceIdSearcher() {
	//           return new SearchLeadershipL10RecurrenceIds();
	//       }
	//   }

}
