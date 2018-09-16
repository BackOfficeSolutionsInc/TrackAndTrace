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
using System.Net.Http;
using RadialReview.Utilities;
using RadialReview.Models.Integrations.Asana;
using RadialReview.Crosscutting.Integrations.Asana;
using RadialReview.Crosscutting.Schedulers;

namespace RadialReview.Controllers {
	public class WebhookController : BaseController {
		private IWebHookStore _store;
		public WebhookController() {
			_store = new RadialWebHookStore();
		}
		public class MandrillWebHookBindingModel {
			[AllowHtml]
			public string mandrill_events { get; set; }
		}

		private static AsanaWebhookEvent LastAsanaRequest = null;
		private static int AsanaRequestCount = 0;
		private static int AsanaSecretCount = 0;
		private static string LastSignature = null;
		private static string LastSecret = null;

		[Access(AccessLevel.Radial)]
		public ActionResult LastAsana() {
			return Json(new {
				content = LastAsanaRequest,
				requestCount = AsanaRequestCount,
				secretCount = AsanaSecretCount,
				lastSignature = LastSignature,
				lastSecret= LastSecret,
			}, JsonRequestBehavior.AllowGet);
		}


		[HttpPost]
		[Access(AccessLevel.Any)]
		public async Task<ActionResult> Asana_305055482F9B4580B89BBFF3301363DF(AsanaWebhookEvent model) {
			var secret = Request.Headers["X-Hook-Secret"];
			if (!string.IsNullOrWhiteSpace(secret)) {
				Response.Headers["X-Hook-Secret"] = secret;
				LastSecret = secret;
				AsanaSecretCount += 1;
				return Content("ok");
			}

			if (model.events == null)
				return Content("no events");

			try {
				var signature = Request.Headers["X-Hook-Signature"];
				LastSignature = signature;
			} catch (Exception e) {
			}

			AsanaRequestCount += 1;
			LastAsanaRequest = model;

			var asanaTaskIds = model.events
				.Where(evt => evt.action == "changed" && evt.type == "task")
				.OrderBy(x=>x.created_at)
				.Select(x => x.resource)
				.ToList();

			if (asanaTaskIds.Any()) {
				Scheduler.Enqueue(() => AsanaAccessor.UpdateTaskFromRemote_Hangfire(asanaTaskIds));
			}

			return Content("ok");
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

			var getallwebhook = webhookAccessor.GetAllWebHookByUser(GetUser());   // get with userid

			foreach (var item in getallwebhook) {
				WebHookViewModel webHookViewModel = new WebHookViewModel();
				var getWebhookEventSubscriptions = webhookAccessor.GetWebhookEventSubscriptions(GetUser(), item.Id);

				List<string> name = new List<string>();
				foreach (var item1 in getWebhookEventSubscriptions.WebhookEventsSubscription.Select(x => x.EventName)) {
					name.Add(item1);
				}

				string nameOfString = (string.Join(" , ", name.Select(x => x.ToString()).ToArray()));
				webHookViewModel.Eventnames = nameOfString;
				webHookViewModel.Id = item.Id;
				webHookViewModel.Description = item.Description;
				webHookViewModel.WebHookUri = item.WebHookUri;
				webHook.Add(webHookViewModel);
			}
			//var getwebhookEvents = webhookAccessor.GetWebHookEvents();
			return View(webHook);
		}

