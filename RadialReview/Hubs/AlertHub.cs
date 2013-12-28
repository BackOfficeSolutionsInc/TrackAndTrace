using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using RadialReview.Models.Json;
using System.Threading.Tasks;

namespace RadialReview.Hubs
{
    public class AlertHub : Hub
    {
        /*
        public void ShowJsonAlert(long userOrgId,ResultObject resultObject,bool showSuccess)
        {
            Clients.Client("" + userOrgId).jsonAlert(resultObject, showSuccess);
        }
         */
    }
}