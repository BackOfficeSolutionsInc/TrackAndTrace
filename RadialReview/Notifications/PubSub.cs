using log4net;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Notifications {


	public class PubSub {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public class NVMRes {
			public Notification Backing = new Notification();


			//public NotificationGroupType GroupType = NotificationGroupType.Individual;

			//public string GetGroupId() {
			//	switch (GroupType) {
			//		case NotificationGroupType.Individual:
			//			return "I_" + Backing.Id;
			//		case NotificationGroupType.NameTime_10minutes:
			//			return "T_" + (long)(Backing.CreateTime.Ticks / TimeSpan.FromMinutes(10).Ticks);
			//		case NotificationGroupType.NameTime_day:
			//			return "D_" + (Backing.CreateTime.Date.Ticks);
			//		case NotificationGroupType.Name:
			//			return "N_" + (Backing.Name);
			//		default:
			//			goto case NotificationGroupType.Individual;
			//	}
			//}
		}

		public static void Publish(ISession s, RealTimeUtility rt, Func<NotificationVM, NVMRes> notification) {
			try {
				var n = notification(new NotificationVM()).Backing;
				var anySubscribers = s.QueryOver<Subscription>().Where(x => x.Parent.ModelId == n.Parent.ModelId && x.Parent.ModelType == n.Parent.ModelType && x.Kind == n.Kind).Take(1).RowCount() > 0;
				if (anySubscribers) {

					var usernames = new List<string>();
					if (rt != null && n.OrganizationId > 0) {
						usernames = GetSubscribersNames_Unsafe(s, n.Kind, n.Parent);
					}

					var notSaved = true;

					if (n.EventId != null) {
						var all = s.QueryOver<Notification>().Where(x => x.DeleteTime == null && x.EventId == n.EventId).List().ToList();
						
						foreach (var a in all) {
							if (a.OrganizationId != n.OrganizationId)
								continue;
							//s.Evict(a);
							var copy = n.Clone();
							copy.Id = a.Id;
							s.Merge(copy);
							notSaved = false;
							if (rt != null && n.OrganizationId > 0) {
								rt.UpdateOrganization(n.OrganizationId).Notification(copy, usernames);
							}
						}

					}

					if (notSaved) {
						s.Save(n);
						if (rt != null && n.OrganizationId > 0) {
							rt.UpdateOrganization(n.OrganizationId).Notification(n, usernames);
						}
					}

				}
			} catch (Exception e) {
				log.Error(e);
			}
		}


		public static Subscription Subscribe(UserOrganizationModel caller, long rgm, ForModel about, NotificationKind kind) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var created = Subscribe(s, perms, rgm, about, kind);
					tx.Commit();
					s.Flush();
					return created;
				}
			}
		}

		public static Subscription Subscribe(ISession s, PermissionsUtility perms, long rgm, ForModel about, NotificationKind kind) {
			perms.ViewRGM(rgm);
			perms.ViewForModel(about);

			var user = s.Get<ResponsibilityGroupModel>(rgm);
			var sub = new Subscription() {
				Parent = about,
				OrganizationId = user.Organization.Id,
				Kind = kind,
				SubscriberId = rgm
			};
			s.Save(sub);
			return sub;
		}

		public static void SetSeenStatus(UserOrganizationModel caller, long userId, long notificationId, bool seen, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var perms = PermissionsUtility.Create(s, caller);
						if (seen)
							MarkSeen(s, perms, rt, userId, notificationId);
						else
							MarkUnseen(s, perms, rt, userId, notificationId);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void MarkUnseen(ISession s, PermissionsUtility perms, RealTimeUtility rt, long userId, long notificationId) {
			perms.Self(userId);
			var notification = s.Get<Notification>(notificationId);

			var all = s.QueryOver<NotificationSeen>().Where(x =>
				x.NotificationId == notificationId &&
				x.UserOrganizationId == userId &&
				x.SeenTime != null
			).List().ToList();

			foreach (var a in all) {
				a.SeenTime = null;
				s.Update(a);
			}
			if (all.Any()) {
				notification.AllSeen = false;
				s.Update(notification);
			}

			var self = s.Get<UserOrganizationModel>(userId);
			if (self != null && self.User != null) {
				rt.UpdateOrganization(notification.OrganizationId).NotificationStatus(notification.Id, false, self.User.UserName);
			}

		}

		public static List<string> GetSubscribersNames_Unsafe(ISession s, NotificationKind kind, ForModel parent) {
			var subscribers = GetSubscribers_Unsafe(s, kind, parent);
			var usernames = s.QueryOver<UserModel>().WhereRestrictionOn(x => x.CurrentRole).IsIn(subscribers.ToArray()).Select(x => x.UserName).List<string>().ToList();
			return usernames;
		}

		public static IEnumerable<long> GetSubscribers_Unsafe(ISession s, NotificationKind kind, ForModel parent) {

			var allSubsRGMs = s.QueryOver<Subscription>().Where(x =>
				x.DeleteTime == null &&
				x.Kind == kind &&
				x.Parent.ModelId == parent.ModelId &&
				x.Parent.ModelType == parent.ModelType
			).Select(x => x.SubscriberId).List<long>().ToList();

			return ResponsibilitiesAccessor.GetMemberIds(s, PermissionsUtility.Create(s, UserOrganizationModel.ADMIN), allSubsRGMs);
		}

		public static void MarkSeen(ISession s, PermissionsUtility perms, RealTimeUtility rt, long userId, long notificationId) {
			var uid = userId;

			perms.Self(userId);

			var alreadySeen = s.QueryOver<NotificationSeen>().Where(x => x.NotificationId == notificationId && x.SeenTime != null).Select(x => x.UserOrganizationId).List<long>().ToList();
			var any = alreadySeen.Any(x => x == uid);

			if (!any) {
				var notification = s.Get<Notification>(notificationId);
				s.Save(new NotificationSeen() {
					NotificationId = notificationId,
					SeenTime = DateTime.UtcNow,
					UserOrganizationId = uid,
				});
				alreadySeen.Add(uid);

				var subscribers = GetSubscribers_Unsafe(s, notification.Kind, notification.Parent).ToList();

				var everyoneSeen = subscribers.All(subId => alreadySeen.Any(alreadySeenId => subId == alreadySeenId));
				if (everyoneSeen) {
					notification.AllSeen = true;
					s.Update(notification);
				}

				var self = s.Get<UserOrganizationModel>(userId);
				if (self != null && self.User != null) {
					rt.UpdateOrganization(notification.OrganizationId).NotificationStatus(notification.Id, true, self.User.UserName);
				}

			}
		}
		public static List<Notification> ListUnseen(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return ListUnseen(s, perms, userId);
				}
			}
		}


		public static List<Notification> ListUnseen(ISession s, PermissionsUtility perms, long userId) {

			perms.Self(userId);

			var groups = ResponsibilitiesAccessor.GetGroupIdsForUser(s, perms, userId).ToArray();

			var subs = s.QueryOver<Subscription>().Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.SubscriberId).IsIn(groups)
				.Select(x => x.Kind, x => x.Parent.ModelId, x => x.Parent.ModelType)
				.List<object[]>()
				.Select(x => new {
					Kind = (NotificationKind)x[0],
					MID = (long)x[1],
					MType = (string)x[2],
				}).ToList();

			var ors = Restrictions.Disjunction();

			foreach (var sub in subs) {
				var ands = Restrictions.Conjunction()
					.Add<Notification>(x => x.Kind == sub.Kind)
					.Add<Notification>(x => x.Parent.ModelId == sub.MID)
					.Add<Notification>(x => x.Parent.ModelType == sub.MType);
				ors.Add(ands);
			}

			var conditions = Restrictions.Conjunction()
					.Add<Notification>(x => x.DeleteTime == null)
					.Add<Notification>(x => x.AllSeen == false)
					.Add(ors);

			var notifications = s.QueryOver<Notification>()
				.Where(x => x.DeleteTime == null && x.AllSeen == false) // <= Seen criteria
				.Where(ors)
				.OrderBy(x => x.CreateTime).Desc
				.List().ToList();

			// Seen criteria
			var seen = s.QueryOver<NotificationSeen>()
				.Where(x => x.UserOrganizationId == userId && x.SeenTime != null)
				.WhereRestrictionOn(x => x.NotificationId).IsIn(notifications.Select(x => x.Id).ToArray())
				.Select(x => x.NotificationId)
				.List<long>()
				.ToList();

			var unseenNotifications = notifications.Where(x => !seen.Any(y => y == x.Id)).ToList();

			return unseenNotifications;
		}
	}

	public class NotificationVM {

		private PubSub.NVMRes CreateMaster(long organizationId, NotificationKind kind, ForModel parent, string name = null, string details = null, string link = null, NotificationGroupType grouping = NotificationGroupType.NameTime_day, string eventId = null) {
			return new PubSub.NVMRes {
				Backing = new Notification() {
					Details = details,
					Link = link ?? "#",
					Kind = kind,
					Parent = parent,
					OrganizationId = organizationId,
					Name = name,
					Grouping = grouping,
					EventId = eventId

				},
				//GroupType = grouping
			};
		}

		private PubSub.NVMRes CreateMaster<T>(long organizationId, NotificationKind kind, T parent, string name = null, string details = null, string link = null, NotificationGroupType grouping = NotificationGroupType.NameTime_day, string eventId = null) where T : ILongIdentifiable {
			return CreateMaster(organizationId, kind, ForModel.Create(parent), name, details, link, grouping, eventId);
		}

		public PubSub.NVMRes Create(long organizationId, string name, string details, string link, NotificationKind kind, ForModel parent, NotificationGroupType grouping, string eventId = null) {
			return CreateMaster(organizationId, kind, parent, name, details, link, grouping, eventId: eventId);
		}

		public PubSub.NVMRes Create(long organizationId, string name, string details, string link, NotificationKind kind, ForModel parent, string eventId = null) {
			return CreateMaster(organizationId, kind, parent, name, details, link, eventId: eventId);
		}

		public PubSub.NVMRes Create(long organizationId, string name, string link, NotificationKind kind, ForModel parent, string eventId = null) {
			return CreateMaster(organizationId, kind, parent, name, null, link, eventId: eventId);
		}

		public PubSub.NVMRes Create(long organizationId, string name, NotificationKind kind, ForModel parent, string eventId = null) {
			return CreateMaster(organizationId, kind, parent, name, eventId: eventId);
		}
		public PubSub.NVMRes Create<T>(long organizationId, string name, string details, string link, NotificationKind kind, T parent, string eventId = null) where T : ILongIdentifiable {
			return CreateMaster(organizationId, kind, parent, name, details, link, eventId: eventId);
		}
		public PubSub.NVMRes Create<T>(long organizationId, string name, NotificationKind kind, T parent, string eventId = null) where T : ILongIdentifiable {
			return CreateMaster(organizationId, kind, parent, name, eventId: eventId);
		}
		public PubSub.NVMRes Create<T>(long organizationId, NotificationKind kind, T parent, string eventId = null) where T : ILongIdentifiable {
			return CreateMaster(organizationId, kind, parent, eventId: eventId);
		}
	}

}