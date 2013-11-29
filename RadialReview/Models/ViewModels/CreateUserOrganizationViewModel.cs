using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class CreateUserOrganizationViewModel
    {
        public List<OrganizationPositionModel> Positions { get; set; }
        public long Position { get; set; }
        public String Email { get; set; } 
        public bool Manager { get; set; }
        public long OrganizationId { get; set; }
    }
}