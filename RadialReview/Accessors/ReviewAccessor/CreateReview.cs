using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
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
using System.Web;

namespace RadialReview.Accessors
{
    public partial class ReviewAccessor : BaseAccessor
    {

        #region Create

        private static void CreateReviewContainer(ISession s, PermissionsUtility perms, UserOrganizationModel caller, ReviewsModel reviewContainer)
        {
            using (var tx = s.BeginTransaction())
            {
                perms.ManagerAtOrganization(caller.Id, caller.Organization.Id);
                reviewContainer.CreatedById = caller.Id;
                reviewContainer.ForOrganization = caller.Organization;
                s.SaveOrUpdate(reviewContainer);
                tx.Commit();
            }
        }

        public ResultObject CreateCompleteReview(UserOrganizationModel caller, long forTeamId, DateTime dueDate, String reviewName, bool emails,
            bool reviewSelf, bool reviewManagers, bool reviewSubordinates, bool reviewTeammates, bool reviewPeers)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                ReviewsModel reviewContainer;
                var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                var userId = caller.User.UserName;
                using (var tx = s.BeginTransaction())
                {

                    hub.Clients.User(userId).status("Creating Review");
                    var perms = PermissionsUtility.Create(s, caller);
                    reviewContainer = new ReviewsModel()
                      {
                          DateCreated = DateTime.UtcNow,
                          DueDate = dueDate,
                          ReviewName = reviewName,
                          CreatedById = caller.Id,

                          ReviewManagers = reviewManagers,
                          ReviewPeers = reviewPeers,
                          ReviewSelf = reviewSelf,
                          ReviewSubordinates = reviewSubordinates,
                          ReviewTeammates = reviewTeammates,

                          ForTeamId = forTeamId
                      };

                    ReviewAccessor.CreateReviewContainer(s, perms, caller, reviewContainer);
                }

                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ManagingTeam(forTeamId);

                    var organization = caller.Organization;
                    OrganizationTeamModel team;

                    var orgId = caller.Organization.Id;


                    hub.Clients.User(userId).status("Gathering Data");

                    var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
                    var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
                    var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
                    var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
                    var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).List();
                    var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
                    var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();

                    var queryProvider = new IEnumerableQuery();
                        queryProvider.AddData(allOrgTeams);
                        queryProvider.AddData(allTeamDurations);
                        queryProvider.AddData(allMembers);
                        queryProvider.AddData(allManagerSubordinates);
                        queryProvider.AddData(allPositions);
                        queryProvider.AddData(applicationQuestions);
                        queryProvider.AddData(application);

                    var updateProvider = new SessionUpdate(s);
                    var dataInteraction = new DataInteraction(queryProvider, updateProvider);

                    team = allOrgTeams.First(x => x.Id == forTeamId);

                    var usersToReview = TeamAccessor.GetTeamMembers(dataInteraction.GetQueryProvider(), perms, caller, forTeamId).ToListAlive();

                    List<Exception> exceptions = new List<Exception>();

                    var toReview = usersToReview.Select(x => x.User).ToList();

                    int sent = 0;
                    int errors = 0;
                    int count = 0;
                    int total = usersToReview.Count();
                    foreach (var beingReviewed in usersToReview)
                    {
                        var beingReviewedUser = beingReviewed.User;
                        AddUserToReview(caller, false, dueDate, emails,
                            reviewContainer.GetParameters(),
                            dataInteraction, reviewContainer, perms, organization, team, exceptions, ref sent, ref errors, beingReviewedUser, toReview);
                        count++;
                        hub.Clients.User(userId).status("Added " + count + " user".Pluralize(count) + " out of " + total + ".");
                    }
                    tx.Commit();
                    s.Flush();
                    hub.Clients.User(userId).status("Done!");

