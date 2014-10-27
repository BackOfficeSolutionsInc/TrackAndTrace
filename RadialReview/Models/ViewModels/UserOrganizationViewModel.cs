using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.UserModels;

namespace RadialReview.Models.ViewModels
{
    public class CreateUserOrganizationViewModel
    {
        public UserPositionViewModel Position { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String Email { get; set; }
        public bool IsManager { get; set; }
        public long OrgId { get; set; }
        public bool StrictlyHierarchical { get; set; }
        public long ManagerId { get; set; }
        public List<SelectListItem> PotentialManagers { get; set; }
    }

    public class EditUserOrganizationViewModel
    {
        public long UserId { get; set; }
        public bool IsManager { get; set; }
        //public long ManagerId { get; set; }
        public Boolean ManagingOrganization { get; set; }
        public Boolean CanSetManagingOrganization { get; set; }

        //public bool StrictlyHierarchical { get; set; }
        //public List<SelectListItem> PotentialManagers { get; set; }
    }

    public class UserViewModel : ICompletable
    {
        public UserModel User { get; set; }

        public int ReviewToComplete { get; set; }

        public ICompletionModel GetCompletion(bool split = false)
        {
            int complete = 1;
            int total = 1;

            complete += (User.ImageGuid != null).ToInt(); 
            total++;

            return new CompletionModel(complete, total);
        }
    }

    public class UserOrganizationDetails
    {
		public UserOrganizationModel User { get; set; }
		public List<String> Responsibilities { get; set; }
		public List<RoleModel> Roles { get; set; }
    }
}