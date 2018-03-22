using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RadialReview.Models {
	public class InvoiceModel : ILongIdentifiable, IHistorical {
		public virtual long Id { get; protected set; }
		[Display(Name = "Sent"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime InvoiceSentDate { get; set; }
		[Display(Name = "Due"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime InvoiceDueDate { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual IList<InvoiceItemModel> InvoiceItems { get; set; }

		public virtual long? ForgivenBy { get; set; }
		public virtual long? ManuallyMarkedPaidBy { get; set; }

		[Display(Name = "Date Paid"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime? PaidTime { get; set; }

		[Display(Name = "Transaction Id")]
		public virtual String TransactionId { get; set; }

		public virtual DateTime ServiceStart { get; set; }
		[Display(Name = "Service Through"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime ServiceEnd { get; set; }

		[Display(Name = "Amount Due"), DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
		public virtual decimal AmountDue { get; set; }

		public virtual String EmailAddress { get; set; }

		public virtual bool WasAutomaticallyPaid() {
			return ManuallyMarkedPaidBy == null && PaidTime != null;
		}
		public virtual bool AnythingDue() {
			return !(PaidTime != null || AmountDue <= 0 || ForgivenBy != null);
		}


		public InvoiceModel() {
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

	public class InvoiceModelMap : ClassMap<InvoiceModel> {
		public InvoiceModelMap() {
			Id(x => x.Id);
			Map(x => x.InvoiceSentDate);
			Map(x => x.InvoiceDueDate);
			Map(x => x.PaidTime);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
			Map(x => x.TransactionId);

			Map(x => x.ForgivenBy);
			Map(x => x.ManuallyMarkedPaidBy);

			Map(x => x.AmountDue);

			Map(x => x.EmailAddress);

			Map(x => x.ServiceStart);
			Map(x => x.ServiceEnd);

			References(x => x.Organization);
			HasMany(x => x.InvoiceItems)
				.Table("InvoiceItems")
				.Cascade.SaveUpdate();
		}

	}
}