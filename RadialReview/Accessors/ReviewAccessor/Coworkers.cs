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
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;

namespace RadialReview.Accessors {
    public partial class ReviewAccessor : BaseAccessor {
        public class Relationships {
            public class FromAccountabilityChart {
                public static List<AngularAccountabilityNode> GetRevieweeNode(AngularAccountabilityChart tree, Reviewee reviewee) {
                    if (reviewee.ACNodeId == null)
                        return AngularTreeUtil.FindUsersNodes(tree.Root, reviewee.RGMId);
                    else
                        return (AngularTreeUtil.FindNode(tree.Root, reviewee.ACNodeId.Value) ?? new AngularAccountabilityNode()).AsList();
                }
                public static RelationshipCollection GetRelationshipsForNode(AngularAccountabilityChart tree, Reviewee reviewee) {
                    var revieweesNodes = GetRevieweeNode(tree, reviewee);

                    var relationships = new List<AccRelationship>();
                    if (reviewee.Type == OriginType.Organization) {
                        var userNodes = AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.User != null).ToList();
                        return new RelationshipCollection(userNodes.Select(x => new AccRelationship() {
                            Reviewer = new Reviewer(x),
                            ReviewerIsThe = AboutType.Subordinate,
                            Reviewee = reviewee
                        }).ToList());
                    } else {
                        //Not an organization
                        if (!revieweesNodes.Any()) {
                            //Fallback for no acc chart node.
                            return new RelationshipCollection(new[] { new AccRelationship() {
                                    Reviewer = reviewee.ConvertToReviewer(),
                                    ReviewerIsThe = AboutType.Self,
                                    Reviewee = reviewee
                            } });
                        } else {
                            foreach (var revieweeNode in revieweesNodes) {
                                var directReports = revieweeNode.GetDirectChildren()
                                    .Where(x => x.User != null)
                                    .Select(dr => new AccRelationship() {
                                        Reviewer = new Reviewer(dr),
                                        ReviewerIsThe = AboutType.Subordinate,
                                        Reviewee = new Reviewee(revieweeNode)
                                    });
                                var managerNode = AngularTreeUtil.GetDirectParent(tree.Root, revieweeNode.Id);
                                var managers = new List<AccRelationship>();
                                if (managerNode != null && managerNode.User != null) {
                                    managers = new AccRelationship() {
                                        Reviewer = new Reviewer(managerNode),
                                        ReviewerIsThe = AboutType.Manager,
                                        Reviewee = new Reviewee(revieweeNode)
                                    }.AsList();
                                }
                                var peers = AngularTreeUtil.GetDirectPeers(tree.Root, revieweeNode.Id)
                                    .Where(x => x.User != null)
                                    .Select(peer => new AccRelationship() {
                                        Reviewer = new Reviewer(peer),
                                        ReviewerIsThe = AboutType.Peer,
                                        Reviewee = new Reviewee(revieweeNode)
                                    });
                                var self = new AccRelationship() {
                                    Reviewer = new Reviewer(revieweeNode),
                                    ReviewerIsThe = AboutType.Self,
                                    Reviewee = new Reviewee(revieweeNode)
                                }.AsList();
                                relationships.AddRange(directReports);
                                relationships.AddRange(managers);
                                relationships.AddRange(peers);
                                relationships.AddRange(self);
                            }
                            return new RelationshipCollection(relationships);
                        }
                    }
                }
                public static RelationshipCollection GetRelationshipsForNode(AngularAccountabilityChart tree, Reviewer reviewer) {
                    var relationships = new List<AccRelationship>();
                    var reviewersNodes = AngularTreeUtil.FindUsersNodes(tree.Root, reviewer.RGMId);
                    foreach (var reviewerNode in reviewersNodes) {
                        var directReports = reviewerNode.GetDirectChildren()
                                    .Where(x => x.User != null)
                                    .Select(dr => new AccRelationship() {
                                        Reviewer = new Reviewer(reviewerNode),
                                        RevieweeIsThe = AboutType.Subordinate,
                                        Reviewee = new Reviewee(dr)
                                    });

                        var managerNode = AngularTreeUtil.GetDirectParent(tree.Root, reviewerNode.Id).NotNull(x => x.AsList()) ?? new List<AngularAccountabilityNode>();
                        var managers = managerNode.Select(manager => new AccRelationship() {
                            Reviewer = new Reviewer(reviewerNode),
                            RevieweeIsThe = AboutType.Manager,
                            Reviewee = new Reviewee(manager)
                        });

                        var peers = AngularTreeUtil.GetDirectPeers(tree.Root, reviewerNode.Id).Select(peer => new AccRelationship() {
                            Reviewer = new Reviewer(reviewerNode),
                            RevieweeIsThe = AboutType.Peer,
                            Reviewee = new Reviewee(peer)
                        });

                        var self = new AccRelationship() {
                            Reviewer = new Reviewer(reviewerNode),
                            RevieweeIsThe = AboutType.Self,
                            Reviewee = new Reviewee(reviewerNode)
                        }.AsList();

                        relationships.AddRange(directReports);
                        relationships.AddRange(managers);
                        relationships.AddRange(peers);
                        relationships.AddRange(self);
                    }

                    return new RelationshipCollection(relationships);
                }


