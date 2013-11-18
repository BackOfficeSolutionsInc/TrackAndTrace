using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class PaymentPlanModel
    {
        public virtual long Id { get; protected set; }
        public virtual String Description { get; set; }
        public virtual DateTime PlanCreated { get; set; }
        public virtual Boolean IsDefault { get; set; }

    }

    public class PaymentPlanModelMap : ClassMap<PaymentPlanModel>
    {
        public PaymentPlanModelMap()
        {
            Id(x => x.Id);
            Map(x => x.IsDefault);
            Map(x => x.Description);
            Map(x => x.PlanCreated);

        }
    }
}