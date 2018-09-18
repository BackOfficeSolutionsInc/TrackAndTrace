using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
	public class SearchAliveMeetingAttendees : BaseSearch<List<L10Meeting.L10Meeting_Attendee>> {

		public long RecurrenceId { get; set; }

		public SearchAliveMeetingAttendees(long recurrenceId) {
			RecurrenceId = recurrenceId;
		}

		public override async Task<List<L10Meeting.L10Meeting_Attendee>> PerformSearch(IEventSettings settings) {
			L10Meeting alias = null;
			return settings.Session.QueryOver<L10Meeting.L10Meeting_Attendee>()
									.JoinAlias(x => x.L10Meeting, () => alias)
									.Where(x => x.DeleteTime == null && alias.L10RecurrenceId == RecurrenceId)
									.List().ToList();
		}

		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
			yield return "" + RecurrenceId;
		}
	}

	public class SearchHistoricalRecurrenceAttendeeIds : BaseSearch<List<L10Recurrence.L10Recurrence_Attendee>> {
		public long RecurrenceId { get; set; }
		public SearchHistoricalRecurrenceAttendeeIds(long recurrenceId) {
			RecurrenceId = recurrenceId;
		}

		public override async Task<List<L10Recurrence.L10Recurrence_Attendee>> PerformSearch(IEventSettings settings) {
			//L10Meeting alias = null;
			return settings.Session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
									//.JoinAlias(x => x.L10Meeting, () => alias)
									.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == RecurrenceId)
									.List().ToList();
									//.Select(x => x.Id, x => x.CreateTime, x => x.DeleteTime)
									//.List<object[]>()
									//.Select(x=> new IHistoricalImpl() {
									//	Id = (long)x[0],
									//	CreateTime = (DateTime) x[1],
									//	DeleteTime = (DateTime?)x[2]
									//}).ToList();
		}

		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
			yield return "" + RecurrenceId;
		}
	}
}