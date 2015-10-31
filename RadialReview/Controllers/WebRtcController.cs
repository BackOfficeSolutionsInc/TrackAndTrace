using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class WebRtcController : Controller
    {
        // GET: WebRtc
        public JsonResult GetIceServers(long? room)
        {
	        var arr = new List<object>(){new {url="stun:74.125.142.127:19302" }};
	        return Json(arr, JsonRequestBehavior.AllowGet);
        }

    }
}