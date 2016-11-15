using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Office365.OutlookServices;
using RadialReview.Models.Json;
using RadialReview.Accessors.VideoConferenceProviders;

namespace RadialReview.Controllers {
	public class IntegrationsController : BaseController {
		// GET: Integrations
		[Access(AccessLevel.UserOrganization)]
		public void OutlookSignin() {
			//(HttpContext).GetOwinContext().Authentication.Challenge(
			//	new AuthenticationProperties { RedirectUri = "/" },
			//	OpenIdConnectAuthenticationDefaults.AuthenticationType);

		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddZoomRoom(long recurId,long userId,string zoomMeetingId, string connectionId = null,string name=null) {

			var link = VideoProviderAccessor.GenerateLink(GetUser(), userId, zoomMeetingId, recurId, name:name);

			if (connectionId != null) {
				VideoProviderAccessor.StartMeeting(GetUser(),  link.Id, connectionId);
			}

			
			
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
	}
}