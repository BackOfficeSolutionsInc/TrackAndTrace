using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Payments;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
    public interface IPaymentHook : IHook {
        Task UpdateCard(ISession s, PaymentSpringsToken token);
        Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token);
    }
}