                public static List<Reviewee> FilterTreeByTeam(AngularAccountabilityChart tree, OrganizationTeamModel team, List<UserOrganizationModel> members) {

                    if (team.Type == TeamType.AllMembers)
                        return members.Select(x => new Reviewee(x)).ToList();
                    if (team.Type == TeamType.Managers)
                        return AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.User != null && x.GetDirectChildren().Any()).Select(x => new Reviewee(x)).ToList();
                    if (team.Type == TeamType.Standard)
                        return AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.User != null && members.Any(y => y.Id == x.User.Id)).Select(x => new Reviewee(x)).ToList();
                    if (team.Type == TeamType.Subordinates)
                        return AngularTreeUtil.FindUsersNodes(tree.Root, team.ManagedBy).SelectMany(x => x.GetDirectChildren().Union(x.AsList())).Where(x => x.User != null).Select(x => new Reviewee(x)).ToList();
                    throw new ArgumentOutOfRangeException("Unrecognized team " + team.Type);

                }
            }

            public class Filters {
                protected static IEnumerable<AccRelationship> Step1_RemoveNonUsers(IEnumerable<AccRelationship> existing) {
                    return existing.Where(x => x.Reviewee != null && x.Reviewer != null);
                }
                protected static IEnumerable<AccRelationship> Step2_RemoveInaccessableUsers(IEnumerable<AccRelationship> existing, List<Reviewee> accessibleUsers) {
                    if (accessibleUsers == null)
                        return existing;

                    var anyAcNode = accessibleUsers.Where(y => y.ACNodeId == null).Select(y => y.RGMId).ToList();
                    return existing.Where(x =>
                        (anyAcNode.Contains(x.Reviewee.RGMId) || accessibleUsers.Any(y => y == x.Reviewee)) &&
                        (anyAcNode.Contains(x.Reviewer.RGMId) || accessibleUsers.Any(y => y.RGMId == x.Reviewer.RGMId))
                    );
                }
                protected static IEnumerable<AccRelationship> Step3_FilterAgainstParameters(IEnumerable<AccRelationship> existing, ReviewParameters parameters) {
                    if (parameters == null)
                        return existing;

                    var output = new List<AccRelationship>();

                    foreach (var e in existing) {
                        if (parameters.ReviewSelf == false && e.RevieweeIsThe == AboutType.Self)
                            continue;
                        if (parameters.ReviewManagers == false && e.RevieweeIsThe == AboutType.Manager)
                            continue;
                        if (parameters.ReviewSubordinates == false && e.RevieweeIsThe == AboutType.Subordinate)
                            continue;
                        if (parameters.ReviewPeers == false && e.RevieweeIsThe == AboutType.Peer)
                            continue;
                        if (parameters.ReviewTeammates == false && e.RevieweeIsThe == AboutType.Teammate)
                            continue;

                        output.Add(e);
                    }
                    return output;

                }

                public static RelationshipCollection Apply(RelationshipCollection existing1, List<Reviewee> accessibleUsers, ReviewParameters parameters) {
                    var existing = existing1.ToList();
                    existing = Step1_RemoveNonUsers(existing).ToList();
                    existing = Step2_RemoveInaccessableUsers(existing, accessibleUsers).ToList();
                    existing = Step3_FilterAgainstParameters(existing, parameters).ToList();

                    return new RelationshipCollection(existing);
                }
            }

            public static List<Reviewee> GetAvailableUsers(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, OrganizationTeamModel forTeam, DateRange range, long? reviewContainerId = null) {
                var allMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, forTeam.Id, true).FilterRange(range).Select(x => x.User).ToList();
                var availableUsers = FromAccountabilityChart.FilterTreeByTeam(tree, forTeam, allMembers).ToList();

                if (reviewContainerId != null) {
                    //Add people not part of team but are part of review.
                    var allExtraUsers = s.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainerId).Select(x => x.ReviewerUser).ToList();
                    allMembers.AddRange(allExtraUsers);
                    availableUsers.AddRange(allExtraUsers.Select(x => new Reviewee(x)));
                }

                availableUsers.Add(new Reviewee(forTeam.Organization.Id, null) { Type = OriginType.Organization });
                return availableUsers;
            }

            protected static RelationshipCollection GetAdditionalRelationshipsForTeam(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewee reviewee, OrganizationTeamModel forTeam, DateRange range, bool includeSelf) {
                var output = new RelationshipCollection();
                var revieweeNode = FromAccountabilityChart.GetRevieweeNode(tree, reviewee).FirstOrDefault();
                if (revieweeNode == null)
                    return output; //Not on the tree
                var teamMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, forTeam.Id, true).FilterRange(range);
                if (!teamMembers.Any(x => x.UserId == reviewee.RGMId))
                    return output; // Not on the team
                foreach (var member in teamMembers) {
                    //Team-members can have multiple nodes.
                    //foreach (var memberNode in AngularTreeUtil.FindUsersNodes(tree.Root, member.UserId)) {
                    if (!includeSelf && member.UserId == reviewee.RGMId)
                        continue;//Skip if self
                    var type = AboutType.Teammate;
                    if (includeSelf && member.UserId == reviewee.RGMId) {
                        type = AboutType.Self;
                    }
                    output.AddRelationship(new Reviewer(member.UserId), new Reviewee(revieweeNode), type);
                    //}
                }
                return output;
            }

            public static RelationshipCollection GetRelationships_Unfiltered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewee reviewee, OrganizationTeamModel forTeam, DateRange range, ReviewParameters parameters) {
                parameters = parameters ?? ReviewParameters.AllTrue();
                //HEY DON'T DO ANY FILTERING IN THIS METHOD. WE WANT INVERSES TO WORK (see GetAllRelationships_Filtered)
                var output = new RelationshipCollection();

                if (forTeam.Type == TeamType.Standard) {
                    //We're just reviewing this team
                    var newParams = ReviewParameters.AllFalse();
                    newParams.ReviewTeammates = parameters.ReviewTeammates;
                    newParams.ReviewSelf = parameters.ReviewSelf;
                    parameters = newParams;
                    output = GetAdditionalRelationshipsForTeam(s, perms, tree, reviewee, forTeam, range, true);
                } else {
                    //Add Interreviewing teams
                    if (parameters.ReviewTeammates) {
                        var interReviewingTeams = ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, reviewee.RGMId)
                            .Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>()
                            .Where(x => x.InterReview).ToList();
                        foreach (var interReviewingTeam in interReviewingTeams) {
                            output.AddRange(GetAdditionalRelationshipsForTeam(s, perms, tree, reviewee, interReviewingTeam, range, false));
                        }
                    }
                    output.AddRange(FromAccountabilityChart.GetRelationshipsForNode(tree, reviewee));
                }




                //var revieweeNode = output.FirstOrDefault(x => x.RevieweeIsThe == AboutType.Self);
                //if (revieweeNode != null) {
                //	output.AddRelationship(revieweeNode.Reviewer, new Reviewee(forTeam.Organization.Id, null), AboutType.Organization);
                //}

                return output;
            }

            public static RelationshipCollection GetRelationships_Filtered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewer reviewer, OrganizationTeamModel forTeam, DateRange range, List<Reviewee> accessibleUsers = null, ReviewParameters parameters = null) {
                var pretendReviewee = reviewer.ConvertToReviewee();
                var output = GetRelationships_Unfiltered(s, perms, tree, pretendReviewee, forTeam, range, parameters);
                foreach (var o in output)
                    o.Invert();

                var reviewerNode = output.FirstOrDefault(x => x.ReviewerIsThe == AboutType.Self);
                if (reviewerNode != null) {
                    output.AddRelationship(reviewerNode.Reviewer, new Reviewee(forTeam.Organization.Id, null), AboutType.Organization);
                }

                return Filters.Apply(output, accessibleUsers, parameters);
            }

            public static RelationshipCollection GetRelationships_Filtered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewee reviewee, OrganizationTeamModel forTeam, DateRange range, List<Reviewee> accessibleUsers = null, ReviewParameters parameters = null) {
                var output = GetRelationships_Unfiltered(s, perms, tree, reviewee, forTeam, range, parameters);
                return Filters.Apply(output, accessibleUsers, parameters);
            }

            public static RelationshipCollection GetAllRelationships_Filtered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, OrganizationTeamModel forTeam, DateRange range, long? reviewContainerId = null, ReviewParameters parameters = null) {
                var allMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, forTeam.Id, true).FilterRange(range).Select(x => x.User).ToList();
                //var allMemberIds = allMembers.Select(x => x.UserId).ToList();


                //var availableUsers = FromAccountabilityChart.FilterTreeByTeam(tree, forTeam, allMembers).Select(x=>new Reviewee(x)).ToList();



                //if (reviewContainerId != null) {
                //	//Add people not part of team but are part of review.
                //	var allExtraUsers = s.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainerId).Select(x => x.ReviewerUser).ToList();
                //	allMembers.AddRange(allExtraUsers);
                //	availableUsers.AddRange(allExtraUsers.Select(x => new Reviewee(x)));
                //}
                allMembers = allMembers.Distinct(x => x.Id).ToList();
                //var accessibleUsers = allMembers.Select(x => x.Id).ToList();
                //availableUsers.Add(new Reviewee(forTeam.Organization.Id, null) { Type = OriginType.Organization});

                var collection = new RelationshipCollection();
                foreach (var member in allMembers) {
                    var memberId = member.Id;
                    var allNodesForUser = AngularTreeUtil.FindUsersNodes(tree.Root, memberId);
                    if (allNodesForUser.Any()) {
                        foreach (var userNode in allNodesForUser) {
                            var relationships = GetRelationships_Unfiltered(s, perms, tree, new Reviewee(userNode), forTeam, range, parameters);
                            collection.AddRange(relationships);
                        }
                    } else {
                        //not on the acc chart.
                        var relationships = GetRelationships_Unfiltered(s, perms, tree, new Reviewee(member), forTeam, range, parameters);
                        collection.AddRange(relationships);
                    }

                    collection.AddRelationship(new Reviewer(memberId), new Reviewee(forTeam.Organization.Id, null) { Type = OriginType.Organization }, AboutType.Organization);
                }

                var availableUsers = GetAvailableUsers(s, perms, tree, forTeam, range, reviewContainerId);

                return Filters.Apply(collection, availableUsers, parameters);
            }
        }

        public static bool ShouldAddToReview(Askable askable, AboutType relationshipToReviewee) {
            var a = (long)askable.OnlyAsk == long.MaxValue;
            var b = (relationshipToReviewee.Invert() & askable.OnlyAsk) != AboutType.NoRelationship;
            return a || b;
        }

        [Obsolete("Fix for AC")]
        private static AskableCollection GetAskables(DataInteraction dataInteraction, PermissionsUtility perms, Reviewer reviewer, IEnumerable<Reviewee> specifiedReviewees, DateRange range) {
            var allAskables = new AskableCollection();

            var q = dataInteraction.GetQueryProvider();

            foreach (var reviewee in specifiedReviewees) {
                var found = dataInteraction.Get<ResponsibilityGroupModel>(reviewee.RGMId);
                if (found == null || !found.AsList().FilterRangeRestricted(range).Any())
                    continue;

                var revieweeAskables = AskableAccessor.GetAskablesForUser(q, perms, reviewee, range);
                var reviewerIsThe = RelationshipAccessor.GetRelationshipsMerged(q, perms, reviewer, reviewee, range);
                allAskables.AddAll(revieweeAskables, reviewerIsThe, reviewee);
            }
            return allAskables;
        }

        [Obsolete("Fix for AC")]
        private static AskableCollection GetAskablesBidirectional(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree,
            Reviewee self, OrganizationTeamModel forTeam, ReviewParameters parameters, List<Reviewee> accessibleUsers, DateRange range, bool addMeToOthersReviews, ref AskableCollection addToOtherReviews) {

            var askableUtil = new AskableCollection();
            var reviewer = self.ConvertToReviewer();
            var whoDoIReview = Relationships.GetRelationships_Filtered(s, perms, tree, reviewer, forTeam, range, accessibleUsers, parameters);

            foreach (var imReviewing in whoDoIReview) {
                var theirQuestions = AskableAccessor.GetAskablesForUser(s.GetQueryProvider(), perms, imReviewing.Reviewee, range);
                askableUtil.AddAll(theirQuestions, imReviewing);
            }

            if (addMeToOthersReviews) {
                addToOtherReviews = addToOtherReviews ?? new AskableCollection();
                //var allMyNodes = self.AsList();
                //if (self.ACNodeId==null)
                //	allMyNodes = AngularTreeUtil.FindUsersNodes(tree.Root, reviewer.RGMId);
                //foreach (var myNode in allMyNodes) {
                var me = self;// new Reviewee(myNode);
                var questionsAboutMe = AskableAccessor.GetAskablesForUser(s.GetQueryProvider(), perms, me, range);
                var whoReviewsMe = Relationships.GetRelationships_Filtered(s, perms, tree, me, forTeam, range, accessibleUsers, parameters);
                foreach (var myReviewer in whoReviewsMe) {
                    askableUtil.AddAll(questionsAboutMe, myReviewer);
                    addToOtherReviews.AddAll(questionsAboutMe, myReviewer);
                }
                //}
            }

            return askableUtil;
        }

        public static RelationshipCollection GetAllRelationships(UserOrganizationModel caller, long forTeam, ReviewParameters parameters, DateRange range = null, long? reviewContainerId = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller).ViewTeam(forTeam);
                    var team = s.Get<OrganizationTeamModel>(forTeam);
					
					var org = team.Organization;
                    var orgId = org.Id;

                    //Order is important
                    var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).Where(range.Filter<OrganizationTeamModel>()).Future();
                    var allTeamDurations = s.QueryOver<TeamDurationModel>().Where(range.Filter<TeamDurationModel>()).JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).Future();
                    var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId && !x.IsClient).Where(range.Filter<UserOrganizationModel>()).Future();
                    var tree = AccountabilityAccessor.GetTree(s, perms, org.AccountabilityChartId, range: range);
                    var queryProvider = new IEnumerableQuery(true);

                    queryProvider.AddData(allOrgTeams);
                    queryProvider.AddData(allTeamDurations);
                    queryProvider.AddData(allMembers);
                    var d = new DataInteraction(queryProvider, s.ToUpdateProvider());

                    return Relationships.GetAllRelationships_Filtered(d, perms, tree, team, range, reviewContainerId: reviewContainerId, parameters: parameters);
                }
            }
        }



        //[Obsolete("Fix for AC")]
        //public List<TheReviewers_CoworkerRelationships> GetReviewersForUsers(UserOrganizationModel caller, ReviewParameters parameters, long forTeam) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			var perms = PermissionsUtility.Create(s, caller).ViewTeam(forTeam);
        //			var orgId = caller.Organization.Id;
        //			var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).List();
        //			var allTeamDurations = s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null).JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
        //			var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId && !x.IsClient && x.DeleteTime == null).List();
        //			var reviewWhoSettings = s.QueryOver<ReviewWhoSettingsModel>().Where(x => x.OrganizationId == orgId).List();
        //			var queryProvider = new IEnumerableQuery(true);
        //			queryProvider.AddData(allOrgTeams);
        //			queryProvider.AddData(allTeamDurations);
        //			queryProvider.AddData(allMembers);
        //			queryProvider.AddData(reviewWhoSettings);
        //			var d = new DataInteraction(queryProvider, s.ToUpdateProvider());
        //			var teamMemberIds = TeamAccessor.GetTeamMembers(queryProvider, perms, forTeam, false).Select(x => x.User.Id).ToList();
        //			var team = allOrgTeams.First(x => x.Id == forTeam);
        //			var reviewWhoDictionary = new Dictionary<long, HashSet<long>>();
        //			var items = new List<TheReviewees_CoworkerRelationships>();
        //			throw new Exception();
        //			foreach (var revieweeId in teamMemberIds) {
        //				var reviewee = new Reviewee(revieweeId);
        //				var usersTheyReview = ReviewAccessor.GetReviewersForUser(caller, perms, d, reviewee, parameters, team, teamMemberIds);
        //				reviewWhoDictionary[revieweeId] = new HashSet<long>(usersTheyReview.Select(x => x.Key.Id));
        //				items.Add(usersTheyReview);
        //			}
        //			foreach (var member in teamMemberIds) {
        //				var orgRelationships = CoworkerRelationships.Create(new Reviewee(team.Organization.Id, null));// new CoworkerRelationships(new Reviewer(member));
        //				orgRelationships.Add(team.Organization);
        //				items.Add(orgRelationships);
        //			}
        //			return items;
        //		}
        //	}
        //}
    }

}

