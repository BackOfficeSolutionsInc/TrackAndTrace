using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Exceptions;
using RadialReview.Utilities;
using NHibernate.Linq;
using NHibernate;

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

        public UserOrganizationModel GetUserOrganization(UserOrganizationModel caller, long userOrganizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization( userOrganizationId);
                    return s.Get<UserOrganizationModel>(userOrganizationId);
                }
            }
        }

        public List<UserOrganizationModel> GetUserOrganizations(String userId,Boolean full=false)
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
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(forId);
                    var forUser=s.Get<UserOrganizationModel>(forId);
                    return forUser.ManagedBy.ToListAlive().Select(x => x.Manager).SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate)).ToList();
                }
            }
        }

        public List<UserOrganizationModel> GetManagers(UserOrganizationModel caller, long forId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(forId);
                    var forUser = s.Get<UserOrganizationModel>(forId);
                    return forUser.ManagedBy.ToListAlive().Select(x => x.Manager).ToList();
                }
            }
        }

        public List<UserOrganizationModel> GetSubordinates(UserOrganizationModel caller, long forId)
        {

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(forId);
                    var forUser = s.Get<UserOrganizationModel>(forId);
                    return forUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate).ToListAlive();
                }
            }
        }
       

        private UserOrganizationModel GetUserOrganizationModel(ISession session, long id, Boolean full )
        {
            /*if (full)
            {*/
                var result=session.Query<UserOrganizationModel>().Where(x=>x.Id==id);
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

        public UserOrganizationModel GetUserOrganizations(String userId, long userOrganizationId,Boolean full=false)
        {
            if (userId == null)
                throw new LoginException();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var user=s.Get<UserModel>(userId);
                                /*.FetchMany(x => x.UserOrganization)
                                .ThenFetch(x => x.Organization)
                                .SingleOrDefault();*/

//                    var user = db.UserModels.Include(x => x.UserOrganization.Select(y => y.Organization)).FirstOrDefault(x => x.IdMapping == userId);
                    if (user == null)
                        throw new LoginException();
                    var match = user.UserOrganization.SingleOrDefault(x => x.Id == userOrganizationId && x.DetachTime == null);
                    if (match == null)
                        throw new OrganizationIdException();
                    return GetUserOrganizationModel(s, match.Id, full);
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


        public void SetHints(UserModel caller, bool turnedOn)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var user=s.Get<UserModel>(caller.Id);
                    user.Hints = turnedOn;
                    s.Update(user);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void EditUser(UserOrganizationModel caller,long userOrganizationId, bool? isManager=null, long? managerId=null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perm=PermissionsUtility.Create(s, caller).EditUserOrganization(userOrganizationId);
                    var found=s.Get<UserOrganizationModel>(userOrganizationId);

                    if (isManager != null)
                    {
                        perm.ManagesUserOrganization(userOrganizationId);
                        found.ManagerAtOrganization = isManager.Value;
                    }

                    if (managerId != null)
                    {
                        perm.ManagesUserOrganization(userOrganizationId);
                        var manager=s.Get<UserOrganizationModel>(managerId.Value);

                        found. = isManager.Value;
                    }


                    tx.Commit();
                    s.Flush();
                }
            }
        }
    }
}