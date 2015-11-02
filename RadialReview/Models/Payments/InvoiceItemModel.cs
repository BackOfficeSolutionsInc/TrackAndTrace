using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace RadialReview.Models
{
    public class InvoiceItemModel : ILongIdentifiable, IHistorical
    {
	    public virtual long Id { get; protected set; }

		[Column(TypeName = "Quantity")]
		public virtual decimal? Quantity { get; set; }
        [Column(TypeName="Money")]

		[Display(Name = "Price/Item"), DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public virtual decimal? PricePerItem { get; set; }

        [Column(TypeName = "Money")]
		[Display(Name = "Price/Item"), DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public virtual decimal AmountDue { get; set; }

        public virtual Currency Currency { get; set; }

		[Display(Name = "Item")]
	    public virtual String Name { get; set; }

		[Display(Name = "Description")]
		public virtual String Description { get; set; }

	    public virtual InvoiceModel ForInvoice { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

	    public InvoiceItemModel()
	    {
		    CreateTime = DateTime.UtcNow;
	    }
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
			Map(x => x.Name);
			Map(x => x.Description);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
            References(x => x.ForInvoice);
        }
    }
}
