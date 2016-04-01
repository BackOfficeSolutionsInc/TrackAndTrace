using System.Web.Mvc;
using Mandrill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using RadialReview.Accessors;
using RadialReview.Controllers;

namespace RadialReview.Content
{
	public class WebhookController :BaseController
    {
		public class MandrillWebHookBindingModel
		{
            [AllowHtml]
			public string mandrill_events { get; set; }
		}

        // GET: Webhook
		[System.Web.Http.HttpPost]
		[Access(AccessLevel.Any)]
		public ActionResult Mandrill_0106CFAB089241C9BEFC7B084408D082(MandrillWebHookBindingModel model)
		{
			var events = JsonConvert.DeserializeObject<IEnumerable<WebHookEvent>>(model.mandrill_events);

			MandrillAccessor.ProcessWebhooks(events);

			return Content("ok");
		}
    }
}