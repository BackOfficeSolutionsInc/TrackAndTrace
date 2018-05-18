using System;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Utilities;
using System.Linq;
using RadialReview.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		public static async Task SharePeopleAnalyzer(UserOrganizationModel caller,long userId, long recurrenceId, L10Recurrence.SharePeopleAnalyzer share) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);

					var attendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.User.Id == userId && x.DeleteTime == null).List().ToList();

					foreach (var a in attendees) {
						if (a.SharePeopleAnalyzer != share) {
							a.SharePeopleAnalyzer = share;
							s.Update(a);
							await HooksRegistry.Each<IRecurrenceSettings>((ses, x) => x.ChangePeopleAnalyzerSharing(ses, userId, recurrenceId, share));
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}