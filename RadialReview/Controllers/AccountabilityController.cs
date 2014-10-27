using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
	public class AccountabilityController : BaseController
    {
        // GET: Accountablity
        public ActionResult Index()
        {
            return View();
        }

		public class AccountabilityChartVM
	    {
		    public long UserId;
		    public long OrganizationId;
	    }


        [Access(AccessLevel.UserOrganization)]
		public ActionResult Chart()
        {
	        var u = GetUser();
			return View(new AccountabilityChartVM() {
		        UserId = u.Id,
				OrganizationId = u.Organization.Id,
	        });
	    }
    }
}