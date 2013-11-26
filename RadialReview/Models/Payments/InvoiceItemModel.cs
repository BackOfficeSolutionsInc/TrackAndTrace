﻿using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace RadialReview.Models
{
    public class InvoiceItemModel
    {
        public virtual long Id { get; protected set; }

        public virtual int? Quantity { get; set; }
        [Column(TypeName="Money")]
        public virtual decimal? PricePerItem { get; set; }

        [Column(TypeName = "Money")]
        public virtual decimal AmountDue { get; set; }

        public virtual Currency Currency { get; set; }

        public virtual InvoiceModel ForInvoice { get; set; }
       
    }

    public class InvoiceItemModelMap : ClassMap<InvoiceItemModel>
    {
        public InvoiceItemModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Quantity);
            Map(x => x.PricePerItem);
            Map(x => x.AmountDue);
            Map(x => x.Currency);
            References(x => x.ForInvoice);
        }
    }
}
