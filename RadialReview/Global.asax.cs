using System.IO;
using System.Text;
using System.Web.Http;
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
			//GlobalConfiguration.Configure(WebApiConfig.Register);
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;

			//AreaRegistration.RegisterAllAreas();
            //AreaRegistration.RegisterAllAreas();

			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
	        GlobalConfiguration.Configure(WebApiConfig.Register);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            //ServerUtility.RegisterCacheEntry();
            //ServerUtility.Reschedule();
			
			//Add Angular serializer to SignalR
			var serializerSettings = new JsonSerializerSettings();
			serializerSettings.Converters.Add(new AngularSerialization());
			var serializer = JsonSerializer.Create(serializerSettings);
			GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), ()=>serializer);
   
            new ApplicationAccessor().EnsureApplicationExists();

			ViewEngines.Engines.Clear(); 
			IViewEngine razorEngine = new RazorViewEngine() { FileExtensions = new [] { "cshtml" } };
			ViewEngines.Engines.Add(razorEngine);

            
        }

		void Application_EndRequest(Object Sender, EventArgs e)
		{
			if ("POST" == Request.HttpMethod){
				try{
					Request.InputStream.Seek(0, SeekOrigin.Begin);
					var bytes = Request.BinaryRead(Request.TotalBytes);
					var s = Encoding.UTF8.GetString(bytes);
					if (!String.IsNullOrEmpty(s) && !s.ToLower().Contains("password")){
						var QueryStringLength = 0;
						if (0 < Request.QueryString.Count){
							QueryStringLength = Request.ServerVariables["QUERY_STRING"].Length;
							Response.AppendToLog("&");
						}

						if (4100 > (QueryStringLength + s.Length)){
							Response.AppendToLog(s);
						}
						else{
							// append only the first 4090 the limit is a total of 4100 char.
							Response.AppendToLog(s.Substring(0, (4090 - QueryStringLength)));
							// indicate buffer exceeded
							Response.AppendToLog("|||...|||");
							// TODO: if s.Length >; 4000 then log to separate file
						}
					}
					Request.InputStream.Seek(0, SeekOrigin.Begin);
				}
				catch (Exception ee){
					Response.AppendToLog("~Error~");
				}
			}
		}

    }
}
