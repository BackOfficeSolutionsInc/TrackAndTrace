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

namespace RadialReview.Accessors {
    public partial class ReviewAccessor : BaseAccessor {
		
        #region Create
        public List<Mail> CreateReviewFromPrereview(
			HttpContext context,
            DataInteraction dataInteraction, PermissionsUtility perms,
            UserOrganizationModel caller, ReviewsModel reviewContainer,
            string organizationName, List<Tuple<long, long>> whoReviewsWho,
            IHubContext hub = null, String userId = null, int total = 0) {
            int count = 0;

            var unsentEmails = new List<Mail>();
	        var nw = DateTime.UtcNow;
			var range = new DateRange(nw,nw);
	        var reviewerIds = whoReviewsWho.Select(x => x.Item1).Distinct().ToList();

            foreach (var reviewerId in reviewerIds) {
                //Create review for user
				var revieweeIds = whoReviewsWho.Where(x => x.Item1 == reviewerId).Distinct().Select(x => x.Item2);
                var user = dataInteraction.Get<UserOrganizationModel>(reviewerId);

				if (user==null || user.DeleteTime != null)
					continue;

	            var allAskables=GetAskables(caller, perms, dataInteraction, revieweeIds, reviewerId,/*reviewContainer.PeriodId,*/range);
				
				if (allAskables.Any()) {
					QuestionAccessor.GenerateReviewForUser(context,dataInteraction, perms, caller, user, reviewContainer, allAskables);
                    if (hub != null) {
                        hub.Clients.User(userId).status("Added " + count + " user".Pluralize(count) + " out of " + total + ".");
                    }

                    //Emails
                    var guid = Guid.NewGuid();
                    var nexus = new NexusModel(guid) {
                        ForUserId = user.Id,
                        ActionCode = NexusActions.TakeReview
                    };
                    NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);
					var org = reviewContainer.ForOrganization;
					var productName = Config.ProductName(org);
                    unsentEmails.Add(
							Mail.To(EmailTypes.NewReviewIssued, user.GetEmail())
                            .Subject(EmailStrings.NewReview_Subject, organizationName)
							.Body(EmailStrings.NewReview_Body, user.GetName(), organizationName, (reviewContainer.DueDate.AddDays(-1)).ToShortDateString(), Config.BaseUrl(org) + "n/" + guid, Config.BaseUrl(org) + "n/" + guid, productName, reviewContainer.ReviewName)
						);
					log.Info("CreateReview user=" + reviewerId + " for review=" + reviewContainer.Id);
                }
                else {
					log.Info("NO ASKABLES, Skipping CreateReview user=" + reviewerId + " review=" + reviewContainer.Id);
                }
            }

	        foreach (var revieweeId in whoReviewsWho.Select(x => x.Item2).Distinct().Where(x => !reviewerIds.Contains(x))){
		        try{
			        var user = dataInteraction.Get<UserOrganizationModel>(revieweeId);
			        if (user != null){
						QuestionAccessor.GenerateReviewForUser(context,dataInteraction, perms, caller, user, reviewContainer, new List<AskableAbout>());
			        }
		        }
		        catch (Exception e){
					log.Error("Error in creating review from prereview",e);
		        }
	        }

	        return unsentEmails;
        }


