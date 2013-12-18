using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Template
{
    public class TemplateModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual long CreatedById { get; set; }
        public virtual long OrganizationId { get; set; }
        public virtual List<TemplateItem> Items { get; set; }
    }

    public class TemplateModelMap : ClassMap<TemplateModel>
    {
        public TemplateModelMap()
        {
            Id(x => x.Id);
            Map(x => x.CreatedById);
            Map(x => x.OrganizationId);
            HasMany(x => x.Items).Not.LazyLoad();
        }
    }
}