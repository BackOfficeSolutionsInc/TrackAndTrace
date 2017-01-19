﻿using System.Web.Mvc;
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
		public AboutType[] OnlyAsk { get; set; }
		public QuestionType QuestionType { get; set; }
		public long? SectionId { get; set; }
		public List<SelectListItem> SectionDropdown { get; set; }
		public string Arguments { get; set; }

		public bool UpdateOutstandingReviews { get; set; }


		public long TypeSelected { get; set; }
		public List<SelectListItem> TypeDropdown { get; set; } 

        public ResponsibilityViewModel(){

        }

        public ResponsibilityViewModel(long responsibilityGroupId, ResponsibilityModel model, List<QuestionCategoryModel> categories,List<AskableSectionModel> sections)
        {
            Id=model.Id;
            Active = model.DeleteTime==null;
            Categories = categories;
			SectionDropdown = sections.ToSelectList(x => x.Name, x => x.Id, model.SectionId,true);
			SectionDropdown.Add(new SelectListItem() { Text = "<none>", Value = "null", Selected = model.SectionId == null });
			SectionDropdown = SectionDropdown.OrderBy(x => x.Text).ToList();
			SectionId = model.SectionId;
			QuestionType = model.GetQuestionType();
			Arguments = model.Arguments;          

	        Required = model.Required;

            Responsibility = model.Responsibility;
            ResponsibilityGroupId = responsibilityGroupId;
            Weight = model.Weight;
			OnlyAsk = model.OnlyAsk.GetFlags<AboutType>().ToArray();
			UpdateOutstandingReviews = true;
        }

    }
}