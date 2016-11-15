using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Notifications {

	public enum NotificationKind {
		Invalid,
		Roles,
	}


	public enum NotificationGroupType {
		Individual,
		NameTime_10minutes,
		NameTime_day,
		Name,
	}

	public class Notification : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }

		public virtual ForModel Parent { get; set; }
		public virtual NotificationKind Kind { get; set; }

		public virtual string Name { get; set; }
		public virtual string Details { get; set; }
		public virtual NotificationGroupType Grouping { get; set; }
		public virtual string Link { get; set; }
		public virtual bool AllSeen { get; set; }
		public virtual string EventId { get; set; }
		//Update the Cloner

		public virtual Notification Clone() {
			return new Notification() {
				Id = Id,
				AllSeen = AllSeen,
				CreateTime = CreateTime,
				DeleteTime = DeleteTime,
				Details = Details,
				EventId = EventId,
				Grouping = Grouping,
				Kind = Kind,
				Link = Link,
				Name = Name,
				OrganizationId = OrganizationId,
				Parent = Parent.Clone()				
			};
		}

		public Notification() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<Notification> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Component(x => x.Parent).ColumnPrefix("Parent_");
				Map(x => x.Kind);
				Map(x => x.Details);
				Map(x => x.Link);
				Map(x => x.Grouping);
				Map(x => x.Name);
				Map(x => x.AllSeen);
				Map(x => x.EventId);
				//HasMany(x => x.SeenBy).Inverse().ExtraLazyLoad().Cascade.None();
			}
		}
	}

	//public class Receiver : ILongIdentifiable, IHistorical {
	//	public virtual long Id { get; set; }
	//	public virtual DateTime CreateTime { get; set; }
	//	public virtual DateTime? DeleteTime { get; set; }
	//	public virtual long OrganizationId { get; set; }
	//	public virtual ForModel ReceiverModel { get; set; }
	//	public virtual string Name { get; set; }

	//	public Receiver() {
	//		CreateTime = DateTime.UtcNow;
	//	}

	//	public class Map : ClassMap<Receiver> {
	//		public Map() {
	//			Id(x => x.Id);
	//			Map(x => x.CreateTime);
	//			Map(x => x.DeleteTime);
	//			Map(x => x.OrganizationId);
	//			Map(x => x.Name);
	//			Component(x => x.ReceiverModel).ColumnPrefix("Receiver_");		
	//		}
	//	}
	//}
	public class Subscription : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		/// <summary>
		/// Can be any RGM Id
		/// </summary>
		public virtual long SubscriberId { get; set; }

		public virtual ForModel Parent { get; set; }
		public virtual NotificationKind Kind { get; set; }


		public Subscription() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<Subscription> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);

				Map(x => x.SubscriberId);
				Component(x => x.Parent).ColumnPrefix("Parent_");
				Map(x => x.Kind);
			}
		}
	}
	public class NotificationSeen : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual DateTime? SeenTime { get; set; }
		public virtual long UserOrganizationId { get; set; }
		public virtual long NotificationId { get; set; }
		//public virtual Notification Notification { get; set; }

		public class Map : ClassMap<NotificationSeen> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.SeenTime);
				Map(x => x.UserOrganizationId);
				Map(x => x.NotificationId).Column("NotificationId");
				//References(x => x.Notification).Column("NotificationId").LazyLoad().Cascade.None();
			}
		}
	}

	
}