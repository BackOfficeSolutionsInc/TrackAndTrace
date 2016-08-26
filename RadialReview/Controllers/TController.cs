using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Models;
using RadialReview.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview {
    public class TController : BaseController {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="a">Admin Only</param>
        /// <returns></returns>
        [Access(AccessLevel.Any)]
        public ActionResult Mark(string id,bool a=false)
        {
            if (string.IsNullOrWhiteSpace(id))
                return File(ImageController.TrackingGif, "image/gif");
            id=id.Replace(".gif", "");
            // Construct absolute image path
            try {
               // long tryId = -1;
                Response.AppendHeader("Cache-Control", "no-cache, max-age=0");
                UserOrganizationModel m=null;
                try{m = GetUser();}catch{}
                if (a && (m == null || m.User == null || !m.User.IsRadialAdmin))
                    return File(ImageController.TrackingGif, "image/gif");

                TrackingAccessor.MarkSeen(id, m, Tracker.TrackerSource.Email);

            } catch (Exception) {
               // var o = "";
            }
            return File(ImageController.TrackingGif, "image/gif");
        }
    }
}