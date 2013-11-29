using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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

        public void AddOrganizationPosition(UserOrganizationModel caller,long organizationId,long positionId,String customName)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditOrganization(organizationId);

                    var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x=>x.Organization.Id==organizationId && positionId==x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    /*if (existing!=null)
                        throw new PermissionsException();*/

                    var org=s.Get<OrganizationModel>(organizationId);
                    var position=s.Get<PositionModel>(positionId);

                    s.Save(new OrganizationPositionModel() { Organization = org, Position = position,CustomName=customName });
                    tx.Commit();
                    s.Flush();
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
                    return s.Get<OrganizationModel>(organizationId).Members.ToList();
                }
            }
        }


        public List<OrganizationTeamModel> GetOrganizationTeams(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    return s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
                }
            }
        }
    }
}