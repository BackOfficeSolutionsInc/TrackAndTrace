using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels {
	public class WebHookViewModel {


		public string Id { get; set; }
		[Required]
		[DisplayName("WebHookUri")]
		public Uri WebHookUri { get; set; }
		public string Description { get; set; }
		//public WebhookEvents WebhookEvent { get; set; }
		//[Required]
		// [DisplayName("WEbhookEvent")]
		//public List<WebhookEvents> WEbhookEvent { get; set; }

		public List<SelectListItem> Events { get; set; }
		public string Eventnames { get; set; }
		public List<string> selected { get; set; }
	}


	public class WebHookEventsViewModel {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

	}



	public class webhookEventsSubscription {
		public long Id { get; set; }
		public string WebhookId { get; set; }
		public string EventName { get; set; }
		public string Description { get; set; }
		public WebhookDetails Webhook { get; set; }
		//public WebhookEvents WebhookEvent { get; set; }

	}

}