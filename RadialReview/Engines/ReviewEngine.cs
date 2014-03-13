using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Engines
{
    public class ReviewEngine : BaseEngine
    {

        public List<ReviewsModel> Filter(List<UserOrganizationModel> subordinates, List<ReviewsModel> allReviews)
        {
            return allReviews.Where(x => x.Reviews.Any(y => subordinates.Any(z => z.Id == y.ForUserId))).ToList();
        }

        public CustomizeModel GetCustomizeModel(UserOrganizationModel caller, long teamId)
        {
            var parameters = new ReviewParameters()
            {
                ReviewManagers = true,
                ReviewPeers = true,
                ReviewSelf = true,
                ReviewSubordinates = true,
                ReviewTeammates = true
            };
            var reviewWho = _ReviewAccessor.GetUsersWhoReviewUsers(caller, parameters, teamId);
            var teamMembers = _TeamAccessor.GetTeamMembers(caller, teamId);
            var team = _TeamAccessor.GetTeam(caller, teamId);
            var reviewWhoRefined = reviewWho.SelectMany(x => x.ToList().SelectMany(y => y.Value.Select(z => new { First = x.User, Second = y.Key, Relationship = z }))).ToList();

            var selectors = new List<CustomizeSelector>();

            //Managers
            var managers = new CustomizeSelector()
            {
                Name = "Managers",
                UniqueId = "Managers",
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
                Name = "Subordinates",
                UniqueId = "Subordinates",
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
            //Default
            var Default = new CustomizeSelector()
            {
                Name = "Default",
                UniqueId = "Default",
                Pairs = managers.Pairs.Union(subordinates.Pairs).Union(peers.Pairs).Union(teams.Pairs).Union(self.Pairs).ToList()
            };

            

            var combine=new List<CustomizeSelector>() {  Default,all, self, managers, subordinates, peers, teams };

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
                AllUsers = new List<UserOrganizationModel>(),
                Selectors = combine,
                Selected=new List<Tuple<long,long>>()
            };

            return model;
        }


    }
}