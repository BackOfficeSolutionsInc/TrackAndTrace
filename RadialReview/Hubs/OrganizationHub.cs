using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using log4net;
using RadialReview.Exceptions;
using RadialReview.Accessors;

namespace RadialReview.Hubs {
    public class OrganizationHub : BaseHub {

        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GenerateId(long organizationId)
        {
            return "OrganzationId_" + organizationId;
        }

        public void Join(long orgId, string connectionId)
        {
            log.Info("Organization.Join (" + Context.ConnectionId + ")");

			PermissionsAccessor.Permitted(GetUser(),x=>x.ViewOrganization(orgId));

			if (connectionId != Context.ConnectionId)
				throw new PermissionsException("wrong connection id");

			var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
			hub.Groups.Add(Context.ConnectionId, OrganizationHub.GenerateId(GetUser().Organization.Id));
			//try {
			//} catch (Exception e) {
			//    log.Error("Meeting.Join  (" + meetingId + ", " + connectionId + ")", e);
			//    throw;
			//}
			//return;
		}
    }
}