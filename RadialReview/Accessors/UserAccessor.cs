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

namespace RadialReview.Accessors {

    public class UserAccessor : BaseAccessor {
        public String GetUserIdByUsername(ISession s, String username)
        {
            return (string)CacheLookup.GetOrAddDefault("username_" + username, x => {
                return GetUserByEmail(s, username).Id;
            });
        }
        public String GetUserNameByUserOrganizationId(long userOrgId)
        {
            return (string)CacheLookup.GetOrAddDefault("userorgid_" + userOrgId, x => {
                return GetUserOrganizationUnsafe(userOrgId).User.UserName;
            });
        }

        public UserModel GetUserById(String userId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return GetUserById(s, userId);
                }
            }
        }

        public UserModel GetUserById(ISession s, String userId)
        {
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


        public List<UserModel> GetUsersByIds(IEnumerable<String> userIds)
        {
            if (userIds == null)
                throw new LoginException();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return s.QueryOver<UserModel>().WhereRestrictionOn(x => x.Id).IsIn(userIds.ToArray()).List().ToList();
                }
            }
        }

        [Obsolete("Dont use this elsewhere", false)]
        public UserModel GetUserByEmail(ISession s, String email)
        {
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
        [Obsolete("Dont use this elsewhere", false)]
        public UserModel GetUserByEmail(String email)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return GetUserByEmail(s, email);
                }
            }
        }


        [Obsolete("Dont use this, its unsafe", false)]
        public UserOrganizationModel GetUserOrganizationUnsafe(long userOrganizationId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return s.Get<UserOrganizationModel>(userOrganizationId);
                }
            }
        }

        public static UserOrganizationModel GetUserOrganization(ISession s, PermissionsUtility perms, long userOrganizationId, bool asManager, bool sensitive, params PermissionType[] alsoCheck)
        {
            return GetUserOrganization(s.ToQueryProvider(true), perms, perms.GetCaller(), userOrganizationId, asManager, sensitive, alsoCheck);
        }

        public UserOrganizationModel GetUserOrganization(UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive, params PermissionType[] alsoCheck)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetUserOrganization(s.ToQueryProvider(true), perms, caller, userOrganizationId, asManager, sensitive, alsoCheck);
                }
            }
        }

        public static UserOrganizationModel GetUserOrganization(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive, params PermissionType[] alsoCheck)
        {
            perms.ViewUserOrganization(userOrganizationId, sensitive, alsoCheck);
            if (asManager) {
                perms.ManagesUserOrganization(userOrganizationId, false, alsoCheck);
            }
            return s.Get<UserOrganizationModel>(userOrganizationId);

        }

        public List<UserOrganizationModel> GetUserOrganizations(ISession s, String userId, String redirectUrl, Boolean full = false)
        {
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

        public int GetUserOrganizationCounts(ISession s, String userId, String redirectUrl, Boolean full = false)
        {
            if (userId == null)
                throw new LoginException(redirectUrl);
            var user = s.Get<UserModel>(userId);
            if (user == null)
                throw new LoginException(redirectUrl);
            return user.UserOrganizationCount;
        }

        public List<UserOrganizationModel> GetPeers(UserOrganizationModel caller, long forId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetPeers(s.ToQueryProvider(true), perms, caller, forId);
                }
            }
        }
        public static List<UserOrganizationModel> GetPeers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forId)
        {
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

        public List<UserOrganizationModel> GetManagers(UserOrganizationModel caller, long forUserId, params PermissionType[] alsoCheck)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetManagers(s.ToQueryProvider(true), perms, caller, forUserId, alsoCheck);
                }
            }
        }
        public static List<UserOrganizationModel> GetManagers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, params PermissionType[] alsoCheck)
        {
            perms.ViewUserOrganization(forUserId, false, alsoCheck);
            var forUser = s.Get<UserOrganizationModel>(forUserId);
            return forUser.ManagedBy
                            .ToListAlive()
                            .Select(x => x.Manager)
                            .Where(x => x.Id != forUserId)
                            .ToList();
        }


        public static List<long> WasAliveAt(ISession s, List<long> userOrgIds, DateTime time)
        {
            return s.QueryOver<UserOrganizationModel>()
                .WhereRestrictionOn(x => x.Id).IsIn(userOrgIds)
                .Where(x => (x.CreateTime <= time) && (x.DeleteTime == null || time <= x.DeleteTime))
                .Select(x => x.Id).List<long>().ToList();
        }



        public List<UserOrganizationModel> GetDirectSubordinates(UserOrganizationModel caller, long forId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetDirectSubordinates(s.ToQueryProvider(true), perms, forId);
                }
            }
        }

        public static List<UserOrganizationModel> GetDirectSubordinates(AbstractQuery s, PermissionsUtility perms, long forId)
        {
            perms.ViewUserOrganization(forId, false);
            //return DeepSubordianteAccessor.GetSubordinatesAndSelfModels(s, caller, forId);
            var forUser = s.Get<UserOrganizationModel>(forId);
            return forUser.ManagingUsers.ToListAlive()
                            .Select(x => x.Subordinate)
                            .Where(x => x.Id != forId)
                            .ToListAlive();
        }


        private UserOrganizationModel GetUserOrganizationModel(ISession session, long id, Boolean full)
        {
            /*if (full)
            {*/
            //var result = session.Query<UserOrganizationModel>().Where(x => x.Id == id);
            ////result.FetchMany(x => x.ManagingGroups).ToFuture();
            ////result.FetchMany(x => x.ManagingUsers).ToFuture();
            //result.FetchMany(x => x.Groups).ToFuture();
            //result.FetchMany(x => x.ManagedBy).ToFuture();
            ////result.FetchMany(x => x.CustomQuestions).ToFuture();
            //result.FetchMany(x => x.CreatedNexuses).ToFuture();
            //result.Fetch(x => x.Organization).ToFuture();
            //result.Fetch(x => x.User).ToFuture();

            var result = session.Get<UserOrganizationModel>(id);
            /*if (result.User!=null)
                result.User = session.Get<UserModel>(result.User.Id);
            result.Organization = session.Get<OrganizationModel>(result.Organization.Id);
            */
            return result;//.AsEnumerable().SingleOrDefault();


            /*return db.UserOrganizationModels
                      .Include(x => x.ManagingGroups.Select(y => y.GroupUsers))
                      .Include(x => x.ManagingUsers.Select(y => y.User))
                      .Include(x => x.Groups.Select(y=>y.GroupUsers))
                      .Include(x => x.ManagedBy.Select(y => y.User))
                      .Include(x => x.BelongingToOrganizations)
                      .Include(x => x.ManagerAtOrganization)
                      .Include(x => x.ManagingOrganizations)
                      .Include(x => x.CustomQuestions)
                      .Include(x => x.CreatedNexuses)
                      .Include(x => x.Organization)
                      .Include(x => x.User)
                      .FirstOrDefault(x => x.Id == id);*/
            /*} else {

                session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .FetchMany(x => x.CustomQuestions).ToFuture();
                session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .FetchMany(x => x.CreatedNexuses).ToFuture();
                var result = session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .FetchMany(x => x.ManagingGroups).ToFuture();
                session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .FetchMany(x => x.ManagingUsers).ToFuture();
                session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .FetchMany(x => x.ManagedBy).ToFuture();
                session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .FetchMany(x => x.Groups).ToFuture();
                session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .Fetch(x => x.Organization).ToFuture();
                session.Query<UserOrganizationModel>().Where(x => x.Id == id)
                    .Fetch(x => x.User).ToFuture();
                return result.AsEnumerable().SingleOrDefault();*/
            /*return db.UserOrganizationModels
                      .Include(x => x.BelongingToOrganizations)
                      .Include(x => x.ManagerAtOrganization)
                      .Include(x => x.ManagingOrganizations)
                      .Include(x => x.CustomQuestions)
                      .Include(x => x.CreatedNexuses)
                      .Include(x => x.ManagingGroups)
                      .Include(x => x.ManagingUsers)
                      .Include(x => x.ManagedBy)
                      .Include(x => x.Groups)
                      .Include(x => x.Organization)
                      .Include(x => x.User)
                      .FirstOrDefault(x => x.Id == id);
        }*/
        }

        public UserOrganizationModel GetUserOrganizations(ISession s, String userId, long userOrganizationId, String redirectUrl, Boolean full = false)
        {
            if (userId == null)
                throw new LoginException();
            //using (var s = HibernateSession.GetCurrentSession())
            //{
            //	using (var tx = s.BeginTransaction())
            //	{
            var user = s.Get<UserModel>(userId);
            /*.FetchMany(x => x.UserOrganization)
            .ThenFetch(x => x.Organization)
            .SingleOrDefault();*/

            //                    var user = db.UserModels.Include(x => x.UserOrganization.Select(y => y.Organization)).FirstOrDefault(x => x.IdMapping == userId);
            long matchId = -1;

            if (user == null || !user.IsRadialAdmin) {
                if (user == null)
                    throw new LoginException();

                var match = s.Get<UserOrganizationModel>(userOrganizationId);
                if (match.DetachTime != null || match.DeleteTime != null || match.User == null || match.User.Id != userId)
                    throw new OrganizationIdException(redirectUrl);

                /*var match = user.UserOrganization.SingleOrDefault(x => x.Id == userOrganizationId && x.DetachTime == null && x.DeleteTime == null);
                if (match == null)
                {
                    throw new OrganizationIdException(redirectUrl);
                }*/
                matchId = match.Id;
            } else {
                matchId = userOrganizationId;
            }
            return GetUserOrganizationModel(s, matchId, full);
            //	}
            //}
        }

        public void CreateUser(UserModel userModel)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    s.Save(userModel);
                    tx.Commit();
                    s.Flush();
                    //db.UserModels.Add(userModel);
                    //db.SaveChanges();
                }
            }
        }


        public bool UpdateTempUser(UserOrganizationModel caller, long userOrgId, String firstName, String lastName, String email, DateTime? lastSent = null)
        {
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

        public void SetHints(UserModel caller, bool turnedOn)
        {
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
        /// <summary>
        /// -3 for managerId sets the user as an organization manager
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="userOrganizationId"></param>
        /// <param name="isManager"></param>
        /// <param name="managerId"></param>
        public void EditUser(UserOrganizationModel caller, long userOrganizationId, bool? isManager = null, bool? manageringOrganization = null)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perm = PermissionsUtility.Create(s, caller);

                    if (manageringOrganization != null)
                        perm.ManagesUserOrganization(userOrganizationId, false);
                    else
                        perm.ManagesUserOrganization(userOrganizationId, false, PermissionType.ChangeEmployeePermissions);

                    var found = s.Get<UserOrganizationModel>(userOrganizationId);

                    var deleteTime = DateTime.UtcNow;

                    if (isManager != null && (isManager.Value != found.ManagerAtOrganization)) {
                        perm.ManagesUserOrganization(userOrganizationId, false, PermissionType.ChangeEmployeePermissions);
                        found.ManagerAtOrganization = isManager.Value;
                        if (isManager == false) {
                            foreach (var m in found.ManagingUsers.ToListAlive()) {
                                DeepSubordianteAccessor.Remove(s, m.Manager, m.Subordinate, deleteTime);
                                m.DeleteTime = deleteTime;
                                m.DeletedBy = caller.Id;
                                s.Update(m);
                                if (m.Subordinate != null)
                                    m.Subordinate.UpdateCache(s);
                                if (m.Manager != null)
                                    m.Manager.UpdateCache(s);
                            }
                            var subordinatesTeam = s.QueryOver<OrganizationTeamModel>()
                                .Where(x => x.Type == TeamType.Subordinates && x.ManagedBy == userOrganizationId)
                                .SingleOrDefault();
                            if (subordinatesTeam != null)
                                s.Delete(subordinatesTeam);
                        } else {
                            s.Save(OrganizationTeamModel.SubordinateTeam(caller, found));
                        }
                    }

                    if (manageringOrganization != null && manageringOrganization.Value != found.ManagingOrganization) {
                        if (found.Id == caller.Id)
                            throw new PermissionsException("You cannot unmanage this organization yourself.");

                        perm.ManagesUserOrganization(userOrganizationId, true).ManagingOrganization(caller.Organization.Id);
                        found.ManagingOrganization = manageringOrganization.Value;
                        /*
                        if (managerId.Value == -3)
                        {
                            perm.ManagingOrganization();
                            found.ManagingOrganization = true;
                        }
                        else
                        {
                            var manager = s.Get<UserOrganizationModel>(managerId.Value);
                            foreach (var m in found.ManagedBy.ToListAlive())
                            {
                                m.DeletedBy = caller.Id;
                                m.DeleteTime = deleteTime;
                                s.Update(m);
                            }
                            found.ManagedBy.Add(new ManagerDuration(managerId.Value, found.Id, caller.Id));
                        }*/
                    }

                    s.Update(found);
                    found.UpdateCache(s);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public int CreateDeepSubordinateTree(UserOrganizationModel caller, long organizationId, DateTime now)
        {
            var count = 0;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).RadialAdmin();

                    var existing = s.QueryOver<DeepSubordinateModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List();
                    foreach (var e in existing) {
                        e.DeleteTime = now;
                        s.Update(e);
                    }

                    var allManagers = s.QueryOver<ManagerDuration>()
                                        .JoinQueryOver(x => x.Manager)
                                        .Where(x => x.Organization.Id == organizationId)
                                        .List()
                                        .ToListAlive();

                    var allIds = allManagers.SelectMany(x => x.ManagerId.AsList(x.SubordinateId)).Distinct().ToList();

                    foreach (var id in allIds) {
                        var found = s.QueryOver<DeepSubordinateModel>().Where(x => x.SubordinateId == id && x.ManagerId == id).List().ToListAlive();
                        if (!found.Any()) {
                            count++;
                            s.Save(new DeepSubordinateModel() { Links = 1, CreateTime = now, ManagerId = id, SubordinateId = id, OrganizationId = organizationId });
                        }
                    }


                    foreach (var manager in allManagers.Distinct(x => x.ManagerId)) {
                        var managerSubordinates = s.QueryOver<DeepSubordinateModel>().Where(x => x.ManagerId == manager.ManagerId).List().ToListAlive();
                        var allSubordinates = SubordinateUtility.GetSubordinates(s, manager.Manager, false);

                        foreach (var sub in allSubordinates) {
                            var found = managerSubordinates.FirstOrDefault(x => x.SubordinateId == sub.Id);
                            if (found == null) {
                                found = new DeepSubordinateModel() {
                                    CreateTime = now,
                                    ManagerId = manager.ManagerId,
                                    SubordinateId = sub.Id,
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

        public void RemoveManager(UserOrganizationModel caller, long managerId, long userId, DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var managerDuration = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.SubordinateId == userId && x.ManagerId == managerId).Take(1).SingleOrDefault();
                    RemoveManager(s, perms, caller, managerDuration.Id, now);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static void RemoveManager(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long managerDurationId, DateTime now)
        {
            var managerDuration = s.Get<ManagerDuration>(managerDurationId);
            perms.ManagesUserOrganization(managerDuration.SubordinateId, true, PermissionType.EditEmployeeManagers).ManagesUserOrganization(managerDuration.ManagerId, false, PermissionType.EditEmployeeManagers);
            RemoveMangerUnsafe(s, caller, managerDuration, now);
        }

        private static void RemoveMangerUnsafe(ISession s, UserOrganizationModel caller, ManagerDuration managerDuration, DateTime now)
        {
            DeepSubordianteAccessor.Remove(s, managerDuration.Manager, managerDuration.Subordinate, now);
            managerDuration.DeletedBy = caller.Id;
            managerDuration.DeleteTime = now;
            s.Update(managerDuration);
            managerDuration.Subordinate.UpdateCache(s);
            managerDuration.Manager.UpdateCache(s);
        }

        public void RemoveManager(UserOrganizationModel caller, long managerDurationId, DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    RemoveManager(s, perms, caller, managerDurationId, now);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void AddManager(UserOrganizationModel caller, long userId, long managerId, DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller)
                        .ManagesUserOrganization(userId, true, PermissionType.EditEmployeeManagers)
                        .ManagesUserOrganization(managerId, false, PermissionType.EditEmployeeManagers);

                    AddMangerUnsafe(s, caller, userId, managerId, now);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        private static void AddMangerUnsafe(ISession s, UserOrganizationModel caller, long userId, long managerId, DateTime now)
        {
            var user = s.Get<UserOrganizationModel>(userId);
            var manager = s.Get<UserOrganizationModel>(managerId);
            var managerNull = user.ManagedBy.ToListAlive().Where(x => x.ManagerId == managerId).FirstOrDefault();

            if (managerNull != null)
                throw new PermissionsException(manager.GetName() + " is already a manager for this user.");

            if (!manager.IsManager())
                throw new PermissionsException(manager.GetName() + " is not a manager.");

            DeepSubordianteAccessor.Add(s, manager, user, manager.Organization.Id, now);
            user.ManagedBy.Add(new ManagerDuration(managerId, userId, caller.Id) {
                Start = now,
                Manager = manager,
                Subordinate = user
            });

            s.Update(user);
            user.UpdateCache(s);
            //s.Update(new DeepSubordinateModel(){ManagerId=manager.Id}

        }

        public void SwapManager(UserOrganizationModel caller, long userId, long oldManagerId, long newManagerId, DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller)
                        .ManagesUserOrganization(userId, true, PermissionType.EditEmployeeManagers)
                        .ManagesUserOrganization(oldManagerId, false, PermissionType.EditEmployeeManagers)
                        .ManagesUserOrganization(newManagerId, false, PermissionType.EditEmployeeManagers);

                    var managerDuration = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.SubordinateId == userId && x.ManagerId == oldManagerId).Take(1).SingleOrDefault();

                    if (managerDuration == null)
                        throw new PermissionsException();

                    RemoveMangerUnsafe(s, caller, managerDuration, now);
                    AddMangerUnsafe(s, caller, userId, newManagerId, now);

                    tx.Commit();
                    s.Flush();
                }
            }

        }

        public void ChangeRole(UserModel caller, UserOrganizationModel callerUserOrg, long roleId)
        {
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

        public void EditUserModel(UserModel caller, string userId, string firstName, string lastName, string imageGuid, bool? sendTodoEmails, int? sendTodoTime,bool? showScorecardColors)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    if (caller.Id != userId)
                        throw new PermissionsException();
                    var userOrg = s.Get<UserModel>(userId);
                    if (firstName != null)
                        userOrg.FirstName = firstName;
                    if (lastName != null)
                        userOrg.LastName = lastName;
                    if (imageGuid != null) {
                        userOrg.ImageGuid = imageGuid;
                    }
                    if (sendTodoEmails != null) {
                        userOrg.SendTodoTime = sendTodoEmails.Value ? sendTodoTime : null;
                    }
                    if (showScorecardColors != null) {
                        var us=s.Get<UserStyleSettings>(userId);
                        us.ShowScorecardColors = showScorecardColors.Value;
                        s.Update(us);
                    }


                    new Cache().Invalidate(CacheKeys.USER);

                    if (userOrg.UserOrganization != null) {
                        foreach (var u in userOrg.UserOrganization) {
                            if (u != null)
                                u.UpdateCache(s);
                        }
                    }

                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public List<String> SideEffectRemove(UserOrganizationModel caller, long userId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).RemoveUser(userId);
                    var user = s.Get<UserOrganizationModel>(userId);
                    var warnings = new List<String>();
                    //managed teams
                    var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId).List().ToList();
                    foreach (var m in managedTeams) {
                        if (m.Type != TeamType.Subordinates) {
                            warnings.Add("The team, " + m.GetName() + " is managed by" + user.GetFirstName() + ". You will be promoted to manager of this team.");
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

        public ResultObject UndeleteUser(UserOrganizationModel caller, long userId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).RemoveUser(userId);
                    var user = s.Get<UserOrganizationModel>(userId);
                    if (user.DeleteTime == null)
                        throw new PermissionsException("Could not undelete");

                    var deleteTime = user.DeleteTime.Value;

                    user.DeleteTime = null;

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
                    DeepSubordianteAccessor.UndeleteAll(s, user, deleteTime);
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
                    var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId).List().ToList();
                    foreach (var m in managedTeams) {
                        if (m.Type != TeamType.Subordinates) {
                            m.ManagedBy = caller.Id;
                            s.Update(m);
                            warnings.Add("You now manage the team: " + m.GetName() + ".");
                        } else {
                            //teams
                            var subordinateTeam = s.QueryOver<TeamDurationModel>().Where(x => x.TeamId == m.Id && x.DeleteTime == deleteTime).List().ToList();
                            foreach (var t in subordinateTeam) {
                                t.DeletedBy = caller.Id;
                                t.DeleteTime = null;
                                s.Update(t);
                            }

                            m.DeleteTime = null;
                            s.Update(m);
                        }
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

        public ResultObject RemoveUser(UserOrganizationModel caller, long userId, DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).RemoveUser(userId);
                    var user = s.Get<UserOrganizationModel>(userId);
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

                    var warnings = new List<String>();
                    //new management structure
                    DeepSubordianteAccessor.RemoveAll(s, user, now);
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
                    var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId).List().ToList();
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

                    if (warnings.Count() == 0) {
                        return ResultObject.CreateMessage(StatusType.Success, "Successfully removed " + user.GetFirstName() + ".");
                    } else {
                        return ResultObject.CreateMessage(StatusType.Warning, "Successfully removed " + user.GetFirstName() + ".<br/><b>Warning:</b><br/>" + string.Join("<br/>", warnings));
                    }
                }
            }
        }

        public void EditJobDescription(UserOrganizationModel caller, long userId, string jobDescription)
        {
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

        public async Task<System.Tuple<string, UserOrganizationModel>> CreateUser(UserOrganizationModel caller, CreateUserOrganizationViewModel model)
        {
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

            var positionId = -2L;
            if (model.Position != null)
                positionId = model.Position.PositionId;

            var nexusIdandUser = await _NexusAccessor.JoinOrganizationUnderManager(
                    user, model.ManagerId, model.IsManager,
                    positionId, model.Email,
                    model.FirstName, model.LastName,
                    model.IsClient,
                    model.SendEmail,
                    model.OrganizationName
                );
            createdUser = nexusIdandUser.Item2;
            var nexusId = nexusIdandUser.Item1;

            return nexusIdandUser;
            //var message = "Successfully added " + model.FirstName + " " + model.LastName + ".";
            //if (caller.Organization.SendEmailImmediately) {
            //	message += " An invitation has been sent to " + model.Email + ".";
            //	return Json(ResultObject.Create(null/*createdUser.GetTree(createdUser.Id.AsList())*/, message));
            //}
            //else {
            //	message += " The invitation has NOT been sent. To send, click \"Send Invites\" below.";
            //	return Json(ResultObject.Create(null/*createdUser.GetTree(createdUser.Id.AsList())*/, message, StatusType.Warning));
            //}
        }

        public static List<Tuple<string, string, long>> Search(UserOrganizationModel caller, long orgId, string search, int take = int.MaxValue, long[] exclude = null)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller).ViewOrganization(orgId);

                    var users = OrganizationAccessor.GetMembers_Tiny(s, perms, orgId);

                    exclude = exclude ?? new long[0];

                    users = users.Where(x => !exclude.Any(y => y == x.Item3)).ToList();

                    var splits = search.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var dist = new DiscreteDistribution<Tuple<string, string, long>>(0, 7, true);

                    foreach (var u in users) {
                        var fname = false;
                        var lname = false;
                        var ordered = false;
                        var fnameStart = false;
                        var lnameStart = false;
                        var wasFirst = false;
                        var exactFirst = false;
                        var exactLast = false;

                        var f = u.Item1.ToLower();
                        var l = u.Item2.ToLower();
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

        private static void AddSettings(IdentityResult result, UserModel user)
        {
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
        public async static Task<IdentityResult> CreateUser(NHibernateUserManager UserManager, UserModel user, ExternalLoginInfo info)
        {
            var result = await UserManager.CreateAsync(user);
            if (result.Succeeded) {
                result = await UserManager.AddLoginAsync(user.Id, info.Login);
                AddSettings(result, user);
            }
            return result;
        }

        public async static Task<IdentityResult> CreateUser(NHibernateUserManager UserManager, UserModel user, string password)
        {
            var resultx = await UserManager.CreateAsync(user, password);
            AddSettings(resultx, user);
            return resultx;
        }

        public static string GetStyles(string userModel)
        {
            var builder = new StringBuilder();
            UserStyleSettings styles = null;

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    styles=s.Get<UserStyleSettings>(userModel);
                }
            }

            if (styles != null) {
                if (styles.ShowScorecardColors == false) {
                    builder.AppendLine(".scorecard-table .score .success,.scorecard-table .score .danger{color: inherit !important;background-color: inherit !important;}");
                }
            }

            return builder.ToString();
        }
    }
}
