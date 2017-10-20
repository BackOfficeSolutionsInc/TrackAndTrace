using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Users;
using RadialReview.Utilities.RealTime;
using RadialReview.Hooks.Realtime;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;

namespace RadialReview.Hooks.Meeting {
	public class AttendeeHooks : IMeetingEvents {
		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}
		

		public async Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee) {
			var auser = AngularUser.CreateUser(user);
			auser.CreateTime = attendee.CreateTime;

			using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(recurrenceId).Update(new AngularRecurrence(recurrenceId) {
					Attendees = AngularList.CreateFrom(AngularListType.Add, auser)
				});
			}
		}
		public async Task RemoveAttendee(ISession s, long recurrenceId, long userId) {
			using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(recurrenceId).Update(new AngularRecurrence(recurrenceId) {
					Attendees = AngularList.CreateFrom(AngularListType.Remove, new AngularUser(userId))
				});
			}
		}


		#region noop
		public async Task ConcludeMeeting(ISession s, L10Recurrence recur, L10Meeting meeting) {
			//noop
		}

		public async Task CreateRecurrence(ISession s, L10Recurrence recur) {
			//noop
		}

		public async Task DeleteMeeting(ISession s, L10Meeting meeting) {
			//noop
		}

		public async Task DeleteRecurrence(ISession s, L10Recurrence recur) {
			//noop
		}

		public async Task StartMeeting(ISession s, L10Recurrence recur, L10Meeting meeting) {
			//noop
		}

		public async Task UndeleteRecurrence(ISession s, L10Recurrence recur) {
			//noop
		} 
		#endregion
	}
}