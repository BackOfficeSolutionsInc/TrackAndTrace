using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class RelationshipAccessor
    {
        public static List<AboutType> GetRelationships(PermissionsUtility perms, AbstractQuery q, long userId, long otherId)
        {
            perms.ViewUserOrganization(userId,false);
            perms.ViewUserOrganization(otherId, false);

            var user = q.Get<UserOrganizationModel>(userId);
            var other = q.Get<UserOrganizationModel>(otherId);
            
            var output = new List<AboutType>();

            //Self
            if (userId == other.Id)
                output.Add(AboutType.Self);
            //Teammates
            var userTeams = q.Where<TeamDurationModel>(x => x.UserId == userId).ToListAlive();
            var otherTeams = q.Where<TeamDurationModel>(x => x.UserId == otherId).ToListAlive();
            var sharedTeams = userTeams.Intersect(otherTeams, new EqualityComparer<TeamDurationModel>((x, y) => x.TeamId == y.TeamId, x => x.TeamId.GetHashCode()));
            if (sharedTeams.Any())
                output.Add(AboutType.Teammate);
            //Peers
            var userManagers = q.Where<ManagerDuration>(x => x.SubordinateId == userId).ToListAlive();
            var otherManagers = q.Where<ManagerDuration>(x => x.SubordinateId == otherId).ToListAlive();
            var sharedManagers = userManagers.Intersect(otherManagers, new EqualityComparer<ManagerDuration>((x, y) => x.ManagerId == y.ManagerId, x => x.ManagerId.GetHashCode()));
            if (sharedManagers.Any())
                output.Add(AboutType.Peer);
            //Subordinates
            var userSubordinates = q.Where<ManagerDuration>(x => x.ManagerId == userId && x.SubordinateId == otherId).ToListAlive();
            if (userSubordinates.Any())
                output.Add(AboutType.Subordinate);
            //Manages
            var userManaging = q.Where<ManagerDuration>(x => x.SubordinateId == userId && x.ManagerId == otherId ).ToListAlive();
            if (userManaging.Any())
                output.Add(AboutType.Manager);
            //No relationship
            if (!output.Any())
                output.Add(AboutType.NoRelationship);

            var preferredOrder = new AboutType[] {
                AboutType.Self,
                AboutType.Manager,
                AboutType.Subordinate,
                AboutType.Peer,
                AboutType.Teammate,
                AboutType.NoRelationship,
            };

            var newOutput= preferredOrder.Where(x => output.Any(y => y == x)).ToList();
            return newOutput;

            //return output.OrderByDescending(x=>(int)x).ToList();
        }

    }
}