using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class OrganizationTeamsViewModel
    {
        public List<OrganizationTeamViewModel> Teams { get; set; }
    }

    public class OrganizationTeamViewModel
    {
        public OrganizationTeamModel Team {get;set;}
        public int Members { get; set; }
    }

    public class OrganizationTeamCreateViewModel
    {
        public long TeamId { get;set;}
        public String TeamName { get; set; }
        public long ManagerId { get; set; }
        public bool InterReview { get; set; }
        public List<SelectListItem> PotentialManagers { get; set; }
	    public bool Standard { get; set; }

	    public OrganizationTeamCreateViewModel()
        {
                    
        }

        public OrganizationTeamCreateViewModel(UserOrganizationModel caller, OrganizationTeamModel team, List<SelectListItem> potentialManagers)
        {
            TeamId = team.Id;
            TeamName = team.Name;
            ManagerId = caller.Id;
            if (team.ManagedBy != 0)
                ManagerId = team.ManagedBy;
            InterReview = team.InterReview;
            PotentialManagers = potentialManagers;
	        Standard = team.Type == TeamType.Standard;
        }
    }
}