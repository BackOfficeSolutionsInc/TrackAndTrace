using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using RadialReview.Models.VTO;

namespace RadialReview.Hubs
{
	public class VtoHub : BaseHub
	{
		public static string GenerateMeetingGroupId(long vto)
		{
			if (vto == 0)
				throw new Exception();
			return "VTO_" + vto;
		}

		public static string GenerateGroupId(VtoModel vto)
		{
			var id = vto.Id;
			return GenerateMeetingGroupId(id);
		}

		public void Hello()
		{
			Clients.All.hello();
		}
	}
}