using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using RadialReview.Accessors;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using RadialReview.Utilities.Serializers;

namespace RadialReview
{

    public class MvcApplication : System.Web.HttpApplication
    {
        protected async void Application_Start()
        {
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;

            //AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ServerUtility.RegisterCacheEntry();
            ServerUtility.Reschedule();
			
			//Add Angular serializer to SignalR
			var serializerSettings = new JsonSerializerSettings();
			serializerSettings.Converters.Add(new AngularSerialization());
			var serializer = JsonSerializer.Create(serializerSettings);
			GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), ()=>serializer);
   
            new ApplicationAccessor().EnsureApplicationExists();


            
        }

    }
}
