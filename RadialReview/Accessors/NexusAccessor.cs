using FluentNHibernate.Utils;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
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
using RadialReview.Utilities.Query;
using RadialReview.Models.Application;
using System.Threading.Tasks;
using RadialReview.Models.Json;

namespace RadialReview.Accessors
{
    public class NexusAccessor : BaseAccessor
    {
        //public static UrlAccessor _UrlAccessor = new UrlAccessor();

        public TempUserModel CreateUserUnderManager(UserOrganizationModel caller, long managerId, Boolean isManager, long orgPositionId, String email, String firstName, String lastName,out UserOrganizationModel createdUser)
        {
            if (!Emailer.IsValid(email))
                throw new RedirectException(ExceptionStrings.InvalidEmail);

            var nexusId = Guid.NewGuid();
            String id = null;

            TempUserModel tempUser;
            var now = DateTime.UtcNow;
            using (var db = HibernateSession.GetCurrentSession())
            {
                long newUserId = 0;
                using (var tx = db.BeginTransaction())
                {

                    var newUser = new UserOrganizationModel();
                    createdUser = newUser;
	                if (managerId == -4){
		                //No manager

	                }else if (managerId == -3){
						//Manager at organization
                        if (!caller.ManagingOrganization)
                            throw new PermissionsException();
                        newUser.ManagingOrganization = true;
                    }else{
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
                        LastSent = null,
                        OrganizationId = caller.Organization.Id,
                    };
                    newUser.TempUser = tempUser;
					
					var position = orgPositionId!= -2?db.Get<OrganizationPositionModel>(orgPositionId):null;

					if (position!=null && position.Organization.Id != newUser.Organization.Id)
                        throw new PermissionsException();

                    db.Save(newUser);
	                newUser.TempUser.UserOrganizationId = newUser.Id;

	                if (position != null){
		                var positionDuration = new PositionDurationModel(position, caller.Id, newUser.Id){
			                Start = now,
		                };
		                newUser.Positions.Add(positionDuration);
	                }

	                if (managerId > 0)
                    {
                        var managerDuration = new ManagerDuration(managerId, newUser.Id, caller.Id){
							Start = now,
							Manager = db.Load<UserOrganizationModel>(managerId),
							Subordinate = db.Load<UserOrganizationModel>(newUser.Id),
                        };
                        var manager = db.Get<UserOrganizationModel>(managerId);
                        //db.Save(new DeepSubordinateModel() { CreateTime = now, Links = 1, ManagerId = newUserId, SubordinateId = newUserId });
                        DeepSubordianteAccessor.Add(db, manager, newUser, caller.Organization.Id, now);
                        newUser.ManagedBy.Add(managerDuration);
                    }

                    db.Update(newUser);

                    if (isManager)
                    {
                        var subordinateTeam = OrganizationTeamModel.SubordinateTeam(caller, newUser);
                        db.Save(subordinateTeam);
                    }

                    newUserId = newUser.Id;
					newUser.UpdateCache(db);
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

        public async Task<Tuple<string,UserOrganizationModel>> JoinOrganizationUnderManager(UserOrganizationModel caller, long managerId, Boolean isManager, long orgPositionId, String email, String firstName, String lastName)
        {
            var sendEmail = caller.Organization.SendEmailImmediately;

            UserOrganizationModel createdUser;

            var tempUser = CreateUserUnderManager(caller, managerId, isManager, orgPositionId, email, firstName, lastName,out createdUser);
            if (sendEmail)
            {
                var mail=CreateJoinEmailToGuid(caller, tempUser);
                await Emailer.SendEmail(mail);
            }
            return Tuple.Create(tempUser.Guid,createdUser);
        }

        public async Task<EmailResult> ResendAllEmails(UserOrganizationModel caller, long organizationId)
        {
            var unsentEmails=new List<MailModel>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);

                    var toSend = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId && x.TempUser != null && x.DeleteTime==null).Fetch(x=>x.TempUser).Eager.List().ToList();
                    foreach (var user in toSend)
                    {
                       unsentEmails.Add(CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, user.TempUser));
					   user.UpdateCache(s);
					}
                    tx.Commit();
                    s.Flush();
                
                } 
            }
            return await Emailer.SendEmails(unsentEmails);
        }

        public async Task<EmailResult> SendAllJoinEmails(UserOrganizationModel caller, long organizationId)
        {
            var unsent = new List<MailModel>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);

                    var toSend = s.QueryOver<TempUserModel>().Where(x => x.OrganizationId == organizationId && x.LastSent == null).List().ToList();
                    
                 
	                var toUpdate = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(toSend.Select(x => x.UserOrganizationId).ToArray()).List().ToList();
	                foreach (var user in toUpdate){
		                if (user.DeleteTime != null)
			                toSend.RemoveAll(x => x.UserOrganizationId == user.Id);

	                }

					foreach (var tempUser in toSend){
                        unsent.Add(CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, tempUser));
                    }
					var output = ((await Emailer.SendEmails(unsent)));
					foreach (var user in toUpdate){
						user.UpdateCache(s);
					}
	                return output;
                }
            }
        }
        
        public MailModel CreateJoinEmailToGuid(UserOrganizationModel caller, TempUserModel tempUser)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var result = CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, tempUser);

	                var user = s.Get<UserOrganizationModel>(tempUser.UserOrganizationId);
					if (user!=null)
						user.UpdateCache(s);

                    tx.Commit();
                    s.Flush();
                    return result;
                }
            }
        }
		[Obsolete("Update userOrganization cache",false)]
        public static MailModel CreateJoinEmailToGuid(DataInteraction s, UserOrganizationModel caller, TempUserModel tempUser)
        {
            var emailAddress = tempUser.Email;
            var firstName = tempUser.FirstName;
            var lastName = tempUser.LastName;
            var id = tempUser.Guid;

            tempUser = s.Get<TempUserModel>(tempUser.Id);
            tempUser.LastSent = DateTime.UtcNow;
            s.SaveOrUpdate(tempUser);
			
            //Send Email
            //[OrganizationName,LinkUrl,LinkDisplay,ProductName]            
            var url = "Account/Register?returnUrl=%2FOrganization%2FJoin%2F" + id;
			url = Config.BaseUrl(caller.Organization) + url;
            //var body = String.Format(;
            //subject = ;
			var productName = Config.ProductName(caller.Organization);
            return MailModel.To(emailAddress)
				.Subject(EmailStrings.JoinOrganizationUnderManager_Subject, firstName, caller.Organization.Name.Translate(), productName)
				.Body(EmailStrings.JoinOrganizationUnderManager_Body, firstName, caller.Organization.Name.Translate(), url, url, productName, id.ToUpper());



            //Emailer.SendEmail(s.GetUpdateProvider(), , subject, body);
            //return id;
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
                    model = Put(s.ToUpdateProvider(), model);
                    tx.Commit();
                    s.Flush();
                }
            }
            return model;
        }

        public static NexusModel Put(AbstractUpdate s, NexusModel model)
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
                    var found = s.Get<NexusModel>(id);
                    if (found.DeleteTime != null && DateTime.UtcNow > found.DeleteTime)
                        throw new PermissionsException("The request has expired.");
                    return found;
                }
            }
        }
    }
}