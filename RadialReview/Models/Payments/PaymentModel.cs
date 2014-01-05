using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class PaymentModel : ILongIdentifiable
    {
        public virtual long Id { get; protected set; }

        public virtual DateTime PaymentDate { get; set; }
         
        [Column(TypeName = "Money")]
        public virtual decimal PaymentAmount { get; set; }

        public virtual Currency Currency { get; set; }
        public virtual OrganizationModel Organization { get; set; }
       
    }

    public class PaymentModelMap : ClassMap<PaymentModel>
    {
        public PaymentModelMap()
        {
            Id(x => x.Id);
            Map(x => x.PaymentDate);
            Map(x => x.PaymentAmount);
            Map(x => x.Currency);
            References(x => x.Organization);
        }
    }
}