using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Responsibilities;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class ResponsibilitiesAccessor
    {
        public ResponsibilityModel GetResponsibility(UserOrganizationModel caller, long responsibilityId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var responsibility = s.Get<ResponsibilityModel>(responsibilityId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(responsibility.ForOrganizationId);
                    return responsibility;
                }
            }
        }

        public List<ResponsibilityModel> GetResponsibilities(UserOrganizationModel caller,long responsibilityGroupId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var responsibilities=s.QueryOver<ResponsibilityModel>().Where(x=>x.ForResponsibilityGroup==responsibilityGroupId).List().ToList();
                    
                    var orgs=responsibilities.Select(x=>x.ForOrganizationId).Distinct().ToList();
                    var permissions=PermissionsUtility.Create(s,caller);
                    foreach(var oId in orgs){
                        permissions.ViewOrganization(oId);
                    }
                    return responsibilities;
                }
            }
        }

        public void AddRespnsibility(UserOrganizationModel caller, long responsibilityGroupId, ResponsibilityModel responsibility)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (responsibility.Id != null)
                        throw new PermissionsException();

                    var responsibilityGroup = s.Get<ResponsibilityGroupModel>(responsibilityGroupId);
                    PermissionsUtility.Create(s, caller).EditOrganization(responsibilityGroup.Organization.Id);

                    responsibilityGroup.Responsibilities.Add(responsibility);

                    s.Update(responsibilityGroup);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void RemoveResponsibility(UserOrganizationModel caller, long responsibilityId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var responsibility = s.Get<ResponsibilityModel>(responsibilityId);
                    PermissionsUtility.Create(s, caller).EditOrganization(responsibility.ForOrganizationId);
                    responsibility.DeleteTime = DateTime.UtcNow;
                    s.Update(responsibility);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

    }
}