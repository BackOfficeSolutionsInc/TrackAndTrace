using RadialReview.Utilities.DataTypes;
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

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Data(long id)
        {
            var tree = _OrganizationAccessor.GetOrganizationTree(GetUser(), id);

            var c=new Chart() {
                height = "80%",
                width = "80%",
                data = tree,
            };

            return Json( c, JsonRequestBehavior.AllowGet);
        }

    }
}