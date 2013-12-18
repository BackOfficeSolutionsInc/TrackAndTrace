using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class SubordinateUtility
    {

        public static List<UserOrganizationModel> GetSubordinates(UserOrganizationModel manager,bool populateParents)
        {
            return Children(manager, new List<String> { "" + manager.Id }, populateParents);
        }

        private static List<UserOrganizationModel> Children(UserOrganizationModel parent, List<String> parents, bool populateParents)
        {
            var children = new List<UserOrganizationModel>();
            if (parent.ManagingUsers == null || parent.ManagingUsers.Count == 0)
                return children;
            foreach (var c in parent.ManagingUsers.ToListAlive().Select(x => x.Subordinate))
            {
                if (populateParents)
                    c.Properties["parents"] = parents;
                children.Add(c);
                var copy = parents.Select(x => x).ToList();
                copy.Add("" + c.Id);
                children.AddRange(Children(c, copy, populateParents));
            }
            return children;
        }
    }
}