using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Template
{
    public abstract class TemplateItem : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual long Ordering { get; set; }
        public virtual String Title { get; set; }
        public virtual long CreatedById { get; set; }
    }

    public class TemplateItemMap : ClassMap<TemplateItem>
    {
        public TemplateItemMap()
        {
            Id(x => x.Id);
            Map(x => x.Title);
            Map(x => x.Ordering);
            Map(x => x.CreatedById);
        }
    }
}