using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Prereview;

namespace RadialReview.Models.ViewModels
{
    public class ReviewsViewModel 
    {

        public ReviewModel UserReview { get; set; }

		public ReviewsModel Review { get; set; }
		public PrereviewModel Prereview { get; set; }

		public long? TakableId { get; set; }
        public bool Viewable { get; set; }
        public bool Editable { get; set; }
		public bool IsPrereview { get; set; }

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

		public ReviewsViewModel(PrereviewModel prereview) {
			Prereview = prereview;
			Review = prereview._ReviewContainer;
			IsPrereview = true;
			SurveyAnswers = new List<AnswerModel>();
			DropdownLinks = new List<MvcHtmlString>();
		}

		public void AddLink(string href, string text, string iconClass = "", string linkClass = "") {
			DropdownLinks.Add(new MvcHtmlString("<a class='"+linkClass+"' href='" + href + "'><span class='"+ iconClass + "'></span> " + HttpUtility.HtmlEncode(text) + "</a>"));
		}
		public void AddAction(string javascript, string text, string iconClass = "", string linkClass = "") {
			DropdownLinks.Add(new MvcHtmlString("<a class='" + linkClass + "' href='#' onclick='"+javascript+"'><span class='" + iconClass + "'></span> " + HttpUtility.HtmlEncode(text) + "</a>"));
		}

		public DateTime GetDueDate() {
			if (IsPrereview) {
				return Prereview.PrereviewDue.Subtract(TimeSpan.FromDays(1));
			} else {
				return Review.DueDate.Subtract(TimeSpan.FromDays(1));
			}
		}

		public MvcHtmlString GetTitle() {

			if (IsPrereview) {
				return new MvcHtmlString("<a href='/Prereview/Customize/"+ Prereview.Id+"' >" + Review.ReviewName + "<small></small></a>");
			} else {
				if (TakableId != null) {
					return new MvcHtmlString("<a href='/Review/Take/"+Review.Id+"' >" + Review.ReviewName + "</a>");
				} else {
					return new MvcHtmlString(Review.ReviewName);
				}
			}
		}

		public void AddDivider() {
			if (DropdownLinks.Any() && DropdownLinks.Last() != null) {
				DropdownLinks.Add(null);
			}
		}
	}
}