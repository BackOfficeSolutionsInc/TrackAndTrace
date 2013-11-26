using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class InvoiceModel
    {
        public virtual long Id { get; protected set; }
        public virtual DateTime InvoiceSentDate { get; set; }
        public virtual DateTime InvoiceDueDate { get; set; }
        public virtual OrganizationModel Organization { get; set; }
        public virtual IList<InvoiceItemModel> InvoiceItems { get;set;}


        public InvoiceModel()
        {
            InvoiceItems = new List<InvoiceItemModel>(); 
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
            References(x => x.Organization);
            HasMany(x => x.InvoiceItems)
                .Table("InvoiceItems")
                .Cascade.SaveUpdate();
        }

    }
}