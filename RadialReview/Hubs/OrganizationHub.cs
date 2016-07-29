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

        public void Join()
        {
            log.Info("Organization.Join (" + Context.ConnectionId + ")");
            //try {
            //} catch (Exception e) {
            //    log.Error("Meeting.Join  (" + meetingId + ", " + connectionId + ")", e);
            //    throw;
            //}
            //return;
        }
    }
}