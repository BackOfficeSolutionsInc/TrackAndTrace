﻿using System;
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

        public StoreResult InsertWebHook(ISession s, string email, WebHook webHook)
        {
            try
            {
                var webhookDetails = ConvertFromWebHook(email, webHook);
                s.Save(webhookDetails);
                // Audit.Log(s, perms.GetCaller());
                return StoreResult.Success;
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, "Operation'{0}' failed with error: '{1}'.", "Insert", ex.Message);
                return StoreResult.InternalError;
            }
        }


        public StoreResult InsertWebHook(string email, WebHook webHook)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    // var perms = PermissionsUtility.Create(s, caller);
                    var o = InsertWebHook(s, email, webHook);
                    tx.Commit();
                    s.Flush();
                    return o;
                }
            }
        }

        public StoreResult UpdateWebHook(string user, WebHook webHook)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var updateWebHook = s.QueryOver<WebhooksDetails>().Where(m => m.Email == user && m.UserId == webHook.Id).SingleOrDefault();
                    if (updateWebHook != null)
                    {
                        UpdateRegistrationFromWebHook(user, webHook, updateWebHook);
                        s.Update(updateWebHook);
                        tx.Commit();
                        s.Flush();
                        return StoreResult.Success;
                    }
                    return StoreResult.NotFound;
                }
            }
        }

        public ICollection<WebHook> GetAllWebHook()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var allWebhook = s.QueryOver<WebhooksDetails>().List().ToList();
                    ICollection<WebHook> list = allWebhook.Select(r => ConvertToWebHook(r)).Where(w => w != null).ToArray();
                    return list;
                }
            }
        }



        public ICollection<WebHook> GetQueryWebHooks(string userId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var getQueryWebHooks = s.QueryOver<WebhooksDetails>().Where(m => m.Email == userId).List().ToList();
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
                    var lookupWebHook = s.QueryOver<WebhooksDetails>().Where(m => m.Email == user && m.UserId == id).SingleOrDefault();

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
                    var deleteWebHook = s.QueryOver<WebhooksDetails>().Where(m => m.Email == user && m.UserId == id).SingleOrDefault();
                    if (deleteWebHook != null)
                    {
                        s.Delete(deleteWebHook);
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
                    var deleteAllWebHook = s.QueryOver<WebhooksDetails>().Where(m => m.Email == user).List().ToList();
                    foreach (var item in deleteAllWebHook)
                    {
                        s.Delete(item);
                    }
                    tx.Commit();
                    s.Flush();
                    return StoreResult.Success;
                }
            }
        }

        




        #region Helper methods

        protected virtual void UpdateRegistrationFromWebHook(string user, WebHook webHook, WebhooksDetails webhooksDetails)
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
            webhooksDetails.UserId = webHook.Id;
            string content = JsonConvert.SerializeObject(webHook, _serializerSettings);
            string protectedData = _protector != null ? _protector.Protect(content) : content;
            webhooksDetails.ProtectedData = protectedData;
        }
        protected virtual WebhooksDetails ConvertFromWebHook(string user, WebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            string content = JsonConvert.SerializeObject(webHook, _serializerSettings);
            string protectedData = _protector != null ? _protector.Protect(content) : content;
            var webhooksDetails = new WebhooksDetails()
            {
                Email = user,
                UserId = webHook.Id,
                ProtectedData = protectedData
            };
            return webhooksDetails;
        }

        protected virtual WebHook ConvertToWebHook(WebhooksDetails webhooksDetails)
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
            catch (Exception ex)
            {

            }
            return null;
        }

        #endregion
    }
}