using RadialReview.Models.Angular.Base;
using RadialReview.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Notifications {
	public class AngularNotification : BaseAngular {

		public AngularNotification() {
		}

		public AngularNotification(long id) : base(id) {
		}

		[Obsolete("Use AngularNotification.Create for lists")]
		public AngularNotification(Notification notification, bool? seen = null) : base(notification.Id) {
			Seen = seen;
			Name = notification.Name;
			Details = notification.Details;
			Url = notification.Link;
			Kind = notification.Kind;
			Grouping = notification.Grouping;
			CreateTime = notification.CreateTime;
		}

		public static IEnumerable<AngularNotification> Create(IEnumerable<Notification> notifications) {

			var orderedNotifications = notifications.OrderBy(x => x.CreateTime);
			var output = new List<AngularNotification>();

			foreach (var n in orderedNotifications) {
				var an = new AngularNotification(n);
				switch (n.Grouping) {
					case NotificationGroupType.Individual: {
							//an.Details = an.Details.Union(an.AsList());
							output.Add(an);
							an.DetailsList = new List<AngularNotification>() ;
							break;
						}
					case NotificationGroupType.Name: {
							var found = output.FirstOrDefault(x => x.Grouping == NotificationGroupType.Name && x.Name == n.Name);
							if (found == null) {
								output.Add(an);
								an.DetailsList = new List<AngularNotification>();
							} else {
								var de = found.DetailsList.ToList();
								de.Add(an);
								found.DetailsList = de;
							}
							break;
						}
					case NotificationGroupType.NameTime_10minutes: {
							var found = output.FirstOrDefault(x =>
								x.Grouping == NotificationGroupType.NameTime_10minutes && x.Name == n.Name &&
								(x.CreateTime - TimeSpan.FromMinutes(10)) <= n.CreateTime);
							if (found == null) {
								//an.Details = an.Details.Union(an.AsList());
								output.Add(an);
								an.DetailsList = new List<AngularNotification>();
							} else {
								var de = found.DetailsList.ToList();
								de.Add(an);
								found.DetailsList = de;
							}
							break;
						}
					case NotificationGroupType.NameTime_day: {
							var found = output.FirstOrDefault(x =>
									x.Grouping == NotificationGroupType.NameTime_day && x.Name == n.Name &&
									(x.CreateTime - TimeSpan.FromDays(1)) <= n.CreateTime
								);
							if (found == null) {
								//an.Details = an.Details.Union(an.AsList());
								output.Add(an);
								an.DetailsList = new List<AngularNotification>();
							} else {
								var de = found.DetailsList.ToList();
								de.Add(an);
								found.DetailsList = de;
							}
							break;
						}
				}
			}
			return output;
		}

		public string Name { get; set; }
		public string Details { get; set; }
		public bool? Seen { get; set; }
		public string Url { get; set; }
		public DateTime? CreateTime { get; set; }
		public NotificationKind? Kind { get; set; }
		public IEnumerable<AngularNotification> DetailsList { get; set; }
		public NotificationGroupType? Grouping { get; set; }

	}
}