using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class OrganizationViewModel
    {
        public long Id { get; set; }
        public String OrganizationName { get; set; }
        public Boolean ManagersCanEdit { get; set; }
        public Boolean StrictHierarchy { get; set; }
        public Boolean ManagersCanEditPositions { get; set; }
        public String ImageUrl { get; set; }
    }
}