using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Template
{
    public class TemplateItemFeedback : TemplateItem
    {
    }

    public class TemplateItemFeedbackMap : SubclassMap<TemplateItemFeedback>
    {
        public TemplateItemFeedbackMap()
        {
        }
    }
}