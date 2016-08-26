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
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Accountability;
using RadialReview.Utilities.RealTime;

namespace RadialReview.Accessors {
    public class NexusAccessor : BaseAccessor {
        //public static UrlAccessor _UrlAccessor = new UrlAccessor();

        public TempUserModel CreateUserUnderManager(UserOrganizationModel caller, long? managerNodeId, Boolean isManager, long orgPositionId, String email, String firstName, String lastName, out UserOrganizationModel createdUser, bool isClient, string organizationName)
        {
            if (!Emailer.IsValid(email))
                throw new PermissionsException(ExceptionStrings.InvalidEmail);
            if (firstName == null)
                throw new PermissionsException("First name cannot be empty.");
            if (lastName == null)
                throw new PermissionsException("Last name cannot be empty.");


            var nexusId = Guid.NewGuid();
            String id = null;

            TempUserModel tempUser;
            var now = DateTime.UtcNow;
            using (var db = HibernateSession.GetCurrentSession()) {
                long newUserId = 0;
                using (var tx = db.BeginTransaction()) {

					AccountabilityNode managerNode= null;
					if (managerNodeId != null) {
						managerNode = db.Get<AccountabilityNode>(managerNodeId.Value);
						if (managerNode == null)
							throw new PermissionsException("Parent does not exist.");
					}

                    var newUser = new UserOrganizationModel();
                    createdUser = newUser;
					if (managerNode == null){
						//No manager
					}else{
						var chart = db.Get<AccountabilityChart>(managerNode.AccountabilityChartId);
						if (chart == null)
							throw new PermissionsException("No accountability chart");

						if (chart.RootId == managerNode.Id){
							//Manager at organization
							if (!caller.ManagingOrganization)
								throw new PermissionsException();
							newUser.ManagingOrganization = true;
						}else
						{
							//var manager = db.Get<UserOrganizationModel>(managerId);
							//Manager and Caller are in the same organization
							if (managerNode.OrganizationId != caller.Organization.Id)
								throw new PermissionsException();
							//Strict Hierarchy stuff
							if (!caller.ManagingOrganization && caller.Organization.StrictHierarchy && (managerNode.UserId==null || caller.Id != managerNode.Id))
								throw new PermissionsException();


							//Am I a manager?  ////Both are managers at the organization
							if (!(caller.ManagerAtOrganization || caller.ManagingOrganization)/* || !(manager.ManagerAtOrganization || manager.ManagingOrganization)*/)
								throw new PermissionsException();

						}
					}
                    newUser.ClientOrganizationName = organizationName;
                    newUser.IsClient = isClient;
                    newUser.ManagerAtOrganization = isManager;
                    newUser.Organization = caller.Organization;
                    newUser.EmailAtOrganization = email;
                    tempUser = new TempUserModel() {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        Guid = nexusId.ToString(),
                        LastSent = null,
                        OrganizationId = caller.Organization.Id,
                        LastSentByUserId = caller.Id,
                        EmailStatus = null
                    };
                    newUser.TempUser = tempUser;

                    var position = orgPositionId != -2 ? db.Get<OrganizationPositionModel>(orgPositionId) : null;

                    if (position != null && position.Organization.Id != newUser.Organization.Id)
                        throw new PermissionsException();

                    db.Save(newUser);
                    newUser.TempUser.UserOrganizationId = newUser.Id;

                    if (position != null) {
                        var positionDuration = new PositionDurationModel(position, caller.Id, newUser.Id) {
                            Start = now,
                        };


                        var template = UserTemplateAccessor._GetAttachedUserTemplateUnsafe(db, position.Id, AttachType.Position);
                        if (template != null)
                            UserTemplateAccessor._AddUserToTemplateUnsafe(db, template.Organization, template.Id, newUser.Id, false);

                        newUser.Positions.Add(positionDuration);
                    }

                    if (managerNode!=null) {
						if (managerNode.UserId != null) {
							var managerDuration = new ManagerDuration(managerNode.UserId.Value, newUser.Id, caller.Id) {
								Start = now,
								Manager = db.Load<UserOrganizationModel>(managerNode.UserId.Value),
								Subordinate = db.Load<UserOrganizationModel>(newUser.Id),
							};
							newUser.ManagedBy.Add(managerDuration);
						}
						// var manager = db.Get<UserOrganizationModel>(managerId);
						//db.Save(new DeepSubordinateModel() { CreateTime = now, Links = 1, ManagerId = newUserId, SubordinateId = newUserId });
						using (var rt = RealTimeUtility.Create()){
							var node = AccountabilityAccessor.AppendNode(db, PermissionsUtility.Create(db, caller),rt, managerNode.Id, userId: newUser.Id);
						}

                        //DeepSubordianteAccessor.Add(db, manager, newUser, caller.Organization.Id, now);
                    }

                    db.Update(newUser);

                    if (isManager) {
                        var subordinateTeam = OrganizationTeamModel.SubordinateTeam(caller, newUser);
                        db.Save(subordinateTeam);
                    }

                    newUserId = newUser.Id;
                    newUser.UpdateCache(db);
                    HooksRegistry.Each<ICreateUserOrganizationHook>(x => x.CreateUser(db, newUser));
                    tx.Commit();
                }

                using (var tx = db.BeginTransaction()) {
                    //Attach 
                    caller = db.Get<UserOrganizationModel>(caller.Id);
                    var nexus = new NexusModel(nexusId) {
                        ActionCode = NexusActions.JoinOrganizationUnderManager,
                        ByUserId = caller.Id,
                        ForUserId = newUserId,
                    };

                    nexus.SetArgs(new string[] { "" + caller.Organization.Id, email, "" + newUserId, firstName, lastName, "" + isClient });
                    id = nexus.Id;
                    db.SaveOrUpdate(nexus);
                    //var newUser=db.Get<UserOrganizationModel>(newUserId);
                    //manager.ManagingUsers.Add(newUser);
                    //caller.CreatedNexuses.Add(nexus);
                    db.SaveOrUpdate(caller);



                    tx.Commit();
                    db.Flush();
                }
            }
            return tempUser;
        }

