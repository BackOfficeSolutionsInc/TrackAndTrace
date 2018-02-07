using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
	/// <summary>
	/// Real Means it's not a preview...
	/// </summary>
	public class SearchRealL10Meeting : BaseSearch<List<L10Meeting>> {

		private long RecurrenceId { get; set; }

		public SearchRealL10Meeting(long recurrenceId) {
			RecurrenceId = recurrenceId;
		}

        protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
           yield return "" + RecurrenceId;
		}

        public override async Task<List<L10Meeting>> PerformSearch(IEventSettings settings) {
			return settings.Session.QueryOver<L10Meeting>()
				.Where(x => x.DeleteTime == null && x.Preview == false && x.L10RecurrenceId == RecurrenceId)
				.List().ToList();
		}
	}
}