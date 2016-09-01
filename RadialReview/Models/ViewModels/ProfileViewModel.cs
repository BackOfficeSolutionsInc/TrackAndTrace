using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class ProfileViewModel
    {
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String ImageUrl { get;set; }

		//public bool EmailTodos { get; set; }
		public int? SendTodoTime { get; set; }

        public ManageUserViewModel Manage { get; set; }

	    public List<SelectListItem> PossibleTimes { get; set; }

        public string UserId { get; set; }

        public bool ShowScorecardColors { get; set; }
		
		public string PersonalTextNumber { get; set; }
		public bool LoggedIn { get; set; }
	}
}