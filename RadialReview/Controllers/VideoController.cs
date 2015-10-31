using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.VideoConference;

namespace RadialReview.Controllers
{
    public class VideoController : BaseController
    {
        // GET: Video
		[Access(AccessLevel.Any)]
        public ActionResult Join(string id)
        {
			ViewBag.VideoChatRoom = new VideoConferenceVM()
			{
		        RoomId = id
	        };
            return View();
        }
    }
}