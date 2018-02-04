using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Events;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers {
	public class EventsFactory {

		public static async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(ISession s, IEventSettings settings) {
			var generators = new List<IEventAnalyzerGenerator>();
			generators.Add(new AverageMeetingRatingBelowForWeeksInARow());
			generators.Add(new ConsecutiveLateStarts());
			generators.Add(new ConsecutiveLateEnds());
			generators.Add(new DaysWithoutL10());
			generators.Add(new LTMissL10PastQuarterGenerator());
			generators.Add(new TodoCompletionConsecutiveWeeks());

			var analyzers = new List<IEventAnalyzer>();
			foreach (var g in generators) {
				foreach (var a in await g.GenerateAnalyzers(settings)) {
					analyzers.Add(a);
				}
			}
			return analyzers;
			//return generators.SelectMany<IEventAnalyzerGenerator,Task<IEventAnalyzer>>(async x => await x.GenerateAnalyzers(settings)).ToList();
		}		
	}
}