using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace RadialReview.Api.V1 {
[RoutePrefix("api/v1")]
	public class Notification : BaseApiController {
		/// <summary>
		/// Get a specific people headline
		/// </summary>
		/// <param name="HEADLINE_ID">People headline ID</param>
		/// <returns>The people headline</returns>
		//[GET/POST/DELETE] /headline/{id}
		[Route("headline/{HEADLINE_ID}")]
		[HttpGet]
		public AngularAppNotification GetHeadline(long HEADLINE_ID) {
			throw new Exception("incomplete");
		}
	}
}

