using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
					var user = s.Get<UserModel>(userId);
					var avail = user.UserOrganization.ToListAlive();

					_CurrentUser = avail.FirstOrDefault(x => x.Id == user.CurrentRole);

					if (_CurrentUser == null)
						_CurrentUser =avail.FirstOrDefault();
					if (_CurrentUser == null)
						throw new NoUserOrganizationException("No user exists.");

					return _CurrentUser;
				}
			}


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