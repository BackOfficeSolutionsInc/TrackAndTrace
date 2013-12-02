using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class CreateUserOrganizationViewModel
    {
        public UserPositionViewModel Position { get; set; }
        public String Email { get; set; } 
        public bool IsManager { get; set; }
        public long OrganizationId { get; set; }

        public bool StrictlyHierarchical { get;set; }
        public long ManagerId { get; set; }
        public List<SelectListItem> PotentialManagers { get; set; }
    }
}