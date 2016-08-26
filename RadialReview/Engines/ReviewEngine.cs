using NHibernate.Engine;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Engines
{
    public class ReviewEngine : BaseEngine
    {

        public static string DEFAULT = "Default";

        public List<ReviewsModel> Filter(List<UserOrganizationModel> subordinates, List<ReviewsModel> allReviews)
        {
            return allReviews.Where(x => x.Reviews.Any(y => subordinates.Any(z => z.Id == y.ForUserId))).ToList();
        }

        public CustomizeModel GetCustomizeModel(UserOrganizationModel caller, long teamId, bool includeCopyFrom)
        {
            var parameters = new ReviewParameters()
            {
                ReviewManagers = true,
                ReviewPeers = true,
                ReviewSelf = true,
                ReviewSubordinates = true,
                ReviewTeammates = true
            };
            var reviewWho = _ReviewAccessor.GetReviewersForUsers(caller, parameters, teamId);
			var teamMembers = _TeamAccessor.GetTeamMembers(caller, teamId, false);
            var team = _TeamAccessor.GetTeam(caller, teamId);
            var reviewWhoRefined = reviewWho.SelectMany(x => x.ToList().SelectMany(y => y.Value.Select(z => new { First = x.Reviewer, Second = y.Key, Relationship = z }))).ToList();

            var selectors = new List<CustomizeSelector>();

            //Managers
            var managers = new CustomizeSelector()
            {
                Name = "Supervisors",
				UniqueId = "Supervisors",
                Pairs = reviewWhoRefined.Where(x => x.Relationship == AboutType.Manager).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
            };
            //Peers
            var peers = new CustomizeSelector()
            {
                Name = "Peers",
                UniqueId = "Peers",
                Pairs = reviewWhoRefined.Where(x => x.Relationship == AboutType.Peer).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
            };
            //Teams
            var teams = new CustomizeSelector()
            {
                Name = "Teams",
                UniqueId = "Teams",
                Pairs = reviewWhoRefined.Where(x => x.Relationship == AboutType.Teammate).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
            };
            //Subordinates
            var subordinates = new CustomizeSelector()
            {
                Name = "Direct Reports",
				UniqueId = "DirectReports",
                Pairs = reviewWhoRefined.Where(x => x.Relationship == AboutType.Subordinate).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
            };
            //All
            var all = new CustomizeSelector()
            {
                Name = "All",
                UniqueId = "All",
                Pairs = reviewWhoRefined.Distinct(x => Tuple.Create(x.First, x.Second)).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
			};
			//Self
			var self = new CustomizeSelector()
			{
				Name = "Self",
				UniqueId = "Self",
				Pairs = reviewWhoRefined.Distinct(x => Tuple.Create(x.First, x.Second)).Where(x => x.First == x.Second).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
			};
			//Self
			var company = new CustomizeSelector()
			{
				Name = "Organization",
				UniqueId = "Organization",
				Pairs = reviewWhoRefined.Where(x => x.Relationship == AboutType.Organization).Select(x => Tuple.Create(x.First.Id, x.Second.Id)).ToList()
			};

	    


	        //Default
            var Default = new CustomizeSelector()
            {
                Name = "Default",
                UniqueId = DEFAULT, //Don't change
				Pairs = managers.Pairs.Union(subordinates.Pairs).Union(peers.Pairs).Union(teams.Pairs).Union(self.Pairs).Union(company.Pairs).ToList()
            };

            var combine=new List<CustomizeSelector>() {  Default,all, self, managers, subordinates, peers, teams,company };

	        if (includeCopyFrom){
		        string reviewName = null;
		        var copy = _PrereviewAccessor.GetPreviousPrereviewForUser(caller, caller.Id, out reviewName);
		        if (reviewName != null){
			        //last review
			        var last = new CustomizeSelector(){
				        Name = "Copy from " + reviewName,
				        UniqueId = "CopyFrom",
				        Pairs = copy
			        };
			        combine.Add(last);
		        }
	        }

	        //selected team
            if(team.Type != TeamType.AllMembers)
            {
                var selectedTeamPairs = new List<Tuple<long, long>>();
                    foreach (var s in teamMembers)
                    {
                        foreach (var o in teamMembers)
                        {
                            selectedTeamPairs.Add(Tuple.Create(s.Id, o.Id));
                        }
                    }

                var selectedTeam = new CustomizeSelector()
                {
                    Name = "Selected Team",
                    UniqueId = "SelectedTeam",
                    Pairs = selectedTeamPairs
                };
                combine.Add(selectedTeam);
            }
            

            var model = new CustomizeModel()
            {
                Subordinates = teamMembers.Select(x => x.User).ToList(),
                AllReviewees = new List<ResponsibilityGroupModel>(),
                Selectors = combine,
                Selected=new List<Tuple<long,long>>(),
            };

            return model;
        }

        public async Task<int> CreateReviewFromPrereview(HttpContext context,NexusModel nexus)
        {
	        try{
		        return await Task.Run(async () =>{
			        var now = DateTime.UtcNow;
			        var admin = UserOrganizationModel.ADMIN;
			        var reviewContainerId = nexus.GetArgs()[0].ToLong();

			        //var prereview = _PrereviewAccessor.GetPrereview(admin, prereviewId);
			        var reviewContainer = _ReviewAccessor.GetReviewContainer(admin, reviewContainerId, false, false);
			        admin.Organization = new OrganizationModel(){Id = reviewContainer.ForOrganizationId};


			        var defaultCustomize = GetCustomizeModel(admin, reviewContainer.ForTeamId, true).Selectors.Where(x => x.UniqueId == DEFAULT).SelectMany(x => x.Pairs).ToList();

			        var whoReviewsWho = _PrereviewAccessor.GetAllMatchesForReview(admin, reviewContainerId, defaultCustomize);
			        var organization = _OrganizationAccessor.GetOrganization(admin, reviewContainer.ForOrganizationId);
			        var unsentEmail = new List<Mail>();



			        using (var s = HibernateSession.GetCurrentSession()){
				        using (var tx = s.BeginTransaction()){
							var datainteraction = ReviewAccessor.GetReviewDataInteraction(s,reviewContainer.ForOrganizationId);

					        var perm = PermissionsUtility.Create(s, admin);
							unsentEmail = _ReviewAccessor.CreateReviewFromPrereview(context, datainteraction, perm, admin, reviewContainer, organization.GetName(), whoReviewsWho);
					        _PrereviewAccessor.UnsafeExecuteAllPrereviews(s, reviewContainerId, now);
					       //Keep these:
					        tx.Commit();
					        s.Flush();
				        }
			        }

			        var result = await Emailer.SendEmails(unsentEmail);
			        return result.Sent;
		        });
	        }
	        catch (Exception){
		        throw;// new PermissionsException(e.Message);
	        }
        }
    }
}