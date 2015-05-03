using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class MeetingController : BaseController
    {
        // GET: Meeting
        public ActionResult Index()
        {
            return View();
        }

	    public ActionResult Attend(long id)
	    {
		    return View();
		}
    }
}