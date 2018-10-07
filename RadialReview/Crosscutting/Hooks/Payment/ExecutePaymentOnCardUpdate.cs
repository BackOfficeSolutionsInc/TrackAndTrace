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
using RadialReview.Exceptions;
using FluentNHibernate.Mapping;

namespace RadialReview.Hooks.CrossCutting {
	public class ExecutePaymentCardUpdate : IPaymentHook {
		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}

		public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public int Test_ThrowExceptionOn = -1;

		public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
			var org = s.Get<OrganizationModel>(token.OrganizationId);
			var plan = org.PaymentPlan;
			if (org.PaymentPlan.FreeUntil < DateTime.UtcNow) {
				var failed = s.QueryOver<InvoiceModel>().Where(x =>
									x.Organization.Id == token.OrganizationId &&
									x.PaidTime == null &&
									x.ForgivenBy == null &&
									x.AmountDue > 0
							).List().ToList();
				var now = DateTime.UtcNow;
				var i = 0;
				foreach (var f in failed) {
					try {
						#region TestOnly
						if (i == Test_ThrowExceptionOn) { throw new TestException(); }
						#endregion
						await PaymentAccessor.Unsafe.ExecuteInvoice(s, f, Config.IsLocal());
					} catch (Exception e) {
						var a = 0;
					}
					i += 1;
				}
			}
		}

		public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
			if (firstAttempt)
				s.Save(new PaymentFailRecord() {
					OrgId = orgId,
					ExecuteTime = executeTime,
					Type = PaymentExceptionType.Uncaptured,
				});
		}

		public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
			if (firstAttempt) {
				s.Save(new PaymentFailRecord() {
					OrgId = orgId,
					ExecuteTime = executeTime,
					Type = e.Type
				});
			}
		}

		public class PaymentFailRecord {
			public virtual long Id { get; set; }
			public virtual DateTime ExecuteTime { get; set; }
			public virtual long OrgId { get; set; }
			public virtual PaymentExceptionType Type { get; set; }
			public virtual DateTime? Resolved { get; set; }

			public class Map : ClassMap<PaymentFailRecord> {
				public Map() {
					Id(x => x.Id);
					Map(x => x.ExecuteTime);
					Map(x => x.OrgId);
					Map(x => x.Type).CustomType<PaymentExceptionType>();
					Map(x => x.Resolved);
				}
			}
		}
	}
}