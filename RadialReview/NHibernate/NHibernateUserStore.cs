using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Security.Claims;

namespace RadialReview.NHibernate
{
    // Summary:
    //     Implements IUserStore using EntityFramework where TUser is the entity type
    //     of the user being stored
    //
    // Type parameters:
    //   TUser:


    //public class UserStore<TUser> : IUserLoginStore<TUser>, IUserClaimStore<TUser>, IUserRoleStore<TUser>,
    //IUserPasswordStore<TUser>, IUserSecurityStampStore<TUser>, IUserStore<TUser>, IDisposable where TUser
    //: global::Microsoft.AspNet.Identity.EntityFramework.IdentityUser

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class NHibernateUserStore : IUserLoginStore<UserModel>, IUserClaimStore<UserModel>,
        IUserRoleStore<UserModel>, IUserPasswordStore<UserModel>, IUserSecurityStampStore<UserModel>,
        IUserStore<UserModel>, IDisposable
    {
		public async Task CreateAsync(UserModel user) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					db.Save(user);
					tx.Commit();
					db.Flush();
				}
			}
		}

		public async Task DeleteAsync(UserModel user)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user = db.Get<UserModel>(user);
                    user.DeleteTime = DateTime.UtcNow;
                    db.SaveOrUpdate(user);
                    tx.Commit();
                    db.Flush();
                }
            }
        }

        public async Task<UserModel> FindByIdAsync(string userId)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    return db.Get<UserModel>(userId);
                }
            }
        }

        public async Task<UserModel> FindByNameAsync(string userName)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    return db.QueryOver<UserModel>().Where(x => x.UserName == userName).SingleOrDefault();
                }
            }
        }

        public async Task UpdateAsync(UserModel user)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    db.SaveOrUpdate(user);
                }
            }
        }

        public void Dispose()
        {
            //TODO should this do anything?
        }

        public async Task<string> GetPasswordHashAsync(UserModel user)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    return db.Get<UserModel>(user.Id).PasswordHash;
                }
            }
        }

        public async Task<bool> HasPasswordAsync(UserModel user)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    return db.Get<UserModel>(user.Id).PasswordHash != null;
                }
            }
        }

        public async Task SetPasswordHashAsync(UserModel user, string passwordHash)
        {
            user.PasswordHash = passwordHash;
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var foundUser = db.Get<UserModel>(user.Id);
                    if (foundUser != null)
                    {
                        foundUser.PasswordHash = passwordHash;
                        db.Update(foundUser);
                        tx.Commit();
                        db.Flush();
                    }
                    //user.PasswordHash = passwordHash;
                }
            }
            /*using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user = db.Get<UserModel>(user.Id);
                    user.PasswordHash = passwordHash;
                    db.SaveOrUpdate(user);
                    tx.Commit();
                    db.Flush();
                }
            }*/
        }

        public async Task AddLoginAsync(UserModel user, UserLoginInfo login)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user = db.Get<UserModel>(user.Id);
                    user.Logins.Add(new IdentityUserLogin()
                    {
                        LoginProvider = login.LoginProvider,
                        ProviderKey = login.ProviderKey,
                       // User = user,
                        UserId = user.Id
                    });

                    db.SaveOrUpdate(user);
                    tx.Commit();
                    db.Flush();
                }
            }
        }

        public async Task<UserModel> FindAsync(UserLoginInfo login)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    return db.QueryOver<UserModel>().Where(x => x.Logins.Any(y => y.ProviderKey == login.ProviderKey && login.LoginProvider == y.LoginProvider)).SingleOrDefault();
                }
            }
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(UserModel user)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    return db.Get<UserModel>(user.Id).Logins.Select(x => new UserLoginInfo(x.LoginProvider, x.ProviderKey)).ToList();
                }
            }
        }

        public async Task RemoveLoginAsync(UserModel user, UserLoginInfo login)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var u = db.Get<UserModel>(user.Id);
                    var toRemove = u.Logins.FirstOrDefault(x => x.LoginProvider == login.LoginProvider && x.ProviderKey == login.ProviderKey);
                    if (toRemove == null)
                        throw new PermissionsException("Login does not exist.");
                    u.Logins.Remove(toRemove);
                    db.SaveOrUpdate(u);
                    tx.Commit();
                    db.Flush();
                }
            }
        }

        public async Task AddToRoleAsync(UserModel user, string role)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user = db.Get<UserModel>(user.Id);
                    user.Roles.Add(new UserRoleModel() { Role = role });
                    db.SaveOrUpdate(user);
                    tx.Commit();
                    db.Flush();
                }
            }
        }

        public async Task<IList<string>> GetRolesAsync(UserModel user)
        {

            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user = db.Get<UserModel>(user.Id);
                    return user.Roles.NotNull(y=>y.Where(x=>!x.Deleted).Select(x => x.Role).ToList());
                }
            }

        }

		public async Task<bool> IsInRoleAsync(UserModel user, string role) {
			return user.Roles.NotNull(y => y.Any(x => x.Role == role && x.Deleted == false));
		}

		public async Task RemoveFromRoleAsync(UserModel user, string role)
        {
            using(var db=HibernateSession.GetCurrentSession())
            {
                using(var tx=db.BeginTransaction())
                {
                    user=db.Get<UserModel>(user.Id);
                    var found=user.Roles.NotNull(y=>y.ToList().FirstOrDefault(x=>x.Role==role));
                    if (found != null){
                        found.Deleted = true;
                    }else{
                        throw new PermissionsException("Role could not be removed because it doesn't exist.");
                    }
                }
            }
        }

        public async Task<string> GetSecurityStampAsync(UserModel user)
        {
            return user.SecurityStamp;
        }

        public async Task SetSecurityStampAsync(UserModel user, string stamp)
        {
            user.SecurityStamp = stamp;
        }

        public async Task AddClaimAsync(UserModel user, System.Security.Claims.Claim claim)
        {
            throw new NotImplementedException();
            /*using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user = db.Get<UserModel>(user.Id);
                    user.Claims.Add(claim);
                    db.SaveOrUpdate(user);
                    tx.Commit();
                    db.Flush();
                }
            }*/

        }

        public async Task<IList<Claim>> GetClaimsAsync(UserModel user)
        {
            return new List<Claim>();
            throw new NotImplementedException();
            /*using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user=db.Get<UserModel>(user.Id);
                    return user.Claims.Cast<Claim>().ToList();
                }
            }*/
        }

        public async Task RemoveClaimAsync(UserModel user, System.Security.Claims.Claim claim)
        {
            throw new NotImplementedException();
            /*using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    user = db.Get<UserModel>(user.Id);
                    user.Claims.FirstOrDefault(x => x.Type == claim.Type && x.Value == claim.Value).NotNull(x => x.Deleted = true);
                    db.SaveOrUpdate(user);
                    tx.Commit();
                    db.Flush();
                }
            }*/
        }

	}
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously