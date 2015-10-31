using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class InvoiceModel : ILongIdentifiable
    {
	    public virtual long Id { get; protected set; }
		public virtual DateTime InvoiceSentDate { get; set; }
		public virtual DateTime InvoiceDueDate { get; set; }
		public virtual DateTime CreateTime { get; set; }
        public virtual OrganizationModel Organization { get; set; }
        public virtual IList<InvoiceItemModel> InvoiceItems { get;set;}

		public virtual DateTime? PaidTime { get; set; }

		public virtual String TransactionId { get; set; }

		public virtual DateTime ServiceStart { get; set; }
	    public virtual DateTime ServiceEnd { get; set; }

	    public virtual decimal AmountDue { get; set; }

	    public InvoiceModel()
        {
            InvoiceItems = new List<InvoiceItemModel>();
	        CreateTime = DateTime.UtcNow;
        }
        /*
        public override int GetHashCode()
        {
            if (Id != 0)
                return (int)Id;
            else
                return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is InvoiceModel)
            {
                if (Id != 0)
                    return ((InvoiceModel)obj).Id == Id;
                else
                    return base.Equals(obj);
            }
            return false;
        }*/
    }

    public class InvoiceModelMap : ClassMap<InvoiceModel>
    {
        public InvoiceModelMap()
        {
            Id(x => x.Id);
			Map(x => x.InvoiceSentDate);
			Map(x => x.InvoiceDueDate);
			Map(x => x.PaidTime);
			Map(x => x.CreateTime);
			Map(x => x.TransactionId);

			Map(x => x.AmountDue);

			Map(x => x.ServiceStart);
			Map(x => x.ServiceEnd);

			References(x => x.Organization);
            HasMany(x => x.InvoiceItems)
                .Table("InvoiceItems")
                .Cascade.SaveUpdate();
        }

    }
}