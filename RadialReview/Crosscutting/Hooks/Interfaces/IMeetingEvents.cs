using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IMeetingEvents : IHook {
		Task CreateRecurrence(ISession s,	L10Recurrence recur);
		Task DeleteRecurrence(ISession s,	L10Recurrence recur);
		Task UndeleteRecurrence(ISession s, L10Recurrence recur);
		Task StartMeeting(ISession s,		L10Recurrence recur, L10Meeting meeting);
		Task ConcludeMeeting(ISession s,	L10Recurrence recur, L10Meeting meeting);


		Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee);
		Task RemoveAttendee(ISession s, long recurrenceId, long userId);

		Task DeleteMeeting(ISession s,		L10Meeting meeting);
	}
}
