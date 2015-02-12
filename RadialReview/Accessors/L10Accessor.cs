using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using NHibernate;
using ListExtensions = WebGrease.Css.Extensions.ListExtensions;

namespace RadialReview.Accessors
{
	public class L10Accessor
	{
		public static void EditL10Recurrence(UserOrganizationModel caller, L10Recurrence l10Recurrence)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditL10Meeting(caller.Organization.Id, l10Recurrence.Id);

					//s.UpdateLists(l10Recurrence,DateTime.UtcNow,x=>x.DefaultAttendees,x=>x.DefaultMeasurables);
					/*if (l10Recurrence.Id != 0){
						var old = s.Get<L10Recurrence>(l10Recurrence.Id);
						//SetUtility.AddRemove(old.DefaultAttendees,l10Recurrence.DefaultAttendees,x=>x.)
					}*/
					var old = s.Get<L10Recurrence>(l10Recurrence.Id);
					LoadRecurrences(s, false, old);

					var now = DateTime.UtcNow;
					s.UpdateList(old._DefaultAttendees, l10Recurrence._DefaultAttendees, now);
					s.UpdateList(old._DefaultMeasurables, l10Recurrence._DefaultMeasurables, now);

					s.Evict(old);
					
					s.SaveOrUpdate(l10Recurrence);
					tx.Commit();
					s.Flush();
				}
			}
			
		}

		private static void LoadRecurrences(ISession s, bool loadUsers, params L10Recurrence[] all)
		{
			var recurrenceIds = all.Where(x=>x!=null).Select(x => x.Id).Distinct().ToArray();

			if (recurrenceIds.Any()){
				var allAttend = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.List().ToList();
				var allMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.List().ToList();

				foreach (var a in all.Where(x => x != null))
				{
					a._DefaultAttendees = allAttend.Where(x => a.Id == x.L10Recurrence.Id).ToList();
					a._DefaultMeasurables = allMeasurables.Where(x => a.Id == x.L10Recurrence.Id).ToList();
					if (loadUsers){
						foreach (var u in a._DefaultAttendees){
							u.User.GetName();
							u.User.ImageUrl();
						}
					}
				}
			}

		}

		public static L10Recurrence GetL10Recurrence(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var found = s.Get<L10Recurrence>(recurrenceId);
					LoadRecurrences(s, true, found);
					return found;
				}
			}
		}



		public static List<L10VM> GetVisibleL10Meetings(UserOrganizationModel caller, long userId, bool loadUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewUsersL10Meetings(userId);

					var allRecurrences = new List<L10Recurrence>();
					if (caller.ManagingOrganization)
					{
						var orgRecurrences = s.QueryOver<L10Recurrence>().Where(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null).List().ToList();
						allRecurrences.AddRange(orgRecurrences);
					}
					//Who should we get this data for? Just Self, or also subordiantes?
					var accessibleUserIds = new[] { userId };
					var user = s.Get<UserOrganizationModel>(userId);
					if (user.Organization.Settings.ManagersCanViewSubordinateL10)
						accessibleUserIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, userId).ToArray();
					
					var attendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.User.Id).IsIn(accessibleUserIds)
						.List().ToList();
					var uniqueL10Ids = attendee.Distinct(x => x.L10Recurrence.Id).Select(x => x.L10Recurrence.Id).ToList();
					//Actually load the Recurrences
					var loadedL10 = s.QueryOver<L10Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids).List().ToList();
					allRecurrences.AddRange(loadedL10);

					//Load extra data
					allRecurrences = allRecurrences.Distinct(x => x.Id).ToList();
					LoadRecurrences(s, true, allRecurrences.ToArray());

					//Make a lookup for self attendance
					var attending = attendee.Where(x => userId == x.User.Id).Select(x => x.L10Recurrence.Id).ToArray();
					return allRecurrences.Select(x => new L10VM(x){
						IsAttendee = attending.Any(y => y == x.Id)
					}).ToList();
				}
			}
		}

	}
}