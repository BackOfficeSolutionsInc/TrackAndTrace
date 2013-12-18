using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
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

        public ResponsibilityGroupModel GetResponsibilityGroup(UserOrganizationModel caller, long responsibilityGroupId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var resGroup = s.Get<ResponsibilityGroupModel>(responsibilityGroupId);
                    long orgId;

                    if (resGroup is OrganizationModel) orgId = resGroup.Id;
                    else orgId = resGroup.Organization.Id;

                    PermissionsUtility.Create(s, caller).ViewOrganization(orgId);
                    return resGroup;
                }
            }
        }

        public List<ResponsibilityModel> GetResponsibilities(UserOrganizationModel caller, long responsibilityGroupId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var responsibilities = s.QueryOver<ResponsibilityModel>().Where(x => x.ForResponsibilityGroup == responsibilityGroupId).List().ToList();

                    var orgs = responsibilities.Select(x => x.ForOrganizationId).Distinct().ToList();
                    var permissions = PermissionsUtility.Create(s, caller);
                    foreach (var oId in orgs)
                    {
                        permissions.ViewOrganization(oId);
                    }
                    return responsibilities;
                }
            }
        }

        private static TeamAccessor _TeamAccessor = new TeamAccessor();
        private static PositionAccessor _PositionAccessor = new PositionAccessor();

        public List<ResponsibilityGroupModel> GetResponsibilityGroupsForUser(UserOrganizationModel caller, long userId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetResponsibilityGroupsForUser(s, perms, caller, userId);
                }
            }
        }

        public static List<ResponsibilityGroupModel> GetResponsibilityGroupsForUser(ISession s, PermissionsUtility permissions, UserOrganizationModel caller, long userId)
        {
            var teams = TeamAccessor.GetUsersTeams(s, permissions, caller, userId);
            /*}
            using (var tx = s.BeginTransaction())
            {*/
            PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
            var user = s.Get<UserOrganizationModel>(userId);

            List<ResponsibilityGroupModel> responsibilityGroups = new List<ResponsibilityGroupModel>();
            //User
            responsibilityGroups.Add(user);
            //Positions
            responsibilityGroups.AddRange(user.Positions.ToListAlive().Select(x => x.Position));
            //Teams
            responsibilityGroups.AddRange(teams.ToListAlive().Select(x => x.Team));

            return responsibilityGroups;
        }

        public void EditResponsibility(UserOrganizationModel caller, long responsibilityId, String responsibility = null, long? categoryId = null, long? responsibilityGroupId = null, bool? active = null, WeightType? weight = null)
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
                        r.Required = true;
                        s.Save(r);
                        rg.Responsibilities.Add(r);
                        s.Update(rg);

                    }
                    else
                    {
                        r = s.Get<ResponsibilityModel>(responsibilityId);

                        if (responsibilityGroupId != null && responsibilityGroupId != r.ForResponsibilityGroup)//Cant change responsibilty Group
                            throw new PermissionsException();
                    }

                    if (responsibility != null)
                        r.Responsibility = responsibility;

                    if (categoryId != null)
                    {
                        permissions.ViewCategory(categoryId.Value);
                        var cat = s.Get<QuestionCategoryModel>(categoryId.Value);
                        r.Category = cat;
                    }

                    if (active != null)
                    {
                        if (active == true)
                            r.DeleteTime = null;
                        else
                            r.DeleteTime = DateTime.UtcNow;
                    }

                    if (weight != null)
                    {
                        r.Weight = weight.Value;
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