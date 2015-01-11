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

        public static List<UserOrganizationModel> GetSubordinates(ISession s, UserOrganizationModel manager,bool populateParents,int levels=int.MaxValue)
        {
            var alreadyHit = new List<long>();
            return Children(s,manager, new List<String> { "" + manager.Id }, populateParents,levels,0,alreadyHit);
        }
		public static List<UserOrganizationModel> GetSuperiors(ISession s, UserOrganizationModel manager, bool populateParents, int levels = int.MaxValue)
        {
            var alreadyHit = new List<long>();
            return Parents(s,manager, new List<String> { "" + manager.Id }, populateParents, levels, 0, alreadyHit);
        }

        private static List<UserOrganizationModel> Children(ISession s,UserOrganizationModel parent, List<String> parents, bool populateParents,int levels,int level,List<long> alreadyHit)
        {
            var children = new List<UserOrganizationModel>();
            if (levels <= 0)
                return children;
            levels = levels - 1;

	        parent = s.Get<UserOrganizationModel>(parent.Id);

			if (parent.ManagingUsers==null)
				throw new Exception("Shouldnt get here.");


            if (/*parent.ManagingUsers == null ||*/ parent.ManagingUsers.Count == 0)
                return children;
            foreach (var c in parent.ManagingUsers.ToListAlive().Select(x => x.Subordinate))
            {
                if (!alreadyHit.Contains(c.Id))
                {
                    if (populateParents)
                        c.Properties["parents"] = parents;
                    alreadyHit.Add(c.Id);
                    children.Add(c);
                    var copy = parents.Select(x => x).ToList();
                    copy.Add("" + c.Id);
                    children.AddRange(Children(s, c, copy, populateParents, levels, level + 1, alreadyHit));
                }
            }
            children.ForEach(x => x.SetLevel(level));
            return children;
        }

		private static List<UserOrganizationModel> Parents(ISession s, UserOrganizationModel child, List<String> children, bool populateChildren, int levels, int level, List<long> alreadyHit)
        {
            var parents = new List<UserOrganizationModel>();
            if (levels <= 0)
                return parents;
			levels = levels - 1;
			child = s.Get<UserOrganizationModel>(child.Id);

			if (child.ManagedBy == null)
				throw new Exception("Shouldnt get here.");

			if (/*child.ManagedBy == null ||*/ child.ManagedBy.Count == 0)
                return parents;
            foreach (var c in child.ManagedBy.ToListAlive().Select(x => x.Manager))
            {
                if (!alreadyHit.Contains(c.Id))
                {
                    if (populateChildren)
                        c.Properties["children"] = children;
                    alreadyHit.Add(c.Id);
                    parents.Add(c);
                    var copy = children.Select(x => x).ToList();
                    copy.Add("" + c.Id);
                    parents.AddRange(Parents(s,c, copy, populateChildren, levels, level + 1, alreadyHit));
                }
            }
            parents.ForEach(x => x.SetLevel(level));
            return parents;
        }

    }
}