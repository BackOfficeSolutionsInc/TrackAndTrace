using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using RadialReview.Accessors;

namespace RadialReview.Hooks.Meeting {
	public class MeetingRockCompletion : IRockHook {

		public bool CanRunRemotely() {
			return true;
		}
		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			if (updates.StatusChanged) {
				var recurIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rock.Id && x.DeleteTime == null).Select(x => x.L10Recurrence.Id).List<long>().ToList();
				if (recurIds.Any()) {
					foreach (var recurrenceId in recurIds) {
						var currentMeeting = L10Accessor._GetCurrentL10Meeting_Unsafe(s, recurrenceId, true, false, false);
						if (currentMeeting != null) {
							var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.L10Meeting.Id == currentMeeting.Id && x.ForRock.Id == rock.Id && x.DeleteTime == null).List().ToList();
							foreach (var r in meetingRocks) {
								r.Completion = rock.Completion;
								r.CompleteTime = rock.CompleteTime;
							}
						}
					}
				}
			}
		}
		#region noop
		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//Nothing
		}
		public async Task CreateRock(ISession s, RockModel rock) {
			//Nothing
		}
		#endregion
	}
}