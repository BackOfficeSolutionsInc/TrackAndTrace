using RadialReview.Models.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class TemplateViewModel
    {
        public string Name { get; set; }

        public List<QuestionCategoryModel> Categories { get; set; }

        public List<TemplateItem> Items { get; set; }

    }
}