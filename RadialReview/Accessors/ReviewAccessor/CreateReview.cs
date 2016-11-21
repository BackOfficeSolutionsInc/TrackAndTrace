using System.Net.Http;
using System.Web;
using log4net.Repository.Hierarchy;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Periods;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {

		#region Create


		[Obsolete("Fix for AC")]
		public List<Mail> CreateReviewFromPrereview(
			HttpContext context,
			DataInteraction dataInteraction, PermissionsUtility perms,
			UserOrganizationModel caller, ReviewsModel reviewContainer,
			List<WhoReviewsWho> whoReviewsWho,
			IHubContext hub = null, String userId = null, int total = 0) {
			int count = 0;


			var unsentEmails = new List<Mail>();
			var nw = DateTime.UtcNow;
			var range = new DateRange(nw, nw);
			// var reviewerIds = whoReviewsWho.Select(x => x.Item1).Distinct().ToList();
			var reviewersWRW = whoReviewsWho.GroupBy(x => x.Reviewer).ToList();
			var reviewers = whoReviewsWho.Select(x => x.Reviewer).Distinct().ToList();

			foreach (var reviewerWRW in reviewersWRW) {
				//Create review for user
				var reviewer = reviewerWRW.Key;
				var reviewees = reviewerWRW.Select(x => x.Reviewee).ToList();//  whoReviewsWho.Where(x => x.Item1 == reviewerId).Distinct().Select(x => x.Item2);
				var reviewerUser = dataInteraction.Get<UserOrganizationModel>(reviewer.RGMId);

				if (reviewerUser == null || reviewerUser.DeleteTime != null)
					continue;

				var askables = GetAskables(dataInteraction, perms, reviewer, reviewees, range);

				if (askables.Any()) {
					QuestionAccessor.GenerateReviewForUser(context, dataInteraction, perms, reviewerUser, reviewContainer, askables);
					if (hub != null)
						hub.Clients.User(userId).status("Added " + count + " user".Pluralize(count) + " out of " + total + ".");

					//Emails
					var guid = Guid.NewGuid();
					var nexus = new NexusModel(guid) { ForUserId = reviewerUser.Id, ActionCode = NexusActions.TakeReview };
					nexus.SetArgs("" + reviewContainer.Id);
					NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);
					var org = reviewContainer.Organization;
					var email = ConstructNewReviewEmail(reviewContainer, reviewerUser, guid, org);
					unsentEmails.Add(email);

					log.Info("CreateReview user=" + reviewer.RGMId + " for review=" + reviewContainer.Id);
				} else {
					log.Info("NO ASKABLES, Skipping CreateReview user=" + reviewer.RGMId + " review=" + reviewContainer.Id);
				}
			}

			var haventGeneratedAReview = new Func<long, bool>(revieweeId => !reviewers.Any(reviewer => reviewer.RGMId == revieweeId));

			foreach (var revieweeId in whoReviewsWho.Select(x => x.Reviewee.RGMId).Distinct().Where(haventGeneratedAReview)) {
				try {
					var user = dataInteraction.Get<UserOrganizationModel>(revieweeId);
					if (user != null) {
						QuestionAccessor.GenerateReviewForUser(context, dataInteraction, perms, user, reviewContainer, new AskableCollection());
					}
				} catch (Exception e) {
					log.Error("Error in creating review from prereview", e);
				}
			}

			return unsentEmails;
		}

		private static Mail ConstructNewReviewEmail(ReviewsModel reviewContainer, UserOrganizationModel reviewerUser, Guid nexusGuid, OrganizationModel org) {
			var format = org.NotNull(y => y.Settings.NotNull(z => z.GetDateFormat())) ?? "MM-dd-yyyy";
			var dueDate = (reviewContainer.DueDate.AddDays(-1)).ToString(format);
			var url = Config.BaseUrl(org) + "n/" + nexusGuid;
			var productName = Config.ProductName(org);
			var orgName = org.GetName();
			var reviewName = reviewContainer.ReviewName;
			var usersName = reviewerUser.GetName();
			return Mail.To(EmailTypes.NewReviewIssued, reviewerUser.GetEmail())
					   .Subject(EmailStrings.NewReview_Subject, orgName)
					   .Body(EmailStrings.NewReview_Body, usersName, orgName, dueDate, url, url, productName, reviewName);
		}

		[Obsolete("Fix for AC")]
		public async Task<ResultObject> CreateReviewFromCustom(
			HttpContext context,
			UserOrganizationModel caller, long forTeamId, DateTime dueDate, String reviewName, bool emails, bool anonFeedback,
			List<WhoReviewsWho> whoReviewsWho/*, long periodId, long nextPeriodId*/) {
			var unsentEmails = new List<Mail>();
			using (var s = HibernateSession.GetCurrentSession()) {
				ReviewsModel reviewContainer;
				var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
				var userId = caller.User.UserName;

				using (var tx = s.BeginTransaction()) {

					hub.Clients.User(userId).status("Creating Review");
					var perms = PermissionsUtility.Create(s, caller);

					bool reviewManagers = true,
						 reviewPeers = true,
						 reviewSelf = true,
						 reviewSubordinates = true,
						 reviewTeammates = true;

					reviewContainer = new ReviewsModel() {
						AnonymousByDefault = anonFeedback,
						DateCreated = DateTime.UtcNow,
						DueDate = dueDate,
						ReviewName = reviewName,
						CreatedById = caller.Id,
						HasPrereview = false,

						ReviewManagers = reviewManagers,
						ReviewPeers = reviewPeers,
						ReviewSelf = reviewSelf,
						ReviewSubordinates = reviewSubordinates,
						ReviewTeammates = reviewTeammates,

						/*PeriodId = periodId,
						NextPeriodId = nextPeriodId,*/

						ForTeamId = forTeamId
					};
					ReviewAccessor.CreateReviewContainer(s, perms, caller, reviewContainer);
				}
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					perms.IssueForTeam(forTeamId);

					var organization = caller.Organization;
					OrganizationTeamModel team;

					var orgId = caller.Organization.Id;


					hub.Clients.User(userId).status("Gathering Data");

					var dataInteraction = GetReviewDataInteraction(s, orgId);

					team = dataInteraction.GetQueryProvider().All<OrganizationTeamModel>().First(x => x.Id == forTeamId);
					var usersToReview = TeamAccessor.GetTeamMembers(dataInteraction.GetQueryProvider(), perms, forTeamId, false).ToListAlive();

					List<Exception> exceptions = new List<Exception>();
					var toReview = usersToReview.Select(x => x.User).ToList();
					//var orgName = organization.Name.Translate();

					////////////////////////////////////////////
					//HEAVY LIFTING HERE:
					var clientReviews = CreateReviewFromPrereview(context, dataInteraction, perms, caller, reviewContainer, whoReviewsWho, hub, userId, usersToReview.Count());
					////////////////////////////////////////////
					unsentEmails.AddRange(clientReviews);

					EventUtil.Trigger(x => x.Create(s, EventType.IssueReview, caller, reviewContainer, message: reviewContainer.ReviewName));

					tx.Commit();
					s.Flush();
					hub.Clients.User(userId).status("Done!");



				}
			}
			var emailResult = new EmailResult();
			if (emails) {
				emailResult = await Emailer.SendEmails(unsentEmails);

			}
			if (emailResult.Errors.Count() > 0) {
				var message = String.Join("\n", emailResult.Errors.Select(x => x.Message));
				return new ResultObject(new RedirectException(emailResult.Errors.Count() + " errors:\n" + message));
			}
			return ResultObject.Create(new { due = dueDate, sent = emailResult.Sent, errors = emailResult.Errors.Count() });
		}

		[Obsolete("Fix for AC")]
		public static DataInteraction GetReviewDataInteraction(ISession s, long orgId) {
			var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).Future();
			var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).Future();
			var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).Future();
			var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).Future();
			var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).Future();
			var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).Future();
			var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).Future();

			var allRoles = s.QueryOver<RoleModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
			var allValues = s.QueryOver<CompanyValueModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
			var allRocks = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
			var allRGM = s.QueryOver<ResponsibilityGroupModel>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).Future();
			var allAboutCompany = s.QueryOver<AboutCompanyAskable>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).Future();

			var allRoleLinks = s.QueryOver<RoleLink>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();

			var accountablityNodes = s.QueryOver<AccountabilityNode>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();

			var queryProvider = new IEnumerableQuery();
			queryProvider.AddData(allOrgTeams.ToList());
			queryProvider.AddData(allTeamDurations);
			queryProvider.AddData(allMembers);
			queryProvider.AddData(allManagerSubordinates);
			queryProvider.AddData(allPositions);
			queryProvider.AddData(allRoles);
			queryProvider.AddData(allValues);
			queryProvider.AddData(allRocks);
			queryProvider.AddData(allAboutCompany);
			queryProvider.AddData(allRGM);
			queryProvider.AddData(allRoleLinks);
			queryProvider.AddData(applicationQuestions);
			queryProvider.AddData(accountablityNodes);
			queryProvider.AddData(application);

			var updateProvider = new SessionUpdate(s);
			var dataInteraction = new DataInteraction(queryProvider, updateProvider);
			return dataInteraction;
		}

		[Obsolete("Fix for AC")]
		public static void CreateReviewContainer(ISession s, PermissionsUtility perms, UserOrganizationModel caller, ReviewsModel reviewContainer) {
			using (var tx = s.BeginTransaction()) {
				perms.ManagerAtOrganization(caller.Id, caller.Organization.Id);
				//foreach(var pid in new[] {reviewContainer.NextPeriodId,reviewContainer.PeriodId})
				//{
				//    var p = s.Get<PeriodModel>(pid);
				//	if (p.OrganizationId != caller.Organization.Id){
				//		throw new PermissionsException("You do not have access to this session.");
				//	}
				//}

				reviewContainer.CreatedById = caller.Id;
				reviewContainer.OrganizationId = caller.Organization.Id;
				reviewContainer.Organization = caller.Organization;

				s.SaveOrUpdate(reviewContainer);
				tx.Commit();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <param name="caller"></param>
		/// <param name="updateOthers"></param>
		/// <param name="dueDate"></param>
		/// <param name="parameters"></param>
		/// <param name="dataInteraction"></param>
		/// <param name="reviewContainer"></param>
		/// <param name="perms"></param>
		/// <param name="organization"></param>
		/// <param name="team"></param>
		/// <param name="exceptions"></param>
		/// <param name="user"></param>
		/// <param name="tree"></param>
		/// <param name="accessibleUsers"></param>
		/// <param name="range"></param>
		/// <returns></returns>
		[Obsolete("Fix for AC")]
		private static List<Mail> AddUserToReview(
			HttpContext context,
			UserOrganizationModel caller,
			bool updateOthers, DateTime dueDate,
			ReviewParameters parameters, DataInteraction dataInteraction, ReviewsModel reviewContainer, PermissionsUtility perms,
			OrganizationModel organization, OrganizationTeamModel team, ref List<Exception> exceptions,
			Reviewee user, AngularAccountabilityChart tree,
			List<Reviewee> accessibleUsers,
			DateRange range) {
			var unsentEmails = new List<Mail>();
			var format = caller.NotNull(x => x.Organization.NotNull(y => y.Settings.NotNull(z => z.GetDateFormat()))) ?? "MM-dd-yyyy";
			try {

				AskableCollection addToOthers = null;
				//var accessibleUserIds = accessibleUsers.Select(x => x.Id).ToList();
				var askables = GetAskablesBidirectional(dataInteraction, perms, tree, user, team, parameters, accessibleUsers, range, updateOthers, ref addToOthers);

				var revieweeUser = dataInteraction.Get<UserOrganizationModel>(user.ConvertToReviewer().RGMId);

				//Create the Review
				if (askables.Askables.Any()) {
					var review = QuestionAccessor.GenerateReviewForUser(context, dataInteraction, perms, revieweeUser, reviewContainer, askables);
					//Generate Review Nexus
					var guid = Guid.NewGuid();
					var nexus = new NexusModel(guid) {
						ForUserId = user.ConvertToReviewer().RGMId,
						ActionCode = NexusActions.TakeReview
					};
					NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);
					var url = Config.BaseUrl(organization) + "n/" + guid;
					unsentEmails.Add(Mail
						.To(EmailTypes.NewReviewIssued, revieweeUser.GetEmail())
						.Subject(EmailStrings.NewReview_Subject, organization.Name.Translate())
						.Body(EmailStrings.NewReview_Body, revieweeUser.GetName(), caller.GetName(), (dueDate.AddDays(-1)).ToString(format), url, url, ProductStrings.ProductName, reviewContainer.ReviewName)
					);
				}



				//Update everyone else's review.
				if (updateOthers) {

					var reviewsLookup = dataInteraction.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainer.Id && x.DeleteTime == null);
					var newReviewers = addToOthers.Reviewers; //addToOthers.GroupBy(x => x.Reviewee).Select(relationship => relationship.OrderByDescending(y => (long)y.ReviewerIsThe).First());


					foreach (var reviewer in newReviewers/*.Where(x => x.ReviewerIsThe != AboutType.Self)*/) {
						try {
							var r = reviewsLookup.Where(x => x.ReviewerUserId == reviewer.RGMId).SingleOrDefault();
							if (r != null) {
								// Check that askableUser.Reviewee.ConvertToReviewer() != reviewee
								var revieweeIsThe = addToOthers.RevieweeIsThe[reviewer];

								AddToReview(dataInteraction, perms, caller, reviewer, r.ForReviewContainerId, user, revieweeIsThe);
							}

							var revieweeReview = reviewsLookup.Where(x => x.ReviewerUserId == user.RGMId).SingleOrDefault();

							if (revieweeReview == null) {
								var u = dataInteraction.Get<UserOrganizationModel>(user.RGMId);
								QuestionAccessor.GenerateReviewForUser(context, dataInteraction, perms,u, reviewContainer, new AskableCollection());
							}

						} catch (Exception e) {
							log.Error(e.Message, e);
							exceptions.Add(e);

						}
					}
				}

			} catch (Exception e) {
				log.Error(e.Message, e);
				exceptions.Add(e);
			}
			return unsentEmails;
		}

		#endregion
	}
}