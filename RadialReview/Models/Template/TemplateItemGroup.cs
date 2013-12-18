using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Template
{
    public class TemplateItemGroup : TemplateItem
    {
        public virtual List<TemplateItem> Items { get; set; }
    }

    public class TemplateItemGroupMap : SubclassMap<TemplateItemGroup>
    {
        public TemplateItemGroupMap()
	    {
            HasMany(x => x.Items).Not.LazyLoad();
	    }

    }
}