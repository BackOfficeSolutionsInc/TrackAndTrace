using System.Web.Mvc;
using Mandrill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using RadialReview.Accessors;
using RadialReview.Controllers;
using Mandrill.Models;
using RadialReview.Models;
using System.Threading.Tasks;
using RadialReview.Models.Json;
using Microsoft.AspNet.WebHooks;
using RadialReview.Models.Webhook;
using RadialReview.Models.ViewModels;

namespace RadialReview.Controllers {
	public class WebhookController : BaseController {
		public class MandrillWebHookBindingModel {
			[AllowHtml]
			public string mandrill_events { get; set; }
		}

		// GET: Webhook
		[System.Web.Http.HttpPost]
		[Access(AccessLevel.Any)]
		public ActionResult Mandrill_0106CFAB089241C9BEFC7B084408D082(MandrillWebHookBindingModel model) {
			try {
				var events = JsonConvert.DeserializeObject<IEnumerable<WebHookEvent>>(model.mandrill_events);
				MandrillAccessor.ProcessWebhooks(events);
			} catch (Exception e1) {
				log.Error(e1);

				try {
					var events = JsonConvert.DeserializeObject<WebHookEvent>(model.mandrill_events);
					MandrillAccessor.ProcessWebhooks(events.AsList());
				} catch (Exception e2) {
					log.Info(model.mandrill_events);
					log.Error(e2);
					throw new Exception(model.mandrill_events, e2);
				}

			}

			return Content("ok");
		}
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			List<WebHookViewModel> webHook = new List<WebHookViewModel>();
			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			var getallwebhook = webhookAccessor.GetAllWebHook();
			foreach (var item in getallwebhook) {
				WebHookViewModel wEBHOOK = new WebHookViewModel();
				wEBHOOK.Id = item.Id;
				wEBHOOK.Description = item.Description;
				wEBHOOK.WebHookUri = item.WebHookUri;
				webHook.Add(wEBHOOK);
			}
			return View(webHook);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Create(string id = "") {
			WebHookViewModel webHook = new WebHookViewModel();

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			//string s = Convert.ToString(id);
			if (id != "") {
				var editWebHook = webhookAccessor.LookupWebHook(GetUser().GetEmail(), id);
				webHook.Id = editWebHook.Id;
				webHook.WebHookUri = editWebHook.WebHookUri;
				webHook.Description = editWebHook.Description;

			}
			return PartialView("Create", webHook);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Create(WebHookViewModel webHook) {

			string email = GetUser().GetEmail();
			WebHook webhook = new WebHook() {
				Id = webHook.Id,
				WebHookUri = webHook.WebHookUri,
				Secret = "12345678901234567890123456789012",
				Description = webHook.Description
			};

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			if (webHook.Id != null) {
				var updateWebHook = webhookAccessor.UpdateWebHook(email, webhook);
			} else {
				var insertWebHook = webhookAccessor.InsertWebHook(email, webhook);
			}
			return Json(ResultObject.SilentSuccess(webhook));
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Delete(string id) {
			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			var s = webhookAccessor.DeleteWebHook(GetUser().GetEmail(), id);
			return Json(ResultObject.SilentSuccess(s), JsonRequestBehavior.AllowGet);
		}
	}
}