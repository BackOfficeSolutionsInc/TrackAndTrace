using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;

namespace RadialReview.Controllers
{
    public partial class L10Controller : BaseController
    {

		[Access(AccessLevel.UserOrganization)]
		public object Details(string id = null,bool complete=false)
		{
			if (id == null)
				return View();

			switch (id.ToLower())
			{
				case "todo": return DetailsTodo(complete);
				case "issues": return DetailsIssues();
				case "scorecard": return DetailsScorecard();
				case "recent": return DetailsRecent();
				default:throw new PermissionsException("Page does not exist");
			}
		}

	    private PartialViewResult DetailsTodo(bool complete)
	    {
		    L10Accessor.GetVisibleTodos(GetUser(), GetUser().Id, complete);
	    }



    }
}