using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class OrganizationTeamViewModel
    {
        public List<OrganizationTeamModel> Teams { get; set; }
    }

    public class OrganizationTeamCreateViewModel
    {
        public long TeamId { get;set;}
        public String TeamName { get; set; }
        public long ManagerId { get; set; }
        public bool InterReview { get; set; }
        public List<SelectListItem> PotentialManagers { get; set; }

        public OrganizationTeamCreateViewModel()
        {
                    
        }

        public OrganizationTeamCreateViewModel(OrganizationTeamModel team, List<SelectListItem> potentialManagers)
        {
            TeamId = team.Id;
            TeamName = team.Name;
            ManagerId = team.ManagedBy;
            InterReview = team.InterReview;
            PotentialManagers = potentialManagers;

        }
    }
}