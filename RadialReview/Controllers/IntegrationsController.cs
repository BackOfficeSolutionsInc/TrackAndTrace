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

namespace RadialReview.Controllers
{
    public class IntegrationsController : Controller
    {
        // GET: Integrations
		[Access(AccessLevel.UserOrganization)]
        public void OutlookSignin() {
			//(HttpContext).GetOwinContext().Authentication.Challenge(
			//	new AuthenticationProperties { RedirectUri = "/" },
			//	OpenIdConnectAuthenticationDefaults.AuthenticationType);

		}
	}
}