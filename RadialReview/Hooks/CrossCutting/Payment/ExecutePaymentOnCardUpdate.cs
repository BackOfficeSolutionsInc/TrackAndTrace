using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Payments;
using System.Threading.Tasks;
using RadialReview.Models;
using RadialReview.Accessors;
using RadialReview.Utilities;

namespace RadialReview.Hooks.CrossCutting {
    public class ExecutePaymentOnFirstCardUpdate : IPaymentHook {
        public bool CanRunRemotely() {
            return false;
        }
        public HookPriority GetHookPriority() {
            return HookPriority.Low;
        }

        public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
           //noop
        }


        public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
            var org = s.Get<OrganizationModel>(token.OrganizationId);
            var plan = org.PaymentPlan;
            if (org.PaymentPlan.FreeUntil < DateTime.UtcNow)
            if (org.PaymentPlan.LastExecuted == null || (DateTime.UtcNow - org.PaymentPlan.LastExecuted) > TimeSpan.FromDays(31)) {
                var executeTime = DateTime.UtcNow.Date;
                try {
                    await PaymentAccessor.ChargeOrganization_Unsafe(s, org, plan, executeTime, Config.IsLocal());
#pragma warning restore CS0618 // Type or member is obsolete
                } finally {
                }
            }
        }
    }
}