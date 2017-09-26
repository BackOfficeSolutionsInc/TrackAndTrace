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

            var getallwebhook = webhookAccessor.GetAllWebHook();   // get with userid

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

            webHook.Events = new List<SelectListItem>();

            //L10 Events
            var getAllL10RecurrenceAtOrganization = L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id);

            for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {
                //L10 Add TODO Events
                string val = WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id;
                webHook.Events.Add(new SelectListItem() { Text = val, Value = val });

                //L10 Checking/Unchecking/Closing TODO Events
                string checking_Unchecking_Closing_Events = WebhookEventType.Checking_Unchecking_Closing_TODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id;
                webHook.Events.Add(new SelectListItem() { Text = checking_Unchecking_Closing_Events, Value = checking_Unchecking_Closing_Events });

                //L10 Changing TODO Events
                string Changing_Events = WebhookEventType.ChangingToDotoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id;
                webHook.Events.Add(new SelectListItem() { Text = Changing_Events, Value = Changing_Events });


                //L10 Add Issue Events
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id,
                    Value = WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id
                });

                //L10 Checking/Unchecking/Closing Issue Events
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.Checking_Unchecking_Closing_IssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id,
                    Value = WebhookEventType.Checking_Unchecking_Closing_IssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id
                });

                //L10 Changing Issue Events
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.ChangingIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id,
                    Value = WebhookEventType.ChangingIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id
                });

            }

            #region Organization Todo Event

            //Organization Event
            webHook.Events.Add(new SelectListItem() {
                Text = WebhookEventType.AddTODOtoOrganization.GetDescription() + GetUser().Organization.GetName(),
                Value = WebhookEventType.AddTODOtoOrganization.GetDescription() + GetUser().Organization.Id
            });

            //Organization Checking/Unchecking/Closing TODO Event
            webHook.Events.Add(new SelectListItem() {
                Text = WebhookEventType.Checking_Unchecking_Closing_TODOtoOrganization.GetDescription() + GetUser().Organization.GetName(),
                Value = WebhookEventType.Checking_Unchecking_Closing_TODOtoOrganization.GetDescription() + GetUser().Organization.Id
            });


            //Organization Changing TODO Event
            webHook.Events.Add(new SelectListItem() {
                Text = WebhookEventType.ChangingTODOtoOrganization.GetDescription() + GetUser().Organization.GetName(),
                Value = WebhookEventType.ChangingTODOtoOrganization.GetDescription() + GetUser().Organization.Id
            });

            #endregion


            #region Organization Issue Event

            //Organization Event
            webHook.Events.Add(new SelectListItem() {
                Text = WebhookEventType.AddIssuetoOrganization.GetDescription() + GetUser().Organization.GetName(),
                Value = WebhookEventType.AddIssuetoOrganization.GetDescription() + GetUser().Organization.Id
            });

            //Organization Checking/Unchecking/Closing TODO Event
            webHook.Events.Add(new SelectListItem() {
                Text = WebhookEventType.Checking_Unchecking_Closing_IssuetoOrganization.GetDescription() + GetUser().Organization.GetName(),
                Value = WebhookEventType.Checking_Unchecking_Closing_IssuetoOrganization.GetDescription() + GetUser().Organization.Id
            });


            //Organization Changing TODO Event
            webHook.Events.Add(new SelectListItem() {
                Text = WebhookEventType.ChangingIssuetoOrganization.GetDescription() + GetUser().Organization.GetName(),
                Value = WebhookEventType.ChangingIssuetoOrganization.GetDescription() + GetUser().Organization.Id
            });

            #endregion
            
            //User Events
            var getUserOrg = DeepAccessor.Tiny.GetSubordinatesAndSelf(GetUser(), GetUser().Id);
            for (int i = 0; i < getUserOrg.Count; i++) {
                //User Add TODO Event
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.AddTODOforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.AddTODOforUser.GetDescription() + getUserOrg[i].UserOrgId
                });


                //User Checking/Unchecking/Closing TODO Event
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.Checking_Unchecking_Closing_TODOforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.Checking_Unchecking_Closing_TODOforUser.GetDescription() + getUserOrg[i].UserOrgId
                });

                //User Changing TODO Event
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId
                });


                //User Add Issue Event
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.AddIssueforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.AddIssueforUser.GetDescription() + getUserOrg[i].UserOrgId
                });


                //User Checking/Unchecking/Closing Issue Event
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.Checking_Unchecking_Closing_IssueforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.Checking_Unchecking_Closing_IssueforUser.GetDescription() + getUserOrg[i].UserOrgId
                });

                //User Changing Issue Event
                webHook.Events.Add(new SelectListItem() {
                    Text = WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId
                });

            }

            webHook.Events = webHook.Events.OrderBy(o => o.Text).ToList();

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
                webhookModel.Id = webhookAccessor.InsertWebHook(GetUser(), webhook, selectedEvents);

            }


            //get subscription events
            var getWebhookEventSubscriptions = webhookAccessor.GetWebhookEventSubscriptions(GetUser(), webhook.Id);
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