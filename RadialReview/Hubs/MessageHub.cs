using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace RadialReview.Hubs
{
	public class MessageHub : BaseHub {
		public static string GenerateGroupId(long orgId) {
			if (orgId == 0)
				throw new Exception();
			return "ManageOrganization_" + orgId;
		}

		public static string GenerateUserId(long userId) {
			if (userId == 0)
				throw new Exception();
			return "UserMsg_" + userId;
		}



		public void JoinUser(string connectionId) {
			
			var hub = GlobalHost.ConnectionManager.GetHubContext<MessageHub>();
			hub.Groups.Add(Context.ConnectionId, MessageHub.GenerateUserId(GetUser().Id));
			//try {
			//} catch (Exception e) {
			//    log.Error("Meeting.Join  (" + meetingId + ", " + connectionId + ")", e);
			//    throw;
			//}
			//return;
		}

	}
}