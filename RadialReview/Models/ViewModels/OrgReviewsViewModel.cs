using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels {
	public class OrgReviewsViewModel : IPagination {
		public List<ReviewsViewModel> Reviews { get; set; }
		public List<TemplateViewModel> Templates { get; set; }

		public double NumPages { get; set; }

		public int Page { get; set; }

		public bool AllowEdit { get; set; }

		public MvcHtmlString CreateLabel(ReviewsViewModel review) {

			var clzz = "label label-";
			var txt = "";
			var hover = "";

			if (review.UserReview != null) {
				if (review.Review.DueDate > DateTime.UtcNow) {
					if (review.UserReview.Complete) {
						clzz += "success";
						txt = "complete";
						hover = "Evals completed.";
					} else if (review.UserReview.Started) {
						clzz += "warning";
						txt = "started";
						hover = "Evals incomplete.";
					} else {
						clzz += "default";
						txt = "start";
						hover = "Evals unstarted.";
					}
				} else {
					//if (review.UserReview.Complete) {
					clzz += "success";
					txt = "concluded";
					hover = "Evals have concluded.";
					//} else {
					//	//overdue
					//	clzz += "warning";
					//	txt = "concluded";
					//	hover = "This review has concluded but you have not completed it.";
					//}
				}
				return new MvcHtmlString("<span class='" + clzz + "' title='" + hover + "' style='display: block; top: 1px;position: relative;'>" + txt + "</span>");

			} else {
				clzz += "default";
				txt = "";
				return new MvcHtmlString("");

			}
		}
	}
}