using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Payments;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Exceptions;

namespace RadialReview.Utilities.Hooks {
    public interface IPaymentHook : IHook {
        Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token);

        Task CardExpiresSoon(ISession s, PaymentSpringsToken token);

        Task UpdateCard(ISession s, PaymentSpringsToken token);
        Task SuccessfulCharge(ISession s, PaymentSpringsToken token);

        Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage,bool firstAttempt);
        Task PaymentFailedCaptured(ISession s, long orgId,DateTime executeTime, PaymentException e,bool firstAttempt);
    }
}