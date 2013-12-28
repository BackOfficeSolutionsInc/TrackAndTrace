using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class AdditionalReviewViewModel
    {
        public long Id { get; set; }
        public long User { get; set; }
        public long Page { get; set; }
        public List<SelectListItem> Possible { get; set; }
    }
}