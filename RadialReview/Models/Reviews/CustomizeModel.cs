using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Controllers;

namespace RadialReview.Models.Reviews
{
    public class CustomizeSelector
    {
        public String Name { get; set; }
        public String UniqueId { get; set; }
        public List<Tuple<long, long>> Pairs { get; set; }
    }

    public class CustomizeModel
    {

        public List<UserOrganizationModel> Subordinates { get; set; }
        public List<UserOrganizationModel> AllUsers { get; set; }
        public List<CustomizeSelector> Selectors { get; set; }
        public List<Tuple<long, long>> Selected { get; set; }

		public List<SelectListItem> Periods { get; set; } 

    }
}