using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.Query;
using RadialReview.Models.Application;
using System.Threading.Tasks;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Accountability;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.ViewModels;
using NHibernate;

namespace RadialReview.Accessors {
	public class AddedUser {
		public TempUserModel TempUser { get; set; }
		public UserOrganizationModel User { get; set; }
	}


	public class JoinOrganizationAccessor : BaseAccessor {

		public static async Task<AddedUser> AddUser(UserOrganizationModel caller, CreateUserOrganizationViewModel settings) {
			//UserOrganizationModel created;
			return await CreateUserUnderManager(caller, settings);
			//return new AddedUser() {
			//	TempUser = temp,
			//	User = created
			//};
		}
		#region Test only
		public static async Task<AddedUser> CreateUserUnderManager_Test(ISession db, PermissionsUtility perms, CreateUserOrganizationViewModel settings) {
			return await CreateUserUnderManager_Test(db, perms, settings.ManagerNodeId, settings.IsManager, settings.OrgPositionId, settings.Email, settings.FirstName, settings.LastName, settings.IsClient, settings.ClientOrganizationName, settings.EvalOnly);
		}
		private static async Task<AddedUser> CreateUserUnderManager_Test(ISession db, PermissionsUtility perms, long? managerNodeId, Boolean isManager, long? orgPositionId, String email, String firstName, String lastName, bool isClient, string organizationName, bool evalOnly) {
			if (!Emailer.IsValid(email))
				throw new PermissionsException(ExceptionStrings.InvalidEmail);
			if (firstName == null)
				throw new PermissionsException("First name cannot be empty.");
			if (lastName == null)
				throw new PermissionsException("Last name cannot be empty.");
			if (managerNodeId == -3)
				managerNodeId = null;

			var nexusId = Guid.NewGuid();
			String id = null;

			var output = new AddedUser();
			//TempUserModel tempUser;
			var now = DateTime.UtcNow;
			var caller = perms.GetCaller();
			long newUserId = 0;
			UserOrganizationModel newUser = new UserOrganizationModel();
			using (var tx = db.BeginTransaction()) {

				AccountabilityNode managerNode = null;
				if (managerNodeId != null) {
					managerNode = db.Get<AccountabilityNode>(managerNodeId.Value);
					if (managerNode == null)
						throw new PermissionsException("Parent does not exist.");
				}

				output.User = newUser;
				if (managerNode == null) {
					//No manager
				} else {
					var chart = db.Get<AccountabilityChart>(managerNode.AccountabilityChartId);
					if (chart == null)
						throw new PermissionsException("No accountability chart");

					if (chart.RootId == managerNode.Id) {
						//Manager at organization
						if (!caller.ManagingOrganization)
							throw new PermissionsException();
						newUser.ManagingOrganization = true;
					} else {
						//var manager = db.Get<UserOrganizationModel>(managerId);
						//Manager and Caller are in the same organization
						if (managerNode.OrganizationId != caller.Organization.Id)
							throw new PermissionsException();
						//Strict Hierarchy stuff
						if (!caller.ManagingOrganization && caller.Organization.StrictHierarchy && (managerNode.UserId == null || caller.Id != managerNode.UserId))
							throw new PermissionsException();


						//Am I a manager?  ////Both are managers at the organization
						if (!(caller.ManagerAtOrganization || caller.ManagingOrganization)/* || !(manager.ManagerAtOrganization || manager.ManagingOrganization)*/)
							throw new PermissionsException();

					}
				}
				newUser.EvalOnly = evalOnly;
				newUser.ClientOrganizationName = organizationName;
				newUser.IsClient = isClient;
				newUser.ManagerAtOrganization = isManager;
				newUser.Organization = caller.Organization;
				newUser.EmailAtOrganization = email;
				output.TempUser = new TempUserModel() {
					FirstName = firstName,
					LastName = lastName,
					Email = email,
					Guid = nexusId.ToString(),
					LastSent = null,
					OrganizationId = caller.Organization.Id,
					LastSentByUserId = caller.Id,
					EmailStatus = null,
				};
				newUser.TempUser = output.TempUser;

				var position = (orgPositionId != -2 && orgPositionId != null) ? db.Get<OrganizationPositionModel>(orgPositionId) : null;

				if (position != null && position.Organization.Id != newUser.Organization.Id)
					throw new PermissionsException();

				db.Save(newUser);
				newUser.TempUser.UserOrganizationId = newUser.Id;

				if (position != null) {
					var positionDuration = new PositionDurationModel(position, caller.Id, newUser.Id) {
						CreateTime = now,
					};


					var template = UserTemplateAccessor._GetAttachedUserTemplateUnsafe(db, position.Id, AttachType.Position);
					if (template != null)
						UserTemplateAccessor._AddUserToTemplateUnsafe(db, template.Organization, template.Id, newUser.Id, false);

					newUser.Positions.Add(positionDuration);
				}

				db.Update(newUser);

				if (isManager) {
					var subordinateTeam = OrganizationTeamModel.SubordinateTeam(caller, newUser);
					db.Save(subordinateTeam);
				}

				newUserId = newUser.Id;
				if (managerNode != null) {
					using (var rt = RealTimeUtility.Create()) {
						//var perms = PermissionsUtility.Create(db, caller);
						var node = AccountabilityAccessor.AppendNode(db, perms, rt, managerNode.Id, userId: newUser.Id);
						if (orgPositionId > 0 && orgPositionId != null)
							AccountabilityAccessor.SetPosition(db, perms, rt, node.Id, orgPositionId);

					}

				} else {
					newUser.UpdateCache(db);

				}
				tx.Commit();
			}

			using (var tx = db.BeginTransaction()) {
#pragma warning disable CS0618 // Type or member is obsolete
				await HooksRegistry.Each<ICreateUserOrganizationHook>(x => x.CreateUserOrganization(db, newUser));
#pragma warning restore CS0618 // Type or member is obsolete
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
				//db.SaveOrUpdate(caller);
				db.SaveOrUpdate(caller);

				tx.Commit();
				db.Flush();
			}
			return output;
		}
		#endregion

