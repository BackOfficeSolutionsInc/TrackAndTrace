using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
	public class SearchL10RecurrenceIds : ISearch<List<long>> {
		public List<long> PerformSearch(IEventSettings settings) {
			return settings.Session.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null && x.TeamType == L10TeamType.LeadershipTeam && x.OrganizationId == settings.OrganizationId)
				.Select(x => x.Id)
				.List<long>().ToList();
		}

		public string UniqueKey(IEventSettings settings) {
			return "RealL10MeetingByOrgId_" + settings.OrganizationId;
		}
	}
}