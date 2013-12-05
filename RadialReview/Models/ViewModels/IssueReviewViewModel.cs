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

        public List<SelectListItem> PotentialTeams { get;set;}

        public String Date { get; set; }
        public String Name { get; set; }
                
    }
}