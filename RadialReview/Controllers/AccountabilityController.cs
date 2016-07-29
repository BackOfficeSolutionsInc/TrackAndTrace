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
            public long ChartId;
	    }


        [Access(AccessLevel.UserOrganization)]
		public ActionResult Chart(long? id=null){
	        var u = GetUser();

            var idr = id ?? u.Organization.AccountabilityChartId;

			return View(new AccountabilityChartVM() {
		        UserId = u.Id,
				OrganizationId = u.Organization.Id,
                ChartId = idr
	        });
	    }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Append(long id){
            var nodeId = id;
            AccountabilityAccessor.AppendNode(GetUser(), nodeId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Swap(long id,long parent){
            var nodeId = id;
            AccountabilityAccessor.SwapParents(GetUser(), nodeId,parent);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Remove(long id){
            var nodeId = id;
            AccountabilityAccessor.RemoveNode(GetUser(), nodeId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }


        [Access(AccessLevel.UserOrganization)]
        public JsonResult Data(long id,long? parent=null)
        {
            var tree = AccountabilityAccessor.GetTree(GetUser(), id, parent);
           // var acTreeId = tree.Flatten().FirstOrDefault(x => x.user.NotNull(y => y.Id == (parent ?? GetUser().Id)));
            var c=new Chart<AngularAccountabilityChart>(parent??id) {
                height = "100%",
                width = "100%",
                data = tree,
            };
            return Json(c, JsonRequestBehavior.AllowGet);
        }

    }
}