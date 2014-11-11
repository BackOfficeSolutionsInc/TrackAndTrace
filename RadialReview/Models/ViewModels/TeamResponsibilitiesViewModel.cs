using RadialReview.Models.Askables;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class TeamResponsibilitiesViewModel
    {
        public OrganizationTeamModel Team { get; set; }
        public List<ResponsibilityModel> Responsibilities { get; set; }

    }
}