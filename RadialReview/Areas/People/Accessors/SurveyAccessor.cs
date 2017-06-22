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

		public static IEnumerable<IByAbout> AvailableByAbouts(UserOrganizationModel caller,bool includeSelf=false) {
			var nodes = AccountabilityAccessor.GetNodesForUser(caller, caller.Id);
			var possible = new List<IByAbout>();
			foreach (var node in nodes) {
				var reports = DeepAccessor.GetDirectReportsAndSelf(caller, node.Id);
				foreach (var report in reports) {
					possible.Add(new ByAbout(caller, report));

					if (includeSelf) {
						possible.Add(new ByAbout(report, report));
					}
				}
			}
			

			return possible;
		}

		public static AngularSurveyContainer GenerateSurveyContainer(UserOrganizationModel caller, string name, IEnumerable<IByAbout> byAbout) {

			var possible = AvailableByAbouts(caller, true);
			if (!byAbout.All(x => possible.Any(y => y.ToKey() == x.ToKey())))
				throw new PermissionsException("Could not create Quarterly Conversation. You cannot view this item.");

			long containerId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CreateSurveyContainer(caller.Organization.Id);

					containerId = GenerateSurvey_Unsafe(s, perms, name, byAbout);

					tx.Commit();
					s.Flush();

				}
			}
			return GetAngularSurveyContainer(caller, caller, containerId);
		}

		public static long GenerateSurvey_Unsafe(ISession s, PermissionsUtility perms, string name, IEnumerable<IByAbout> byAbout) {
			var caller = perms.GetCaller();
			var engine = new SurveyBuilderEngine(new QuarterlyConversationInitializer(caller, name, caller.Organization.Id), new SurveyBuilderEventsSaveStrategy(s));

			byAbout = TransformByAbouts(s,byAbout);

			var container = engine.BuildSurveyContainer(byAbout);
			var containerId = container.Id;
			var permItems = new[] {
				PermTiny.Creator(),
				PermTiny.Admins(),
				PermTiny.Members(true, true, false)
			};
			PermissionsAccessor.CreatePermItems(s, caller, PermItem.ResourceType.SurveyContainer, containerId, permItems);

			return containerId;

		}
		/// <summary>
		/// Converts the By's to UserOrganizationModels
		///  
		/// </summary>
		/// <param name="s"></param>
		/// <param name="byAbout"></param>
		/// <returns></returns>
		private static IEnumerable<IByAbout> TransformByAbouts(ISession s, IEnumerable<IByAbout> byAbout) {
			var bys = byAbout.Select(x => x.GetBy()).ToList();
			var accNodeBys = bys.Where(x => x.Is<AccountabilityNode>());
			var newByAbouts = new List<IByAbout>();
			if (accNodeBys.Any()) {
				var accNodeIds = accNodeBys.Select(x => x.ModelId).ToArray();
				var accNodes = s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.Id).IsIn(accNodeIds)
					.List().ToList();
				foreach (var ba in byAbout) {
					var toAdd = ba;
					if (ba.GetBy().Is<AccountabilityNode>()) {
						var foundUser = accNodes.FirstOrDefault(x => x.Id == ba.GetBy().ModelId).NotNull(x => x.User);
						if (foundUser != null) {
							toAdd = new ByAbout(foundUser, ba.GetAbout());
						}
					}
					newByAbouts.Add(toAdd);

				}
			} else {
				newByAbouts = byAbout.ToList();
			}
			return newByAbouts.Distinct(x=>x.ToKey()).ToList();


		}

		public static AngularSurveyContainer GetAngularSurveyContainer(UserOrganizationModel caller, IForModel by, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewSurveyContainer(surveyContainerId);
					perms.Self(by);//TODO make this less restrictive
					
					var container = s.Get<SurveyContainer>(surveyContainerId);
					if (container.OrgId != caller.Organization.Id)
						throw new PermissionsException();
					
					var engine = new SurveyReconstructionEngine(surveyContainerId, container.OrgId, new DatabaseAggregator(s,by), new SurveyReconstructionEventsNoOp());
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

					var containerIds = s.QueryOver<Survey>().Where(x => x.DeleteTime == null && x.SurveyType == type)
						.Where(x => x.By.ModelId == byModel.ModelId && x.By.ModelType == byModel.ModelType)
						.Select(x => Projections.Group<Survey>(y => y.SurveyContainerId))
						.Select(x => x.SurveyContainerId)
						.List<long>().ToArray();

					return s.QueryOver<SurveyContainer>().Where(x => x.DeleteTime == null && x.SurveyType == type)
						.WhereRestrictionOn(x => x.Id).IsIn(containerIds)
						.List().Select(x => new AngularSurveyContainer(x));

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