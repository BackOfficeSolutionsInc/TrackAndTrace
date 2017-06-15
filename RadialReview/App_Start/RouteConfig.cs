using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace RadialReview
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(name: "url",url: "u/{id}",defaults: new { controller = "Url", action = "Index", id = "" });
            routes.MapRoute(name: "image", url: "i/{id}", defaults: new { controller = "Image", action = "Index", id = "" });
            routes.MapRoute(name: "nexus", url: "n/{id}", defaults: new { controller = "Nexus", action = "Index", id = "" });
            routes.MapRoute(name: "privacy", url: "privacy", defaults: new { controller = "Legal", action = "Privacy", id = "" });
            routes.MapRoute(name: "tos", url: "tos", defaults: new { controller = "Legal", action = "TOS", id = "" });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );




        }
    }
}
