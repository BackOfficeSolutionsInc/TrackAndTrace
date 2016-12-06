using NHibernate.Engine;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static RadialReview.Models.Reviews.CustomizeModel;

namespace RadialReview.Engines {
	public class ReviewEngine : BaseEngine {

		public static string DEFAULT = "Default";

		public List<ReviewsModel> Filter(List<UserOrganizationModel> subordinates, List<ReviewsModel> allReviews) {
			return allReviews.Where(x => x.Reviews.Any(y => subordinates.Any(z => z.Id == y.ReviewerUserId))).ToList();
		}

		[Obsolete("Fix for AC")]
		public CustomizeModel GetCustomizeModel(UserOrganizationModel caller, long teamId, bool includeCopyFrom, DateRange dateRange = null) {			

			var parameters = new ReviewParameters() {
				ReviewManagers = true,
				ReviewPeers = true,
				ReviewSelf = true,
				ReviewSubordinates = true,
				ReviewTeammates = true
			};

			var relationships = ReviewAccessor.GetAllRelationships(caller, teamId, parameters);

			//var reviewWho = _ReviewAccessor.GetReviewersForUsers(caller, parameters, teamId);
			var teamMembers = TeamAccessor.GetTeamMembers(caller, teamId, false);
			var team = TeamAccessor.GetTeam(caller, teamId);
			//       var reviewWhoRefined = reviewWho.SelectMany(coworkers => coworkers.ToList()
			//.SelectMany(reviewer => reviewer.Value.Select(z =>
			//	new { First = coworkers.Reviewer, Second = reviewer.Key, Relationship = z }
			//))).ToList();

			var selectors = new List<CustomizeSelector>();

			//Managers
			var managers = new CustomizeSelector() {
				Name = "Supervisors",
				UniqueId = "Supervisors",
				Pairs = relationships.ToWhoReviewsWho(AboutType.Manager)//reviewWhoRefined.Where(x => x.Relationship == AboutType.Manager).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
			};
			//Peers
			var peers = new CustomizeSelector() {
				Name = "Peers",
				UniqueId = "Peers",
				Pairs = relationships.ToWhoReviewsWho(AboutType.Peer)
			};
			//Teams
			var teams = new CustomizeSelector() {
				Name = "Teams",
				UniqueId = "Teams",
				Pairs = relationships.ToWhoReviewsWho(AboutType.Teammate)
			};
			//Subordinates
			var subordinates = new CustomizeSelector() {
				Name = "Direct Reports",
				UniqueId = "DirectReports",
				Pairs = relationships.ToWhoReviewsWho(AboutType.Subordinate)
			};
			//All
			var all = new CustomizeSelector() {
				Name = "All",
				UniqueId = "All",
				Pairs = relationships.ToAllWhoReviewsWho()
			};
			//Self
			var self = new CustomizeSelector() {
				Name = "Self",
				UniqueId = "Self",
				Pairs = relationships.ToWhoReviewsWho(AboutType.Self)
			};
			//Self
			var company = new CustomizeSelector() {
				Name = "Organization",
				UniqueId = "Organization",
				Pairs = relationships.ToWhoReviewsWho(AboutType.Organization)
			};




			//Default
			var Default = new CustomizeSelector() {
				Name = "360 Degree Review",
				UniqueId = DEFAULT, //Don't change
				Pairs = managers.Pairs.Union(subordinates.Pairs).Union(peers.Pairs).Union(teams.Pairs).Union(self.Pairs).Union(company.Pairs).ToList()
			};

			var combine = new List<CustomizeSelector>() { Default, all, self, managers, subordinates, peers, teams, company };

			if (includeCopyFrom) {
				string reviewName = null;
				var copy = _PrereviewAccessor.GetPreviousPrereviewForUser(caller, caller.Id, out reviewName);
				if (reviewName != null) {
					//last review
					var last = new CustomizeSelector() {
						Name = "Copy from " + reviewName,
						UniqueId = "CopyFrom",
						Pairs = copy
					};
					combine.Add(last);
				}
			}

			//selected team
			if (team.Type != TeamType.AllMembers) {
				var selectedTeamPairs = new List<WhoReviewsWho>();
				foreach (var s in teamMembers) {
					foreach (var o in teamMembers) {
						selectedTeamPairs.Add(new WhoReviewsWho(new Reviewer(s.UserId),new Reviewee(o.UserId,null)));
					}
				}

				var selectedTeam = new CustomizeSelector() {
					Name = "Selected Team",
					UniqueId = "SelectedTeam",
					Pairs = selectedTeamPairs
				};
				combine.Add(selectedTeam);
			}

			teamMembers = teamMembers.OrderBy(x => x.User.GetName()).ToList();

			var allReviewees = ReviewAccessor.GetPossibleOrganizationReviewees(caller, team.Organization.Id, dateRange);
			allReviewees = allReviewees.OrderByDescending(x=>x.Type).ThenBy(x=> x._Name).ToList();

			var model = new CustomizeModel() {
				Reviewers = teamMembers.Select(x => new Reviewer(x.User)).ToList(),
				AllReviewees = allReviewees,//new List<Reviewee>(),
				Selectors = combine,
				Selected = new List<WhoReviewsWho>(),
				
			};
			var masterList = model.Reviewers.Select(x=>x.RGMId).ToList();


			var masterIndex = 0;

			foreach (var m in model.AllReviewees.Select(x => x.RGMId)) {
				var index = masterList.IndexOf(m);
				if (index != -1) {
					masterIndex = index;
				}
				masterList.Insert(masterIndex, m);
				masterIndex += 1;
			}

			masterList = masterList.Distinct(x => x).ToList();

			model.MasterList = masterList;
            var lookup = new DefaultDictionary<string, ReviewerRevieweeInfo>(x=>new ReviewerRevieweeInfo());

            foreach(var selector in model.Selectors) {
                foreach(var p in selector.Pairs) {
                    lookup[p.Reviewer.ToId() + "~" + p.Reviewee.ToId()].Classes += " is" + selector.UniqueId;
                }
            }

            foreach(var p in model.Selected) {
                lookup[p.Reviewer.ToId() + "~" + p.Reviewee.ToId()].Selected = true;
            }

            model.Lookup = lookup;

            return model;
		}

