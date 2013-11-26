using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class IssueReviewViewModel
    {
        public DateTime Today { get; set; }
        public List<UserOrganizationModel> ForUsers { get; set; }

    }
}