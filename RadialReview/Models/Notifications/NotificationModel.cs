using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Notifications {
	public enum NotificationType {
		Standard = 0,
	}
	public enum NotificationPriority {
		Low = -100,
		Normal = 0,
		High = 100,
	}
	public enum NotificationStatus {
		Unsent = 0,
		Unread = 1,
		Read = 2,
		Delete = 3,
	}


	public class NotificationModel : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Name { get; set; }
		public virtual string Details { get; set; }
		public virtual DateTime? Seen { get; set; }
		public virtual DateTime? Sent { get; set; }
		public virtual string ImageUrl { get; set; }
		public virtual NotificationType Type { get; set; }
		public virtual NotificationPriority Priority { get; set; }

		public virtual NotificationStatus GetStatus() {
			if (DeleteTime != null)
				return NotificationStatus.Delete;
			if (Seen != null)
				return NotificationStatus.Read;
			if (Sent == null)
				return NotificationStatus.Unsent;
			return NotificationStatus.Unread;
		}

		public NotificationModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<NotificationModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Name);
				Map(x => x.Details);
				Map(x => x.Seen);
				Map(x => x.Sent);
				Map(x => x.ImageUrl);
				Map(x => x.Type);
				Map(x => x.Priority);
				Map(x => x.UserId);
			}

		}
	}

	public class UserDevice : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime LastUsed { get; set; }
		public virtual string UserName { get; set; }
		public virtual string DeviceId { get; set; }
		public virtual string DeviceType { get; set; }
		public virtual string DeviceVersion { get; set; }
		public virtual bool IgnoreDevice { get; set; }

		public UserDevice() {
			CreateTime = DateTime.UtcNow;
			LastUsed = CreateTime;
		}
		public class Map : ClassMap<UserDevice> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.LastUsed);
				Map(x => x.UserName);
				Map(x => x.DeviceId);
				Map(x => x.DeviceType);
				Map(x => x.DeviceVersion);
				Map(x => x.IgnoreDevice);
			}

		}
	}
}