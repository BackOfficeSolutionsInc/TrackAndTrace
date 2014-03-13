using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class DeepSubordianteAccessor : BaseAccessor
    {

        public List<long> GetSubordinatesAndSelf(UserOrganizationModel caller, long userId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    return GetSubordinatesAndSelf(s, caller, userId);
                }
            }
        }

        public static List<long> GetSubordinatesAndSelf(ISession s, UserOrganizationModel caller, long userId)
        {
            if (caller.Id != userId)
            {
                var found = s.QueryOver<DeepSubordinateModel>().Where(x => x.DeleteTime == null && x.ManagerId == userId && x.SubordinateId==userId).SingleOrDefault();
                if (found == null)
                    throw new PermissionsException("You don't have access to this user");
            }
            var subordinates=s.QueryOver<DeepSubordinateModel>().Where(x => x.DeleteTime == null && x.ManagerId == userId).List().Select(x=>x.SubordinateId).ToList();
            return subordinates;
        }

        public List<DeepSubordinateModel> GetOrganizationMap(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);

                    return s.QueryOver<DeepSubordinateModel>().Where(x => x.OrganizationId == organizationId).List().ToList();

                }
            }

        }

        public static void Remove(ISession s, UserOrganizationModel manager, UserOrganizationModel subordinate, DateTime now)
        {
            //Grab all subordinates' deep subordinates
            var allSuperiors = SubordinateUtility.GetSuperiors(manager, false);
            allSuperiors.Add(manager);
            var allSubordinates = SubordinateUtility.GetSubordinates(subordinate, false);
            allSubordinates.Add(subordinate);

            foreach (var SUP in allSuperiors)
            {
                var managerSubordinates = s.QueryOver<DeepSubordinateModel>().Where(x => x.ManagerId == SUP.Id).List().ToListAlive();

                foreach (var sub in allSubordinates)
                {
                    var found = managerSubordinates.FirstOrDefault(x => x.SubordinateId == sub.Id);
                    if (found == null)
                    {
                        log.Error("Manager link doesn't exist for orgId=(" + manager.Organization.Id + "). Advise that you run deep subordinate creation.");
                    }
                    else
                    {
                        found.Links -= 1;
                        if (found.Links == 0)
                            found.DeleteTime = now;
                        if (found.Links < 0)
                            throw new Exception("This shouldn't happen.");
                        s.Update(found);
                    }
                }
            }
        }

        public static void Add(ISession s, UserOrganizationModel manager, UserOrganizationModel subordinate,long organizationId,DateTime now)
        {
            //Get **users** subordinates, make them deep subordinates of manager
            var allSubordinates = SubordinateUtility.GetSubordinates(subordinate, false);
            allSubordinates.Add(subordinate);
            var allSuperiors = SubordinateUtility.GetSuperiors(manager, false);
            allSuperiors.Add(manager);

            //for manager and each of his superiors
            foreach (var SUP in allSuperiors)
            {
                var managerSubordinates = s.QueryOver<DeepSubordinateModel>().Where(x => x.ManagerId == SUP.Id).List().ToListAlive();

                foreach (var sub in allSubordinates)
                {
                    if (sub.Id == SUP.Id)
                        throw new PermissionsException("A circular dependency was found. " + manager.GetName() + " cannot manage " + subordinate.GetName() + " because " + manager.GetName() + " is " + subordinate.GetName() + "'s subordinate.");
                    
                    var found = managerSubordinates.FirstOrDefault(x => x.SubordinateId == sub.Id);
                    if (found == null)
                    {
                        found = new DeepSubordinateModel()
                        {
                            CreateTime = now,
                            ManagerId = SUP.Id,
                            SubordinateId = sub.Id,
                            Links = 0,
                            OrganizationId=organizationId
                        };
                    }
                    found.Links += 1;
                    s.SaveOrUpdate(found);
                }
            }
        }

    }
}