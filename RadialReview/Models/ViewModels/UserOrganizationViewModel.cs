﻿using RadialReview.Models.Responsibilities;
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
        public long ManagerId { get; set; }
        public bool StrictlyHierarchical { get; set; }
        public List<SelectListItem> PotentialManagers { get; set; }
    }
}