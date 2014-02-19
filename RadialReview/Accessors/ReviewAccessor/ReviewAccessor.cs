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

        /// <summary>
        /// Requires:
        ///     <br/>
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="perms"></param>
        /// <param name="s"></param>
        /// <param name="beingReviewed"></param>
        /// <param name="parameters"></param>
        /// <param name="forTeam"></param>
        /// <param name="accessableUsers"></param>
        /// <returns></returns>
        public static CoworkerRelationships GetUsersThatReviewUser(
            UserOrganizationModel caller, PermissionsUtility perms,
            DataInteraction s, UserOrganizationModel beingReviewed,
            ReviewParameters parameters, OrganizationTeamModel forTeam,
            List<UserOrganizationModel> accessableUsers)
        {


            CoworkerRelationships coworkerRelationship = new CoworkerRelationships(beingReviewed);


            var responsibilityGroups = ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, beingReviewed.Id);

            if (parameters.ReviewSelf){
                coworkerRelationship.Add(beingReviewed, AboutType.Self);
            }
            if (parameters.ReviewTeammates) // Team members 
            {
                List<OrganizationTeamModel> teams;

                if (forTeam.Type != TeamType.Standard)
                    teams = responsibilityGroups.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>().Where(x => x.InterReview).ToList();
                else
                    teams = forTeam.AsList();

                foreach (var team in teams)
                {
                    var teamMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, caller, team.Id).Where(x => x.User.Id != beingReviewed.Id && accessableUsers.Any(y => y.Id == x.UserId)).ToListAlive();
                    foreach (var teammember in teamMembers)
                        coworkerRelationship.Add(teammember.User, AboutType.Teammate);
                }
            }
            if (parameters.ReviewPeers)// Peers
            {
                if (forTeam.Type != TeamType.Standard)
                {
                    List<UserOrganizationModel> peers = UserAccessor.GetPeers(s.GetQueryProvider(), perms, caller, beingReviewed.Id).Where(x => accessableUsers.Any(y => y.Id == x.Id)).ToListAlive();
                    foreach (var peer in peers)
                        coworkerRelationship.Add(peer, AboutType.Peer);
                }
            }
            // Managers
            if (parameters.ReviewManagers)
            {
                List<UserOrganizationModel> managers = new List<UserOrganizationModel>();
                //also want to add the issuer of the review
                if (forTeam.Type != TeamType.Standard && forTeam.ManagedBy != beingReviewed.Id && accessableUsers.Any(y => y.Id == forTeam.ManagedBy))
                {
                    managers = UserAccessor.GetUserOrganization(s.GetQueryProvider(), perms, caller, forTeam.ManagedBy, false, false).AsList().ToListAlive();
                }
                managers.AddRange(UserAccessor.GetManagers(s.GetQueryProvider(), perms, caller, beingReviewed.Id).Where(x => accessableUsers.Any(y => y.Id == x.Id)).ToListAlive());

                foreach (var manager in managers)
                {
                    coworkerRelationship.Add(manager, AboutType.Manager);
                }
            }
            // Subordinates
            if (parameters.ReviewSubordinates)
            {
                if (forTeam.Type != TeamType.Standard)
                {
                    List<UserOrganizationModel> subordinates = UserAccessor.GetSubordinates(s.GetQueryProvider(), perms, caller, beingReviewed.Id)
                                                              .Where(x => accessableUsers.Any(y => y.Id == x.Id))
                                                              .Where(x => x.Id != beingReviewed.Id)
                                                              .ToListAlive();
                    foreach (var subordinate in subordinates)
                    {
                        coworkerRelationship.Add(subordinate, AboutType.Subordinate);
                    }
                }
            }

            return coworkerRelationship;
        }
        private IEnumerableQuery GetReviewQueryProvider(ISession s, long orgId, long? reviewContainerId = null)
        {
            var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
            var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
            var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
            var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
            var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).List();
            var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
            var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();

            var queryProvider = new IEnumerableQuery(true);
            queryProvider.AddData(allOrgTeams);
            queryProvider.AddData(allTeamDurations);
            queryProvider.AddData(allMembers);
            queryProvider.AddData(allManagerSubordinates);
            queryProvider.AddData(allPositions);
            queryProvider.AddData(applicationQuestions);
            queryProvider.AddData(application);
            if (reviewContainerId != null)
            {
                var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId.Value).List();
                queryProvider.AddData(reviews);
            }

            return queryProvider;
        }

    }
}