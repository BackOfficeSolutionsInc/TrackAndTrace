using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class PaymentAccessor :BaseAccessor
    {
        public PaymentPlanModel BasicPaymentPlan()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PaymentPlanModel basicPlan=null;
                    try{
                        basicPlan = s.QueryOver<PaymentPlanModel>().Where(x => x.IsDefault).SingleOrDefault();
                    }catch(Exception e){
                        log.Error(e);
                    }
                    if (basicPlan == null)
                    {
                        basicPlan = new PaymentPlanModel() { 
                            Description = "Employee count model",
                            IsDefault = true,
                            PlanCreated=DateTime.UtcNow
                        };
                        s.Save(basicPlan);
                        tx.Commit();
                        s.Flush();
                    }
                    return basicPlan;
                }
            }
        }
    }
}