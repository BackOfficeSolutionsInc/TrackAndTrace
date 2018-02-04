using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
	public class SearchL10RecurrenceIds : BaseSearch<List<long>> {

        public override async Task<List<long>> PerformSearch(IEventSettings settings) {
			return settings.Session.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null && x.OrganizationId == settings.OrganizationId)
				.Select(x => x.Id)
				.List<long>().ToList();
		}

		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
			yield return ""+settings.OrganizationId;
		}        
    }
}