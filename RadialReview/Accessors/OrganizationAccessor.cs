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
                        Type = TeamType.AllMembers
                    };
                    db.Save(allMemberTeam);
                    //Add team for every manager
                    var managerTeam = new OrganizationTeamModel()
                    {
                        CreatedBy = userOrgModel.Id,
                        Name = "Managers at "+organization.Name.Translate(),
                        OnlyManagersEdit = true,
                        Organization = organization,
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

        public UserOrganizationModel JoinOrganization(UserModel user, long managerId,long userOrgPlaceholder)
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
                    userOrg.User= user;
                    userOrg.Organization = organization;

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

      

        public List<UserOrganizationModel> GetOrganizationMembers(UserOrganizationModel caller,long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    return s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
                }
            }
        }

        public OrganizationPositionModel AddOrganizationPosition(UserOrganizationModel caller, long organizationId, long positionId, String customName)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditOrganization(organizationId);

                    /*var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x=>x.Organization.Id==organizationId && positionId==x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    if (existing!=null)
                        throw new PermissionsException();*/

                    var org=s.Get<OrganizationModel>(organizationId);
                    var position=s.Get<PositionModel>(positionId);

                    var orgPos = new OrganizationPositionModel() { Organization = org,CreatedBy=caller.Id, Position = position, CustomName = customName };

                    s.Save(orgPos);
                    tx.Commit();
                    s.Flush();

                    return orgPos;
                }
            }
        }
        public OrganizationTeamModel AddOrganizationTeam(UserOrganizationModel caller, long organizationId, string teamName,bool onlyManagersEdit,bool secret)
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

                    var orgTeam = new OrganizationTeamModel() { 
                            Organization = org,
                            CreatedBy=caller.Id,
                            Name=teamName,
                            OnlyManagersEdit=onlyManagersEdit,
                            Secret=secret,
                        };

                    s.Save(orgTeam);
                    tx.Commit();
                    s.Flush();

                    return orgTeam;
                }
            }
        }

        public void Edit(UserOrganizationModel caller,long organizationId,  string organizationName=null,
                                                                            bool? managersCanEdit=null,
                                                                            bool? strictHierarchy=null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditOrganization(organizationId);
                    var org = s.Get<OrganizationModel>(organizationId);
                    if (organizationName != null)
                        org.Name.UpdateDefault(organizationName);
                    if (managersCanEdit != null && managersCanEdit.Value!=org.ManagersCanEdit)
                    {
                        if (caller.ManagingOrganization)
                            org.ManagersCanEdit = managersCanEdit.Value;
                        else
                            throw new PermissionsException();
                    }
                    if (organizationName != null)
                        org.StrictHierarchy = strictHierarchy.Value;

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
                    PermissionsUtility.Create(s,caller).ViewOrganization(organizationId);
                    var managers = s.QueryOver<UserOrganizationModel>()
                                            .Where(x => x.Organization.Id == organizationId && (x.ManagerAtOrganization || x.ManagingOrganization))
                                            .List()
                                            .ToList();
                    return managers;
                }
            }
        }
    }
}