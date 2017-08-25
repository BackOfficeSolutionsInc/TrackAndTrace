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

namespace RadialReview.Accessors
{
    public class WebhooksAccessor : BaseAccessor
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { Formatting = Formatting.None };
        private readonly IDataProtector _protector;

        public WebhooksAccessor()
        {
            _protector = DataSecurity.GetDataProtector();
        }
        public StoreResult InsertWebHook(ISession s, string email, WebHook webHook, string userId, List<string> events)
        {
            try
            {
                var webhookDetails = ConvertToWebHook(email, webHook);

                webhookDetails.UserId = userId;
                s.Save(webhookDetails);
                AddSubscribeEvents(s, events, webhookDetails.Id);

                return StoreResult.Success;
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, "Operation'{0}' failed with error: '{1}'.", "Insert", ex.Message);
                return StoreResult.InternalError;
            }
        }
        public StoreResult InsertWebHook(string email, WebHook webHook, List<string> events)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var getUser = s.QueryOver<UserModel>().Where(t => t.UserName == email).SingleOrDefault();
                    var o = InsertWebHook(s, email, webHook, getUser.Id, events);
                    tx.Commit();
                    s.Flush();
                    return o;
                }
            }
        }
        public StoreResult UpdateWebHook(string user, WebHook webHook, List<string> events)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var updateWebHook = s.QueryOver<WebhookDetails>().Where(m => m.Email == user && m.Id == webHook.Id).SingleOrDefault();
                    if (updateWebHook != null)
                    {
                        UpdateRegistrationFromWebHook(user, webHook, updateWebHook);
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
        public void AddSubscribeEvents(ISession s, List<string> events, string webhookId, bool isRemove = false)
        {
            if (events != null)
            {
                if (isRemove)
                {
                    var getEvents = s.QueryOver<WebhookEventsSubscription>().Where(m => m.WebhookId == webhookId).List().ToList();
                    for (int i = 0; i < getEvents.Count; i++)
                    {
                        s.Delete(getEvents[i]);
                    }
                }

                if (events != null)
                {
                    foreach (var item in events)
                    {
                        WebhookEventsSubscription webhookEventsSubscription = new WebhookEventsSubscription();
                        webhookEventsSubscription.EventName = item;
                        webhookEventsSubscription.WebhookId = webhookId;
                        s.Save(webhookEventsSubscription);
                    }
                }
            }
        }
        public ICollection<WebHook> GetAllWebHook()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var allWebhook = s.QueryOver<WebhookDetails>().Where(m => m.DeleteTime == null).List().ToList();
                    ICollection<WebHook> list = allWebhook.Select(r => ConvertToWebHook(r)).Where(w => w != null).ToArray();
                    return list;
                }
            }
        }
        public List<WebHook> GetQueryWebHooksAcrossAllUsers(IEnumerable<string> actions, Func<WebHook, string, bool> predicate = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var matches = new List<WebHook>();
                    var allEventSubscriptions = s.QueryOver<WebhookEventsSubscription>()
                        .WhereRestrictionOn(x => x.EventName).IsIn(actions.ToList()).Where(x => x.EventName != null && x.DeleteTime == null)
                        .List().ToArray();

                    foreach (var item in allEventSubscriptions)
                    {
                        var match = ConvertToWebHook(item.Webhook);
                        if (match != null && (predicate == null || predicate(match, item.Webhook.UserId)))
                        {
                            matches.Add(match);
                        }
                    }

                    return matches;
                }
            }
        }
        public static Func<WebHook, string, bool> PermissionsPredicate(ISession s, Action<PermissionsUtility> action)
        {

            return new Func<WebHook, String, bool>((WebHook, userId) =>
            {
                bool IsPermissionSucceed = false;
                var orgList = new UserAccessor().GetUserOrganizations(s, userId, null);

                foreach (var org in orgList)
                {
                    var perms = PermissionsUtility.Create(s, org);
                    try
                    {
                        action(perms);
                        IsPermissionSucceed = true;
                        break;
                    }
                    catch (Exception)
                    {
                        IsPermissionSucceed = false;
                    }
                }

                return IsPermissionSucceed;
            });
        }
        public ICollection<WebHook> GetQueryWebHooks(string userId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var getQueryWebHooks = s.QueryOver<WebhookDetails>().Where(m => m.Email == userId && m.DeleteTime == null).List().ToList();
                    ICollection<WebHook> list = getQueryWebHooks.Select(r => ConvertToWebHook(r)).Where(w => w != null).ToArray();
                    return list;
                }
            }
        }
        public WebHook LookupWebHook(string user, string id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var lookupWebHook = s.QueryOver<WebhookDetails>().Where(m => m.Email == user && m.Id == id && m.DeleteTime == null).SingleOrDefault();

                    if (lookupWebHook != null)
                    {
                        return ConvertToWebHook(lookupWebHook);
                    }
                    return null;
                }
            }
        }
        public StoreResult DeleteWebHook(string user, string id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var deleteWebHookSubscription = s.QueryOver<WebhookEventsSubscription>().Where(m => m.WebhookId == id).List().ToList();
                    if (deleteWebHookSubscription.Count() > 0)
                    {
                        foreach (var item in deleteWebHookSubscription)
                        {
                            item.DeleteTime = DateTime.UtcNow;
                            s.Update(item);
                        }
                    }
                    var deleteWebHook = s.QueryOver<WebhookDetails>().Where(m => m.Email == user && m.Id == id).SingleOrDefault();
                    if (deleteWebHook != null)
                    {
                        deleteWebHook.DeleteTime = DateTime.UtcNow;
                        s.Update(deleteWebHook);

                        tx.Commit();
                        s.Flush();
                        return StoreResult.Success;
                    }
                    return StoreResult.NotFound;
                }
            }
        }
        public StoreResult DeleteAllWebHook(string user)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var deleteAllWebHook = s.QueryOver<WebhookDetails>().Where(m => m.Email == user).List().ToList();
                    foreach (var item in deleteAllWebHook)
                    {
                        item.DeleteTime = DateTime.UtcNow;
                        s.Update(item);
                    }

                    tx.Commit();
                    s.Flush();
                    return StoreResult.Success;
                }
            }
        }
        public void AddWebhookEventsSubscription(WebhookEventsSubscription webhookEventsSubscription)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    s.Save(webhookEventsSubscription);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public WebhookDetails GetWebhookEventSubscriptions(string email, string webhookId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                var getUser = s.QueryOver<UserModel>().Where(t => t.UserName == email).SingleOrDefault();
                var getSubscriptionList =
                      s.QueryOver<WebhookDetails>()
                      .Where(t => t.UserId == getUser.Id && t.Id == webhookId && t.DeleteTime == null)
                      .Fetch(t => t.WebhookEventsSubscription).Eager.SingleOrDefault();

                s.Flush();
                return getSubscriptionList;
            }
        }

        #region WebHook Events methods
        public void DeleteWebHookEvents(long id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    //var deleteWebHookEvents = s.QueryOver<WebhookEvents>().Where(m => m.Id == id).SingleOrDefault();
                    //if (deleteWebHookEvents != null) {
                    //	s.Delete(deleteWebHookEvents);
                    //	tx.Commit();
                    //	s.Flush();
                    //}

                }
            }
        }

        //public void UpdateWebHookEvents(WebhookEvents webhookEvents) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var updateWebHookEvents = s.QueryOver<WebhookEvents>().Where(m => m.Id == webhookEvents.Id).SingleOrDefault();
        //			if (updateWebHookEvents != null) {
        //				s.Clear();
        //				s.Update(webhookEvents);
        //				tx.Commit();
        //				s.Flush();
        //			}
        //		}
        //	}
        //}

        //public void CreateWebhookEvents(WebhookEvents webhookEvents) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			s.Save(webhookEvents);
        //			tx.Commit();
        //			s.Flush();
        //		}
        //	}
        //}
        //public WebhookEvents LookupWebHookEvents(long id) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var lookupWebHookEvents = s.QueryOver<WebhookEvents>().Where(m => m.Id == id).SingleOrDefault();
        //			return lookupWebHookEvents;
        //		}
        //	}
        //}
        //public ICollection<WebhookEvents> GetWebHookEvents() {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var allWebhook = s.QueryOver<WebhookEvents>().List().ToList();
        //			return allWebhook;
        //		}
        //	}
        //}
        #endregion

        #region Helper methods

        protected virtual void UpdateRegistrationFromWebHook(string user, WebHook webHook, WebhookDetails webhooksDetails)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }
            if (webhooksDetails == null)
            {
                throw new ArgumentNullException(nameof(webhooksDetails));
            }

            webhooksDetails.Email = user;
            webhooksDetails.Id = webHook.Id;
            string content = JsonConvert.SerializeObject(webHook, _serializerSettings);
            string protectedData = _protector != null ? _protector.Protect(content) : content;
            webhooksDetails.ProtectedData = protectedData;
        }

        protected virtual WebhookDetails ConvertToWebHook(string user, WebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            string content = JsonConvert.SerializeObject(webHook, _serializerSettings);
            string protectedData = _protector != null ? _protector.Protect(content) : content;
            var webhooksDetails = new WebhookDetails()
            {
                Email = user,
                Id = webHook.Id,
                ProtectedData = protectedData
            };
            return webhooksDetails;
        }
        protected virtual WebHook ConvertToWebHook(WebhookDetails webhooksDetails)
        {
            if (webhooksDetails == null)
            {
                return null;
            }

            try
            {
                string content = _protector != null ? _protector.Unprotect(webhooksDetails.ProtectedData) : webhooksDetails.ProtectedData;
                WebHook webHook = JsonConvert.DeserializeObject<WebHook>(content, _serializerSettings);
                return webHook;
            }
            catch (Exception)
            {

            }
            return null;
        }

        #endregion
    }
}