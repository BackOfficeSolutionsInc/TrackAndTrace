using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;


namespace RadialReview.Models.Template
{
    public class TemplateItemChart : TemplateItem
    {
        public virtual QuestionCategoryModel XAxis { get; set; }
        public virtual QuestionCategoryModel YAxis { get; set; }
        public virtual String ChartTitle { get; set; }
    }

    public class TemplateItemChartMap : SubclassMap<TemplateItemChart>
    {
        public TemplateItemChartMap()
        {
            Map(x => x.ChartTitle);
            References(x => x.XAxis).Not.LazyLoad().ReadOnly();
            References(x => x.YAxis).Not.LazyLoad().ReadOnly();
        }
    }
}