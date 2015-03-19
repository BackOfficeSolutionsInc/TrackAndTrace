using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Askables;

namespace RadialReview.Models.ViewModels
{
    public class OrganizationViewModel
    {
        public long Id { get; set; }
        public String OrganizationName { get; set; }
        public Boolean ManagersCanEdit { get; set; }
        public Boolean StrictHierarchy { get; set; }
        public Boolean ManagersCanEditPositions { get; set; }
        public Boolean SendEmailImmediately { get; set; }
        public bool ManagersCanRemoveUsers { get; set; }
		public String ImageUrl { get; set; }
		public Boolean ManagersCanEditSelf { get; set; }
		public Boolean EmployeesCanEditSelf { get; set; }

		public List<String> CompanyValues { get; set; }
		public List<RockModel> CompanyRocks { get; set; } 

    }
}