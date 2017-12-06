using System.Threading.Tasks;
using System.Web.Mvc;
using Amazon.ElasticTranscoder.Model;
using FluentNHibernate.Utils;
using NHibernate.Cache;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Exceptions;
using RadialReview.Models.Askables;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using NHibernate.Linq;
using NHibernate;
using RadialReview.Models.UserModels;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Enums;
using RadialReview.Utilities.Query;
using RadialReview.Models.Json;
using System.Security.Principal;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Users;
using RadialReview.Utilities.DataTypes;
using RadialReview.NHibernate;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Text;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Accountability;
using RadialReview.Models.Reviews;
using RadialReview.Utilities.RealTime;

namespace RadialReview.Accessors {

	public class UserAccessor : BaseAccessor {
		public String GetUserIdByUsername(ISession s, String username) {
			return (string)CacheLookup.GetOrAddDefault("username_" + username, x => {
#pragma warning disable CS0618 // Type or member is obsolete
				return GetUserByEmail(s, username).Id;
#pragma warning restore CS0618 // Type or member is obsolete
			});
		}

		public String GetUserNameByUserOrganizationId(long userOrgId) {
			return (string)CacheLookup.GetOrAddDefault("userorgid_" + userOrgId, x => {
#pragma warning disable CS0618 // Type or member is obsolete
				return GetUserOrganizationUnsafe(userOrgId).User.UserName;
#pragma warning restore CS0618 // Type or member is obsolete
			});
		}

