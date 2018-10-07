using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Common.Logging;
using RadialReview.Exceptions;
using RadialReview.Areas.CoreProcess.Accessors;
using static CamundaCSharpClient.Query.Task.TaskQuery;
using RadialReview.Models.Askables;
using RadialReview.Utilities.CoreProcess;

namespace RadialReview.Hubs
{
	//public class MessageHub : BaseHub {
	//	public static string GenerateGroupId(long orgId) {
	//		if (orgId == 0)
	//			throw new Exception();
	//		return "ManageOrganization_" + orgId;
	//	}

	//	//public static string GenerateMessageUserId(long userId) {
	//	//	if (userId == 0)
	//	//		throw new Exception();
	//	//	return "UserMsg_" + userId;
	//	//}



	//	//public void JoinMessageHub(string connectionId) {			
	//	//	var hub = GlobalHost.ConnectionManager.GetHubContext<MessageHub>();
	//	//	hub.Groups.Add(Context.ConnectionId, RealTimeHub.Keys.UserId(GetUser().Id));
	//	//}
	//}
}