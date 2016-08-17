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
            
            public long? FocusNode { get; internal set; }
        }


        [Access(AccessLevel.UserOrganization)]
		public ActionResult Chart(long? id=null, long? user = null, long? node = null)
        {
	        var u = GetUser();

            var idr = id ?? u.Organization.AccountabilityChartId;

            if (node == null && user != null)
                node = AccountabilityAccessor.GetNodesForUser(GetUser(), user.Value).FirstOrDefault().NotNull(x=>(long?)x.Id);

			return View(new AccountabilityChartVM() {
		        UserId = u.Id,
				OrganizationId = u.Organization.Id,
                ChartId = idr,
                FocusNode = node
            });
	    }
    }
}