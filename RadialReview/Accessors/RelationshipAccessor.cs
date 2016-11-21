using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class RelationshipAccessor
    {
        public static List<AboutType> GetRelationships(AbstractQuery q, PermissionsUtility perms, Reviewer reviewer, Reviewee reviewee, DateRange range) {

            var user = q.Get<ResponsibilityGroupModel>(reviewer.RGMId);
			var other = q.Get<ResponsibilityGroupModel>(reviewee.RGMId);

            perms.ViewUserOrganization(reviewer.RGMId, false);
			if (other is UserOrganizationModel)
				perms.ViewUserOrganization(reviewee.RGMId, false);
			else if (other is OrganizationModel)
				perms.ViewOrganization(reviewee.RGMId);
			else 
				throw new PermissionsException("Unhandled. "+reviewee.RGMId);
            
            var output = new List<AboutType>();

            //Self
	        if (other.Organization.Id == other.Id){
				output.Add(AboutType.Organization);
	        }else if (reviewer.RGMId == other.Id){
		        output.Add(AboutType.Self);
	        }else{
		        //Teammates
		        var userTeams = q.Where<TeamDurationModel>(x => x.UserId == reviewer.RGMId).FilterRange(range);
				var otherTeams = q.Where<TeamDurationModel>(x => x.UserId == reviewee.RGMId).FilterRange(range);
				var sharedTeams = userTeams.Intersect(otherTeams, new EqualityComparer<TeamDurationModel>((x, y) => x.TeamId == y.TeamId, x => x.TeamId.GetHashCode()));
		        if (sharedTeams.Any())
			        output.Add(AboutType.Teammate);
		        //Peers
		        var userManagers = q.Where<ManagerDuration>(x => x.SubordinateId == reviewer.RGMId).FilterRange(range);
		        var otherManagers = q.Where<ManagerDuration>(x => x.SubordinateId == reviewee.RGMId).FilterRange(range);
		        var sharedManagers = userManagers.Intersect(otherManagers, new EqualityComparer<ManagerDuration>((x, y) => x.ManagerId == y.ManagerId, x => x.ManagerId.GetHashCode()));
		        if (sharedManagers.Any())
			        output.Add(AboutType.Peer);
		        //Subordinates
		        var userSubordinates = q.Where<ManagerDuration>(x => x.ManagerId == reviewer.RGMId && x.SubordinateId == reviewee.RGMId).FilterRange(range);
				if (userSubordinates.Any())
			        output.Add(AboutType.Subordinate);
		        //Manages
		        var userManaging = q.Where<ManagerDuration>(x => x.SubordinateId == reviewer.RGMId && x.ManagerId == reviewee.RGMId).FilterRange(range);
				if (userManaging.Any())
			        output.Add(AboutType.Manager);
	        }
	        //No relationship
            if (!output.Any())
                output.Add(AboutType.NoRelationship);

            var preferredOrder = new [] {
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


		public static AboutType GetRelationshipsMerged(AbstractQuery q, PermissionsUtility perms, Reviewer reviewer, Reviewee reviewee, DateRange range) {
			var relationships=GetRelationships(q, perms, reviewer, reviewee, range);
			return relationships.Aggregate(AboutType.NoRelationship, (o, n) => (o | n));
		}

	}
}