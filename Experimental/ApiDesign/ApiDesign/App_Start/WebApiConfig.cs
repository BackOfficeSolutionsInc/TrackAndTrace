using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Serialization;
using ApiDesign.Models.Database;
using ApiDesign.Utilites.DTO;
using ApiDesign.Utilities.API;
using System.Web.Http.Dispatcher;

namespace ApiDesign
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            // Web API routes
            config.MapHttpAttributeRoutes();

			var router = config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/v{version}/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional },
				constraints: new { version = @"\d+" }

				
			);

			/*router.DataTokens = 
			router.DataTokens["Namespaces"] = new string[] { "Foo" };*/

			config.Formatters.Remove(config.Formatters.XmlFormatter);
			config.Formatters.Add(config.Formatters.JsonFormatter);
			config.Formatters.Add(config.Formatters.FormUrlEncodedFormatter);
			config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
			//foreach (var v in Const.API.Versions) {
				config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new DtoSerializer(/*v*/));
			//}
			//config.Formatters.Insert(0, new ApiVersionFormatter());

			var availableNamespaces = new List<string> { "ApiDesign.Controllers.V1","ApiDesign.Controllers.V2" };

			GlobalConfiguration.Configuration.Services.Replace(
				typeof(IHttpControllerSelector),
				new NamespaceHttpControllerSelector(GlobalConfiguration.Configuration, availableNamespaces)
			);


			DtoConverterConfig.Register(config);
		}
    }
}
