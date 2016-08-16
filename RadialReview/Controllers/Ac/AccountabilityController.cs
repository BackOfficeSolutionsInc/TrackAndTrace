using RadialReview.Accessors;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Json;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
	public partial class AccountabilityController : BaseController
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
            public long ChartId;

			public long? FocusUser { get; internal set; }
		}


        [Access(AccessLevel.UserOrganization)]
		public ActionResult Chart(long? id=null,long? user=null){
	        var u = GetUser();

            var idr = id ?? u.Organization.AccountabilityChartId;

			return View(new AccountabilityChartVM() {
		        UserId = u.Id,
				OrganizationId = u.Organization.Id,
                ChartId = idr,
				FocusUser = user
	        });
	    }
    }
}