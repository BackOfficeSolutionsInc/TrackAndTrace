using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Controllers;
using RadialReview.Models.Askables;

namespace RadialReview.Models.Reviews
{
    public class CustomizeSelector
    {
        public String Name { get; set; }
        public String UniqueId { get; set; }
        public List<WhoReviewsWho> Pairs { get; set; }
    }

    public class CustomizeModel
    {
        public List<Reviewer> Reviewers { get; set; }
		public List<Reviewee> AllReviewees { get; set; }
        public List<CustomizeSelector> Selectors { get; set; }
        public List<WhoReviewsWho> Selected { get; set; }

		public List<SelectListItem> Periods { get; set; }
		public List<long> MasterList { get; internal set; }
	}
}