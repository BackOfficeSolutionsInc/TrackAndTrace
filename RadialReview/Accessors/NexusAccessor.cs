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

namespace RadialReview.Accessors
{
    public class NexusAccessor : BaseAccessor
    {
        public static UrlAccessor _UrlAccessor = new UrlAccessor();
        public String JoinOrganizationUnderManager(UserOrganizationModel manager, OrganizationModel organization,Boolean isManager,String title, String email)
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
                    if (!manager.ManagerAtOrganization)
                        throw new PermissionsException();
                    var newUser=new UserOrganizationModel();
                    newUser.ManagedBy.Add(manager);
                    newUser.ManagerAtOrganization=isManager;
                    newUser.Organization = manager.Organization;
                    newUser.EmailAtOrganization = email;
                    newUser.Title = title;
                    db.Save(newUser);
                    newUserId = newUser.Id;
                    tx.Commit();
                }

                using (var tx = db.BeginTransaction())
                {
                    //Attach 
                    manager = db.Get<UserOrganizationModel>(manager.Id);
                    var nexus = new NexusModel(nexusId)
                    {
                        ActionCode = NexusActions.JoinOrganizationUnderManager,
                        ByUserId = manager.Id,
                        ForUserId = newUserId,
                    };

                    nexus.SetArgs(new string[] { "" + organization.Id, email, "" + newUserId });
                    id = nexus.Id;
                    db.SaveOrUpdate(nexus);
                    //var newUser=db.Get<UserOrganizationModel>(newUserId);
                    //manager.ManagingUsers.Add(newUser);
                    manager.CreatedNexuses.Add(nexus);
                    db.SaveOrUpdate(manager);
                    tx.Commit();
                    db.Flush();
                }
            }
            //Send Email
            var subject = String.Format(EmailStrings.JoinOrganizationUnderManager_Subject, organization.Name.Translate(), ProductStrings.ProductName);
            //[OrganizationName,LinkUrl,LinkDisplay,ProductName]            
            var url =  "Account/Login?message=Please%20login%20to%20join%20" + organization.Name.Translate() + ".&returnUrl=%2FOrganization%2FJoin%2F" + id;
            url = ProductStrings.BaseUrl + url;
            //var shorenedUrl = ProductStrings.BaseUrl + _UrlAccessor.RecordUrl(url, email);
            var body = String.Format(EmailStrings.JoinOrganizationUnderManager_Body, organization.Name.Translate(), url, url, ProductStrings.ProductName);
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