		public async Task<ResultObject> CreateReviewFromCustom(
			HttpContext context,
			UserOrganizationModel caller, long forTeamId, DateTime dueDate, String reviewName, bool emails, bool anonFeedback,
			List<Tuple<long, long>> whoReviewsWho/*, long periodId, long nextPeriodId*/)
		{
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

					reviewContainer = new ReviewsModel()
					{
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
                    var orgName = organization.Name.Translate();
                    int sent, errors;

					////////////////////////////////////////////
					//HEAVY LIFTING HERE:
					var clientReviews = CreateReviewFromPrereview(context,dataInteraction, perms, caller, reviewContainer, orgName, whoReviewsWho, hub, userId, usersToReview.Count());
					////////////////////////////////////////////
                    unsentEmails.AddRange(clientReviews);


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

        public static DataInteraction GetReviewDataInteraction(ISession s, long orgId) {
            var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
            var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
            var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
            var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
            var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).List();
            var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
			var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();

			var allRoles = s.QueryOver<RoleModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).List();
			var allValues = s.QueryOver<CompanyValueModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).List();
			var allRocks = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).List();
			var allRGM = s.QueryOver<ResponsibilityGroupModel>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).List();
			var allAboutCompany = s.QueryOver<AboutCompanyAskable>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).List();


            var queryProvider = new IEnumerableQuery();
            queryProvider.AddData(allOrgTeams);
            queryProvider.AddData(allTeamDurations);
            queryProvider.AddData(allMembers);
            queryProvider.AddData(allManagerSubordinates);
			queryProvider.AddData(allPositions);
			queryProvider.AddData(allRoles);
			queryProvider.AddData(allValues);
			queryProvider.AddData(allRocks);
			queryProvider.AddData(allAboutCompany);
			queryProvider.AddData(allRGM);
			queryProvider.AddData(applicationQuestions);
			queryProvider.AddData(application);

            var updateProvider = new SessionUpdate(s);
            var dataInteraction = new DataInteraction(queryProvider, updateProvider);
            return dataInteraction;
        }

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
                reviewContainer.ForOrganizationId = caller.Organization.Id;
	            reviewContainer.ForOrganization = caller.Organization;

                s.SaveOrUpdate(reviewContainer);
                tx.Commit();
            }
        }

        private static List<Mail> AddUserToReview(
			HttpContext context,
            UserOrganizationModel caller,
            bool updateOthers, DateTime dueDate,
            ReviewParameters parameters, DataInteraction dataInteraction, ReviewsModel reviewContainer, PermissionsUtility perms,
            OrganizationModel organization, OrganizationTeamModel team, ref List<Exception> exceptions,
            UserOrganizationModel beingReviewedUser,
            List<UserOrganizationModel> accessibleUsers,
			DateRange range) {
            var unsentEmails = new List<Mail>();
            try {
				var askables = GetAskablesBidirectional(
					dataInteraction, perms, caller, beingReviewedUser,
					team, parameters, accessibleUsers.Select(x=>x.Id).ToList(),/*reviewContainer.PeriodId,*/range);

                //Create the Review
                if (askables.Askables.Any()) {
					var review = QuestionAccessor.GenerateReviewForUser(context,dataInteraction, perms, caller, beingReviewedUser, reviewContainer, askables.Askables);
                    //Generate Review Nexus
                    var guid = Guid.NewGuid();
                    var nexus = new NexusModel(guid) {
                        ForUserId = beingReviewedUser.Id,
                        ActionCode = NexusActions.TakeReview
                    };
                    NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);
					var url = Config.BaseUrl(organization) + "n/" + guid;
                    unsentEmails.Add(Mail
                        .To(EmailTypes.NewReviewIssued,beingReviewedUser.GetEmail())
                        .Subject(EmailStrings.NewReview_Subject, organization.Name.Translate())
                        .Body(EmailStrings.NewReview_Body, beingReviewedUser.GetName(), caller.GetName(), (dueDate.AddDays(-1)).ToShortDateString(),url,url, ProductStrings.ProductName,reviewContainer.ReviewName)
                    );
                }

                

                //Update everyone else's review.
                if (updateOthers) {
                    var beingReviewedUserId = beingReviewedUser.Id;
					
                    var reviewsLookup = dataInteraction.Where<ReviewModel>(x => x.ForReviewsId == reviewContainer.Id);
                    var newUsers = askables.AllUsers.GroupBy(x => x.Item1.Id).Select(x => x.OrderByDescending(y => (long)y.Item2).First());


                    foreach (var askableUser in newUsers.Where(x => x.Item2 != AboutType.Self)) {
                        try {
                            var r = reviewsLookup.Where(x => x.ForUserId == askableUser.Item1.Id).SingleOrDefault();
                            if (r != null) {
                                AddToReview(dataInteraction, perms, caller, askableUser.Item1.Id, r.ForReviewsId, beingReviewedUserId, askableUser.Item2.Invert());
                            }
                        }
                        catch (Exception e) {
                            log.Error(e.Message, e);
                            exceptions.Add(e);

                        }
                    }
                }

            }
            catch (Exception e) {
                log.Error(e.Message, e);
                exceptions.Add(e);
            }
            return unsentEmails;
        }

        #endregion
	}
}