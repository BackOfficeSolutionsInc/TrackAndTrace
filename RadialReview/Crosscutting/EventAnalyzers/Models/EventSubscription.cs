using FluentNHibernate.Mapping;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Models {
	public class EventSubscription : IHistorical, ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual String EventSettings { get; set; }
		public virtual string EventType { get; set; }
		public virtual long OrgId { get; set; }
		public virtual OrganizationModel Org { get; set; }
		public virtual long SubscriberId { get; set; }
		public virtual UserOrganizationModel Subscriber { get; set; }
		public virtual EventFrequency Frequency { get; set; }
		public virtual DateTime? LastExecution { get; set; }

		
		public EventSubscription() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<EventSubscription> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.EventType);
				Map(x => x.EventSettings);
				Map(x => x.OrgId);
				References(x => x.Org).ReadOnly().LazyLoad().Column("OrgId");
				Map(x => x.SubscriberId);
				Map(x => x.LastExecution);
				References(x => x.Subscriber).ReadOnly().LazyLoad().Column("SubscriberId");
				Map(x => x.Frequency).CustomType<EventFrequency>();
			}
		}
	}
}