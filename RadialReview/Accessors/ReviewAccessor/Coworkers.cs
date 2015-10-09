using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {

		public static bool ShouldAddToReview(Askable askable, AboutType relationshipToReviewee)
		{
			var a = (long) askable.OnlyAsk == long.MaxValue;
			var b = (relationshipToReviewee.Invert() & askable.OnlyAsk) != AboutType.NoRelationship;
			return a || b;
		}


		private static List<AskableAbout> GetAskables(UserOrganizationModel caller,PermissionsUtility perms, DataInteraction dataInteraction,IEnumerable<long> revieweeIds,long reviewerId,long? periodId,DateRange range)
		{
			var allAskables = new List<AskableAbout>();
			var queryProvider = dataInteraction.GetQueryProvider();

			//var applicationQuestions = ApplicationAccessor.GetApplicationQuestions(queryProvider).ToList();
			
			foreach (var revieweeId in revieweeIds){
				var found = dataInteraction.Get<ResponsibilityGroupModel>(revieweeId);
				if (found == null || found.DeleteTime != null)
					continue;

				var revieweeAskables = AskableAccessor.GetAskablesForUser(caller, queryProvider, perms, revieweeId, periodId, range);
				var relationships = RelationshipAccessor.GetRelationships(perms, queryProvider, reviewerId, revieweeId);

				//Merge relationships
				var relationshipToReviewee = relationships.Aggregate(AboutType.NoRelationship, (o, n) => (o|n));

				foreach (var askable in revieweeAskables){
					//Filter only where OnlyAsk is satisfied
					if (ShouldAddToReview(askable,relationshipToReviewee))
					{
						var askableAbout = new AskableAbout(){
							AboutType = relationshipToReviewee,
							AboutUserId = revieweeId,
							Askable = askable,
						};
						allAskables.Add(askableAbout);
					}
					else{
						int a = 0;
						a++;
					}
				}
				//allAskables.AddRange(revieweeAskables.Select(x => new AskableAbout() { AboutType = bestRelationship, AboutUserId = revieweeId, Askable = x }));
				//allAskables.AddRange(applicationQuestions.Select(aq => new AskableAbout() { AboutType = bestRelationship, AboutUserId = revieweeId, Askable = aq }));
			}
			return allAskables;
		}

		/// <summary>
		/// Requires:
		///     <br/>
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="perms"></param>
		/// <param name="s"></param>
		/// <param name="reviewee"></param>
		/// <param name="parameters"></param>
		/// <param name="forTeam"></param>
		/// <param name="accessableUsers"></param>
		/// <returns></returns>
		//public static CoworkerRelationships GetUsersThatReviewUser(
		public static CoworkerRelationships GetReviewersForUser(
			UserOrganizationModel caller, PermissionsUtility perms,
			DataInteraction s, UserOrganizationModel reviewee,
			ReviewParameters parameters, OrganizationTeamModel forTeam,
			IEnumerable<long> accessableUsers) {


			var coworkerRelationship = new CoworkerRelationships(reviewee);


			var responsibilityGroups = ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, reviewee.Id);

			if (parameters.ReviewSelf) {
				coworkerRelationship.Add(reviewee, AboutType.Self);
			}
			if (parameters.ReviewTeammates) // Team members 
            {
				List<OrganizationTeamModel> teams;

				if (forTeam.Type != TeamType.Standard)
					teams = responsibilityGroups.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>().Where(x => x.InterReview).ToList();
				else
					teams = forTeam.AsList();

				foreach (var team in teams) {
					var teamMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, team.Id,false).Where(x => x.User.Id != reviewee.Id && accessableUsers.Any(id => id == x.UserId)).ToListAlive();
					foreach (var teammember in teamMembers)
						coworkerRelationship.Add(teammember.User, AboutType.Teammate);
				}
			}
			if (parameters.ReviewPeers)// Peers
            {
				if (forTeam.Type != TeamType.Standard) {
					var peers = UserAccessor.GetPeers(s.GetQueryProvider(), perms, caller, reviewee.Id).Where(x => accessableUsers.Any(id => id == x.Id)).ToListAlive();
					foreach (var peer in peers)
						coworkerRelationship.Add(peer, AboutType.Peer);
				}
			}
			// Managers
			if (parameters.ReviewManagers) {
				var managers = new List<UserOrganizationModel>();
				//also want to add the issuer of the review
				if (forTeam.Type != TeamType.Standard && forTeam.ManagedBy != reviewee.Id && accessableUsers.Any(id =>id == forTeam.ManagedBy)) {
					managers = UserAccessor.GetUserOrganization(s.GetQueryProvider(), perms, caller, forTeam.ManagedBy, false, false).AsList().ToListAlive();
				}
				managers.AddRange(UserAccessor.GetManagers(s.GetQueryProvider(), perms, caller, reviewee.Id).Where(x => accessableUsers.Any(id => id == x.Id)).ToListAlive());

				foreach (var manager in managers) {
					coworkerRelationship.Add(manager, AboutType.Manager);
				}
			}
			// Subordinates
			if (parameters.ReviewSubordinates) {
				if (forTeam.Type != TeamType.Standard) {
					var subordinates = UserAccessor.GetDirectSubordinates(s.GetQueryProvider(), perms, reviewee.Id)
															  .Where(x => accessableUsers.Any(id => id == x.Id))
															  .Where(x => x.Id != reviewee.Id)
															  .ToListAlive();
					foreach (var subordinate in subordinates) {
						coworkerRelationship.Add(subordinate, AboutType.Subordinate);
					}
				}
			}

			//coworkerRelationship.AddOrganization(forTeam.Organization);

			return coworkerRelationship;
		}
		public List<CoworkerRelationships> GetReviewersForUsers(
				UserOrganizationModel caller,
				ReviewParameters parameters,
				long forTeam) {
			/*var teams = _TeamAccessor.GetTeamsDirectlyManaged(caller, caller.Id).ToList();
			if (!teams.Any(x => x.Id == forTeam)){
				throw new PermissionsException("You do not have access to that team.");
			}*/
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

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

					var teamMemberIds = TeamAccessor.GetTeamMembers(queryProvider, perms, forTeam, false).Select(x => x.User.Id).ToList();

					var team = allOrgTeams.First(x => x.Id == forTeam);

					var reviewWhoDictionary = new Dictionary<long, HashSet<long>>();

					var items = new List<CoworkerRelationships>();

					foreach (var memberId in teamMemberIds) {
						var reviewing = queryProvider.Get<UserOrganizationModel>(memberId);
						var usersTheyReview = ReviewAccessor.GetReviewersForUser(caller, perms, d, reviewing, parameters, team, teamMemberIds);//.ToList();
						reviewWhoDictionary[memberId] = new HashSet<long>(usersTheyReview.Select(x => x.Key.Id));
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

					foreach (var member in teamMemberIds){
						var orgRelationships = new CoworkerRelationships(queryProvider.Get<UserOrganizationModel>(member));
						orgRelationships.Add(team.Organization);
						items.Add(orgRelationships);
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

		
		private static AskableUtility GetAskablesBidirectional(
		DataInteraction s, PermissionsUtility perms, UserOrganizationModel caller,
		UserOrganizationModel reviewee, OrganizationTeamModel team, ReviewParameters parameters,
		List<long> accessibleUsers, long? periodId,
		DateRange range) {
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

			//var appQuestions = ApplicationAccessor.GetApplicationQuestions(s.GetQueryProvider()).ToList();//, ApplicationAccessor.FEEDBACK);

			//Ensures uniqueness and removes people not in the review.
			var askableUtil = new AskableUtility();
			var reviewers= GetReviewersForUser(caller, perms, s, reviewee, parameters, team, accessibleUsers);
			var questions = AskableAccessor.GetAskablesForUser(caller, s.GetQueryProvider(), perms, reviewee.Id, periodId, range);

			if (parameters.ReviewSelf) {
				askableUtil.AddUnique(questions, AboutType.Self, reviewee.Id);
			}

			foreach (var reviewer in reviewers.Relationships) {
				var reviewerId =/* aboutSelf ? beingReviewed.Id :*/ reviewer.Key.Id;

				var reviewerAskables = AskableAccessor.GetAskablesForUser(caller, s.GetQueryProvider(), perms, reviewerId, periodId,range);
				/* .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, id)
				 .SelectMany(x => x.Responsibilities).ToListAlive();*/
				foreach (var relationship in reviewer.Value) {
					foreach (var reviewerAskable in reviewerAskables){
						if (ReviewAccessor.ShouldAddToReview(reviewerAskable,relationship)){// (long)reviewerAskable.OnlyAsk == long.MaxValue || (relationship.Invert() & reviewerAskable.OnlyAsk) != AboutType.NoRelationship)
							askableUtil.AddUnique(reviewerAskable, relationship, reviewerId);
						}
					}

					//foreach (var aq in appQuestions)
					//	askableUtil.AddUnique(aq, aboutType, revieweeId);
					askableUtil.AddUser(reviewer.Key, relationship);
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
			return askableUtil;
		}
	}

}