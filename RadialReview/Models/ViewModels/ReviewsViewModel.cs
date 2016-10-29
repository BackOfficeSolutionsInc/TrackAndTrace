using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;

namespace RadialReview.Models.ViewModels
{
    public class ReviewsViewModel 
    {

        public ReviewModel UserReview { get; set; }

        public ReviewsModel Review {get;set;}

        public long? TakableId { get; set; }
        public bool Viewable { get; set; }
        public bool Editable { get; set; }

		public List<AnswerModel> SurveyAnswers { get; set; }  

		public List<MvcHtmlString> DropdownLinks { get; set; }
		public SimpleAnswerLookup SimpleAnswersLookup { get; set; }

		//public Dictionary<long,RatioModel> Completed { get; set; }
		//public RatioModel Signed { get; set; }

		public ReviewsViewModel(ReviewsModel review)
        {
            Review = review;
			SurveyAnswers = new List<AnswerModel>();
			DropdownLinks = new List<MvcHtmlString>();
		}

		public void AddLink(string href, string text, string iconClass = "", string linkClass = "") {
			DropdownLinks.Add(new MvcHtmlString("<a class='"+linkClass+"' href='" + href + "'><span class='"+ iconClass + "'></span> " + HttpUtility.HtmlEncode(text) + "</a>"));
		}

		public void AddDivider() {
			if (DropdownLinks.Any() && DropdownLinks.Last() != null) {
				DropdownLinks.Add(null);
			}
		}
	}
}