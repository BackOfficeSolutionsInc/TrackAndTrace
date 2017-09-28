using NHibernate;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Hooks.Realtime {
	public static class RealTimeHelpers {

		[Untested("Supply the connection string in a way that SQS can access.")]
		public static string GetConnectionString() {
			return null;
		}

		[Untested("make sure the querys are working correctly")]
		public static Rock_Data GetRecurrenceRockData(ISession s, long rockId) {
			var rockRecurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
										.Where(x => x.DeleteTime == null && x.ForRock.Id == rockId)
										.Select(x => x.L10Recurrence.Id, x => x.Id)
										.Future<object[]>()
										.Select(x => new Rock_RecurId() {
											RecurrenceId = (long)x[0],
											RecurrenceRockId = (long)x[1]
										});

			L10Recurrence recurA = null;
			var rockMeetingIds = s.QueryOver<L10Meeting.L10Meeting_Rock>()
										.JoinAlias(x => x.ForRecurrence, () => recurA)
										.Where(x => x.DeleteTime == null && x.ForRock.Id == rockId && recurA.MeetingInProgress == x.L10Meeting.Id)
										.Select(x => x.ForRecurrence.Id, x => x.Id)
										.Future<object[]>()
										.Select(x => new Rock_MeetingId() {
											RecurrenceId = (long)x[0],
											MeetingRockId = (long)x[1]
										});

			return new Rock_Data {
				MeetingData = rockMeetingIds,
				RecurData = rockRecurrenceIds,
			};
		}

		public class Rock_RecurId {
			public long RecurrenceId { get; internal set; }
			public long RecurrenceRockId { get; internal set; }
		}
		public class Rock_MeetingId {
			public long RecurrenceId { get; internal set; }
			public long MeetingRockId { get; internal set; }
			//public long MeetingId { get; internal set; }
		}

		public class Rock_Data {
			public IEnumerable<Rock_RecurId> RecurData { get; set; }
			public IEnumerable<Rock_MeetingId> MeetingData { get; set; }

			public List<long> GetRecurrenceIds() {
				return RecurData.Select(x => x.RecurrenceId).ToList();
			}
		}

	}
}