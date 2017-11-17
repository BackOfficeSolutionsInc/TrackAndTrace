using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using log4net;
using RadialReview.Exceptions;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Utilities.CoreProcess;
using static CamundaCSharpClient.Query.Task.TaskQuery;

namespace RadialReview.Hubs {
    public class CoreProcessHub : BaseHub {
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Join(string connectionId) {
            long? userId = null;
            try {
                userId = GetUser().Id;
                if (connectionId != Context.ConnectionId)
                    throw new PermissionsException("Ids do not match");
                ProcessDefAccessor.JoinCoreProcessHub(GetUser(), connectionId);
            } catch (Exception e) {
                log.Error("CP.Join  ("+ userId + ","+ connectionId + ")", e);
                throw;
            }
            return;
        }

        public static string GenerateRgm(IdentityLink model) {
            if (model.userId != null)
                return "rspgrpmdl_" + model.userId.SubstringAfter("u_");
            if (model.groupId != null)
                return "rspgrpmdl_" + model.groupId.SubstringAfter("rgm_");

            throw new Exception();
        }

        public static string GenerateRgm(ResponsibilityGroupModel model) {
            if (model == null)
                throw new Exception();
            return "rspgrpmdl_" + model.Id;
        }
        [Obsolete("Use other method if possible.")]
        public static string GenerateRgm(string model) {
            var found = BpmnUtility.ParseCandidateGroupIds(model).Single();
            if (found <= 0)
                throw new Exception();
            return "rspgrpmdl_" + found;
        }
        [Obsolete("Use other method if possible.")]
        public static string GenerateRgm(long rgmId) {
            if (rgmId<= 0)
                throw new Exception();
            return "rspgrpmdl_" + rgmId;
        }

    }
}