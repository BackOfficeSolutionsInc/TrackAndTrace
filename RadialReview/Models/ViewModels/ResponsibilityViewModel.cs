using System.Web.Mvc;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
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
		public Boolean Active { get; set; }
		public Boolean Anonymous { get; set; }
		public Boolean Required { get; set; }
        public String NewCategory { get; set; }
        public WeightType Weight { get; set; }

		public long TypeSelected { get; set; }
		public List<SelectListItem> TypeDropdown { get; set; } 

        public ResponsibilityViewModel()
        {

        }

        public ResponsibilityViewModel(long responsibilityGroupId, ResponsibilityModel model, List<QuestionCategoryModel> categories)
        {
            Id=model.Id;
            Active = model.DeleteTime==null;
            Categories = categories;
            /*Categories.Add(new QuestionCategoryModel() { Id = -1, Category = new LocalizedStringModel("<" + DisplayNameStrings.createNew + ">") });

			TypeDropdown = new List<SelectListItem>();
			TypeDropdown.AddRange(categories.Select(x=>new SelectListItem(){
				Text = x.Category.Translate() + @" Slider" ,
				Value = ""+x.Id,
	        }));

			TypeDropdown.Add(new SelectListItem() { Text = "Thumbs", Value = "-9" });
			TypeDropdown.Add(new SelectListItem() { Text = "Text", Value = "-10" });

	        switch(model.GetQuestionType()){
		        case QuestionType.Slider:   
					if (model.Category.Id > 0)
						TypeSelected = model.Category.Id;
			        break;
		        case QuestionType.Thumbs:
			        TypeSelected = -9;
			        break;
		        case QuestionType.Feedback:
			        TypeSelected = -10;
			        break;
		        default:
			        throw new ArgumentOutOfRangeException();
	        }*/

	        Required = model.Required;

            Responsibility = model.Responsibility;
            //CategoryId = model.Category.Id;
            ResponsibilityGroupId = responsibilityGroupId;
            Weight = model.Weight;
        }

    }
}