using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models.Payments;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Crosscutting.Flags;
using RadialReview.Models;

namespace RadialReview.Crosscutting.Hooks.Payment {
    public class SetDelinquentFlag : IPaymentHook, IInvoiceHook {

        public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
            await OrganizationAccessor.AddFlag(s, PermissionsUtility.CreateAdmin(s), orgId, OrganizationFlagType.Delinquent);
        }

        public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
            await OrganizationAccessor.AddFlag(s, PermissionsUtility.CreateAdmin(s), orgId, OrganizationFlagType.Delinquent);
        }

        private async Task AreAllPaid(ISession s, long orgId) {
            var allInvoice = s.QueryOver<InvoiceModel>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).List().ToList();
            if (allInvoice.All(x => !x.AnythingDue())) {
                await OrganizationAccessor.RemoveFlag(s, PermissionsUtility.CreateAdmin(s), orgId, OrganizationFlagType.Delinquent);
            }else {             
                await OrganizationAccessor.AddFlag(s, PermissionsUtility.CreateAdmin(s), orgId, OrganizationFlagType.Delinquent);
            }
        }

        public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token) {
            await AreAllPaid(s, token.OrganizationId);
        }

        public async Task UpdateInvoice(ISession s, InvoiceModel invoice, IInvoiceUpdates updates) {
            await AreAllPaid(s, invoice.Organization.Id);
        }

        public bool CanRunRemotely() {
            return false;
        }

        public HookPriority GetHookPriority() {
            return HookPriority.Low;
        }
        #region noop
        public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
            //noop
        }

        public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
            //noop
        }
        public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
            //noop
        }

        public async Task InvoiceCreated(ISession s, InvoiceModel invoice) {
            //noop
        }
        #endregion
    }
}