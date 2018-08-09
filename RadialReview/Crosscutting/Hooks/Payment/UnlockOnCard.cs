using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models.Payments;
using System.Threading.Tasks;
using RadialReview.Models;

namespace RadialReview.Crosscutting.Hooks.Payment {
    public class UnlockOnCard : IPaymentHook {
        public bool CanRunRemotely() {
            return false;
        }
        public HookPriority GetHookPriority() {
            return HookPriority.Highest;
        }

        public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
            //noop
        }

        public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
            //noop
        }
    
        public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
            //noop
        }

        public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
            //noop
        }

        public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token) {
            //noop
        }

        public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
            var o =s.Get<OrganizationModel>(token.OrganizationId);
            if (o.Lockout == LockoutType.Payment) {
                o.DeleteTime = null;
                o.Lockout = LockoutType.NoLockout;
                s.Update(o);
            }
        }
    }
}