        public async Task<Tuple<string, UserOrganizationModel>> JoinOrganizationUnderManager(UserOrganizationModel caller, long? managerNodeId, Boolean isManager, long orgPositionId, String email, String firstName, String lastName, bool isClient, bool sendEmail, string organizationName)
        {
            //var sendEmail = caller.Organization.SendEmailImmediately;

            UserOrganizationModel createdUser;

            var tempUser = CreateUserUnderManager(caller, managerNodeId, isManager, orgPositionId, email, firstName, lastName, out createdUser, isClient, organizationName);
            if (sendEmail) {
                var mail = CreateJoinEmailToGuid(caller, tempUser);
                await Emailer.SendEmail(mail);
            }
            return Tuple.Create(tempUser.Guid, createdUser);
        }

        public async Task<EmailResult> ResendAllEmails(UserOrganizationModel caller, long organizationId)
        {
            var unsentEmails = new List<Mail>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);

                    var toSend = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId && x.TempUser != null && x.DeleteTime == null && x.User == null).Fetch(x => x.TempUser).Eager.List().ToList();
                    foreach (var user in toSend) {
#pragma warning disable CS0618 // Type or member is obsolete
						unsentEmails.Add(CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, user.TempUser));
#pragma warning restore CS0618 // Type or member is obsolete
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
            var unsent = new List<Mail>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);

                    var toSend = s.QueryOver<TempUserModel>().Where(x => x.OrganizationId == organizationId && x.LastSent == null).List().ToList();


                    var toUpdate = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(toSend.Select(x => x.UserOrganizationId).ToArray()).List().ToList();
                    foreach (var user in toUpdate) {
                        if (user.DeleteTime != null)
                            toSend.RemoveAll(x => x.UserOrganizationId == user.Id);

                    }

                    foreach (var tempUser in toSend) {
                        var found = toUpdate.FirstOrDefault(x => x.Id == tempUser.UserOrganizationId);
                        if (found == null || found.DeleteTime != null)
                            continue;
#pragma warning disable CS0618 // Type or member is obsolete
						unsent.Add(CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, tempUser));
#pragma warning restore CS0618 // Type or member is obsolete
					}

                    foreach (var user in toUpdate) {
                        user.UpdateCache(s);
                    }
                    tx.Commit();
                    s.Flush();
                }
            }
            var output = ((await Emailer.SendEmails(unsent)));
            return output;
        }

        public Mail CreateJoinEmailToGuid(UserOrganizationModel caller, TempUserModel tempUser)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
#pragma warning disable CS0618 // Type or member is obsolete
					var result = CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, tempUser);
#pragma warning restore CS0618 // Type or member is obsolete

					var user = s.Get<UserOrganizationModel>(tempUser.UserOrganizationId);
                    if (user != null)
                        user.UpdateCache(s);

                    tx.Commit();
                    s.Flush();
                    return result;
                }
            }
        }
        [Obsolete("Update userOrganization cache", false)]
        public static Mail CreateJoinEmailToGuid(DataInteraction s, UserOrganizationModel caller, TempUserModel tempUser)
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
            return Mail.To(EmailTypes.JoinOrganization, emailAddress)
                .Subject(EmailStrings.JoinOrganizationUnderManager_Subject, firstName, caller.Organization.Name.Translate(), productName)
                .Body(EmailStrings.JoinOrganizationUnderManager_Body, firstName, caller.Organization.Name.Translate(), url, url, productName, id.ToUpper());



            //Emailer.SendEmail(s.GetUpdateProvider(), , subject, body);
            //return id;
        }


        public void Execute(NexusModel nexus)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
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
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
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
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    /*var found = db.Nexuses.Find(id);
                    if (found == null)
                        throw new PermissionsException();
                    return found;*/
                    var found = s.Get<NexusModel>(id);
                    if (found == null)
                        throw new PermissionsException("The request was not found.");
                    if (found.DeleteTime != null && DateTime.UtcNow > found.DeleteTime) {
                        var message = "The request has expired.";
                        if (found.ActionCode == NexusActions.ResetPassword) {
                            message += " You can only use this password reset code once.";
                        }
                        throw new PermissionsException(message);
                    }
                    return found;
                }
            }
        }
    }
}