using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;
using NHibernate;
using RadialReview.Models.UserModels;

namespace RadialReview.Accessors
{
    public class OrganizationAccessor : BaseAccessor
    {

        public OrganizationModel CreateOrganization(UserModel user, LocalizedStringModel name, Boolean managersCanAddQuestions, PaymentPlanModel paymentPlan)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var organization = new OrganizationModel()
                    {
                        CreationTime = DateTime.UtcNow,
                        PaymentPlan = paymentPlan,
                        Name = name,
                        ManagersCanEdit = managersCanAddQuestions,
                    };

                    db.Save(organization);
                    //db.Organizations.Add(organization);
                    //db.SaveChanges();
                    //db.UserModels.Attach(user);
                    user = db.Get<UserModel>(user.Id);

                    var userOrgModel = new UserOrganizationModel()
                    {
                        Organization = organization,
                        User = user,
                        ManagerAtOrganization = true,
                        ManagingOrganization = true,
                        EmailAtOrganization = user.Email,
                    };


                    //userOrgModel.ManagingOrganizations.Add(organization);
                    //userOrgModel.BelongingToOrganizations.Add(organization);
                    //userOrgModel.ManagerAtOrganization.Add(organization);

                    user.UserOrganization.Add(userOrgModel);

                    //organization.ManagedBy.Add(userOrgModel);
                    organization.Members.Add(userOrgModel);

                    db.Save(user);
                    db.Save(organization);

                    //Add team for every member
                    var allMemberTeam = new OrganizationTeamModel()
                    {
                        CreatedBy = userOrgModel.Id,
                        Name = organization.Name.Translate(),
                        OnlyManagersEdit = true,
                        Organization = organization,
                        InterReview = false,
                        Type = TeamType.AllMembers
                    };
                    db.Save(allMemberTeam);
                    //Add team for every manager
                    var managerTeam = new OrganizationTeamModel()
                    {
                        CreatedBy = userOrgModel.Id,
                        Name = "Managers at " + organization.Name.Translate(),
                        OnlyManagersEdit = true,
                        Organization = organization,
                        InterReview = false,
                        Type = TeamType.Managers
                    };
                    db.Save(managerTeam);

                    tx.Commit();
                    db.Flush();
                    return organization;
                    //db.UserOrganizationModels.Add(userOrgModel);
                    //db.SaveChanges();

