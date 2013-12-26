using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using NHibernate;

namespace RadialReview.Accessors
{
    public class NexusAccessor : BaseAccessor
    {
        public static UrlAccessor _UrlAccessor = new UrlAccessor();

        public TempUserModel CreateUserUnderManager(UserOrganizationModel caller, long managerId, Boolean isManager, long orgPositionId, String email, String firstName, String lastName)
        {
            if (!Emailer.IsValid(email))
                throw new RedirectException(ExceptionStrings.InvalidEmail);

            var nexusId = Guid.NewGuid();
            String id = null;

            TempUserModel tempUser;
            using (var db = HibernateSession.GetCurrentSession())
            {
                long newUserId = 0;
                using (var tx = db.BeginTransaction())
                {

                    var newUser = new UserOrganizationModel();
                    if (managerId == -3)
                    {
                        if (!caller.ManagingOrganization)
                            throw new PermissionsException();
                        newUser.ManagingOrganization = true;
                    }
                    else
                    {
                        var manager = db.Get<UserOrganizationModel>(managerId);
                        //Manager and Caller are in the same organization
                        if (manager.Organization.Id != caller.Organization.Id)
                            throw new PermissionsException();
                        //Strict Hierarchy stuff
                        if (caller.Organization.StrictHierarchy && caller.Id != managerId)
                            throw new PermissionsException();
                        //Both are managers at the organization
                        if (!(caller.ManagerAtOrganization || caller.ManagingOrganization) || !(manager.ManagerAtOrganization || manager.ManagingOrganization))
                            throw new PermissionsException();

                    }

                    newUser.ManagerAtOrganization = isManager;
                    newUser.Organization = caller.Organization;
                    newUser.EmailAtOrganization = email;
                    tempUser = new TempUserModel()
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        Guid = nexusId.ToString(),
                        LastSent = DateTime.UtcNow
                    };
                    newUser.TempUser = tempUser;

                    var position = db.Get<OrganizationPositionModel>(orgPositionId);

                    if (position.Organization.Id != newUser.Organization.Id)
                        throw new PermissionsException();

                    db.Save(newUser);

                    var positionDuration = new PositionDurationModel(position, caller.Id, newUser.Id);
                    newUser.Positions.Add(positionDuration);

                    if (managerId > 0)
                    {
                        var managerDuration = new ManagerDuration(managerId, newUser.Id, caller.Id);
                        newUser.ManagedBy.Add(managerDuration);
                    }

                    db.Update(newUser);

                    if (isManager)
                    {
                        var subordinateTeam = OrganizationTeamModel.SubordinateTeam(caller, newUser);
                        db.Save(subordinateTeam);
                    }

                    newUserId = newUser.Id;
                    tx.Commit();
                }

                using (var tx = db.BeginTransaction())
                {
                    //Attach 
                    caller = db.Get<UserOrganizationModel>(caller.Id);
                    var nexus = new NexusModel(nexusId)
                    {
                        ActionCode = NexusActions.JoinOrganizationUnderManager,
                        ByUserId = caller.Id,
                        ForUserId = newUserId,
                    };

                    nexus.SetArgs(new string[] { "" + caller.Organization.Id, email, "" + newUserId, firstName, lastName });
                    id = nexus.Id;
                    db.SaveOrUpdate(nexus);
                    //var newUser=db.Get<UserOrganizationModel>(newUserId);
                    //manager.ManagingUsers.Add(newUser);
                    caller.CreatedNexuses.Add(nexus);
                    db.SaveOrUpdate(caller);
                    tx.Commit();
                    db.Flush();
                }
            }
            return tempUser;
        }

        public String JoinOrganizationUnderManager(UserOrganizationModel caller, long managerId, Boolean isManager, long orgPositionId, String email,String firstName,String lastName)
        {
            var tempUser=CreateUserUnderManager(caller, managerId, isManager, orgPositionId, email, firstName, lastName);
            return SendJoinEmailToGuid(caller,tempUser);
        }

        public String SendJoinEmailToGuid(UserOrganizationModel caller, TempUserModel tempUser)
        {

            var email = tempUser.Email;
            var firstName = tempUser.FirstName;
            var lastName = tempUser.LastName;
            var id = tempUser.Guid;

            //Send Email
            var subject = String.Format(EmailStrings.JoinOrganizationUnderManager_Subject, firstName, caller.Organization.Name.Translate(), ProductStrings.ProductName);
            //[OrganizationName,LinkUrl,LinkDisplay,ProductName]            
            var url = "Account/Register?message=Please%20login%20to%20join%20" + caller.Organization.Name.Translate() + ".&returnUrl=%2FOrganization%2FJoin%2F" + id;
            url = ProductStrings.BaseUrl + url;
            //var shorenedUrl = ProductStrings.BaseUrl + _UrlAccessor.RecordUrl(url, email);
            var body = String.Format(EmailStrings.JoinOrganizationUnderManager_Body, firstName, caller.Organization.Name.Translate(), url, url, ProductStrings.ProductName);
            subject = Regex.Replace(subject, @"[^A-Za-z0-9 \.\,&]", "");
            Emailer.SendEmail(email, subject, body);
            return id;
        }


        public void Execute(NexusModel nexus)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    nexus = s.Get<NexusModel>(nexus.Id);

                    //db.Nexuses.Attach(nexus);
                    nexus.DateExecuted = DateTime.UtcNow;
                    //db.SaveChanges();
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public NexusModel Put(NexusModel model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    model = Put(s, model);
                    tx.Commit();
                    s.Flush();
                }
            }
            return model;
        }

        public static NexusModel Put(ISession s,NexusModel model)
        {
            s.Save(model);
            return model;
        }


        public NexusModel Get(String id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    /*var found = db.Nexuses.Find(id);
                    if (found == null)
                        throw new PermissionsException();
                    return found;*/

                    return s.Get<NexusModel>(id);
                }
            }
        }
    }
}