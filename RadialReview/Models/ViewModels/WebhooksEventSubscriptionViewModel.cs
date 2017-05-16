using FluentNHibernate.Mapping;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Application;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels {
	public class WebhooksEventSubscriptionViewModel {
		public WebhooksEventSubscriptionViewModel() {
			WebhookEventsSubscription = new List<Models.WebhookEventsSubscription>();
		}
		public string Id { get; set; }
		public string Email { get; set; }

		public string UserId { get; set; }

		public AngularUser angularUser { get; set; }

		public string ProtectedData { get; set; }

		public IList<WebhookEventsSubscription> WebhookEventsSubscription { get; set; }
	}
}