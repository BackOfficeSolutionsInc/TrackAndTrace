using Microsoft.AspNet.SignalR;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Reconstructor;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Traverse;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using NHibernate.Criterion;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models.Accountability;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Angular.Survey.SurveyAbout;
using static RadialReview.Models.PermItem;

namespace RadialReview.Areas.People.Accessors {
	public class SurveyAccessor {

#pragma warning disable CS0618 // Type or member is obsolete
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

		public static ISurvey GetSurvey(UserOrganizationModel caller, IForModel by, IForModel about, long surveyContainerId) {
			return GetAngularSurveyContainerBy(caller, by, surveyContainerId)
				.GetSurveys()
				.SingleOrDefault(x => x.GetAbout().ToKey() == about.ToKey());
		}

#pragma warning restore CS0618 // Type or member is obsolete

		//private static IForModel StandardizeAbout(ISession s, IForModel about) {

		//	if (about.Is<AccountabilityNode>()) {
		//		return about;
		//	} else if (about.Is<SurveyUserNode>()) {
		//		var node = s.Get<SurveyUserNode>(about.ModelId);
		//		return StandardizeAbout(s, node.AccountabilityNode);
		//	} else {
		//		throw new ArgumentOutOfRangeException("About type:" + about.ModelType);
		//	}
		//}

		public static ISurveyAboutContainer GetSurveyContainerAbout(UserOrganizationModel caller, IForModel about, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewSurveyContainer(surveyContainerId);
					var container = s.Get<SurveyContainer>(surveyContainerId);
					perms.ViewSurveyResultsAbout(about, container.OrgId);

					if (container.DeleteTime != null)
						throw new PermissionsException("Does not exist");

					if (container.OrgId != caller.Organization.Id)
						throw new PermissionsException();

					var aboutTransformed =/* StandardizeAbout(s,*/(about);

					var engine = new SurveyReconstructionEngine(surveyContainerId, container.OrgId, new DatabaseAggregator(s, about: aboutTransformed), new SurveyReconstructionEventsNoOp());
					var output = new AngularSurveyAboutContainer();
					engine.Traverse(new TraverseBuildAboutAngular(s, aboutTransformed, x => { output = x; }));
					return output;

				}
			}
		}

		public static void RemoveSurveyContainer(UserOrganizationModel caller, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanAdmin(ResourceType.SurveyContainer, surveyContainerId);
					var sc = s.Get<SurveyContainer>(surveyContainerId);
					sc.DeleteTime = DateTime.UtcNow;
					s.Update(sc);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static AngularSurveyContainer GetAngularSurveyContainerBy(UserOrganizationModel caller, IForModel by, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewSurveyContainer(surveyContainerId);
					perms.Self(by);//TODO make this less restrictive

					var container = s.Get<SurveyContainer>(surveyContainerId);

					if (container.DeleteTime != null) 
						throw new PermissionsException("Does not exist");
					if (container.OrgId != caller.Organization.Id)
						throw new PermissionsException();

					var engine = new SurveyReconstructionEngine(surveyContainerId, container.OrgId, new DatabaseAggregator(s, by), new SurveyReconstructionEventsNoOp());
					var output = new AngularSurveyContainer();
					engine.Traverse(new TraverseBuildAngular(output));
					return output;

				}
			}
		}

		public static IEnumerable<AngularSurveyContainer> GetSurveyContainersBy(UserOrganizationModel caller, IForModel byModel, SurveyType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(byModel);

					//var sunIds =s.QueryOver<SurveyUserNode>().Where(x => x.DeleteTime==null && x.UserOrganizationId == byUserId).Select(x=>x.Id).List<long>().ToList();

					var containerIds = s.QueryOver<Survey>().Where(x => x.DeleteTime == null && x.SurveyType == type)
						.Where(x => x.By.ModelId == byModel.ModelId && x.By.ModelType == byModel.ModelType)
						//.Where(x=>x.By.ModelType == ForModel.GetModelType<SurveyUserNode>())
						//.WhereRestrictionOn(x => x.By.ModelId).IsIn(sunIds)
						
						.Select(x => Projections.Group<Survey>(y => y.SurveyContainerId))
						.Select(x => x.SurveyContainerId)
						.List<long>().ToArray();

					return s.QueryOver<SurveyContainer>().Where(x => x.DeleteTime == null && x.SurveyType == type)
						.WhereRestrictionOn(x => x.Id).IsIn(containerIds)
						.List().Select(x => new AngularSurveyContainer(x, x.DueDate<DateTime.UtcNow ));

					//perms.ViewSurveyContainer(surveyContainerId);
				}
			}
		}

		public static bool UpdateAngularSurveyResponse(UserOrganizationModel caller, long responseId, string answer, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					perms.EditSurveyResponse(responseId);

					var response = s.Get<SurveyResponse>(responseId);

					if (answer != null) {
						if (answer != response.Answer)
							response.CompleteTime = DateTime.UtcNow;
					} else {
						response.CompleteTime = null;
					}

					response.Answer = answer;
					s.Update(response);

					tx.Commit();
					s.Flush();

					return true;
				}
			}
		}
	}
}