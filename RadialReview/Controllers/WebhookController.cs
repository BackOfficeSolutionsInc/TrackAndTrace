﻿using System.Web.Mvc;
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
				WebHookViewModel webHookViewModel = new WebHookViewModel();
				webHookViewModel.Id = item.Id;
				webHookViewModel.Description = item.Description;
				webHookViewModel.WebHookUri = item.WebHookUri;
				webHook.Add(webHookViewModel);
			}

			//var getwebhookEvents = webhookAccessor.GetWebHookEvents();





			return View(webHook);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Create(string id = "") {
			WebHookViewModel webHook = new WebHookViewModel();

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			//string s = Convert.ToString(id);
			if (id != "") {
				var editWebHook = webhookAccessor.LookupWebHook(GetUser().GetEmail(), id);
				var getEventsSubscribe = webhookAccessor.GetWebhookEventSubscriptions(GetUser().GetEmail(), editWebHook.Id);

				webHook.Id = editWebHook.Id;
				webHook.WebHookUri = editWebHook.WebHookUri;
				webHook.Description = editWebHook.Description;

				if (getEventsSubscribe.WebhookEventsSubscription.Count > 0) {
					var selectedEvents = new List<long>();
					foreach (var item in getEventsSubscribe.WebhookEventsSubscription) {
						selectedEvents.Add(item.EventId);

					}
					webHook.selected = selectedEvents;
				} else {
					webHook.selected = null;
				}
			}

			//var getwebhookEvents = new WebHookEvent(); //webhookAccessor.GetWebHookEvents();

			//webHook.Events = new SelectList(getwebhookEvents, "ID", "Name", "Description");
			//ViewBag.Events = new SelectList(getwebhookEvents, "ID", "Name", "Description");

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
				Description = webHook.Description,
			};

			List<long> selectedEventsIDs = webHook.selected;

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			//var getwebhookevents = webhookAccessor.GetWebHookEvents();

			if (webHook.Id != null) {
				var updateWebHook = webhookAccessor.UpdateWebHook(email, webhook);
			} else {

				var insertWebHook = webhookAccessor.InsertWebHook(email, webhook, selectedEventsIDs);
			}
			return Json(ResultObject.SilentSuccess(webhook));
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Delete(string id) {
			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			var s = webhookAccessor.DeleteWebHook(GetUser().GetEmail(), id);
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
			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			webhookAccessor.DeleteWebHookEvents(id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		#endregion



		#region WebHook Events Subscription Method



		#endregion
	}
}