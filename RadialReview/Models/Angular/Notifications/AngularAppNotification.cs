using RadialReview.Models.Angular.Base;
using RadialReview.Models.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Notifications {
	public class AngularAppNotification : BaseAngular{

		public string Name { get; set; }
		public string Details { get; set; }
		public string ImageUrl { get; set; }
		public DateTime? Date { get; set; }
		public bool? IsRead { get; set; }
		public DateTime? Seen { get; set; }

        public AngularAppNotification(long id) :base(id) {
        }

        public AngularAppNotification(NotificationModel notification):this(notification.Id) {
            Name = notification.Name;
            Details = notification.Details;
            ImageUrl = notification.ImageUrl;
            Date = notification.CreateTime;
            IsRead = notification.Seen != null;
            Seen = notification.Seen;
        }
	}
}