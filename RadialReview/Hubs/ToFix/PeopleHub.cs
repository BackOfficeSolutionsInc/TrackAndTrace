using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using log4net;
using RadialReview.Exceptions;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;

namespace RadialReview.Hubs {
    //public class PeopleHub : BaseHub {
    //    protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    //    public void Join(long? surveyContainerId, long? surveyId, string connectionId) {
    //        try {
    //            if (connectionId != Context.ConnectionId)
    //                throw new PermissionsException("Ids do not match");
    //            SurveyAccessor.JoinPeopleHub(GetUser(), surveyContainerId, surveyId, connectionId);
    //        } catch (Exception e) {
    //            log.Error("People.Join  (" + surveyContainerId + ", " + surveyId + ", " + connectionId + ")", e);
    //            throw;
    //        }
    //        return;
    //    }

    //    [Obsolete("Be careful. This is SurveyContainer, not Survey.")]
    //    public static string GenerateSurveyContainerId(long surveyContainerId) {
    //        if (surveyContainerId <= 0)
    //            throw new Exception();
    //        return "SurveyContainer_" + surveyContainerId;
    //    }
    //    [Obsolete("Be careful. This is Survey, not SurveyContainer.")]
    //    public static string GenerateSurveyId(long surveyId) {
    //        if (surveyId <= 0)
    //            throw new Exception();
    //        return "Survey_" + surveyId;
    //    }
    //    public static string GenerateUserId(long userId) {
    //        if (userId <= 0)
    //            throw new Exception();
    //        return "PeopleUser_" + userId;
    //    }
    //}
}