		[Obsolete("Fix for AC")]
		public async Task<int> CreateReviewFromPrereview(HttpContext context, NexusModel nexus) {
			try {
				return await Task.Run(async () => {
					var now = DateTime.UtcNow;
					var admin = UserOrganizationModel.ADMIN;
					var reviewContainerId = nexus.GetArgs()[0].ToLong();

					//var prereview = _PrereviewAccessor.GetPrereview(admin, prereviewId);
					var reviewContainer = _ReviewAccessor.GetReviewContainer(admin, reviewContainerId, false, false);
					admin.Organization = new OrganizationModel() { Id = reviewContainer.OrganizationId };


					var defaultCustomize = GetCustomizeModel(admin, reviewContainer.ForTeamId, true)
											.Selectors
											.Where(x => x.UniqueId == DEFAULT).SelectMany(x => x.Pairs).ToList();

					var whoReviewsWho = _PrereviewAccessor.GetAllMatchesForReview(admin, reviewContainerId, defaultCustomize);
					var organization = _OrganizationAccessor.GetOrganization(admin, reviewContainer.OrganizationId);
					var unsentEmail = new List<Mail>();

					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var datainteraction = ReviewAccessor.GetReviewDataInteraction(s, reviewContainer.OrganizationId);
							var perm = PermissionsUtility.Create(s, admin);
							unsentEmail = _ReviewAccessor.CreateReviewFromPrereview(context, datainteraction, perm, admin, reviewContainer, whoReviewsWho);
							_PrereviewAccessor.UnsafeExecuteAllPrereviews(s, reviewContainerId, now);
							//Keep these:
							tx.Commit();
							s.Flush();
						}
					}
					var result = await Emailer.SendEmails(unsentEmail);
					return result.Sent;
				});
			} catch (Exception) {
				throw;// new PermissionsException(e.Message);
			}
		}
	}
}