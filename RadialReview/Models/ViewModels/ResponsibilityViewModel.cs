using RadialReview.Models.Responsibilities;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ResponsibilityViewModel
    {
        public long Id { get; set; }
        public String Responsibility { get;set;}
        public List<QuestionCategoryModel> Categories { get; set; }
        public long CategoryId { get; set; }
        public long ResponsibilityGroupId { get; set; }
        public Boolean Active { get;set;}
        public String NewCategory { get; set; }
        public WeightType Weight { get; set; }

        public ResponsibilityViewModel()
        {

        }

        public ResponsibilityViewModel(long responsibilityGroupId, ResponsibilityModel model, List<QuestionCategoryModel> categories)
        {
            Id=model.Id;
            Active = model.DeleteTime==null;
            Categories = categories;
            Categories.Add(new QuestionCategoryModel() { Id = -1, Category = new LocalizedStringModel("<" + DisplayNameStrings.createNew + ">") });
            Responsibility = model.Responsibility;
            CategoryId = model.Category.Id;
            ResponsibilityGroupId = responsibilityGroupId;
            Weight = model.Weight;
        }

    }
}