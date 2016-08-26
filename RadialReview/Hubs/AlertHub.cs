using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using RadialReview.Models.Json;
using System.Threading.Tasks;
using RadialReview.Accessors;
using System.Collections.Concurrent;
using System.Web.Caching;
using RadialReview.Models;
using RadialReview.Utilities;

namespace RadialReview.Hubs
{
    public class AlertHub : Hub
    {
        protected UserAccessor _UserAccessor = new UserAccessor();

        public static String REGISTERED_KEY = "HubRegistered_";

        public async override Task OnConnected()
        {
            var username = Context.User.Identity.Name;

            var now = DateTime.UtcNow;
            var httpContext= Context.Request.GetHttpContext();
	        var cache = new Cache(httpContext);

			if (!cache.Contains(REGISTERED_KEY + username))
            {
				using (var s = HibernateSession.GetCurrentSession())
				{
					using (var tx = s.BeginTransaction())
					{
					    var userId = _UserAccessor.GetUserIdByUsername(s,username);
						var userOrgs = _UserAccessor.GetUserOrganizations(s,userId, "");
						cache.Push(REGISTERED_KEY + username, userOrgs,LifeTime.Request/*AppDomain*/, now.AddDays(1));
					}
				}
             
            }

            var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
            foreach (var u in (List<UserOrganizationModel>)cache.Get(REGISTERED_KEY + username))
            {
                try{
                    await hub.Groups.Add(Context.ConnectionId, "organization_" + u.Organization.Id);
                    if (u.IsManager()){
                        await hub.Groups.Add(Context.ConnectionId, "manager_" + u.Organization.Id);
                    }
                }catch (Exception){
                   // var a = 0;
                }
            }

            await base.OnConnected();
        }
        /*
        public void ShowJsonAlert(long userOrgId,ResultObject resultObject,bool showSuccess)
        {
            Clients.Client("" + userOrgId).jsonAlert(resultObject, showSuccess);
        }
         */
    }
}