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

namespace RadialReview
{

    public class MvcApplication : System.Web.HttpApplication
    {
        protected async void Application_Start()
        {
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ServerUtility.RegisterCacheEntry();
            await ServerUtility.ExecuteAllTasks();

            new ApplicationAccessor().EnsureApplicationExists();


            
        }
    }
}
