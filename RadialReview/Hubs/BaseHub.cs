using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;

namespace RadialReview.Hubs
{
	public class BaseHub : Hub
	{
		protected UserAccessor _UserAccessor = new UserAccessor();
		public static String REGISTERED_KEY = "BaseHubRegistered_";

		private UserOrganizationModel _CurrentUser = null;
		private string _CurrentUserOrganizationId = null;
		private UserOrganizationModel ForceGetUser(ISession s, string userId)
		{
			var user = s.Get<UserModel>(userId);
			if (user.IsRadialAdmin)
				_CurrentUser = s.Get<UserOrganizationModel>(user.CurrentRole);
			else
			{
                if (user.CurrentRole == 0)
                {
                    if (user.UserOrganizationIds!=null && user.UserOrganizationIds.Count()==1){
                        user.CurrentRole = user.UserOrganizationIds[0];
                        s.Update(user);
                    }else{
                        throw new OrganizationIdException();
                    }
                }

				var found = s.Get<UserOrganizationModel>(user.CurrentRole);
				if (found.DeleteTime != null || found.User.Id == userId)
				{
					//Expensive
					var avail = user.UserOrganization.ToListAlive();
					_CurrentUser = avail.FirstOrDefault(x => x.Id == user.CurrentRole);
					if (_CurrentUser == null)
						_CurrentUser = avail.FirstOrDefault();
					if (_CurrentUser == null)
						throw new NoUserOrganizationException("No user exists.");
				}
				else
				{
					_CurrentUser = found;
				}


			}
			return _CurrentUser;
		}

		protected UserOrganizationModel GetUser()//long? organizationId, Boolean full = false)
		{
			if (_CurrentUser != null)
				return _CurrentUser;

			var userId = Context.User.Identity.GetUserId();
			if (userId==null)
				throw new LoginException("Not logged in.");

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return ForceGetUser(s, userId);
				}
			}
		}

		

		protected UserOrganizationModel GetUser(ISession s)//long? organizationId, Boolean full = false)
		{
			if (_CurrentUser != null)
				return _CurrentUser;

			var userId = Context.User.Identity.GetUserId();
			if (userId == null)
				throw new LoginException("Not logged in.");

			return ForceGetUser(s, userId);

		}


		/*public async override Task OnConnected()
		{
			var username = Context.User.Identity.Name;

			var now = DateTime.UtcNow;
			var httpContext = Context.Request.GetHttpContext();



			if (!httpContext.CacheContains(REGISTERED_KEY + username))
			{
				var userId = _UserAccessor.GetUserIdByUsername(username);
				var userOrgs = _UserAccessor.GetUserOrganizations(userId, "");
				httpContext.CacheAdd(REGISTERED_KEY + username, userOrgs, now.AddDays(1));
			}

			var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
			foreach (var u in httpContext.CacheGet<List<UserOrganizationModel>>(REGISTERED_KEY + username))
			{
				try
				{
					await hub.Groups.Add(Context.ConnectionId, "organization_" + u.Organization.Id);
					if (u.IsManager())
					{
						await hub.Groups.Add(Context.ConnectionId, "manager_" + u.Organization.Id);
					}
				}
				catch (Exception e)
				{
					var a = 0;
				}
			}

			await base.OnConnected();
		}*/
	}
}