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
using System.Text.RegularExpressions;

namespace RadialReview.Accessors
{
    public class OrganizationAccessor : BaseAccessor
    {

        public OrganizationModel CreateOrganization(UserModel user, LocalizedStringModel name, Boolean managersCanAddQuestions, PaymentPlanModel paymentPlan,DateTime now,out long newUserId)
        {
            UserOrganizationModel userOrgModel;
            OrganizationModel organization;

            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    organization = new OrganizationModel()
                    {
                        CreationTime = now,
                        PaymentPlan = paymentPlan,
                        Name = name,
                        ManagersCanEdit = false,
                        
                    };

                    db.Save(organization);
                    //db.Organizations.Add(organization);
                    //db.SaveChanges();
                    //db.UserModels.Attach(user);
                    user = db.Get<UserModel>(user.Id);

                    userOrgModel = new UserOrganizationModel()
                    {
                        Organization = organization,
                        User = user,
                        ManagerAtOrganization = true,
                        ManagingOrganization = true,
                        EmailAtOrganization = user.Email,
                        AttachTime=now
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
                    //db.UserOrganizationModels.Add(userOrgModel);
                    //db.SaveChanges();

                    //organization.ManagedBy.Add(userOrgModel);
                    //db.SaveChanges();
                }
                using (var tx = db.BeginTransaction())
                {
                    db.Save(new DeepSubordinateModel
                    {
                        CreateTime = now,
                        Links = 1,
                        SubordinateId = userOrgModel.Id,
                        ManagerId = userOrgModel.Id,
                        OrganizationId = organization.Id,
                    });
                    newUserId = userOrgModel.Id;
                    tx.Commit();
                    db.Flush();
                    return organization;
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


        public List<ManagerDuration> GetOrganizationManagerLinks(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    return s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == organizationId).List().ToList();
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
                                                                            bool? managersHaveAdmin = null,
                                                                            bool? strictHierarchy = null,
                                                                            bool? managersCanEditPositions = null,
                                                                            bool? sendEmailImmediately = null,
                                                                            bool? managersCanRemoveUsers = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditOrganization(organizationId).ManagingOrganization();
                    var org = s.Get<OrganizationModel>(organizationId);
                    if (managersHaveAdmin != null && managersHaveAdmin.Value != org.ManagersCanEdit)
                    {
                        if (caller.ManagingOrganization)
                            org.ManagersCanEdit = managersHaveAdmin.Value;
                        else
                            throw new PermissionsException("You cannot change whether managers are admins at the organization.");
                    }
                    if (organizationName != null)
                        org.Name.UpdateDefault(organizationName);
                    if (strictHierarchy != null)
                        org.StrictHierarchy = strictHierarchy.Value;

                    if (managersCanEditPositions != null)
                        org.ManagersCanEditPositions = managersCanEditPositions.Value;

                    if (sendEmailImmediately != null)
                        org.SendEmailImmediately = sendEmailImmediately.Value;

                    if (managersCanRemoveUsers != null)
                        org.ManagersCanRemoveUsers = managersCanRemoveUsers.Value;

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
                    var perms = PermissionsUtility.Create(s, caller).ViewOrganization(orgId);

                    var org = s.Get<OrganizationModel>(orgId);

                    var managers = s.QueryOver<UserOrganizationModel>()
                                        .Where(x => x.Organization.Id == orgId && x.ManagingOrganization)
                                        .Fetch(x => x.Teams).Default
                                        .List()
                                        .ToListAlive();

                    var deep = DeepSubordianteAccessor.GetSubordinatesAndSelf(s,caller,caller.Id);

                    //var classes = "organizations".AsList("admin");

                    var managingOrg = caller.ManagingOrganization && orgId ==caller.Organization.Id;

                    var tree = new Tree(){
                        name=org.Name.Translate(),
                        @class="organizations",
                        id = -1*orgId,
                        children = managers.Select(x => x.GetTree(deep, caller.Id, force: managingOrg)).ToList()
                    };

                    return tree;
                }
            }
        }
        /*
        private Tree Children(String name, String subtext, String classStr,bool manager, long id, List<UserOrganizationModel> users,List<long> deep,long youId)
        {

            /*var newClasses = classes.ToList();
            if (classes.Count > 0)
                newClasses.RemoveAt(0);*/
            /*var managing =deep.Any(x=>x==id);

            if (managing)
                classStr += " managing";
            if (id == youId)
                classStr += " you";

            return new Tree()
            {
                name = name,
                id = id,
                subtext = subtext,
                @class = classStr,
                managing = managing,
                manager = manager,
                children = users.ToListAlive().Select(x =>{
                    var selfClasses = x.Teams.ToListAlive().Select(y=>y.Team.Name).ToList();
                    selfClasses.Add("employee");
                    if (x.ManagingOrganization)
                        selfClasses.Add("admin");
                    if(x.ManagerAtOrganization)
                        selfClasses.Add("manager");


                    return Children(
                            x.GetName(),
                            x.GetTitles(),
                            String.Join(" ", selfClasses.Select(y => Regex.Replace(y, "[^a-zA-Z0-9]", "_"))),
                            x.IsManager(),
                            x.Id,
                            x.ManagingUsers.ToListAlive().Select(y => y.Subordinate).ToList(),
                            deep,
                            youId
                        );
                }
                    ).ToList()
            };*
        }*/

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

        //Gets all users and populates direct subordinates
        /*
        public List<UserOrganizationModel> GetOrganizationMembersAndSubordinates(UserOrganizationModel caller, long forUserId, bool allSubordinates)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (allSubordinates == false)
                        throw new NotImplementedException("All subordinates not implemented. Only direct subordinates.");

                    var perms=PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId,false);

                    var user=s.Get<UserOrganizationModel>(forUserId);
                    var allOrgUsers=s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == user.Organization.Id).List().ToList();

                    var directReports = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == forUserId).List().ToList();

                    foreach (var u in allOrgUsers)
                    {
                        u.SetPersonallyManaging(directReports.Any(x => x.SubordinateId == u.Id));
                    }

                    return allOrgUsers;
                }
            }
        }*/

        public OrganizationModel GetOrganization(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    return s.Get<OrganizationModel>(organizationId);
                }
            }
        }
    }
}