		public UserModel GetUserById(String userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetUserById(s, userId);
				}
			}
		}

		public UserModel GetUserById(ISession s, String userId) {
			if (userId == null)
				throw new LoginException();
			//using (var s = HibernateSession.GetCurrentSession())
			//{
			//using (var tx = s.BeginTransaction())
			//{
			return s.Get<UserModel>(userId);
			//}
			//}
		}

		public List<UserModel> GetUsersByIds(IEnumerable<String> userIds) {
			if (userIds == null)
				throw new LoginException();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return s.QueryOver<UserModel>().WhereRestrictionOn(x => x.Id).IsIn(userIds.ToArray()).List().ToList();
				}
			}
		}

		[Obsolete("Dont use this elsewhere")]
		public UserModel GetUserByEmail(ISession s, String email) {
			if (email == null)
				throw new LoginException();
			//using (var s = HibernateSession.GetCurrentSession())
			//{
			//	using (var tx = s.BeginTransaction())
			//	{
			var lower = email.ToLower();
			return s.QueryOver<UserModel>().Where(x => x.UserName == lower).SingleOrDefault();
			//	}
			//}
		}

		[Obsolete("Dont use this elsewhere")]
		public UserModel GetUserByEmail(String email) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetUserByEmail(s, email);
				}
			}
		}

		[Obsolete("Dont use this, its unsafe")]
		public UserOrganizationModel GetUserOrganizationUnsafe(long userOrganizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return s.Get<UserOrganizationModel>(userOrganizationId);
				}
			}
		}

		public static UserOrganizationModel GetUserOrganization(ISession s, PermissionsUtility perms, long userOrganizationId, bool asManager, bool sensitive, params PermissionType[] alsoCheck) {
			return GetUserOrganization(s.ToQueryProvider(true), perms, perms.GetCaller(), userOrganizationId, asManager, sensitive, alsoCheck);
		}

		public UserOrganizationModel GetUserOrganization(UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive, params PermissionType[] alsoCheck) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetUserOrganization(s.ToQueryProvider(true), perms, caller, userOrganizationId, asManager, sensitive, alsoCheck);
				}
			}
		}

		public static UserOrganizationModel GetUserOrganization(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive, params PermissionType[] alsoCheck) {
			perms.ViewUserOrganization(userOrganizationId, sensitive, alsoCheck);
			if (asManager) {
				perms.ManagesUserOrganization(userOrganizationId, false, alsoCheck);
			}
			return s.Get<UserOrganizationModel>(userOrganizationId);

		}

		public List<UserOrganizationModel> GetUserOrganizations(ISession s, String userId, String redirectUrl, Boolean full = false) {
			if (userId == null)
				throw new LoginException(redirectUrl);
			//using (var s = HibernateSession.GetCurrentSession())
			//{
			//	using (var tx = s.BeginTransaction())
			//	{
			var user = s.Get<UserModel>(userId);
			//var user = users.SingleOrDefault();
			//.FetchMany(x=>x.UserOrganization)
			//.SingleOrDefault();// db.UserModels.AsNoTracking().FirstOrDefault(x => x.IdMapping == userId);
			if (user == null)
				throw new LoginException(redirectUrl);
			var userOrgs = new List<UserOrganizationModel>();

			foreach (var userOrg in user.UserOrganization.ToListAlive()) {
				userOrgs.Add(GetUserOrganizationModel(s, userOrg.Id, full));
			}
			return userOrgs;
			//	}
			//}
		}

		public int GetUserOrganizationCounts(ISession s, String userId, String redirectUrl, Boolean full = false) {
			if (userId == null)
				throw new LoginException(redirectUrl);
			var user = s.Get<UserModel>(userId);
			if (user == null)
				throw new LoginException(redirectUrl);
			return user.UserOrganizationCount;
		}
		
		public List<UserOrganizationModel> GetPeers(UserOrganizationModel caller, long forId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetPeers(s.ToQueryProvider(true), perms, caller, forId);
				}
			}
		}
		
		public static List<UserOrganizationModel> GetPeers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forId) {
			perms.ViewUserOrganization(forId, false);
			var forUser = s.Get<UserOrganizationModel>(forId);
			if (forUser.ManagingUsers.All(x => x.DeleteTime != null)) {
				return forUser.ManagedBy.ToListAlive()
					.Select(x => x.Manager)
					.SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate))
					.Where(x => x.Id != forId)
					.ToList();
			}
			return new List<UserOrganizationModel>();
		}

		public List<UserOrganizationModel> GetManagers(UserOrganizationModel caller, long forUserId, params PermissionType[] alsoCheck) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetManagers(s.ToQueryProvider(true), perms, caller, forUserId, alsoCheck);
				}
			}
		}

		public static List<UserOrganizationModel> GetManagers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, params PermissionType[] alsoCheck) {
			perms.ViewUserOrganization(forUserId, false, alsoCheck);
			var forUser = s.Get<UserOrganizationModel>(forUserId);
			return forUser.ManagedBy
							.ToListAlive()
							.Select(x => x.Manager)
							.Where(x => x.Id != forUserId)
							.ToList();
		}
		
		public static List<long> WasAliveAt(ISession s, List<long> userOrgIds, DateTime time) {
			return s.QueryOver<UserOrganizationModel>()
				.WhereRestrictionOn(x => x.Id).IsIn(userOrgIds)
				.Where(x => (x.CreateTime <= time) && (x.DeleteTime == null || time <= x.DeleteTime))
				.Select(x => x.Id).List<long>().ToList();
		}
		
		public List<UserOrganizationModel> GetDirectSubordinates(UserOrganizationModel caller, long forId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetDirectSubordinates(s.ToQueryProvider(true), perms, forId);
				}
			}
		}

		public static List<UserOrganizationModel> GetDirectSubordinates(AbstractQuery s, PermissionsUtility perms, long forId) {
			perms.ViewUserOrganization(forId, false);
			//return DeepSubordianteAccessor.GetSubordinatesAndSelfModels(s, caller, forId);
			var forUser = s.Get<UserOrganizationModel>(forId);
			return forUser.ManagingUsers.ToListAlive()
							.Select(x => x.Subordinate)
							.Where(x => x.Id != forId)
							.ToListAlive();
		}


		private UserOrganizationModel GetUserOrganizationModel(ISession session, long id, Boolean full) {
			var result = session.Get<UserOrganizationModel>(id);
			return result;
		}

		public UserOrganizationModel GetUserOrganizations(ISession s, String userId, long userOrganizationId, String redirectUrl, Boolean full = false) {
			if (userId == null)
				throw new LoginException();
			var user = s.Get<UserModel>(userId);
			long matchId = -1;

			if (user == null || !user.IsRadialAdmin) {
				if (user == null)
					throw new LoginException();

				var match = s.Get<UserOrganizationModel>(userOrganizationId);
				if (match.DetachTime != null || match.DeleteTime != null || match.User == null || match.User.Id != userId)
					throw new OrganizationIdException(redirectUrl);

				matchId = match.Id;
			} else {
				matchId = userOrganizationId;
			}
			return GetUserOrganizationModel(s, matchId, full);
		}

		public void CreateUser(UserModel userModel) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					s.Save(userModel);
					tx.Commit();
					s.Flush();
				}
			}
		}
		
		public bool UpdateTempUser(UserOrganizationModel caller, long userOrgId, String firstName, String lastName, String email, DateTime? lastSent = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.Get<UserOrganizationModel>(userOrgId);
					if (found == null || found.DeleteTime != null)
						throw new PermissionsException("User does not exist.");
					var tempUser = found.TempUser;
					if (tempUser == null)
						throw new PermissionsException("User has already joined.");

					bool changed = false;

					if (tempUser.FirstName != firstName) {
						tempUser.FirstName = firstName;
						changed = true;
					}
					if (tempUser.LastName != lastName) {
						tempUser.LastName = lastName;
						changed = true;
					}

					if (tempUser.Email != email) {
						tempUser.Email = email;
						found.EmailAtOrganization = email;
						s.Update(found);
						changed = true;
					}

					if (lastSent != null) {
						tempUser.LastSent = lastSent.Value;
						changed = true;
					}

					if (changed) {
						PermissionsUtility.Create(s, caller).ManagesUserOrganization(userOrgId, false, PermissionType.EditEmployeeDetails);

						var guid = s.Get<NexusModel>(tempUser.Guid);
						if (guid != null) {
							var existingArg = guid.GetArgs();
							existingArg[1] = tempUser.Email;
							existingArg[3] = tempUser.FirstName;
							existingArg[4] = tempUser.LastName;
							guid.SetArgs(existingArg);
							s.Update(guid);
						}


						s.Update(tempUser);
						if (found != null)
							found.UpdateCache(s);
						tx.Commit();
						s.Flush();
					}

					return changed;
				}
			}
		}

		public void SetHints(UserModel caller, bool turnedOn) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var user = s.Get<UserModel>(caller.Id);
					user.Hints = turnedOn;
					s.Update(user);

					new Cache().Invalidate(CacheKeys.USERORGANIZATION);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public class EditUserResult {
			public bool? OverrideEvalOnly { get; set; }
			public bool? OverrideManageringOrganization { get; set; }
			public bool? OverrideIsManager { get; set; }

			public List<string> Errors { get; set; }

			public EditUserResult() {
				Errors = new List<string>();
			}
		}
		/// <summary>
		/// -3 for managerId sets the user as an organization manager
		/// </summary>
		public EditUserResult EditUser(UserOrganizationModel caller, long userOrganizationId, bool? isManager = null, bool? manageringOrganization = null, bool? evalOnly = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					var output = EditUserPermissionLevel(s, perm, userOrganizationId, isManager, manageringOrganization, evalOnly);
					tx.Commit();
					s.Flush();
					return output;
				}
			}
		}
		public static EditUserResult EditUserPermissionLevel(ISession s, PermissionsUtility perm, long userOrganizationId, bool? isManager = null, bool? manageringOrganization = null, bool? evalOnly = null) {
			var o = new EditUserResult();
			using (var rt = RealTimeUtility.Create()) {
				var found = s.Get<UserOrganizationModel>(userOrganizationId);
				var acId = found.Organization.AccountabilityChartId;
				perm.CanEdit(PermItem.ResourceType.AccountabilityHierarchy, acId);

				//if (manageringOrganization != null)
				//	perm.ManagesUserOrganization(userOrganizationId, false);
				//else
				//	perm.ManagesUserOrganization(userOrganizationId, false, PermissionType.ChangeEmployeePermissions);

				var deleteTime = DateTime.UtcNow;
				if (manageringOrganization != null && manageringOrganization.Value != found.ManagingOrganization) {
					if (found.Id == perm.GetCaller().Id) {
						o.OverrideManageringOrganization = found.ManagingOrganization;
						o.Errors.Add("You cannot unmanage this organization yourself.");
					} else {
						perm/*.ManagesUserOrganization(userOrganizationId, true)*/.ManagingOrganization(found.Organization.Id); // ! Changed the organization from callers, to found
						if (found.ManagingOrganization && !manageringOrganization.Value) {
							//maybe set manager to false
							if (!DeepAccessor.Users.HasChildren(s, perm, userOrganizationId)) {
								isManager = false;
								o.OverrideIsManager = false;
							}
						} else {
							//maybe set manager to true
							if (DeepAccessor.Users.HasChildren(s, perm, userOrganizationId)) {
								isManager = true;
								o.OverrideIsManager = true;
							}
						}
						found.ManagingOrganization = manageringOrganization.Value;
					}
				}

				if (isManager != null && (isManager.Value != found.ManagerAtOrganization)) {
					//perm.ManagesUserOrganization(userOrganizationId, false, PermissionType.ChangeEmployeePermissions);
					found.ManagerAtOrganization = isManager.Value;
					if (isManager == false) {
						var subordinatesTeams = s.QueryOver<OrganizationTeamModel>()
							.Where(x => x.Type == TeamType.Subordinates && x.ManagedBy == userOrganizationId && x.DeleteTime == null)
							.List();
						foreach (var subordinatesTeam in subordinatesTeams) {
							subordinatesTeam.DeleteTime = DateTime.UtcNow;
							s.Update(subordinatesTeam);
						}
					} else {
						var anyTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Type == TeamType.Subordinates && x.ManagedBy == userOrganizationId && x.DeleteTime == null).RowCount();
						if (anyTeams == 0) {
							s.Save(OrganizationTeamModel.SubordinateTeam(perm.GetCaller(), found));
							s.Flush();
						}
					}
				}

				if (evalOnly != null) {
					perm.CanUpgradeUser(found.Id);
					var anyMeetings = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == found.Id).RowCount();
					if (anyMeetings == 0 || evalOnly.Value == false) {
						found.EvalOnly = evalOnly.Value;
					} else {
						o.OverrideEvalOnly = found.EvalOnly;
						o.Errors.Add("Could not convert to " + Config.ReviewName() + " only. Remove user from L10 meetings first.");
					}
				}

				s.Update(found);
				found.UpdateCache(s);
			}
			return o;
		}

		public int CreateDeepSubordinateTree(UserOrganizationModel caller, long organizationId, DateTime now) {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).RadialAdmin();

					var existing = s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List();
					foreach (var e in existing) {
						e.DeleteTime = now;
						s.Update(e);
					}

					var allManagers = s.QueryOver<AccountabilityNode>()
										//.JoinQueryOver(x => x.Manager)
										.Where(x => x.OrganizationId == organizationId)
										.List()
										.ToListAlive();

					var allIds = allManagers.Select(x => x.Id).ToList();//.SelectMany(x => x.ParentNodeId.AsList(x.)).Distinct().ToList();

					foreach (var id in allIds) {
						var found = s.QueryOver<DeepAccountability>().Where(x => x.ChildId == id && x.ParentId == id).List().ToListAlive();
						if (!found.Any()) {
							count++;
							s.Save(new DeepAccountability() { Links = 1, CreateTime = now, ParentId = id, ChildId = id, OrganizationId = organizationId });
						}
					}


					foreach (var manager in allManagers.Distinct(x => x.Id)) {
						var managerSubordinates = s.QueryOver<DeepAccountability>()
							.Where(x => x.DeleteTime == null && x.ParentId == manager.Id)
							.List().ToList();
						var allSubordinates = DeepAccessor.Dive.GetSubordinates(s, manager);

						foreach (var sub in allSubordinates) {
							var found = managerSubordinates.FirstOrDefault(x => x.ChildId == sub.Id);
							if (found == null) {
								found = new DeepAccountability() {
									CreateTime = now,
									ParentId = manager.Id,
									ChildId = sub.Id,
									Links = 0,
									OrganizationId = organizationId,
								};
							}
							found.Links += 1;
							count++;
							s.SaveOrUpdate(found);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count;
		}
		
		[Obsolete("This is old. Only used for testing.")]
		public static void AddManager(ISession s, PermissionsUtility perms, long userId, long managerId, DateTime now, bool ignoreCircular = false) {

			perms.ManagesUserOrganization(userId, true, PermissionType.EditEmployeeManagers)
				 .ManagesUserOrganization(managerId, false, PermissionType.EditEmployeeManagers);

			AddMangerUnsafe(s, perms.GetCaller(), userId, managerId, now, ignoreCircular);
		}
		
		[Obsolete("This is old. Only used for testing.")]
		public void AddManager(UserOrganizationModel caller, long userId, long managerId, DateTime now) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					AddManager(s, perms, userId, managerId, now);

					tx.Commit();
					s.Flush();
				}
			}
		}

		[Obsolete("This is old. Only used for testing.")]
		private static void AddMangerUnsafe(ISession s, UserOrganizationModel caller, long userId, long managerId, DateTime now, bool ignoreCircular = false) {
			var user = s.Get<UserOrganizationModel>(userId);
			var manager = s.Get<UserOrganizationModel>(managerId);
			var managerNull = user.ManagedBy.ToListAlive().Where(x => x.ManagerId == managerId).FirstOrDefault();

			if (managerNull != null)
				throw new PermissionsException(manager.GetName() + " is already a " + Config.ManagerName() + " for this user.");

			if (!manager.IsManager())
				throw new PermissionsException(manager.GetName() + " is not a " + Config.ManagerName() + ".");

			//DeepSubordianteAccessor.Add(s, manager, user, manager.Organization.Id, now, ignoreCircular);
			user.ManagedBy.Add(new ManagerDuration(managerId, userId, caller.Id) {
				CreateTime = now,
				Manager = manager,
				Subordinate = user
			});

			s.Update(user);
			user.UpdateCache(s);
			//s.Update(new DeepSubordinateModel(){ManagerId=manager.Id}

		}
		
		public void ChangeRole(UserModel caller, UserOrganizationModel callerUserOrg, long roleId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					caller = s.Get<UserModel>(caller.Id);
					if ((caller != null && caller.IsRadialAdmin) || (callerUserOrg != null && callerUserOrg.IsRadialAdmin) || caller.UserOrganizationIds.Any(x => x == roleId))
						caller.CurrentRole = roleId;
					else
						throw new PermissionsException();
					s.Update(caller);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public async Task EditUserModel(UserModel caller, string userId, string firstName, string lastName, string imageGuid, bool? sendTodoEmails, int? sendTodoTime, bool? showScorecardColors, bool? reverseScorecard, bool? disableTips) {
			UserModel user;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (caller.Id != userId)
						throw new PermissionsException();
					user = s.Get<UserModel>(userId);
					if (firstName != null)
						user.FirstName = firstName;
					if (lastName != null)
						user.LastName = lastName;
					if (imageGuid != null) {
						user.ImageGuid = imageGuid;
					}
					if (sendTodoEmails != null) {
						user.SendTodoTime = sendTodoEmails.Value ? sendTodoTime : null;
					}
					if (showScorecardColors != null) {
						var us = s.Get<UserStyleSettings>(userId);
						us.ShowScorecardColors = showScorecardColors.Value;
						s.Update(us);
					}
					if (reverseScorecard != null) {
						user.ReverseScorecard = reverseScorecard.Value;
					}
					if (disableTips != null) {
						user.DisableTips = disableTips.Value;
					}


					new Cache().Invalidate(CacheKeys.USER);

					if (user.UserOrganization != null) {
						foreach (var u in user.UserOrganization) {
							if (u != null)
								u.UpdateCache(s);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await HooksRegistry.Each<IUpdateUserModelHook>((ses, x) => x.UpdateUserModel(ses, user));
					tx.Commit();
					s.Flush();
				}
			}

		}

		public List<String> SideEffectRemove(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).RemoveUser(userId);
					var user = s.Get<UserOrganizationModel>(userId);
					var warnings = new List<String>();
					//managed teams
					var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId && x.DeleteTime == null).List().ToList();
					foreach (var m in managedTeams) {
						if (m.Type != TeamType.Subordinates) {
							warnings.Add("The team, " + m.GetName() + " is managed by" + user.GetFirstName() + ". You will be promoted to " + Config.ManagerName() + " of this team.");
						}
					}
					var subordinates = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == userId && x.DeleteTime == null).List().ToList();
					foreach (var subordinate in subordinates) {
						warnings.Add(user.GetFirstName() + " manages " + subordinate.Subordinate.GetName() + ".");
					}

					return warnings;
				}
			}
		}

		public async Task<ResultObject> UndeleteUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).RemoveUser(userId);
					var user = s.Get<UserOrganizationModel>(userId);
					if (user.DeleteTime == null)
						throw new PermissionsException("Could not undelete");

					var deleteTime = user.DeleteTime.Value;

					user.DeleteTime = null;
					user.DetachTime = null;

					if (user.User != null) {
						var newArray = user.User.UserOrganizationIds.ToList();
						//if (newArray.Any(userId))
						//    throw new PermissionsException("User does not exist.");
						newArray.Add(userId);
						user.User.UserOrganizationIds = newArray.ToArray();
					}


					var tempUser = user.TempUser;
					if (tempUser != null) {
						//s.Delete(tempUser);
					}

					var warnings = new List<String>();
					//new management structure
					DeepAccessor.Users.UndeleteAll(s, user, deleteTime, ref warnings);
					//old management structure
					var asSubordinate = s.QueryOver<ManagerDuration>().Where(x => x.SubordinateId == userId && x.DeleteTime == deleteTime).List().ToList();
					foreach (var sub in asSubordinate) {
						sub.DeletedBy = caller.Id;
						sub.DeleteTime = null;
						s.Update(sub);
						//sub.Subordinate.UpdateCache(s);
						if (sub.Manager != null)
							sub.Manager.UpdateCache(s);
					}
					var subordinates = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == userId && x.DeleteTime == deleteTime).List().ToList();
					foreach (var subordinate in subordinates) {
						subordinate.DeletedBy = caller.Id;
						subordinate.DeleteTime = null;
						s.Update(subordinate);
						if (subordinate.Subordinate != null)
							subordinate.Subordinate.UpdateCache(s);
						//subordinate.Manager.UpdateCache(s);

						//warnings.Add(user.GetFirstName() + " no longer manages " + subordinate.Subordinate.GetNameAndTitle() + ".");
					}
					//teams
					var teams = s.QueryOver<TeamDurationModel>().Where(x => x.UserId == userId && x.DeleteTime == deleteTime).List().ToList();
					foreach (var t in teams) {
						t.DeletedBy = caller.Id;
						t.DeleteTime = null;
						s.Update(t);
						//subordinate.Subordinate.UpdateCache(s);
						//subordinate.Manager.UpdateCache(s);
					}
					//managed teams
					var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId && x.DeleteTime == deleteTime).List().ToList();
					foreach (var m in managedTeams) {
						//if (m.Type != TeamType.Subordinates) {
						//	m.ManagedBy = caller.Id;
						//	m.DeleteTime = null;
						//	s.Update(m);
						//	warnings.Add("You now manage the team: " + m.GetName() + ".");
						//} else {
						//teams
						var subordinateTeam = s.QueryOver<TeamDurationModel>().Where(x => x.TeamId == m.Id && x.DeleteTime == deleteTime).List().ToList();
						foreach (var t in subordinateTeam) {
							t.DeletedBy = caller.Id;
							t.DeleteTime = null;
							s.Update(t);
						}

						m.DeleteTime = null;
						s.Update(m);
						//}
					}

					var attendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
						.Where(x => x.User.Id == userId && x.DeleteTime == deleteTime)
						.List().ToList();

					foreach (var f in attendees) {
						f.DeleteTime = null;
						s.Update(f);
					}
					var meetingAttendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
					.Where(x => x.User.Id == userId && x.DeleteTime == deleteTime)
					.List().ToList();

					foreach (var f in meetingAttendees) {
						f.DeleteTime = null;
						s.Update(f);
					}

					s.Update(user);
					user.UpdateCache(s);

					await HooksRegistry.Each<IDeleteUserOrganizationHook>((ses, x) => x.UndeleteUser(ses, user));
					tx.Commit();
					s.Flush();

					if (warnings.Count() == 0) {
						return ResultObject.CreateMessage(StatusType.Success, "Successfully re-added " + user.GetFirstName() + ".");
					} else {
						return ResultObject.CreateMessage(StatusType.Warning, "Successfully re-added " + user.GetFirstName() + ".<br/><b>Warning:</b><br/>" + string.Join("<br/>", warnings));
					}
				}
			}
		}

		public async Task<ResultObject> RemoveUser(UserOrganizationModel caller, long userId, DateTime now) {
			UserOrganizationModel user;
			var warnings = new List<String>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).RemoveUser(userId);
					user = s.Get<UserOrganizationModel>(userId);
					user.DetachTime = now;
					user.DeleteTime = now;

					if (user.User != null) {
						var newArray = user.User.UserOrganizationIds.ToList();
						if (!newArray.Remove(userId))
							throw new PermissionsException("User does not exist.");
						user.User.UserOrganizationIds = newArray.ToArray();
					}
					var tempUser = user.TempUser;
					if (tempUser != null) {
						//s.Delete(tempUser);
					}

					//new management structure
					DeepAccessor.Users.DeleteAll(s, user, now);
					//old management structure
					var asSubordinate = s.QueryOver<ManagerDuration>().Where(x => x.SubordinateId == userId && x.DeleteTime == null).List().ToList();
					foreach (var sub in asSubordinate) {
						sub.DeletedBy = caller.Id;
						sub.DeleteTime = now;
						s.Update(sub);
						//sub.Subordinate.UpdateCache(s);
						if (sub.Manager != null)
							sub.Manager.UpdateCache(s);
					}
					var subordinates = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == userId && x.DeleteTime == null).List().ToList();
					foreach (var subordinate in subordinates) {
						subordinate.DeletedBy = caller.Id;
						subordinate.DeleteTime = now;
						s.Update(subordinate);
						if (subordinate.Subordinate != null)
							subordinate.Subordinate.UpdateCache(s);
						//subordinate.Manager.UpdateCache(s);

						warnings.Add(user.GetFirstName() + " no longer manages " + subordinate.Subordinate.GetNameAndTitle() + ".");
					}
					//teams
					var teams = s.QueryOver<TeamDurationModel>().Where(x => x.UserId == userId && x.DeleteTime == null).List().ToList();
					foreach (var t in teams) {
						t.DeletedBy = caller.Id;
						t.DeleteTime = now;
						s.Update(t);
						//subordinate.Subordinate.UpdateCache(s);
						//subordinate.Manager.UpdateCache(s);
					}
					//managed teams
					var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId && x.DeleteTime == null).List().ToList();
					foreach (var m in managedTeams) {
						if (m.Type != TeamType.Subordinates) {
							m.ManagedBy = caller.Id;
							s.Update(m);
							warnings.Add("You now manage the team: " + m.GetName() + ".");
						} else {
							//teams
							var subordinateTeam = s.QueryOver<TeamDurationModel>().Where(x => x.TeamId == m.Id && x.DeleteTime == null).List().ToList();
							foreach (var t in subordinateTeam) {
								t.DeletedBy = caller.Id;
								t.DeleteTime = now;
								s.Update(t);
							}

							m.DeleteTime = now;
							s.Update(m);
						}
					}


					var l10Attendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.User.Id == userId && x.DeleteTime == null).List().ToList();
					foreach (var m in l10Attendee) {
						m.DeleteTime = now;
						s.Update(m);
					}

					var l10MeetingAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.User.Id == userId && x.DeleteTime == null)
						.List().ToList();
					foreach (var m in l10MeetingAttendee.OrderByDescending(x => x.Id).GroupBy(x => x.Id).Select(x => x.First())) {
						m.DeleteTime = now;
						s.Update(m);
					}
					s.Update(user);
					user.UpdateCache(s);

					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await HooksRegistry.Each<IDeleteUserOrganizationHook>((ses, x) => x.DeleteUser(ses, user));
					tx.Commit();
					s.Flush();
				}
			}
			if (warnings.Count() == 0) {
				return ResultObject.CreateMessage(StatusType.Success, "Successfully removed " + user.GetFirstName() + ".");
			} else {
				return ResultObject.CreateMessage(StatusType.Warning, "Successfully removed " + user.GetFirstName() + ".<br/><b>Warning:</b><br/>" + string.Join("<br/>", warnings));
			}
		}

		public void EditJobDescription(UserOrganizationModel caller, long userId, string jobDescription) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditQuestionForUser(userId);
					var user = s.Get<UserOrganizationModel>(userId);
					if (user.JobDescription != jobDescription) {
						user.JobDescription = jobDescription;
						user.JobDescriptionFromTemplateId = null;
						s.Update(user);
						user.UpdateCache(s);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}

		public async Task<System.Tuple<string, UserOrganizationModel>> CreateUser(UserOrganizationModel caller, CreateUserOrganizationViewModel model) {
			var _OrganizationAccessor = new OrganizationAccessor();
			var _NexusAccessor = new NexusAccessor();
			UserOrganizationModel createdUser = null;
			var user = caller.Hydrate().Organization().Execute();
			var org = user.Organization;
			if (org == null)
				throw new PermissionsException();
			if (org.Id != model.OrgId)
				throw new PermissionsException();

			if (model.Position != null && model.Position.CustomPosition != null) {
				var newPosition = _OrganizationAccessor.EditOrganizationPosition(user, 0, user.Organization.Id, /*model.Position.CustomPositionId,*/ model.Position.CustomPosition);
				model.Position.PositionId = newPosition.Id;
			}

			var nexusIdandUser = await JoinOrganizationAccessor.JoinOrganizationUnderManager(user, model);
			createdUser = nexusIdandUser.Item2;
			var nexusId = nexusIdandUser.Item1;

			return nexusIdandUser;
		}

		public static List<TinyUser> Search(UserOrganizationModel caller, long orgId, string search, int take = int.MaxValue, long[] exclude = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewOrganization(orgId);

					var users = TinyUserAccessor.GetOrganizationMembers(s, perms, orgId);

					exclude = exclude ?? new long[0];

					users = users.Where(x => !exclude.Any(y => y == x.UserOrgId)).ToList();

					var splits = search.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

					var dist = new DiscreteDistribution<TinyUser>(0, 7, true);

					foreach (var u in users) {
						var fname = false;
						var lname = false;
						var ordered = false;
						var fnameStart = false;
						var lnameStart = false;
						var wasFirst = false;
						var exactFirst = false;
						var exactLast = false;

						var f = u.FirstName.ToLower();
						var l = u.LastName.ToLower();
						foreach (var t in splits) {
							if (f.Contains(t))
								fname = true;
							if (f == t)
								exactFirst = true;
							if (f.StartsWith(t))
								fnameStart = true;
							if (l.Contains(t))
								lname = true;
							if (l.StartsWith(t))
								lnameStart = true;
							if (fname && !wasFirst && lname)
								ordered = true;
							if (l == t)
								exactLast = true;
							wasFirst = true;
						}

						var score = fname.ToInt() + lname.ToInt() + ordered.ToInt() + fnameStart.ToInt() + lnameStart.ToInt() + exactFirst.ToInt() + exactLast.ToInt();
						if (score > 0)
							dist.Add(u, score);
					}

					return dist.GetProbabilities().OrderByDescending(x => x.Value).Select(x => x.Key).Take(take).ToList();
				}
			}
		}


		public async static Task<IdentityResult> CreateUser(NHibernateUserManager UserManager, UserModel user, ExternalLoginInfo info) {
			var result = await UserManager.CreateAsync(user);
			if (result.Succeeded) {
				result = await UserManager.AddLoginAsync(user.Id, info.Login);
				AddSettings(result, user);
			}
			return result;
		}

		public async static Task<IdentityResult> CreateUser(NHibernateUserManager UserManager, UserModel user, string password) {
			user.UserName = user.UserName.NotNull(x => x.ToLower());
			var resultx = await UserManager.CreateAsync(user, password);
			AddSettings(resultx, user);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.OnUserRegister(ses, user));
					tx.Commit();
					s.Flush();
				}
			}

			return resultx;
		}

		private static void AddSettings(IdentityResult result, UserModel user) {
			if (result.Succeeded) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var settings = new UserStyleSettings() {
							Id = user.Id,
							ShowScorecardColors = true
						};
						s.Save(settings);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static string GetStyles(string userModel) {
			var builder = new StringBuilder();
			UserStyleSettings styles = null;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					styles = s.Get<UserStyleSettings>(userModel);
				}
			}

			if (styles != null) {
				if (styles.ShowScorecardColors == false) {
					builder.AppendLine(".scorecard-table .score .success,.scorecard-table .score .danger,.scorecard-table .score.success input, .scorecard-table .score input.success, .scorecard-table .score.success{color: inherit !important;background-color: inherit !important;}");
				}
			}

			return builder.ToString();
		}


		public static List<UserRole> GetUserRolesAtOrganization(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					return s.QueryOver<UserRole>().Where(x => x.DeleteTime == null && x.OrgId == orgId).List().ToList();
				}
			}
		}

		public static async Task SetRole(UserOrganizationModel caller, long userId, UserRoleType type, bool enabled) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					if (enabled) {
						await AddRole(s, perms, userId, type);
					} else {
						await RemoveRole(s, perms, userId, type);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task RemoveRole(UserOrganizationModel caller, long userId, UserRoleType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await RemoveRole(s, perms, userId, type);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task AddRole(UserOrganizationModel caller, long userId, UserRoleType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await AddRole(s, perms, userId, type);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task AddRole(ISession s, PermissionsUtility perms, long userId, UserRoleType type) {
			perms.ViewUserOrganization(userId, false);
			var any = s.QueryOver<UserRole>().Where(x => x.UserId == userId && type == x.RoleType && x.DeleteTime == null).RowCount();
			var user = s.Get<UserOrganizationModel>(userId);
			if (any == 0) {
				s.Save(new UserRole() {
					OrgId = user.Organization.Id,
					RoleType = type,
					UserId = userId,
				});
				await HooksRegistry.Each<IUserRoleHook>((ses, x) => x.AddRole(ses, userId, type));
			}
		}

		public static async Task RemoveRole(ISession s, PermissionsUtility perms, long userId, UserRoleType type) {
			perms.ViewUserOrganization(userId, false);
			var any = s.QueryOver<UserRole>().Where(x => x.UserId == userId && type == x.RoleType && x.DeleteTime == null).List().ToList();
			var user = s.Get<UserOrganizationModel>(userId);
			if (any.Count > 0) {
				foreach (var a in any) {
					a.DeleteTime = DateTime.UtcNow;
					s.Update(a);
				}
				await HooksRegistry.Each<IUserRoleHook>((ses, x) => x.RemoveRole(ses, userId, type));
			}
		}

		#region Deleted
		//[Obsolete("Fix for AC")]
		//public static List<UserOrganizationModel> GetPeers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, Reviewer reviewer) {
		//	throw new NotImplementedException();
		//}
		//[Obsolete("Fix for AC",true)]
		//public static List<UserOrganizationModel> GetPeers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, Reviewee reviewee) {
		//	throw new NotImplementedException();
		//}
		//[Obsolete("Fix for AC", true)]
		//public static List<UserOrganizationModel> GetManagers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, Reviewee reviewee) {
		//	throw new NotImplementedException();
		//}
		//[Obsolete("Fix for AC", true)]
		//public static List<UserOrganizationModel> GetDirectSubordinates(AbstractQuery s, PermissionsUtility perms, Reviewee reviewee) {
		//	throw new NotImplementedException();
		//}

		//[Obsolete("Cannot remove manager like this", true)]
		//public void RemoveManager(UserOrganizationModel caller, long userId, long managerId, DateTime now) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var perms = PermissionsUtility.Create(s, caller);
		//			var managerDuration = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.SubordinateId == userId && x.ManagerId == managerId).Take(1).SingleOrDefault();
		//			RemoveManager(s, perms, caller, managerDuration.Id, now);
		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}
		//[Obsolete("Cannot remove manager like this", true)]
		//public static void RemoveManager(ISession s, PermissionsUtility perms, long userId, long managerId, DateTime now) {
		//	var managerDuration = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.SubordinateId == userId && x.ManagerId == managerId).Take(1).SingleOrDefault();
		//	if (managerDuration != null) {
		//		RemoveManager(s, perms, perms.GetCaller(), managerDuration.Id, now);
		//	} else {
		//		log.Error("manager duration does not exist " + userId + " " + managerId + " " + now.ToString());
		//	}

		//}
		//[Obsolete("Cannot remove manager like this", true)]
		//public static void RemoveManager(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long managerDurationId, DateTime now) {
		//	var managerDuration = s.Get<ManagerDuration>(managerDurationId);
		//	perms.ManagesUserOrganization(managerDuration.SubordinateId, true, PermissionType.EditEmployeeManagers).ManagesUserOrganization(managerDuration.ManagerId, false, PermissionType.EditEmployeeManagers);
		//	RemoveMangerUnsafe(s, caller, managerDuration, now);
		//}
		//[Obsolete("This is old. Only used for testing.", true)]
		//private static void RemoveMangerUnsafe(ISession s, UserOrganizationModel caller, ManagerDuration managerDuration, DateTime now) {
		//	//DeepSubordianteAccessor.Remove(s, managerDuration.Manager, managerDuration.Subordinate, now);
		//	managerDuration.DeletedBy = caller.Id;
		//	managerDuration.DeleteTime = now;
		//	s.Update(managerDuration);
		//	managerDuration.Subordinate.UpdateCache(s);
		//	managerDuration.Manager.UpdateCache(s);
		//}
		//[Obsolete("This is old. Only used for testing.", true)]
		//public void RemoveManager(UserOrganizationModel caller, long managerDurationId, DateTime now) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var perms = PermissionsUtility.Create(s, caller);
		//			RemoveManager(s, perms, caller, managerDurationId, now);
		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}
		//[Obsolete("Cannot remove manager like this", true)]
		//public void SwapManager(UserOrganizationModel caller, long userId, long oldManagerId, long newManagerId, DateTime now) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			PermissionsUtility.Create(s, caller)
		//				.ManagesUserOrganization(userId, true, PermissionType.EditEmployeeManagers)
		//				.ManagesUserOrganization(oldManagerId, false, PermissionType.EditEmployeeManagers)
		//				.ManagesUserOrganization(newManagerId, false, PermissionType.EditEmployeeManagers);

		//			var managerDuration = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.SubordinateId == userId && x.ManagerId == oldManagerId).Take(1).SingleOrDefault();

		//			if (managerDuration == null)
		//				throw new PermissionsException();

		//			RemoveMangerUnsafe(s, caller, managerDuration, now);
		//			AddMangerUnsafe(s, caller, userId, newManagerId, now);

		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}

		#endregion
	}
}