		public static async Task<AddedUser> CreateUserUnderManager(UserOrganizationModel caller, CreateUserOrganizationViewModel settings) {
			return await CreateUserUnderManager(caller, settings.ManagerNodeId, settings.IsManager, settings.OrgPositionId, settings.Email, settings.FirstName, settings.LastName, settings.IsClient, settings.ClientOrganizationName, settings.EvalOnly);
		}

		private static async Task<AddedUser> CreateUserUnderManager(UserOrganizationModel caller, long? managerNodeId, Boolean isManager, long? orgPositionId, String email, String firstName, String lastName, bool isClient, string organizationName, bool evalOnly) {
			if (!Emailer.IsValid(email))
				throw new PermissionsException(ExceptionStrings.InvalidEmail);
			if (firstName == null)
				throw new PermissionsException("First name cannot be empty.");
			if (lastName == null)
				throw new PermissionsException("Last name cannot be empty.");
			if (managerNodeId == -3)
				managerNodeId = null;

			var nexusId = Guid.NewGuid();
			String id = null;

			var output = new AddedUser();

			//TempUserModel tempUser;
			var newUser = new UserOrganizationModel();
			var now = DateTime.UtcNow;
			using (var db = HibernateSession.GetCurrentSession()) {
				long newUserId = 0;
				using (var tx = db.BeginTransaction()) {

					AccountabilityNode managerNode = null;
					if (managerNodeId != null) {
						managerNode = db.Get<AccountabilityNode>(managerNodeId.Value);
						if (managerNode == null)
							throw new PermissionsException("Parent does not exist.");
					}

					output.User = newUser;
					if (managerNode == null) {
						//No manager
					} else {
						var chart = db.Get<AccountabilityChart>(managerNode.AccountabilityChartId);
						if (chart == null)
							throw new PermissionsException("No accountability chart");

						if (chart.RootId == managerNode.Id) {
							//Manager at organization
							if (!caller.ManagingOrganization)
								throw new PermissionsException();
							newUser.ManagingOrganization = true;
						} else {
							//var manager = db.Get<UserOrganizationModel>(managerId);
							//Manager and Caller are in the same organization
							if (managerNode.OrganizationId != caller.Organization.Id)
								throw new PermissionsException();
							//Strict Hierarchy stuff
							if (!caller.ManagingOrganization && caller.Organization.StrictHierarchy && (managerNode.UserId == null || caller.Id != managerNode.UserId))
								throw new PermissionsException();


							//Am I a manager?  ////Both are managers at the organization
							if (!(caller.ManagerAtOrganization || caller.ManagingOrganization)/* || !(manager.ManagerAtOrganization || manager.ManagingOrganization)*/)
								throw new PermissionsException();

						}
					}
					newUser.EvalOnly = evalOnly;
					newUser.ClientOrganizationName = organizationName;
					newUser.IsClient = isClient;
					newUser.ManagerAtOrganization = isManager;
					newUser.Organization = caller.Organization;
					newUser.EmailAtOrganization = email;
					output.TempUser = new TempUserModel() {
						FirstName = firstName,
						LastName = lastName,
						Email = email,
						Guid = nexusId.ToString(),
						LastSent = null,
						OrganizationId = caller.Organization.Id,
						LastSentByUserId = caller.Id,
						EmailStatus = null,
					};
					newUser.TempUser = output.TempUser;

					var position = (orgPositionId != -2 && orgPositionId != null) ? db.Get<OrganizationPositionModel>(orgPositionId) : null;

					if (position != null && position.Organization.Id != newUser.Organization.Id)
						throw new PermissionsException();

					db.Save(newUser);
					newUser.TempUser.UserOrganizationId = newUser.Id;

					if (position != null) {
						var positionDuration = new PositionDurationModel(position, caller.Id, newUser.Id) {
							CreateTime = now,
						};


						var template = UserTemplateAccessor._GetAttachedUserTemplateUnsafe(db, position.Id, AttachType.Position);
						if (template != null)
							UserTemplateAccessor._AddUserToTemplateUnsafe(db, template.Organization, template.Id, newUser.Id, false);

						//REMOVED, CAUSES DUPLICATE POSITION
						//newUser.Positions.Add(positionDuration);
					}

					db.Update(newUser);

					if (isManager) {
						var subordinateTeam = OrganizationTeamModel.SubordinateTeam(caller, newUser);
						db.Save(subordinateTeam);
					}

					newUserId = newUser.Id;
					if (managerNode != null) {
						using (var rt = RealTimeUtility.Create()) {
							var perms = PermissionsUtility.Create(db, caller);
							var node = AccountabilityAccessor.AppendNode(db, perms, rt, managerNode.Id, userId: newUser.Id);
							if (orgPositionId > 0 && orgPositionId != null)
								AccountabilityAccessor.SetPosition(db, perms, rt, node.Id, orgPositionId);

						}

					} else {
						newUser.UpdateCache(db);

					}
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
				using (var tx = db.BeginTransaction()) {
#pragma warning disable CS0618 // Type or member is obsolete
					await HooksRegistry.Each<ICreateUserOrganizationHook>(x => x.CreateUserOrganization(db, newUser));
#pragma warning restore CS0618 // Type or member is obsolete
					tx.Commit();
					db.Flush();
				}
			}
			return output;
		}

		public static async Task<Tuple<string, UserOrganizationModel>> JoinOrganizationUnderManager(UserOrganizationModel caller, CreateUserOrganizationViewModel settings) // long? managerNodeId, Boolean isManager, long orgPositionId, String email, String firstName, String lastName, bool isClient, bool sendEmail, string organizationName,bool evalOnly)
		{
			//var sendEmail = caller.Organization.SendEmailImmediately;
			//UserOrganizationModel createdUser;

			var addedUser = await CreateUserUnderManager(caller, settings);// managerNodeId, isManager, orgPositionId, email, firstName, lastName, out createdUser, isClient, organizationName,evalOnly);
			if (settings.SendEmail) {
				var mail = CreateJoinEmailToGuid(caller, addedUser.TempUser);
				await Emailer.SendEmail(mail);
			}
			return Tuple.Create(addedUser.TempUser.Guid, addedUser.User);
		}

		public static async Task<EmailResult> ResendAllEmails(UserOrganizationModel caller, long organizationId) {
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

		public static async Task<EmailResult> SendAllJoinEmails(UserOrganizationModel caller, long organizationId) {
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

		public static Mail CreateJoinEmailToGuid(UserOrganizationModel caller, TempUserModel tempUser) {
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
		public static Mail CreateJoinEmailToGuid(DataInteraction s, UserOrganizationModel caller, TempUserModel tempUser) {
			var emailAddress = tempUser.Email;
			var firstName = tempUser.FirstName;
			var lastName = tempUser.LastName;
			var id = tempUser.Guid;

			tempUser = s.Get<TempUserModel>(tempUser.Id);
			tempUser.LastSent = DateTime.UtcNow;
			s.Merge(tempUser);

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
	}
}