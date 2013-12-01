﻿using RadialReview.Exceptions;
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

        public ResponsibilityGroupModel GetResponsibilityGroup(UserOrganizationModel caller,long responsibilityGroupId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var resGroup=s.Get<ResponsibilityGroupModel>(responsibilityGroupId);
                    long orgId;

                    if (resGroup is OrganizationModel)  orgId = resGroup.Id;
                    else                                orgId = resGroup.Organization.Id;

                    PermissionsUtility.Create(s, caller).ViewOrganization(orgId);
                    return resGroup;
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

        public void EditResponsibility(UserOrganizationModel caller, long responsibilityId, String responsibility = null,long? categoryId = null,long? responsibilityGroupId = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                var r = new ResponsibilityModel();
                using (var tx = s.BeginTransaction())
                {
                    var permissions = PermissionsUtility.Create(s, caller);

                    if (responsibilityId == 0)
                    {
                        if (responsibility == null || categoryId == null || responsibilityGroupId == null)
                            throw new PermissionsException();
                        r.ForOrganizationId = caller.Organization.Id;

                        var rg = s.Get<ResponsibilityGroupModel>(responsibilityGroupId.Value);
                        permissions.ViewOrganization(rg.Organization.Id);
                        r.ForResponsibilityGroup = responsibilityGroupId.Value;
                        r.Responsibility = responsibility;
                        s.Save(r);
                        rg.Responsibilities.Add(r);
                        s.Update(rg);

                    }else{
                        r=s.Get<ResponsibilityModel>(responsibilityId);

                        if (responsibilityGroupId != null && responsibilityGroupId!=r.ForResponsibilityGroup)//Cant change responsibilty Group
                            throw new PermissionsException();
                    }

                    if (responsibility != null)
                        r.Responsibility = responsibility;

                    if (categoryId != null)
                    {
                        permissions.ViewCategory(categoryId.Value);
                        var cat=s.Get<QuestionCategoryModel>(categoryId.Value);
                        r.Category = cat;
                    }

                    permissions.EditOrganization(r.ForOrganizationId);

                    s.Update(r);
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