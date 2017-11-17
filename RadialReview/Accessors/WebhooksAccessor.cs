using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.DataTypes;
using System.Text;
using System.Web;
using RadialReview.Utilities.RealTime;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection;
using System.Globalization;
using System.ComponentModel;
using System.Reflection;
using System.Web.Mvc;

namespace RadialReview.Accessors {
    public class WebhooksAccessor : BaseAccessor {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { Formatting = Formatting.None };
        private readonly IDataProtector _protector;

        public WebhooksAccessor() {
            _protector = DataSecurity.GetDataProtector();
        }
        public WebhookDetails InsertWebHook(ISession s, string email, WebHook webHook, string userId, List<string> events) {
            try {
                var webhookDetails = ConvertToWebHook(email, webHook);

                webhookDetails.UserId = userId;
                s.Save(webhookDetails);
                AddSubscribeEvents(s, events, webhookDetails.Id);

                return webhookDetails;
            } catch (Exception ex) {
                throw ex;
            }
        }
        public WebhookDetails InsertWebHook(UserOrganizationModel caller, WebHook webHook, List<string> events) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    int count = 0;
                    foreach (var item in events) {
                        var result = GetEventList(caller).Where(t => t.Text == item).SingleOrDefault();
                        if (result != null)
                            count++;
                    }

                    if (count != events.Count) {
                        throw new PermissionsException("Events doesn't exist.");
                    }

                    var getUser = s.QueryOver<UserModel>().Where(t => t.UserName == caller.GetEmail()).SingleOrDefault();

                    PermissionsUtility perms = PermissionsUtility.Create(s, caller);
                    //perms.Self();

