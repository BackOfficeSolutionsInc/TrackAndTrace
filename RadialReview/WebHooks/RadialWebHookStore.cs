// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;
using RadialReview.Accessors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebHooks {
    /// <summary>
    /// Provides an implementation of <see cref="IWebHookStore"/> storing registered WebHooks in memory.
    /// </summary>
    /// <remarks>Actual deployments should replace this with a persistent store, for example provided by
    /// <c>Microsoft.AspNet.WebHooks.Custom.AzureStorage</c>.
    /// </remarks>
    public class RadialWebHookStore : WebHookStore {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebHook>> _store =
        new ConcurrentDictionary<string, ConcurrentDictionary<string, WebHook>>();

        /// <inheritdoc />
        public override Task<ICollection<WebHook>> GetAllWebHooksAsync(string user) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }

            user = NormalizeKey(user);
            try {
                ICollection<WebHook> list = null;
                WebhooksAccessor webHookAcc = new WebhooksAccessor();
                //var result = webHookAcc.GetAllWebHook();
                //return Task.FromResult(result);
                return Task.FromResult(list);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.CurrentCulture, "General error during '{0}' operation: '{1}'.", "Get", ex.Message);
                throw new InvalidOperationException(msg, ex);
            }
        }

        /// <inheritdoc />
        public override Task<ICollection<WebHook>> QueryWebHooksAsync(string user, IEnumerable<string> actions, Func<WebHook, string, bool> predicate) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (actions == null) {
                throw new ArgumentNullException(nameof(actions));
            }

            user = NormalizeKey(user);
            try {
                ICollection<WebHook> list = null;
                WebhooksAccessor webHookAcc = new WebhooksAccessor();
                //var result = webHookAcc.GetAllWebHook();
                //ICollection<WebHook> matches = result
                //    .Where(w => MatchesAnyAction(w, actions) && predicate(w, user))
                //    .ToArray();
                //return Task.FromResult(matches);
                return Task.FromResult(list);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.CurrentCulture, "General error during '{0}' operation: '{1}'.", "Get", ex.Message);
                throw new InvalidOperationException(msg, ex);
            }
        }

        /// <inheritdoc />
        public override Task<WebHook> LookupWebHookAsync(string user, string id) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            user = NormalizeKey(user);

            //WebHook result = null;
            try {
                WebhooksAccessor webHookAcc = new WebhooksAccessor();
                //result = webHookAcc.LookupWebHook(user, id);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.CurrentCulture, "General error during '{0}' operation: '{1}'.", "Lookup", ex.Message);
                throw new InvalidOperationException(msg, ex);
            }

            return Task.FromResult(new WebHook());
        }

        /// <inheritdoc />
        public override Task<StoreResult> InsertWebHookAsync(string user, WebHook webHook) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (webHook == null) {
                throw new ArgumentNullException(nameof(webHook));
            }

            user = NormalizeKey(user);
            WebhooksAccessor webHookAcc = new WebhooksAccessor();
            //return Task.FromResult(webHookAcc.InsertWebHook(user, webHook, null));
            return Task.FromResult(new StoreResult());

        }

        /// <inheritdoc />
        public override Task<StoreResult> UpdateWebHookAsync(string user, WebHook webHook) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (webHook == null) {
                throw new ArgumentNullException(nameof(webHook));
            }

            user = NormalizeKey(user);
            StoreResult result = StoreResult.NotFound;
            try {
                WebhooksAccessor webHookAcc = new WebhooksAccessor();
                //result = webHookAcc.UpdateWebHook(user, webHook, null);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.CurrentCulture, "General error during '{0}' operation: '{1}'.", "Lookup", ex.Message);
                throw new InvalidOperationException(msg, ex);
            }
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override Task<StoreResult> DeleteWebHookAsync(string user, string id) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            user = NormalizeKey(user);
            //StoreResult result;
            try {
                WebhooksAccessor webHookAcc = new WebhooksAccessor();
                // result = webHookAcc.DeleteWebHook(user, id);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.CurrentCulture, "General error during '{0}' operation: '{1}'.", "Lookup", ex.Message);
                throw new InvalidOperationException(msg, ex);
            }
            return Task.FromResult(new StoreResult());
        }

        /// <inheritdoc />
        public override Task DeleteAllWebHooksAsync(string user) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }

            user = NormalizeKey(user);
            //StoreResult result;
            try {
                WebhooksAccessor webHookAcc = new WebhooksAccessor();
                // result = webHookAcc.DeleteAllWebHook(user);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.CurrentCulture, "General error during '{0}' operation: '{1}'.", "Lookup", ex.Message);
                throw new InvalidOperationException(msg, ex);
            }
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public override Task<ICollection<WebHook>> QueryWebHooksAcrossAllUsersAsync(IEnumerable<string> actions, Func<WebHook, string, bool> predicate) {
            if (actions == null) {
                throw new ArgumentNullException(nameof(actions));
            }

            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }

            var matches = new List<WebHook>();
            WebhooksAccessor webHookAcc = new WebhooksAccessor();
            matches = webHookAcc.GetQueryWebHooksAcrossAllUsers(actions, predicate);

            return Task.FromResult<ICollection<WebHook>>(matches);
        }
    }
}
