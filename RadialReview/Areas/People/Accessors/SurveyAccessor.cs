using Microsoft.AspNet.SignalR;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Engines.Surveys;
using RadialReview.Engines.Surveys.Impl.QuarterlyConversation;
using RadialReview.Engines.Surveys.Strategies.Events;
using RadialReview.Engines.Surveys.Strategies.Reconstructor;
using RadialReview.Engines.Surveys.Strategies.Traverse;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Accessors {
    public class SurveyAccessor {

        public static void JoinPeopleHub(UserOrganizationModel caller, long? surveyContainerId, long? surveyId, string connectionId) {
            var hub = GlobalHost.ConnectionManager.GetHubContext<PeopleHub>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    
                    var perms = PermissionsUtility.Create(s, caller);
                    if (surveyContainerId != null) {
                        perms.ViewSurveyContainer(surveyContainerId.Value);
                        hub.Groups.Add(connectionId, PeopleHub.GenerateSurveyContainerId(surveyContainerId.Value));
                    }
                    if (surveyId != null) {
                        perms.ViewSurvey(surveyId.Value);
                        hub.Groups.Add(connectionId, PeopleHub.GenerateSurveyId(surveyId.Value));
                    }
                    hub.Groups.Add(connectionId, MeetingHub.GenerateUserId(caller.Id));
                }
            }            
        }

        public static AngularSurveyContainer GenerateSurvey_Unsafe(UserOrganizationModel caller,string name, IEnumerable<ByAbout> byAbout) {
            long containerId;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var engine = new SurveyBuilderEngine(new QuarterlyConversationInitializer(caller,name,caller.Organization.Id), new SurveyBuilderEventsSaveStrategy(s));
                    var container = engine.BuildSurveyContainer(byAbout);
                    containerId = container.Id;

                    s.Save(new PermItem() {
                        CanAdmin = true,
                        CanEdit = true,
                        CanView = true,
                        AccessorType = PermItem.AccessType.Creator,
                        AccessorId = caller.Id,
                        ResType = PermItem.ResourceType.SurveyContainer,
                        ResId = containerId,
                        CreatorId = caller.Id,
                        OrganizationId = caller.Organization.Id,
                        IsArchtype = false,
                    });

                    tx.Commit();
                    s.Flush();
                    
                }
            }
            return GetAngularSurveyContainer(caller, containerId,null);
        }

        public static AngularSurveyContainer GetAngularSurveyContainer(UserOrganizationModel caller, long surveyContainerId, long? surveyId/*,IForModel? by,IForModel? about*/) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewSurveyContainer(surveyContainerId);
                    

                    var container = s.Get<SurveyContainer>(surveyContainerId);
                    if (container.OrgId != caller.Organization.Id)
                        throw new PermissionsException();

                    if (surveyId != null) {
                        perms.ViewSurvey(surveyId.Value);
                    }
                    var engine =new SurveyReconstructionEngine(surveyContainerId, container.OrgId, new DatabaseAggregator(s), new SurveyReconstructionEventsNoOp());
                    var output = new AngularSurveyContainer();
                    engine.Traverse(new TraverseBuildAngular(output));
                    return output;

                }
            }
        }
    }
}