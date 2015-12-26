using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace RadialReview.Hubs
{
	public class MessageHub : BaseHub
	{
		public static string GenerateGroupId(long orgId)
		{
			if (orgId == 0)
				throw new Exception();
			return "ManageOrganization_" + orgId;
		}
	

	}
}