using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Exceptions;
using RadialReview.Utilities;
using NHibernate.Linq;
using NHibernate;
using RadialReview.Models.UserModels;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Enums;
using RadialReview.Utilities.Query;

namespace RadialReview.Accessors
{

    public class UserAccessor : BaseAccessor
    {
        public UserModel GetUser(String userId)
        {
            if (userId == null)
                throw new LoginException();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    return s.Get<UserModel>(userId);
                }
            }
        }
        [Obsolete("Dont use this elsewhere",false)]
        public UserModel GetUserByEmail(String email)
        {
            if (email == null)
                throw new LoginException();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var lower = email.ToLower();
                    return s.QueryOver<UserModel>().Where(x => x.UserName == lower).SingleOrDefault();
                }
            }
        }

        public UserOrganizationModel GetUserOrganization(UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetUserOrganization(s.ToQueryProvider(true), perms, caller, userOrganizationId, asManager, sensitive);
                }
            }
        }

        public static UserOrganizationModel GetUserOrganization(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive)
        {
            perms.ViewUserOrganization(userOrganizationId, sensitive);
            if (asManager)
            {
                perms.ManagesUserOrganization(userOrganizationId);
            }
            return s.Get<UserOrganizationModel>(userOrganizationId);
        }

        public List<UserOrganizationModel> GetUserOrganizations(String userId, Boolean full = false)
        {
            if (userId == null)
                throw new LoginException();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var user = s.Get<UserModel>(userId);
                    //var user = users.SingleOrDefault();
                    //.FetchMany(x=>x.UserOrganization)
                    //.SingleOrDefault();// db.UserModels.AsNoTracking().FirstOrDefault(x => x.IdMapping == userId);
                    if (user == null)
                        throw new LoginException();
                    var userOrgs = new List<UserOrganizationModel>();

                    foreach (var userOrg in user.UserOrganization)
                    {
                        userOrgs.Add(GetUserOrganizationModel(s, userOrg.Id, full));
                    }
                    return userOrgs;
                }
            }
        }

        public List<UserOrganizationModel> GetPeers(UserOrganizationModel caller, long forId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetPeers(s.ToQueryProvider(true), perms, caller, forId);
                }
            }
        }
        public static List<UserOrganizationModel> GetPeers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forId)
        {
            perms.ViewUserOrganization(forId, false);
            var forUser = s.Get<UserOrganizationModel>(forId);
            return forUser.ManagedBy.ToListAlive()
                          .Select(x => x.Manager)
                          .SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate))
                          .Where(x=>x.Id!=forId)
                          .ToList();
        }

        public List<UserOrganizationModel> GetManagers(UserOrganizationModel caller, long forId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetManagers(s.ToQueryProvider(true), perms, caller, forId);
                }
            }
        }
        public static List<UserOrganizationModel> GetManagers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forId)
        {
            perms.ViewUserOrganization(forId, false);
            var forUser = s.Get<UserOrganizationModel>(forId);
            return forUser.ManagedBy
                            .ToListAlive()
                            .Select(x => x.Manager)
                            .Where(x=>x.Id!=forId)
                            .ToList();
        }



        public List<UserOrganizationModel> GetSubordinates(UserOrganizationModel caller, long forId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms=PermissionsUtility.Create(s, caller);
                    return GetSubordinates(s.ToQueryProvider(true), perms, caller, forId);
                }
            }
        }
        public static List<UserOrganizationModel> GetSubordinates(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forId)
        {
            perms.ViewUserOrganization(forId, false);
            var forUser = s.Get<UserOrganizationModel>(forId);
            return forUser.ManagingUsers
                            .ToListAlive()
                            .Select(x => x.Subordinate)
                            .Where(x => x.Id != forId)
                            .ToListAlive();
        }


        private UserOrganizationModel GetUserOrganizationModel(ISession session, long id, Boolean full)
        {
            /*if (full)
            {*/
            var result = session.Query<UserOrganizationModel>().Where(x => x.Id == id);
            result.FetchMany(x => x.ManagingGroups).ToFuture();
            result.FetchMany(x => x.ManagingUsers).ToFuture();
            result.FetchMany(x => x.Groups).ToFuture();
            result.FetchMany(x => x.ManagedBy).ToFuture();
            result.FetchMany(x => x.CustomQuestions).ToFuture();
            result.FetchMany(x => x.CreatedNexuses).ToFuture();
            result.Fetch(x => x.Organization).ToFuture();
            result.Fetch(x => x.User).ToFuture();

            return result.AsEnumerable().SingleOrDefault();


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

        public UserOrganizationModel GetUserOrganizations(String userId, long userOrganizationId, Boolean full = false)
        {
            if (userId == null)
                throw new LoginException();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var user = s.Get<UserModel>(userId);
                    /*.FetchMany(x => x.UserOrganization)
                    .ThenFetch(x => x.Organization)
                    .SingleOrDefault();*/

                    //                    var user = db.UserModels.Include(x => x.UserOrganization.Select(y => y.Organization)).FirstOrDefault(x => x.IdMapping == userId);
                    long matchId = -1;

                    if (!user.IsRadialAdmin)
                    {
                        if (user == null)
                            throw new LoginException();
                        var match = user.UserOrganization.SingleOrDefault(x => x.Id == userOrganizationId && x.DetachTime == null);
                        if (match == null)
                            throw new OrganizationIdException();
                        matchId = match.Id;
                    }
                    else
                    {
                        matchId = userOrganizationId;
                    }
                    return GetUserOrganizationModel(s, matchId, full);
                }
            }
        }

        public void CreateUser(UserModel userModel)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    s.Save(userModel);
                    tx.Commit();
                    s.Flush();
                    //db.UserModels.Add(userModel);
                    //db.SaveChanges();
                }
            }
        }


        public bool UpdateTempUser(UserOrganizationModel caller, long userOrgId,String firstName,String lastName,String email,DateTime? lastSent =null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var found=s.Get<UserOrganizationModel>(userOrgId);
                    var tempUser = found.TempUser;
                    if (tempUser == null)
                        throw new PermissionsException();

                    bool changed = false;

                    if (tempUser.FirstName != firstName)
                    {
                        tempUser.FirstName = firstName;
                        changed = true;
                    }
                    if (tempUser.LastName != lastName)
                    {
                        tempUser.LastName = lastName;
                        changed = true;
                    }

                    if (tempUser.Email != email)
                    {
                        tempUser.Email = email;
                        changed = true;
                    }

                    if (lastSent != null)
                    {
                        tempUser.LastSent = lastSent.Value;
                        changed = true;
                    }

                    if (changed)
                    {
                        PermissionsUtility.Create(s, caller).ManagesUserOrganization(userOrgId);
                        s.Update(tempUser);
                        tx.Commit();
                        s.Flush();
                    }

                    return changed;
                }
            }
        }

        public void SetHints(UserModel caller, bool turnedOn)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var user = s.Get<UserModel>(caller.Id);
                    user.Hints = turnedOn;
                    s.Update(user);

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
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perm = PermissionsUtility.Create(s, caller).ManagesUserOrganization(userOrganizationId);
                    var found = s.Get<UserOrganizationModel>(userOrganizationId);

                    DateTime deleteTime = DateTime.UtcNow;

                    if (isManager != null && (isManager.Value != found.ManagerAtOrganization))
                    {
                        perm.ManagesUserOrganization(userOrganizationId);
                        found.ManagerAtOrganization = isManager.Value;
                        if (isManager == false)
                        {
                            foreach (var m in found.ManagingUsers.ToListAlive())
                            {
                                m.DeleteTime = deleteTime;
                                m.DeletedBy = caller.Id;
                                s.Update(m);
                            }
                            var subordinatesTeam = s.QueryOver<OrganizationTeamModel>()
                                .Where(x => x.Type == TeamType.Subordinates && x.ManagedBy == userOrganizationId)
                                .SingleOrDefault();
                            s.Delete(subordinatesTeam);
                        }
                        else
                        {
                            s.Save(OrganizationTeamModel.SubordinateTeam(caller,found));
                        }
                    }

                    if (manageringOrganization != null && manageringOrganization.Value!=found.ManagingOrganization)
                    {
                        if (found.Id == caller.Id)
                            throw new PermissionsException("You cannot unmanage this organization yourself.");

                        perm.ManagesUserOrganization(userOrganizationId).ManagingOrganization();
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

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void RemoveManager(UserOrganizationModel caller, long managerDurationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var managerDuration = s.Get<ManagerDuration>(managerDurationId);

                    PermissionsUtility.Create(s, caller).ManagesUserOrganization(managerDuration.SubordinateId);

                    managerDuration.DeletedBy = caller.Id;
                    managerDuration.DeleteTime = DateTime.UtcNow;

                    s.Update(managerDuration);

                    tx.Commit();
                    s.Flush();
                }
            }

        }

        public void AddManager(UserOrganizationModel caller, long userId, long managerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagesUserOrganization(userId);

                    var user = s.Get<UserOrganizationModel>(userId);
                    var manager = user.ManagedBy.ToListAlive().Where(x => x.ManagerId == managerId).FirstOrDefault();

                    if (manager != null)
                        throw new PermissionsException("Already a manager of this user.");

                    user.ManagedBy.Add(new ManagerDuration(managerId, userId, caller.Id));

                    s.Update(user);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void ChangeRole(UserModel caller,UserOrganizationModel callerUserOrg, long roleId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    caller = s.Get<UserModel>(caller.Id);
                    if ((caller != null && caller.IsRadialAdmin) || (callerUserOrg != null && callerUserOrg.IsRadialAdmin) || caller.UserOrganization.Any(x => x.Id == roleId))
                        caller.CurrentRole = roleId;
                    else
                        throw new PermissionsException();
                    s.Update(caller);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void EditUserModel(UserModel caller, string userId, string firstName, string lastName, string imageGuid)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (caller.Id != userId)
                        throw new PermissionsException();
                    var userOrg = s.Get<UserModel>(userId);
                    if (firstName!=null)
                        userOrg.FirstName = firstName;
                    if(lastName!=null)
                        userOrg.LastName = lastName;
                    if (imageGuid != null)
                    {
                        userOrg.ImageGuid = imageGuid;
                    }

                    tx.Commit();
                    s.Flush();
                }
            }
        }
    }
}