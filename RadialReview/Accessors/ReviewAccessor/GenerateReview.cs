using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
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
        public static TeamAccessor _TeamAccessor = new TeamAccessor();


        public List<CoworkerRelationships> GetUsersWhoReviewUsers(
                UserOrganizationModel caller, 
                ReviewParameters parameters,
                long forTeam)
        {
            /*var teams = _TeamAccessor.GetTeamsDirectlyManaged(caller, caller.Id).ToList();
            if (!teams.Any(x => x.Id == forTeam)){
                throw new PermissionsException("You do not have access to that team.");
            }*/
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var perms = PermissionsUtility.Create(s, caller).ViewTeam(forTeam);
                    var orgId = caller.Organization.Id;

                    var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
                    var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
                    var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
                    //var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
                    //var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).List();
                    //var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
                    //var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();

                    var reviewWhoSettings = s.QueryOver<ReviewWhoSettingsModel>().Where(x => x.OrganizationId == orgId).List();

                    var queryProvider = new IEnumerableQuery(true);
                    queryProvider.AddData(allOrgTeams);
                    queryProvider.AddData(allTeamDurations);
                    queryProvider.AddData(allMembers);
                    //queryProvider.AddData(allManagerSubordinates);
                    //queryProvider.AddData(application);

                    queryProvider.AddData(reviewWhoSettings);

                    var d = new DataInteraction(queryProvider, s.ToUpdateProvider());

                    var teamMembers = TeamAccessor.GetTeamMembers(queryProvider, perms, caller, forTeam).Select(x => x.User);

                    var team = allOrgTeams.First(x => x.Id == forTeam);

                    var reviewWhoDictionary = new Dictionary<long, HashSet<long>>();

                    var items = new List<CoworkerRelationships>();

                    foreach (var member in teamMembers)
                    {
                        var reviewing = queryProvider.Get<UserOrganizationModel>(member.Id);
                        var usersTheyReview = ReviewAccessor.GetUsersThatReviewUser(caller, perms, d, reviewing, parameters, team, teamMembers.ToList());//.ToList();
                        reviewWhoDictionary[member.Id] = new HashSet<long>(usersTheyReview.Select(x => x.Key.Id));
                        items.Add(usersTheyReview);
                        /*var reviewWho = queryProvider.Where<ReviewWhoSettingsModel>(x => x.ByUserId == member.Id).ToList();
                        foreach (var r in reviewWho)
                        {
                            if (r.ForceState)
                            {
                                reviewWhoDictionary[member.Id].Add(r.ForUserId);
                            }
                            else
                            {
                                reviewWhoDictionary[member.Id].Remove(r.ForUserId);
                            }
                        }*/
                    }

                    //var output = reviewWhoDictionary.ToDictionary(x=>x.Key,x=>x.Value.ToList());
                                        /*.ToDictionary(
                                            x => queryProvider.Get<UserOrganizationModel>(x.Key),
                                            x => x.Value.Select(y => queryProvider.Get<UserOrganizationModel>(y)).ToList()
                                        );*/
                    return items;
                }
            }
        }


        #region Generate Review
        private static AskableUtility GetAskables(
            DataInteraction s, PermissionsUtility perms, UserOrganizationModel caller,
            UserOrganizationModel beingReviewed, OrganizationTeamModel team, ReviewParameters parameters,
            List<UserOrganizationModel> usersToReview)
        {
            #region comment
            /** Old questions way to do things.
            var review = _QuestionAccessor.GenerateReviewForUser(user, s, reviewContainer);
            //review.ForReviewsId = reviewContainer.Id;
            //review.DueDate = reviewContainer.DueDate;
            //review.Name = reviewContainer.ReviewName;
            //_ReviewAccessor.UpdateIndividualReview(user, review);
            */

            //var feedbackLSM = ApplicationAccessor.GetApplicationLocalizedStringModel(s, ApplicationAccessor.FEEDBACK);
            //var feedbackCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK);
            #endregion

            var appQuestions = ApplicationAccessor.GetApplicationQuestions(s.GetQueryProvider()).ToList();//, ApplicationAccessor.FEEDBACK);
            
            //Ensures uniqueness and removes people not in the review.
            var askable = new AskableUtility();
            var reviewUsers = GetUsersThatReviewUser(caller, perms, s, beingReviewed, parameters, team, usersToReview);
            var questions = QuestionAccessor.GetQuestionsForUser(s.GetQueryProvider(), perms, caller, beingReviewed.Id);
            
            if (parameters.ReviewSelf){
                askable.AddUnique(questions,AboutType.Self,beingReviewed.Id);
            }

            foreach(var user in reviewUsers.Relationships)
            {
                var id =/* aboutSelf ? beingReviewed.Id :*/ user.Key.Id;

                var responsibilities = ResponsibilitiesAccessor.GetResponsibilitiesForUser(caller,s.GetQueryProvider(),perms,id).ToListAlive();
                           /* .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, id)
                            .SelectMany(x => x.Responsibilities).ToListAlive();*/
                foreach (var aboutType in user.Value)
                {
                    foreach (var responsibility in responsibilities)
                        askable.AddUnique(responsibility, aboutType, id);

                    foreach(var aq in appQuestions)
                        askable.AddUnique(aq, aboutType, id);
                    askable.AddUser(user.Key, aboutType);

                }
            }

            #region comment
            /*

            // Personal Responsibilities 
            if (parameters.ReviewSelf)
            {
                var responsibilities = responsibilityGroups.SelectMany(x => x.Responsibilities).ToListAlive();
                var questions = QuestionAccessor.GetQuestionsForUser(s.GetQueryProvider(), perms, caller, beingReviewed.Id);

                askable.AddUnique(responsibilities, AboutType.Self, beingReviewed.Id);
                askable.AddUnique(questions, AboutType.Self, beingReviewed.Id);
                askable.AddUnique(feedbackQuestion, AboutType.Self, beingReviewed.Id);
                askable.AddUser(beingReviewed, AboutType.Self);
            }
            // Team members 
            if (parameters.ReviewTeammates)
            {
                List<OrganizationTeamModel> teams;

                if (team.Type != TeamType.Standard)
                    teams = responsibilityGroups.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>().Where(x => x.InterReview).ToList();
                else
                    teams = team.AsList();

                foreach (var t in teams)
                {
                    var teamMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, caller, t.Id)
                        .Where(x => x.User.Id != beingReviewed.Id)
                        .Where(x => usersToReview.Any(y => y.Id == x.UserId))
                        .ToListAlive();
                    foreach (var teammember in teamMembers)
                    {
                        var teamMemberResponsibilities = ResponsibilitiesAccessor
                                                            .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, teammember.User.Id)
                                                            .SelectMany(x => x.Responsibilities)
                                                            .ToListAlive();
                        askable.AddUnique(teamMemberResponsibilities, AboutType.Teammate, teammember.User.Id);
                        askable.AddUnique(feedbackQuestion, AboutType.Teammate, teammember.User.Id);
                        askable.AddUser(teammember.User, AboutType.Teammate);
                    }
                }
            }
            // Peers
            if (parameters.ReviewPeers)
            {
                if (team.Type != TeamType.Standard)
                {
                    List<UserOrganizationModel> peers = UserAccessor.GetPeers(s.GetQueryProvider(), perms, caller, beingReviewed.Id)
                        .Where(x => usersToReview.Any(y => y.Id == x.Id))
                        .ToListAlive();
                    foreach (var peer in peers)
                    {
                        var peerResponsibilities = ResponsibilitiesAccessor
                                                            .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, peer.Id)
                                                            .SelectMany(x => x.Responsibilities)
                                                            .ToListAlive();
                        askable.AddUnique(peerResponsibilities, AboutType.Peer, peer.Id);
                        askable.AddUnique(feedbackQuestion, AboutType.Peer, peer.Id);
                        askable.AddUser(peer, AboutType.Peer);
                    }
                }
            }
            // Managers
            if (parameters.ReviewManagers)
            {
                List<UserOrganizationModel> managers;
                if (team.Type != TeamType.Standard)
                    managers = UserAccessor.GetManagers(s.GetQueryProvider(), perms, caller, beingReviewed.Id)
                        .Where(x => usersToReview.Any(y => y.Id == x.Id))
                        .ToListAlive();
                else
                    managers = UserAccessor.GetUserOrganization(s.GetQueryProvider(), perms, caller, team.ManagedBy, false, false)
                                            .AsList()
                                            .Where(x => x.Id != beingReviewed.Id)
                                            .Where(x => usersToReview.Any(y => y.Id == x.Id))
                                            .ToList();

                foreach (var manager in managers)
                {
                    var managerResponsibilities = ResponsibilitiesAccessor
                                                        .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, manager.Id)
                                                        .SelectMany(x => x.Responsibilities)
                                                        .ToListAlive();
                    askable.AddUnique(managerResponsibilities, AboutType.Manager, manager.Id);
                    askable.AddUnique(feedbackQuestion, AboutType.Manager, manager.Id);
                    askable.AddUser(manager, AboutType.Manager);
                }
            }
            // Subordinates
            if (parameters.ReviewSubordinates)
            {
                if (team.Type != TeamType.Standard)
                {
                    List<UserOrganizationModel> subordinates = UserAccessor.GetSubordinates(s.GetQueryProvider(), perms, caller, beingReviewed.Id)
                                                              .Where(x => usersToReview.Any(y => y.Id == x.Id))
                                                              .Where(x => x.Id != beingReviewed.Id)
                                                              .ToListAlive();
                    foreach (var subordinate in subordinates)
                    {
                        var subordinateResponsibilities = ResponsibilitiesAccessor
                                                            .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, subordinate.Id)
                                                            .SelectMany(x => x.Responsibilities)
                                                            .ToListAlive();
                        askable.AddUnique(subordinateResponsibilities, AboutType.Subordinate, subordinate.Id);
                        askable.AddUnique(feedbackQuestion, AboutType.Subordinate, subordinate.Id);
                        askable.AddUser(subordinate, AboutType.Subordinate);
                    }
                }
            }*/
            #endregion
            return askable;
        }

            #region Generate Answers
        private static void GenerateSliderAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {

            var slider = new SliderAnswer()
            {
                Complete = false,
                Percentage = null,
                Askable = askable.Askable,
                Required = askable.Askable.Required,
                ForReviewId = review.Id,
                ByUserId = forUser.Id,
                AboutUserId = askable.AboutUserId,
                ForReviewContainerId = review.ForReviewsId,
                AboutType = askable.AboutType

            };
            session.Save(slider);

        }
        private static void GenerateFeedbackAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {
            var feedback = new FeedbackAnswer()
            {
                Complete = false,
                Feedback = null,
                Askable = askable.Askable,
                Required = askable.Askable.Required,
                ForReviewId = review.Id,
                ByUserId = forUser.Id,
                AboutUserId = askable.AboutUserId,
                ForReviewContainerId = review.ForReviewsId,
                AboutType = askable.AboutType
            };
            session.Save(feedback);

        }

        private static void GenerateThumbsAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {
            var thumbs = new ThumbsAnswer()
            {
                Complete = false,
                Thumbs = ThumbsType.None,
                Askable = askable.Askable,
                Required = askable.Askable.Required,
                ForReviewId = review.Id,
                ByUserId = forUser.Id,
                AboutUserId = askable.AboutUserId,
                ForReviewContainerId = review.ForReviewsId,
                AboutType = askable.AboutType
            };
            session.Save(thumbs);

        }

        private static void GenerateRelativeComparisonAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {
            var peers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager).SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate));
            var managers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager);
            var managing = forUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate);

            var groupMembers = forUser.Groups.SelectMany(x => x.GroupUsers);

            var union = peers.UnionBy(x => x.Id, managers, managing, groupMembers).ToList();

            var len = union.Count();
            List<Tuple<UserOrganizationModel, UserOrganizationModel>> items = new List<Tuple<UserOrganizationModel, UserOrganizationModel>>();
            for (int i = 0; i < len - 1; i++)
            {
                for (int j = i + 1; j < len; j++)
                {
                    var relComp = new RelativeComparisonAnswer()
                    {
                        Required = askable.Askable.Required,
                        Askable = askable.Askable,
                        Complete = false,
                        First = union[i],
                        Second = union[j],
                        Choice = RelativeComparisonType.Skip,
                        ForReviewId = review.Id,
                        ByUserId = forUser.Id,
                        AboutUserId = askable.AboutUserId,
                        ForReviewContainerId = review.ForReviewsId,
                        AboutType = askable.AboutType
                    };
                    items.Add(Tuple.Create(union[i], union[j]));
                    session.Save(relComp);
                }
            }

        }
        #endregion
        #endregion
    }
}