		[Access(AccessLevel.Any)]
		public ActionResult API() {
			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Create(string id = "") {
			Config.ThrowNotImplementedOnProduction();
			WebHookViewModel webHook = new WebHookViewModel();
			WebhooksAccessor webhookAccessor = new WebhooksAccessor();

			if (!string.IsNullOrEmpty(id)) {
				var editWebHook = webhookAccessor.LookupWebHook(GetUser(), id);
				var getEventsSubscribe = webhookAccessor.GetWebhookEventSubscriptions(GetUser(), editWebHook.Id);

				webHook.Id = editWebHook.Id;
				webHook.WebHookUri = editWebHook.WebHookUri;
				webHook.Description = editWebHook.Description;

				if (getEventsSubscribe.WebhookEventsSubscription.Count > 0) {
					var selectedEvents = new List<string>();
					foreach (var item in getEventsSubscribe.WebhookEventsSubscription) {
						selectedEvents.Add(item.EventName);
					}
					webHook.selected = selectedEvents;
				} else {
					webHook.selected = null;
				}
			}
			webHook.Events = webhookAccessor.GetEventList(GetUser()).OrderBy(o => o.Text).ToList();
			return PartialView("Create", webHook);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Create(WebHookViewModel webhookModel) {
			Config.ThrowNotImplementedOnProduction();
			string email = GetUser().GetEmail();

			WebHook webhook = new WebHook() {
				WebHookUri = webhookModel.WebHookUri,
				Secret = "12345678901234567890123456789012",
				Description = webhookModel.Description,
			};
			webhook.Filters.Add("*");

			List<string> selectedEvents = webhookModel.selected;

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();

			if (webhookModel.Id != null) {
				webhook.Id = webhookModel.Id;
				var updateWebHook = webhookAccessor.UpdateWebHook(GetUser(), webhook, selectedEvents);
			} else {
				var result = webhookAccessor.InsertWebHook(GetUser(), webhook, selectedEvents);
				webhookModel.Id = result.Id;

			}


			//get subscription events
			var getWebhookEventSubscriptions = webhookAccessor.GetWebhookEventSubscriptions(GetUser(), webhookModel.Id);
			List<string> name = new List<string>();
			if (getWebhookEventSubscriptions.WebhookEventsSubscription != null) {
				foreach (var item1 in getWebhookEventSubscriptions.WebhookEventsSubscription.Select(x => x.EventName)) {
					name.Add(item1);
				}
				string nameOfString = (string.Join(" , ", name.Select(x => x.ToString()).ToArray()));
				webhookModel.Eventnames = nameOfString;
			}

			if (getWebhookEventSubscriptions.WebhookEventsSubscription == null) {
				string nameOfString = (string.Join(" , ", webhookModel.selected.Select(x => x.ToString()).ToArray()));
				webhookModel.Eventnames = nameOfString;
			}

			return Json(ResultObject.SilentSuccess(webhookModel));
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Delete(string id) {
			Config.ThrowNotImplementedOnProduction();
			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			var s = webhookAccessor.DeleteWebHook(GetUser(), id);
			return Json(ResultObject.SilentSuccess(s), JsonRequestBehavior.AllowGet);
		}

		#region EventsMethod


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Events() {
			List<WebHookEventsViewModel> webHookEventsViewModel = new List<WebHookEventsViewModel>();
			//WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			//var getWebHookEvents = webhookAccessor.GetWebHookEvents();
			//foreach (var item in getWebHookEvents) {
			//	WebHookEventsViewModel webHookeventsviewmodel = new WebHookEventsViewModel();
			//	webHookeventsviewmodel.Id = item.Id;
			//	webHookeventsviewmodel.Description = item.Description;
			//	webHookeventsviewmodel.Name = item.Name;
			//	webHookEventsViewModel.Add(webHookeventsviewmodel);
			//}
			return View(webHookEventsViewModel);
		}


		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateEvents(long id = 0) {
			WebHookEventsViewModel webhookEventsViewModel = new WebHookEventsViewModel();

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			if (id != 0) {
				//var editWebHookEvents = webhookAccessor.LookupWebHookEvents(id);
				//webhookEventsViewModel.Id = editWebHookEvents.Id;
				//webhookEventsViewModel.Name = editWebHookEvents.Name;
				//webhookEventsViewModel.Description = editWebHookEvents.Description;

			}
			return PartialView("CreateEvents", webhookEventsViewModel);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateEvents(WebHookEventsViewModel webhookEventsViewModel) {

			//WebhookEvents webhookEvents = new WebhookEvents() {
			//	Id = webhookEventsViewModel.Id,
			//	Name = webhookEventsViewModel.Name,
			//	Description = webhookEventsViewModel.Description
			//};

			//WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			//if (webhookEventsViewModel.Id > 0) {
			//	webhookAccessor.UpdateWebHookEvents(webhookEvents);
			//} else {
			//	webhookAccessor.CreateWebhookEvents(webhookEvents);
			//}

			return Json(ResultObject.SilentSuccess(null));
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult DeleteEvents(long id) {
			throw new NotImplementedException();
			//WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			//webhookAccessor.DeleteWebHookEvents(id);
			//return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		#endregion



		#region WebHook Events Subscription Method



		#endregion
	}
}