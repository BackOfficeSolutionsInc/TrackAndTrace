using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class OrgReviewsViewModel : IPagination
    {
        public List<ReviewsViewModel> Reviews { get; set; }
        public List<TemplateViewModel> Templates { get; set; }

        public double NumPages { get; set; }

        public int Page { get; set; }
    }
}