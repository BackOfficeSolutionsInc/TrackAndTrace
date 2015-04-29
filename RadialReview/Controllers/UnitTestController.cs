using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class UnitTestController : BaseController
    {
        //
        // GET: /UnitTest/
        [Access(AccessLevel.Radial)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Radial)]
        public String DbType()
        {
	        return ""+Config.GetEnv();
        }

        [Access(AccessLevel.Radial)]
        public void Status(string text="Test Status")
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
            var hubUsers = hub.Clients.User(GetUserModel().UserName);
            hubUsers.status(text);
        }



		[Access(AccessLevel.Radial)]
		public ActionResult Tristate(Tristate state = Models.Enums.Tristate.Indeterminate)
		{
			return View(state);
		}

		[Access(AccessLevel.Radial)]
		public JsonResult TestRequest(bool error = false, string message = "TestMessage")
		{

			var res = new ResultObject(error,message);
			if (!error){
				res.Status = StatusType.Success;
			}
			return Json(res, JsonRequestBehavior.AllowGet);
		}


	}
}