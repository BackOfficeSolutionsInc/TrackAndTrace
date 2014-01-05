using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class IssueReviewViewModel
    {
        public DateTime Today { get; set; }
        public List<UserOrganizationModel> ForUsers { get; set; }
        public long ForTeamId { get; set; }

        public List<SelectListItem> PotentialTeams { get; set; }

        public String Date { get; set; }
        public String Name { get; set; }
        public bool Emails { get; set; }
        public bool ReviewSelf { get; set; }
        public bool ReviewPeers { get; set; }
        public bool ReviewManagers { get; set; }
        public bool ReviewTeammates { get; set; }
        public bool ReviewSubordinates { get; set; }

        public IssueReviewViewModel()
        {
            Emails = false;

            ReviewSelf = true;
            ReviewPeers = true;
            ReviewManagers = true;
            ReviewTeammates = true;
            ReviewSubordinates = true;
        }
    }
}