//[Obsolete("Fix for AC")]
//public static TheReviewees_CoworkerRelationships GetReviewersForUser(
//	UserOrganizationModel caller, PermissionsUtility perms,
//	DataInteraction s, Reviewee reviewee,
//	ReviewParameters parameters, OrganizationTeamModel forTeam,
//	IEnumerable<long> accessableUsers) {
//	var coworkerRelationship = CoworkerRelationships.Create(reviewee);
//	var responsibilityGroups = ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, reviewee.RGMId);

//	if (parameters.ReviewSelf) {
//		coworkerRelationship.Add(new Reviewer(reviewee.RGMId), AboutType.Self);
//	}
//	if (parameters.ReviewTeammates) // Team members 
//	{
//		List<OrganizationTeamModel> teams;
//		if (forTeam.Type != TeamType.Standard)
//			teams = responsibilityGroups.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>().Where(x => x.InterReview).ToList();
//		else
//			teams = forTeam.AsList();
//		foreach (var team in teams) {
//			var teamMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, team.Id, false).Where(x => x.User.Id != reviewee.RGMId && accessableUsers.Any(id => id == x.UserId)).ToListAlive();
//			foreach (var teammember in teamMembers)
//				coworkerRelationship.Add(new Reviewer(teammember.User.Id), AboutType.Teammate);
//		}
//	}
//	if (parameters.ReviewPeers)// Peers
//	{
//		if (forTeam.Type != TeamType.Standard) {
//			var peers = UserAccessor.GetPeers(s.GetQueryProvider(), perms, caller, reviewee).Where(x => accessableUsers.Any(id => id == x.Id)).ToListAlive();
//			foreach (var peer in peers)
//				coworkerRelationship.Add(new Reviewer(peer.Id), AboutType.Peer);
//		}
//	}
//	// Managers
//	if (parameters.ReviewManagers) {
//		var managers = new List<UserOrganizationModel>();
//		//also want to add the issuer of the review
//		if (forTeam.Type != TeamType.Standard && forTeam.ManagedBy != reviewee.RGMId && accessableUsers.Any(id => id == forTeam.ManagedBy)) {
//			managers = UserAccessor.GetUserOrganization(s.GetQueryProvider(), perms, caller, forTeam.ManagedBy, false, false).AsList().ToListAlive();
//		}
//		managers.AddRange(UserAccessor.GetManagers(s.GetQueryProvider(), perms, caller, reviewee).Where(x => accessableUsers.Any(id => id == x.Id)).ToListAlive());
//		foreach (var manager in managers) {
//			coworkerRelationship.Add(new Reviewer(manager.Id), AboutType.Manager);
//		}
//	}
//	// Subordinates
//	if (parameters.ReviewSubordinates) {
//		if (forTeam.Type != TeamType.Standard) {
//			var subordinates = UserAccessor.GetDirectSubordinates(s.GetQueryProvider(), perms, reviewee)
//													  .Where(x => accessableUsers.Any(id => id == x.Id))
//													  .Where(x => x.Id != reviewee.RGMId)
//													  .ToListAlive();
//			foreach (var subordinate in subordinates) {
//				coworkerRelationship.Add(new Reviewer(subordinate.Id), AboutType.Subordinate);
//			}
//		}
//	}
//	return coworkerRelationship;
//}
