using RadialReview.Accessors;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Models.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace RadialReview.Api.V1 {
	[RoutePrefix("api/v1")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class NotificationController : BaseApiController {

		/// <summary>
		/// Get a specific notification
		/// </summary>
		/// <param name="NOTIFICATION_ID">Notification ID</param>
		/// <returns>The people headline</returns>
		//[GET/POST/DELETE] /headline/{id}
		[Route("notification/{NOTIFICATION_ID}")]
		[HttpGet]
		public AngularAppNotification GetNotification(long NOTIFICATION_ID) {
			return new AngularAppNotification(NotificationAccessor.GetNotification(GetUser(), NOTIFICATION_ID));
		}



		public class NotificationStatusModel {
			/// <summary>
			/// Notification status
			/// </summary>
			[Required]
			public NotificationStatus status { get; set; }

		}
		/// <summary>
		/// Set notification status
		/// </summary>
		/// <param name="NOTIFICATION_ID">Notification ID</param>
		/// <returns>The people headline</returns>
		[Route("notification/{NOTIFICATION_ID}")]
		[HttpPost]
		public async Task Status(long NOTIFICATION_ID, [FromBody] NotificationStatusModel model) {
			await NotificationAccessor.SetNotificationStatus(GetUser(), NOTIFICATION_ID, model.status);
		}

		/// <summary>
		/// Get a list of notifications
		/// </summary>
		/// <returns></returns>
		[Route("notification/list")]
		[HttpGet]
		public List<AngularAppNotification> List(bool seen = false) {
			return NotificationAccessor.GetNotificationsForUser(GetUser(), GetUser().Id, seen)
				.Select(x => new AngularAppNotification(x))
				.ToList();
		}
	}
}

