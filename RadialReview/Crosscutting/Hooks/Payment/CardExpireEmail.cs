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
using RadialReview.Models.Application;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Accessors;
using RadialReview.Variables;

namespace RadialReview.Crosscutting.Hooks.Payment {
    public class CardExpireEmail : IPaymentHook {
        public bool CanRunRemotely() {
            return false;
        }

        public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
            var org = s.Get<OrganizationModel>(token.OrganizationId);
            var admins  = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null && x.ManagingOrganization == true && x.Id == token.OrganizationId).List().ToList();
            var emails = new List<Mail>();
            foreach (var a in admins) {
                var mail = Mail
                    .To("CardExpire", a.GetEmail())
                    .SubjectPlainText("[Action Required] Traction® Tools - Update Payment Information")
                    .Body(EmailStrings.UpdateCard_Body, Config.ProductName(), Config.BaseUrl(null));
                mail.ReplyToAddress = s.GetSettingOrDefault("SupportEmail", "client-success@mytractiontools.com");
                mail.ReplyToName = "Traction Tools Support";
            }
            await Emailer.SendEmails(emails);
        }

        public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
            //noop
        }

        public HookPriority GetHookPriority() {
            //noop
            return HookPriority.Low;
        }

        public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e,bool firstAttempt) {
            //noop
        }

        public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
            //noop
        }

        public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token) {
            //noop
        }

        public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
            //noop
        }
    }
}