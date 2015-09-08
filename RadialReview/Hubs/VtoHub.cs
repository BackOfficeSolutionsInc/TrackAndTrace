using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.VTO;

namespace RadialReview.Hubs
{
	public class VtoHub : BaseHub
	{
		public static string GenerateVtoGroupId(long vto)
		{
			if (vto == 0)
				throw new Exception();
			return "VTO_" + vto;
		}

		public static string GenerateGroupId(VtoModel vto)
		{
			var id = vto.Id;
			return GenerateVtoGroupId(id);
		}

		public void Hello()
		{
			Clients.All.hello();
		}

		public void Join(long vtoId, string connectionId)
		{
			//var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), meetingId);
			if (connectionId != Context.ConnectionId)
				throw new PermissionsException("Ids do not match");

			VtoAccessor.JoinVto(GetUser(), vtoId, Context.ConnectionId);
			//return new UserMeetingModel(conn);
			//SendUserListUpdate();
			return;
		}
	}
}