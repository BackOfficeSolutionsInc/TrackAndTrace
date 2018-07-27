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
using RadialReview.Models.Angular.Users;

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

		public static IEnumerable<AngularSurveyContainer> GetArchivedSurveyContainersBy(UserOrganizationModel caller, IForModel byModel, SurveyType type) {
			return GetSurveyContainerBy(caller, byModel, type, true);
		}

		public static AngularSurveyContainer GetSurveyContainer(UserOrganizationModel caller, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanView(ResourceType.SurveyContainer, surveyContainerId);
					return new AngularSurveyContainer(s.Get<SurveyContainer>(surveyContainerId),false,null);
				}
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		public static AngularSurveyContainer UpdateSurveyContainer(UserOrganizationModel caller, long surveyContainerId, string name = null, DateTime? dueDate = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanAdmin(ResourceType.SurveyContainer, surveyContainerId);
					var sc = s.Get<SurveyContainer>(surveyContainerId);
					var surveysQ = s.QueryOver<Survey>().Where(x => x.SurveyContainerId == surveyContainerId).List().ToList();
					if (dueDate != null) {
						sc.DueDate = dueDate.Value;
						foreach (var survey in surveysQ) {
							survey.DueDate = dueDate.Value;
							s.Update(survey);
						}
						s.Update(sc);
					}
					if (name != null) {
						sc.Name = name;
						//foreach (var survey in surveysQ) {
						//	survey.DueDate = dueDate.;
						//	s.Update(survey);
						//}
						s.Update(sc);
					}
					tx.Commit();
					s.Flush();
					AngularUser issuedBy = null;
					if (sc.CreatedBy.Is<UserOrganizationModel>()) {
						issuedBy = AngularUser.CreateUser(s.Get<UserOrganizationModel>(sc.CreatedBy.ModelId));
					}

					return new AngularSurveyContainer(sc, sc.DueDate < DateTime.UtcNow, issuedBy);
				}
			}
		}

		public static ISurveyAboutContainer GetSurveyContainerAbout(UserOrganizationModel caller, IForModel about, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewSurveyContainer(surveyContainerId);
					var container = s.Get<SurveyContainer>(surveyContainerId);
					perms.ViewSurveyResultsAbout(surveyContainerId, about/*, container.OrgId*/);

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

		public static void UndeleteSurveyContainer(UserOrganizationModel caller, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanAdmin(ResourceType.SurveyContainer, surveyContainerId);
					var sc = s.Get<SurveyContainer>(surveyContainerId);
					sc.DeleteTime = null;
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
					var surveys = output.Surveys ?? new List<AngularSurvey>();

					var i = 0;

					foreach (var o in surveys.OrderBy(x => x.Name)) {
						o.Ordering = i;
						i += 1;
						if ((o.Name ?? "").Contains(by.ToPrettyString()))
							o.Ordering = -1;
					}

					if (container.DueDate.HasValue && container.DueDate.Value < DateTime.UtcNow)
						output.Locked = true;

					return output;

				}
			}
		}

		public static bool IsLocked(UserOrganizationModel caller, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewSurveyContainer(surveyContainerId);
					var container = s.Get<SurveyContainer>(surveyContainerId);
					return container.DueDate < DateTime.UtcNow;
				}
			}
		}

		public static List<SurveyUserNode> GetAllSurveyUserNodesForUser_IncludingDelete(UserOrganizationModel caller, long userId) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ManagesUserOrganization(userId, false);

					return s.QueryOver<SurveyUserNode>().Where(x => x.UserOrganizationId == userId).List().ToList();

				}
			}
		}

		public static IEnumerable<AngularSurveyContainer> GetSurveyContainersAbout(UserOrganizationModel caller, IForModel aboutModel, SurveyType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					//PERMISSIONS HANDELED BELOW, SPECIAL CASE...be careful please

					var containerIds = s.QueryOver<Survey>().Where(x => x.DeleteTime == null && x.SurveyType == type)
						.Where(x => x.About.ModelId == aboutModel.ModelId && x.About.ModelType == aboutModel.ModelType)
						.Select(x => Projections.Group<Survey>(y => y.SurveyContainerId))
						.Select(x => x.SurveyContainerId)
						.List<long>().ToArray();

					var any = containerIds.Any();

					containerIds = containerIds.Distinct().Where(x =>
						//Permissions filter here
						perms.IsPermitted(p => p.ViewSurveyResultsAbout(x, aboutModel))
					).ToArray();

					//Throw exception instead of presenting empty..
					if (any && !containerIds.Any())
						throw new PermissionsException("Cannot view this");

					var containers = s.QueryOver<SurveyContainer>()
						.Where(x => x.DeleteTime == null && x.SurveyType == type)
						.WhereRestrictionOn(x => x.Id).IsIn(containerIds)
						.List();

					var issuedBy = containers
						.Select(x => x.CreatedBy)
						.Where(x => x.ModelType == ForModel.GetModelType<UserOrganizationModel>())
						.Select(x => x.ModelId)
						.Distinct().ToList();

					var creatorLookup = TinyUserAccessor.GetUsers_Unsafe(s, issuedBy, false).ToDefaultDictionary(x => x.ToKey(), x => AngularUser.CreateUser(x), null);

					return containers.Select(x => new AngularSurveyContainer(x, x.DueDate < DateTime.UtcNow, creatorLookup[x.CreatedBy.ToKey()]));
				}
			}
		}

		public static IEnumerable<IForModel> GetAllAboutsForSurveyContainer(UserOrganizationModel caller, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CreatedSurvey(surveyContainerId);
					var surveys = s.QueryOver<Survey>().Where(x => x.DeleteTime == null && x.SurveyContainerId == surveyContainerId).List().ToList();
					return surveys.Select(x => x.GetAbout()).Distinct(x => x.ToKey()).ToList();
				}
			}
		}

		public static IEnumerable<AngularSurveyContainer> GetSurveyContainersIssuedBy(UserOrganizationModel caller, IForModel creatorModel, SurveyType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					//Hacky, but probably overlimiting..
					var createdBy = AngularUser.CreateUser(caller);
					if (caller.ToKey() != creatorModel.ToKey())
						throw new PermissionsException();
					///////////////////

					var containers = s.QueryOver<SurveyContainer>()
								.Where(x => x.DeleteTime == null && x.CreatedBy == creatorModel.ToImpl() && x.SurveyType == type)
								.List().ToList();

					return containers.Select(x => new AngularSurveyContainer(x, x.DueDate < DateTime.UtcNow, createdBy));
				}
			}
		}

		public static IEnumerable<AngularSurveyContainer> GetSurveyContainersBy(UserOrganizationModel caller, IForModel byModel, SurveyType type) {
			return GetSurveyContainerBy(caller, byModel, type, false);
		}

		private static IEnumerable<AngularSurveyContainer> GetSurveyContainerBy(UserOrganizationModel caller, IForModel byModel, SurveyType type, bool archivedOnly) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(byModel);

					//var sunIds =s.QueryOver<SurveyUserNode>().Where(x => x.DeleteTime==null && x.UserOrganizationId == byUserId).Select(x=>x.Id).List<long>().ToList();

					var containerIds = s.QueryOver<Survey>()
						.Where(x => x.DeleteTime == null && x.SurveyType == type)
						.Where(x => x.By.ModelId == byModel.ModelId && x.By.ModelType == byModel.ModelType)
						.Select(x => Projections.Group<Survey>(y => y.SurveyContainerId))
						.Select(x => x.SurveyContainerId)
						.List<long>().ToArray();

					var containersQ = s.QueryOver<SurveyContainer>();
					if (archivedOnly) {
						containersQ = containersQ.Where(x => x.DeleteTime != null);
					} else {
						containersQ = containersQ.Where(x => x.DeleteTime == null);
					}
					var containers = containersQ
						.Where(x => x.SurveyType == type)
						.WhereRestrictionOn(x => x.Id).IsIn(containerIds)
						.List();

					var issuedBy = containers
						.Select(x => x.CreatedBy)
						.Where(x => x.ModelType == ForModel.GetModelType<UserOrganizationModel>())
						.Select(x => x.ModelId)
						.Distinct().ToList();

					var creatorLookup = TinyUserAccessor.GetUsers_Unsafe(s, issuedBy, false).ToDefaultDictionary(x => x.ToKey(), x => AngularUser.CreateUser(x), null);

					return containers.Select(x => new AngularSurveyContainer(x, x.DueDate < DateTime.UtcNow, creatorLookup[x.CreatedBy.ToKey()]));
				}
			}
		}

		public static List<IForModel> GetForModelsWithIncompleteSurveysForSurveyContainers(UserOrganizationModel caller, long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanAdmin(ResourceType.SurveyContainer, surveyContainerId);
					var sc = s.Get<SurveyContainer>(surveyContainerId);
					var byAndLockedIn = s.QueryOver<Survey>()
						.Where(x => x.DeleteTime == null && x.SurveyContainerId == surveyContainerId)
						.Select(x => x.By, x => x.LockedIn)
						.List<object[]>()
						.Select(x => new {
							By = (ForModel)x[0],
							LockedIn = (((bool?)x[1]) ?? false)
						}).ToList();

					var allBy = byAndLockedIn.Select(x => x.By).Distinct(x => x.ToKey()).ToDictionary(x => x.ToKey(), x => x);
					var allLockedIn = byAndLockedIn.Select(x => x.By).Distinct(x => x.ToKey()).ToDictionary(x => x.ToKey(), x => true);
					foreach (var bl in byAndLockedIn) {
						if (bl.LockedIn == false) {
							allLockedIn[bl.By.ToKey()] = false;
						}
					}

					return allLockedIn.Where(x => x.Value == false)
						.Select(x => (IForModel)allBy[x.Key])
						.ToList();
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

		public static IEnumerable<AngularSurvey> GetSurveysForContainer_Unsafe(long surveyContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var surveys = s.QueryOver<Survey>().Where(x => x.SurveyContainerId == surveyContainerId).List().ToList();
					var sections = s.QueryOver<SurveySection>().Where(x => x.SurveyContainerId == surveyContainerId).List().ToList();
					var items = s.QueryOver<SurveyItem>().Where(x => x.SurveyContainerId == surveyContainerId).List().ToList();
					var itemFormats = s.QueryOver<SurveyItemFormat>().Where(x => x.SurveyContainerId == surveyContainerId).List().ToList().ToDefaultDictionary(x=>x.Id,x=>x,x=>null);

					var containers = items.Select(x => new AngularSurveyItemContainer() {
						Item = new AngularSurveyItem(x),
						ItemFormat = itemFormats[x.ItemFormatId].NotNull(y=>new AngularSurveyItemFormat(y))
					}).ToList();

					UserModel uA = null;
					var uoms = s.QueryOver<UserOrganizationModel>()
								.JoinAlias(x => x.User, () => uA)						
								.WhereRestrictionOn(x => x.Id).IsIn(surveys.Select(x => x.By.ModelId).ToList())
								.Select(x=>x.Id,x=>uA.FirstName,x=>uA.LastName)
								.List<object[]>().ToList().ToDefaultDictionary(x => (long)x[0], x => ((string)x[1]) + " " + ((string)x[2]), x => null);

					var suns = s.QueryOver<SurveyUserNode>()								
								.WhereRestrictionOn(x => x.Id).IsIn(surveys.Select(x => x.About.ModelId).ToList())
								.Select(x=>x.Id,x=>x.UsersName,x=>x.PositionName)
								.List<object[]>().ToList().ToDefaultDictionary(x => (long)x[0], x => ((string)x[1])+" - "+((string)x[2]), x => null);


					return surveys.Select(survey => {
						var ss = new AngularSurvey(survey) {
							Sections = sections.Where(y => y.SurveyId == survey.Id).Select(section => new AngularSurveySection(section) {
								Items = containers.Where(z => z.Item.SectionId == section.Id).ToList()
							}).ToList()
						};
						ss.Help = uoms[ss.GetBy().ModelId]+"("+ ss.GetBy().ModelId + ")" + " ==>  " + suns[ss.GetAbout().ModelId]+ "("+ss.GetAbout().ModelId+")";
						return ss;
					}).ToList();
				}
			}
		}
	}
}