                    if (errors > 0)
                    {
                        var message = String.Join("\n", exceptions.Select(x => x.Message));
                        return new ResultObject(new RedirectException(errors + " errors:\n" + message));
                    }
                    return ResultObject.Create(new { due = dueDate, sent = sent, errors = errors });
                }
            }
        }

        private static void AddUserToReview(
            UserOrganizationModel caller,
            bool updateOthers, DateTime dueDate,
            bool sendEmail, ReviewParameters parameters,
            DataInteraction dataInteraction, ReviewsModel reviewContainer, PermissionsUtility perms,
            OrganizationModel organization, OrganizationTeamModel team, List<Exception> exceptions, ref int sent, ref int errors,
            UserOrganizationModel beingReviewedUser,
            List<UserOrganizationModel> usersToReview)
        {
            try
            {
                #region comment
                /*about self
                var askable = new List<AskableAbout>();
                askable.AddRange(responsibilities.Select(x => new AskableAbout() { Askable = x, AboutUserId = beingReviewed.Id, AboutType = AboutType.Self }));
                askable.AddRange(questions.Select(x => new AskableAbout() { Askable = x, AboutUserId = beingReviewed.Id, AboutType = AboutType.Self }));
                */
                //var allowableUserIds = TeamAccessor.GetTeamMembers(dataInteraction.GetQueryProvider(), perms, caller, team.Id).ToListAlive().Select(x => x.UserId).ToList();

                //Generate Askables
                /*var parameters = new ReviewParameters(){
                    ReviewManagers=reviewManagers,
                    ReviewPeers=reviewPeers,
                    ReviewSelf=reviewSelf,
                    ReviewSubordinates=reviewSubordinates,
                    ReviewTeammates=reviewTeammates
                };*/
                #endregion

                var askables = GetAskables(dataInteraction, perms, caller, beingReviewedUser/*, aboutSelf*/, team, parameters, usersToReview);

                //Create the Review
                var review = QuestionAccessor.GenerateReviewForUser(dataInteraction, perms, caller, beingReviewedUser, reviewContainer, askables.Askables);
                //Generate Review Nexus
                Guid guid = Guid.NewGuid();
                NexusModel nexus = new NexusModel(guid)
                {
                    ForUserId = beingReviewedUser.Id,
                    ActionCode = NexusActions.TakeReview
                };
                NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);
                if (sendEmail)
                {
                    try
                    {
                        //Send email
                        var subject = String.Format(RadialReview.Properties.EmailStrings.NewReview_Subject, organization.Name.Translate());
                        var body = String.Format(EmailStrings.NewReview_Body, beingReviewedUser.GetName(), caller.GetName(), dueDate.ToShortDateString(), ProductStrings.BaseUrl + "n/" + guid, ProductStrings.BaseUrl + "n/" + guid, ProductStrings.ProductName);
                        Emailer.SendEmail(dataInteraction.GetUpdateProvider(), beingReviewedUser.EmailAtOrganization, subject, body);
                    }
                    catch (Exception e)
                    {
                        log.Error(e.Message, e);
                        errors++;
                        exceptions.Add(e);
                    }
                }
                sent++;

                //TODO not wroking...
                // var newUsers=askables.GroupBy(x=>x.AboutUserId).Select(x=>x.OrderByDescending(y=>y.AboutType).First());


                //Update everyone else's review.
                if (updateOthers)
                {
                    var beingReviewedUserId = beingReviewedUser.Id;

                    #region comments
                    /*
                if (reviewManagers)
                {
                    var subordinates = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == beingReviewedUserId).List().ToListAlive();
                    foreach (var subordinate in subordinates)
                    {
                        var r = reviewsLookup.Where(x => x.ForUserId == subordinate.Id).SingleOrDefault();
                        AddToReview(s, perms, caller, caller.Id, r.Id, beingReviewedUserId, AboutType.Manager);
                    }
                }

                if (reviewSubordinates)
                {
                    var managers = s.QueryOver<ManagerDuration>().Where(x => x.SubordinateId == beingReviewedUserId).List().ToListAlive();
                    foreach (var manager in managers)
                    {
                        var r = reviewsLookup.Where(x => x.ForUserId == manager.Id).SingleOrDefault();
                        AddToReview(s, perms, caller, caller.Id, r.Id, beingReviewedUserId, AboutType.Subordinate);
                    }
                }

                if (reviewPeers)
                {
                    List<UserOrganizationModel> peers = UserAccessor.GetPeers(s, perms, caller, beingReviewedUserId);
                    foreach (var peer in peers)
                    {
                        var r = reviewsLookup.Where(x => x.ForUserId == peer.Id).SingleOrDefault();
                        AddToReview(s, perms, caller, caller.Id, r.Id, beingReviewedUserId, AboutType.Peer);
                    }
                }

                if (reviewTeammates)
                {
                    List<OrganizationTeamModel> teams;

                    if (team.Type != TeamType.Standard)
                        teams = responsibilityGroups.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>().Where(x => x.InterReview).ToList();
                    else
                        teams = team.AsList();

                    foreach (var t in teams)
                    {
                        var teamMembers = TeamAccessor.GetTeamMembers(s, perms, caller, t.Id).Where(x => x.User.Id != beingReviewed.Id).ToListAlive();
                        foreach (var teammember in teamMembers)
                        {
                            var teamMemberResponsibilities = ResponsibilitiesAccessor
                                                                .GetResponsibilityGroupsForUser(s, perms, caller, teammember.User.Id)
                                                                .SelectMany(x => x.Responsibilities)
                                                                .ToListAlive();
                            askable.AddUnique(teamMemberResponsibilities, AboutType.Teammate, teammember.User.Id);
                            askable.AddUnique(feedbackQuestion, AboutType.Teammate, teammember.User.Id);
                        }
                    }


                    List<UserOrganizationModel> teammates = UserAccessor.GetPeers(s, perms, caller, beingReviewedUserId);
                    foreach (var peer in peers)
                    {
                        var r = reviewsLookup.Where(x => x.ForUserId == peer.Id).SingleOrDefault();
                        AddToReview(s, perms, caller, caller.Id, r.Id, beingReviewedUserId, AboutType.Teammate);
                    }
                }*/
                    #endregion

                    var reviewsLookup = dataInteraction.Where<ReviewModel>(x => x.ForReviewsId == reviewContainer.Id);
                    var newUsers = askables.AllUsers.GroupBy(x => x.Item1.Id).Select(x => x.OrderByDescending(y => (long)y.Item2).First());


                    foreach (var askableUser in newUsers.Where(x => x.Item2 != AboutType.Self))
                    {
                        try
                        {
                            var r = reviewsLookup.Where(x => x.ForUserId == askableUser.Item1.Id).SingleOrDefault();
                            if (r != null)
                            {
                                AddToReview(dataInteraction, perms, caller, caller.Id, r.Id, beingReviewedUserId, askableUser.Item2.Invert());
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message, e);
                            errors++;
                            exceptions.Add(e);

                        }
                    }
                }

            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                errors++;
                exceptions.Add(e);
            }
        }

        /// <summary>
        /// Not secure
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="review"></param>
        /*public void UpdateIndividualReview(UserOrganizationModel caller,ReviewModel review)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    //PermissionsUtility.Create(s, caller).EditReview();
                 
                    s.SaveOrUpdate(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }*/

        #endregion



    }
}