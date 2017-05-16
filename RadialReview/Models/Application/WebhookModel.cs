using FluentNHibernate.Mapping;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models {


	public class WebhookDetails {
		public virtual string Id { get; set; }
		public virtual string Email { get; set; }

		public virtual string UserId { get; set; }

		[JsonIgnore]
		public virtual UserModel User { get; set; }

		public virtual string ProtectedData { get; set; }

		public virtual IList<WebhookEventsSubscription> WebhookEventsSubscription { get; set; }

		public class Map : ClassMap<WebhookDetails> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Email).Length(256);
				References(x => x.User).Column("UserId").LazyLoad().ReadOnly();
				Map(x => x.UserId).Length(64);
				Map(x => x.ProtectedData);
				HasMany(x => x.WebhookEventsSubscription).LazyLoad();
			}
		}
	}


	public class WebhookEvents {
		public virtual long Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string Description { get; set; }
		public class Map : ClassMap<WebhookEvents> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Name).Length(256);
				Map(x => x.Description).Length(256);
			}
		}
	}

	public class WebhookEventsSubscription {
		public virtual long Id { get; set; }
		public virtual string WebhookId { get; set; }
		public virtual long EventId { get; set; }
		public virtual WebhookDetails Webhook { get; set; }
		public virtual WebhookEvents WebhookEvent { get; set; }
		public class Map : ClassMap<WebhookEventsSubscription> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.WebhookId);
				Map(x => x.EventId);
				References(x => x.Webhook).Column("WebhookId").LazyLoad().ReadOnly();
				References(x => x.WebhookEvent).Column("EventId").LazyLoad().ReadOnly();
			}
		}
	}

}