                    //organization.ManagedBy.Add(userOrgModel);
                    //db.SaveChanges();
                }
            }

        }

        public UserOrganizationModel JoinOrganization(UserModel user, long managerId, long userOrgPlaceholder)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var manager = db.Get<UserOrganizationModel>(managerId);
                    var orgId = manager.Organization.Id;
                    var organization = db.Get<OrganizationModel>(orgId);
                    user = db.Get<UserModel>(user.Id);
                    var userOrg = db.Get<UserOrganizationModel>(userOrgPlaceholder);

                    userOrg.AttachTime = DateTime.UtcNow;
                    userOrg.User = user;
                    userOrg.Organization = organization;
                    user.CurrentRole = userOrgPlaceholder;

                    user.UserOrganization.Add(userOrg);

                    db.Delete(userOrg.TempUser);

                    userOrg.TempUser = null;

                    //manager.ManagingUsers.Add(userOrg);
                    //organization.Members.Add(userOrg);

                    db.SaveOrUpdate(user);

                    tx.Commit();
                    db.Flush();
                    return userOrg;
                }
            }
        }

        public List<OrganizationPositionModel> GetOrganizationPositions(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    var positions = s.QueryOver<OrganizationPositionModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
                    return positions;
                }
            }
        }

        public OrganizationPositionModel GetOrganizationPosition(UserOrganizationModel caller, long positionId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var position = s.Get<OrganizationPositionModel>(positionId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(position.Organization.Id);
                    return position;
                }
            }
        }



        public List<UserOrganizationModel> GetOrganizationMembers(UserOrganizationModel caller, long organizationId, bool teams, bool managers)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    var users = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
                    
                    if (managers)
                    {
                        var allManagers = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == organizationId).List().ToList();
                        foreach (var user in users)
                            user.PopulateManagers(allManagers);
                    }

                    if (teams)
                    {
                        var allTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
                        var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == organizationId).List().ToList();
                        foreach (var user in users)
                        {
                            user.PopulateTeams(allTeams, allTeamDurations);
                        }
                    }



                    return users;
                }
            }
        }

        public OrganizationPositionModel EditOrganizationPosition(UserOrganizationModel caller, long orgPositionId, long organizationId,
            long? positionId = null, String customName = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditPositions().ManagingPosition(orgPositionId);

                    /*var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x=>x.Organization.Id==organizationId && positionId==x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    if (existing!=null)
                        throw new PermissionsException();*/


                    OrganizationPositionModel orgPos;
                    if (orgPositionId == 0)
                    {
                        var org = s.Get<OrganizationModel>(organizationId);
                        if (positionId == null || String.IsNullOrWhiteSpace(customName))
                            throw new PermissionsException();

                        orgPos = new OrganizationPositionModel() { Organization = org, CreatedBy = caller.Id };
                    }
                    else
                    {
                        orgPos = s.Get<OrganizationPositionModel>(orgPositionId);
                    }


                    if (positionId != null)
                    {
                        var position = s.Get<PositionModel>(positionId);
                        orgPos.Position = position;
                    }

                    if (customName != null)
                    {
                        orgPos.CustomName = customName;
                    }


                    s.SaveOrUpdate(orgPos);
                    tx.Commit();
                    s.Flush();

                    return orgPos;
                }
            }
        }
        public OrganizationTeamModel AddOrganizationTeam(UserOrganizationModel caller, long organizationId, string teamName, bool onlyManagersEdit, bool secret)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditOrganization(organizationId);

                    /*var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x => x.Organization.Id == organizationId && positionId == x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    if (existing!=null)
                        throw new PermissionsException();*/

                    var org = s.Get<OrganizationModel>(organizationId);

                    var orgTeam = new OrganizationTeamModel()
                    {
                        Organization = org,
                        CreatedBy = caller.Id,
                        Name = teamName,
                        OnlyManagersEdit = onlyManagersEdit,
                        Secret = secret,
                    };

                    s.Save(orgTeam);
                    tx.Commit();
                    s.Flush();

                    return orgTeam;
                }
            }
        }

        public void Edit(UserOrganizationModel caller, long organizationId, string organizationName = null,
                                                                            bool? managersCanEdit = null,
                                                                            bool? strictHierarchy = null,
                                                                            bool? managersCanEditPositions = null,
                                                                            bool? sendEmailImmediately = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditOrganization(organizationId).ManagingOrganization();
                    var org = s.Get<OrganizationModel>(organizationId);
                    if (managersCanEdit != null && managersCanEdit.Value != org.ManagersCanEdit)
                    {
                        if (caller.ManagingOrganization)
                            org.ManagersCanEdit = managersCanEdit.Value;
                        else
                            throw new PermissionsException();
                    }
                    if (organizationName != null)
                        org.Name.UpdateDefault(organizationName);
                    if (strictHierarchy != null)
                        org.StrictHierarchy = strictHierarchy.Value;

                    if (managersCanEditPositions != null)
                        org.ManagersCanEditPositions = managersCanEditPositions.Value;

                    if (sendEmailImmediately != null)
                        org.SendEmailImmediately = sendEmailImmediately.Value;

                    s.Update(org);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public List<UserOrganizationModel> GetOrganizationManagers(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    var managers = s.QueryOver<UserOrganizationModel>()
                                            .Where(x => x.Organization.Id == organizationId && (x.ManagerAtOrganization || x.ManagingOrganization))
                                            .List()
                                            .ToList();
                    return managers;
                }
            }
        }



        public Tree GetOrganizationTree(UserOrganizationModel caller, long orgId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(orgId);

                    var org = s.Get<OrganizationModel>(orgId);

                    var managers = s.QueryOver<UserOrganizationModel>()
                                        .Where(x => x.Organization.Id == orgId && x.ManagingOrganization)
                                        .Fetch(x => x.Teams).Default
                                        .List()
                                        .ToListAlive();

                    var classes = "organizations".AsList("admin");

                    return Children(org.Name.Translate(), "", "", -1, classes, managers);
                }
            }
        }

        private Tree Children(String name, String subtext, String classStr, long id, List<String> classes, List<UserOrganizationModel> users)
        {
            var newClasses = classes.ToList();
            if (classes.Count > 0)
                newClasses.RemoveAt(0);

            return new Tree()
            {
                name = name,
                id = id,
                subtext = subtext,
                @class = classes.FirstOrDefault() + " " + classStr,
                children = users.ToListAlive().Select(x =>
                    Children(
                        x.GetTitles(),
                        x.GetName(),
                        String.Join(" ", x.Teams.ToListAlive().Select(y => y.Team.Name.Replace(' ', '_'))),
                        x.Id,
                        newClasses,
                        x.ManagingUsers.ToListAlive().Select(y => y.Subordinate).ToList())
                    ).ToList()
            };
        }

        public List<QuestionCategoryModel> GetOrganizationCategories(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    var orgCategories = s.QueryOver<QuestionCategoryModel>()
                                    .Where(x => (x.OriginId == organizationId && x.OriginType == OriginType.Organization))
                                    .List()
                                    .ToList();

                    var appCategories = ApplicationAccessor.GetApplicationCategories(s);

                    return orgCategories.Union(appCategories).ToList();
                }
            }
        }
    }
}