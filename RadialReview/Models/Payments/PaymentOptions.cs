using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments
{
    public class PaymentOptions
    {
        public static PaymentPlanModel MonthlyPlan(decimal l10Price_Per_Person, decimal reviewPrice_Per_Person, DateTime freeUntil, int firstN_users_free){
            return new PaymentPlan_Monthly()
            {
                PlanCreated = DateTime.UtcNow,
                L10PricePerPerson = l10Price_Per_Person,
                ReviewPricePerPerson = reviewPrice_Per_Person,
                FreeUntil = Math2.Max(DateTime.UtcNow.Date,freeUntil.Date),
                FirstN_Users_Free = Math.Max(0,firstN_users_free),
            };
        }
    }
}