                    var o = InsertWebHook(s, getUser.Email, webHook, getUser.Id, events);
                    tx.Commit();
                    s.Flush();
                    return o;
                }
            }
        }
        public StoreResult UpdateWebHook(UserOrganizationModel caller, WebHook webHook, List<string> events) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var updateWebHook = s.QueryOver<WebhookDetails>().Where(m => m.Email == caller.GetEmail() && m.Id == webHook.Id).SingleOrDefault();
                    if (updateWebHook != null) {
                        UpdateRegistrationFromWebHook(caller.GetEmail(), webHook, updateWebHook);
                        s.Update(updateWebHook);
                        AddSubscribeEvents(s, events, webHook.Id, true);
                        tx.Commit();
                        s.Flush();
                        return StoreResult.Success;
                    }
                    return StoreResult.NotFound;
                }
            }
        }
        public void AddSubscribeEvents(ISession s, List<string> events, string webhookId, bool isRemove = false) {
            if (events != null) {
                if (isRemove) {
                    var getEvents = s.QueryOver<WebhookEventsSubscription>().Where(m => m.WebhookId == webhookId && m.DeleteTime == null).List().ToList();
                    for (int i = 0; i < getEvents.Count; i++) {
                        getEvents[i].DeleteTime = DateTime.UtcNow;
                        s.Update(getEvents[i]);
                        // s.Delete(getEvents[i]);
                    }
                }

                if (events != null) {
                    foreach (var item in events) {
                        WebhookEventsSubscription webhookEventsSubscription = new WebhookEventsSubscription();
                        webhookEventsSubscription.EventName = item;
                        webhookEventsSubscription.WebhookId = webhookId;
                        s.Save(webhookEventsSubscription);
                    }
                }
            }
        }
        public ICollection<WebHook> GetAllWebHookByUser(UserOrganizationModel caller) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility perms = PermissionsUtility.Create(s, caller);
                    perms.Self(caller.Id);
                    var allWebhook = s.QueryOver<WebhookDetails>().Where(m => m.DeleteTime == null && m.Email == caller.GetEmail()).List().ToList();
                    ICollection<WebHook> list = allWebhook.Select(r => ConvertToWebHook(r)).Where(w => w != null).ToArray();
                    return list;
                }
            }
        }

        [Obsolete("Use in WebhookStore")]
        public List<WebHook> GetQueryWebHooksAcrossAllUsers(IEnumerable<string> actions, Func<WebHook, string, bool> predicate = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var matches = new List<WebHook>();
                    var allEventSubscriptions = s.QueryOver<WebhookEventsSubscription>()
                        .WhereRestrictionOn(x => x.EventName).IsIn(actions.ToList()).Where(x => x.EventName != null && x.DeleteTime == null)
                        .List().ToArray();

                    foreach (var item in allEventSubscriptions) {
                        var match = ConvertToWebHook(item.Webhook);
                        if (match != null && (predicate == null || predicate(match, item.Webhook.UserId))) {
                            matches.Add(match);
                        }
                    }

                    return matches;
                }
            }
        }
        public static Func<WebHook, string, bool> PermissionsPredicate(ISession s, Action<PermissionsUtility> action) {

            return new Func<WebHook, String, bool>((WebHook, userId) => {
                bool IsPermissionSucceed = false;
                var orgList = new UserAccessor().GetUserOrganizations(s, userId, null);

                foreach (var org in orgList) {
                    var perms = PermissionsUtility.Create(s, org);
                    try {
                        action(perms);
                        IsPermissionSucceed = true;
                        break;
                    } catch (Exception) {
                        IsPermissionSucceed = false;
                    }
                }

                return IsPermissionSucceed;
            });
        }
        public ICollection<WebHook> GetQueryWebHooks(string userId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var getQueryWebHooks = s.QueryOver<WebhookDetails>().Where(m => m.Email == userId && m.DeleteTime == null).List().ToList();
                    ICollection<WebHook> list = getQueryWebHooks.Select(r => ConvertToWebHook(r)).Where(w => w != null).ToArray();
                    return list;
                }
            }
        }
        public WebHook LookupWebHook(UserOrganizationModel caller, string id) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var lookupWebHook = s.QueryOver<WebhookDetails>().Where(m => m.Email == caller.GetEmail() && m.Id == id && m.DeleteTime == null).SingleOrDefault();

                    if (lookupWebHook != null) {
                        return ConvertToWebHook(lookupWebHook);
                    }
                    return null;
                }
            }
        }
        public StoreResult DeleteWebHook(UserOrganizationModel caller, string id) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var deleteWebHookSubscription = s.QueryOver<WebhookEventsSubscription>().Where(m => m.WebhookId == id && m.DeleteTime == null).List().ToList();
                    if (deleteWebHookSubscription.Count() > 0) {
                        foreach (var item in deleteWebHookSubscription) {
                            item.DeleteTime = DateTime.UtcNow;
                            s.Update(item);
                        }
                    }
                    var deleteWebHook = s.QueryOver<WebhookDetails>().Where(m => m.Email == caller.GetEmail() && m.Id == id && m.DeleteTime == null).SingleOrDefault();
                    if (deleteWebHook != null) {
                        deleteWebHook.DeleteTime = DateTime.UtcNow;
                        s.Update(deleteWebHook);

                        // important to do add it here because of permission checks
                        tx.Commit();
                        s.Flush();
                        return StoreResult.Success;
                    }

                    return StoreResult.NotFound;
                }
            }
        }

        public WebhookDetails GetWebhookEventSubscriptions(UserOrganizationModel caller, string webhookId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                var getUser = s.QueryOver<UserModel>().Where(t => t.UserName == caller.GetEmail()).SingleOrDefault();

                var getSubscriptionList = s.Get<WebhookDetails>(webhookId);

                if (getSubscriptionList == null || getSubscriptionList.UserId != getUser.Id || getSubscriptionList.DeleteTime != null) {
                    throw new PermissionsException();
                }

                //var r = s.QueryOver<WebhookEventsSubscription>().Where(t => t.WebhookId == webhookId).List();
                var a = getSubscriptionList.WebhookEventsSubscription.ToList();

                //.Where(t => t.UserId == getUser.Id && t.Id == webhookId && t.DeleteTime == null)
                //.Fetch(t => t.WebhookEventsSubscription).Eager.SingleOrDefault();

                s.Flush();
                return getSubscriptionList;
            }
        }

        public List<SelectListItem> GetEventList(UserOrganizationModel caller) {

            var eventList = new List<SelectListItem>();

            //L10 Events
            var getAllL10RecurrenceAtOrganization = L10Accessor.GetVisibleL10Meetings_Tiny(caller, caller.Id);

            for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {
                //L10 Add TODO Events
                string val = WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id;
                eventList.Add(new SelectListItem() { Text = val, Value = val });

                //L10 Checking/Unchecking/Closing TODO Events
                string checking_Unchecking_Closing_Events = WebhookEventType.Checking_Unchecking_Closing_TODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id;
                eventList.Add(new SelectListItem() { Text = checking_Unchecking_Closing_Events, Value = checking_Unchecking_Closing_Events });

                //L10 Changing TODO Events
                string Changing_Events = WebhookEventType.ChangingToDotoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id;
                eventList.Add(new SelectListItem() { Text = Changing_Events, Value = Changing_Events });


                //L10 Add Issue Events
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id,
                    Value = WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id
                });

                //L10 Checking/Unchecking/Closing Issue Events
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.Checking_Unchecking_Closing_IssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id,
                    Value = WebhookEventType.Checking_Unchecking_Closing_IssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id
                });

                //L10 Changing Issue Events
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.ChangingIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id,
                    Value = WebhookEventType.ChangingIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id
                });

            }

            #region Organization Todo Event

            //Organization Event
            eventList.Add(new SelectListItem() {
                Text = WebhookEventType.AddTODOtoOrganization.GetDescription() + caller.Organization.GetName(),
                Value = WebhookEventType.AddTODOtoOrganization.GetDescription() + caller.Organization.Id
            });

            //Organization Checking/Unchecking/Closing TODO Event
            eventList.Add(new SelectListItem() {
                Text = WebhookEventType.Checking_Unchecking_Closing_TODOtoOrganization.GetDescription() + caller.Organization.GetName(),
                Value = WebhookEventType.Checking_Unchecking_Closing_TODOtoOrganization.GetDescription() + caller.Organization.Id
            });


            //Organization Changing TODO Event
            eventList.Add(new SelectListItem() {
                Text = WebhookEventType.ChangingTODOtoOrganization.GetDescription() + caller.Organization.GetName(),
                Value = WebhookEventType.ChangingTODOtoOrganization.GetDescription() + caller.Organization.Id
            });

            #endregion


            #region Organization Issue Event

            //Organization Event
            eventList.Add(new SelectListItem() {
                Text = WebhookEventType.AddIssuetoOrganization.GetDescription() + caller.Organization.GetName(),
                Value = WebhookEventType.AddIssuetoOrganization.GetDescription() + caller.Organization.Id
            });

            //Organization Checking/Unchecking/Closing TODO Event
            eventList.Add(new SelectListItem() {
                Text = WebhookEventType.Checking_Unchecking_Closing_IssuetoOrganization.GetDescription() + caller.Organization.GetName(),
                Value = WebhookEventType.Checking_Unchecking_Closing_IssuetoOrganization.GetDescription() + caller.Organization.Id
            });


            //Organization Changing TODO Event
            eventList.Add(new SelectListItem() {
                Text = WebhookEventType.ChangingIssuetoOrganization.GetDescription() + caller.Organization.GetName(),
                Value = WebhookEventType.ChangingIssuetoOrganization.GetDescription() + caller.Organization.Id
            });

            #endregion

            //User Events
            var getUserOrg = DeepAccessor.Tiny.GetSubordinatesAndSelf(caller, caller.Id);
            for (int i = 0; i < getUserOrg.Count; i++) {
                //User Add TODO Event
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.AddTODOforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.AddTODOforUser.GetDescription() + getUserOrg[i].UserOrgId
                });


                //User Checking/Unchecking/Closing TODO Event
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.Checking_Unchecking_Closing_TODOforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.Checking_Unchecking_Closing_TODOforUser.GetDescription() + getUserOrg[i].UserOrgId
                });

                //User Changing TODO Event
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId
                });


                //User Add Issue Event
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.AddIssueforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.AddIssueforUser.GetDescription() + getUserOrg[i].UserOrgId
                });


                //User Checking/Unchecking/Closing Issue Event
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.Checking_Unchecking_Closing_IssueforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.Checking_Unchecking_Closing_IssueforUser.GetDescription() + getUserOrg[i].UserOrgId
                });

                //User Changing Issue Event
                eventList.Add(new SelectListItem() {
                    Text = WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId,
                    Value = WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId
                });

            }

            return eventList;
        }


        #region Helper methods

        protected virtual void UpdateRegistrationFromWebHook(string user, WebHook webHook, WebhookDetails webhooksDetails) {
            if (webHook == null) {
                throw new ArgumentNullException(nameof(webHook));
            }
            if (webhooksDetails == null) {
                throw new ArgumentNullException(nameof(webhooksDetails));
            }

            webhooksDetails.Email = user;
            webhooksDetails.Id = webHook.Id;
            string content = JsonConvert.SerializeObject(webHook, _serializerSettings);
            string protectedData = _protector != null ? _protector.Protect(content) : content;
            webhooksDetails.ProtectedData = protectedData;
        }

        protected virtual WebhookDetails ConvertToWebHook(string user, WebHook webHook) {
            if (webHook == null) {
                throw new ArgumentNullException(nameof(webHook));
            }

            string content = JsonConvert.SerializeObject(webHook, _serializerSettings);
            string protectedData = _protector != null ? _protector.Protect(content) : content;
            var webhooksDetails = new WebhookDetails() {
                Email = user,
                Id = webHook.Id,
                ProtectedData = protectedData
            };
            return webhooksDetails;
        }
        protected virtual WebHook ConvertToWebHook(WebhookDetails webhooksDetails) {
            if (webhooksDetails == null) {
                return null;
            }

            try {
                string content = _protector != null ? _protector.Unprotect(webhooksDetails.ProtectedData) : webhooksDetails.ProtectedData;
                WebHook webHook = JsonConvert.DeserializeObject<WebHook>(content, _serializerSettings);
                return webHook;
            } catch (Exception) {

            }
            return null;
        }

        #endregion
    }
}