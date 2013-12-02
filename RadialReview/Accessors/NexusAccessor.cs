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

namespace RadialReview.Accessors
{
    public class NexusAccessor : BaseAccessor
    {
        public static UrlAccessor _UrlAccessor = new UrlAccessor();
        public String JoinOrganizationUnderManager(UserOrganizationModel caller,long managerId,Boolean isManager,long orgPositionId, String email)
        {
            if (!Emailer.IsValid(email))
                throw new RedirectException(ExceptionStrings.InvalidEmail);

            var nexusId = Guid.NewGuid();
            String id = null;
            using (var db = HibernateSession.GetCurrentSession())
            {
                long newUserId = 0;
                using (var tx = db.BeginTransaction())
                {
                    var manager= db.Get<UserOrganizationModel>(managerId);

                    //Strict Hierarchy stuff
                    if (caller.Organization.StrictHierarchy && caller.Id != managerId)
                        throw new PermissionsException();
                    //Manager and Caller are in the same organization
                    if (manager.Organization.Id != caller.Organization.Id)
                        throw new PermissionsException();
                    //Both are managers at the organization
                    if (!caller.ManagerAtOrganization || !manager.ManagerAtOrganization)
                        throw new PermissionsException();


                    var newUser=new UserOrganizationModel();
                    newUser.ManagedBy.Add(manager);
                    newUser.ManagerAtOrganization=isManager;
                    newUser.Organization = caller.Organization;
                    newUser.EmailAtOrganization = email;

                    var position=db.Get<OrganizationPositionModel>(orgPositionId);

                    if (position.Organization.Id != newUser.Organization.Id)
                        throw new PermissionsException();

                    db.Save(newUser);

                    var positionDuration = new PositionDurationModel(position,caller.Id,newUser.Id);
                    newUser.Positions.Add(positionDuration);

                    db.Update(newUser);
                    
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

                    nexus.SetArgs(new string[] { "" + caller.Organization.Id, email, "" + newUserId });
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
            //Send Email
            var subject = String.Format(EmailStrings.JoinOrganizationUnderManager_Subject, caller.Organization.Name.Translate(), ProductStrings.ProductName);
            //[OrganizationName,LinkUrl,LinkDisplay,ProductName]            
            var url = "Account/Login?message=Please%20login%20to%20join%20" + caller.Organization.Name.Translate() + ".&returnUrl=%2FOrganization%2FJoin%2F" + id;
            url = ProductStrings.BaseUrl + url;
            //var shorenedUrl = ProductStrings.BaseUrl + _UrlAccessor.RecordUrl(url, email);
            var body = String.Format(EmailStrings.JoinOrganizationUnderManager_Body, caller.Organization.Name.Translate(), url, url, ProductStrings.ProductName);
            subject = Regex.Replace(subject, @"[^A-Za-z0-9 \.\,&]", "");
            Emailer.SendEmail(email, subject, body);
            return nexusId.ToString();
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
                    s.Save(model);
                    tx.Commit();
                    s.Flush();
                }
            }
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