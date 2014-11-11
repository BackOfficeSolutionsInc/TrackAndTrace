using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Enums;

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
            return System.Configuration.ConfigurationManager.AppSettings["DBType"];
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
	}
}