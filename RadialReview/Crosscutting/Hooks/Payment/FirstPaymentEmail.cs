using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Payments;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Models.Application;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Variables;
using RadialReview.Exceptions;
using RadialReview.Properties;

namespace RadialReview.Hooks.CrossCutting.Payment {
	public class FirstPaymentEmail : IPaymentHook {
		public bool CanRunRemotely() {
			return false;
		}

		public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
			var creator = s.Get<UserOrganizationModel>(token.CreatedBy);
			if (creator != null) {
				var mail = Mail
					.To("FirstCharge", creator.GetEmail())
					.SubjectPlainText("Welcome to Traction® Tools")
					.Body(EmailStrings.FirstCharge_Body, new string[] { });
				mail.ReplyToAddress = s.GetSettingOrDefault("SupportEmail", "client-success@mytractiontools.com");
				mail.ReplyToName = "Traction Tools Support";

				await Emailer.SendEmail(mail);
			}
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Low;
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
			//noop
		}
	}
}