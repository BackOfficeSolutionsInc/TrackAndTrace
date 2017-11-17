using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNet.WebHooks.Diagnostics;
using Microsoft.AspNet.WebHooks.Services;
using RadialReview.Utilities.Synchronize;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.DataProtection;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using System.Web.Http.Routing;

namespace RadialReview {
	public static class WebApiConfig {
		public static void Register(HttpConfiguration config) {
			config.MapHttpAttributeRoutes();

			/*config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );*/

			//config.Services.Replace(typeof(IHttpControllerSelector), new NamespaceHttpControllerSelector(config));
			config.Formatters.Remove(config.Formatters.XmlFormatter);
			config.Formatters.Add(config.Formatters.JsonFormatter);
			config.Formatters.Add(config.Formatters.FormUrlEncodedFormatter);
			config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

			config.Filters.Add(new ClientTimestampFilter());
			config.Filters.Add(new HandleApiExceptionAttribute());

			config.InitializeCustomWebHooks();
			config.InitializeCustomWebHooksApis();
			config.InitializeReceiveCustomWebHooks();


			if (config == null) {
				throw new ArgumentNullException(nameof(config));
			}

			WebHooksConfig.Initialize(config);

			ILogger logger = config.DependencyResolver.GetLogger();
			SettingsDictionary settings = config.DependencyResolver.GetSettings();

            // We explicitly set the DB initializer to null to avoid that an existing DB is initialized wrongly.
            //Database.SetInitializer<WebHookStoreContext>(null);

            IWebHookStore store = new RadialWebHookStore();
            CustomServices.SetStore(store);
        }
	}

	public class ClientTimestampFilter : ActionFilterAttribute {
		public override void OnActionExecuting(HttpActionContext actionContext) {			
			HttpContext.Current.Items[SyncUtil.NO_SYNC_EXCEPTION] = true;
			base.OnActionExecuting(actionContext);
		}
	}

	public class HandleApiExceptionAttribute : ExceptionFilterAttribute {
		public override void OnException(HttpActionExecutedContext context) {
			var request = context.ActionContext.Request;

			var response = new {
				error = true,
				message = context.Exception.Message
			};

			context.Response = request.CreateResponse(HttpStatusCode.BadRequest, response);
		}
	}

	public class NamespaceHttpControllerSelector : IHttpControllerSelector {
		private const string NamespaceKey = "namespace";
		private const string ControllerKey = "controller";

		private readonly HttpConfiguration _configuration;
		private readonly Lazy<Dictionary<string, HttpControllerDescriptor>> _controllers;
		private readonly HashSet<string> _duplicates;

		public NamespaceHttpControllerSelector(HttpConfiguration config) {
			_configuration = config;
			_duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_controllers = new Lazy<Dictionary<string, HttpControllerDescriptor>>(InitializeControllerDictionary);
		}

		private Dictionary<string, HttpControllerDescriptor> InitializeControllerDictionary() {
			var dictionary = new Dictionary<string, HttpControllerDescriptor>(StringComparer.OrdinalIgnoreCase);

			// Create a lookup table where key is "namespace.controller". The value of "namespace" is the last
			// segment of the full namespace. For example:
			// MyApplication.Controllers.V1.ProductsController => "V1.Products"
			var assembliesResolver = _configuration.Services.GetAssembliesResolver();
			var controllersResolver = _configuration.Services.GetHttpControllerTypeResolver();

			var controllerTypes = controllersResolver.GetControllerTypes(assembliesResolver);

			foreach (var t in controllerTypes) {
				var segments = t.Namespace.Split(Type.Delimiter);

				// For the dictionary key, strip "Controller" from the end of the type name.
				// This matches the behavior of DefaultHttpControllerSelector.
				var controllerName = t.Name.Remove(t.Name.Length - DefaultHttpControllerSelector.ControllerSuffix.Length);

				var key = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", segments[segments.Length - 1], controllerName);

				// Check for duplicate keys.
				if (dictionary.Keys.Contains(key)) {
					_duplicates.Add(key);
				} else {
					dictionary[key] = new HttpControllerDescriptor(_configuration, t.Name, t);
				}
			}

			// Remove any duplicates from the dictionary, because these create ambiguous matches. 
			// For example, "Foo.V1.ProductsController" and "Bar.V1.ProductsController" both map to "v1.products".
			foreach (var s in _duplicates) {
				dictionary.Remove(s);
			}
			return dictionary;
		}

		// Get a value from the route data, if present.
		private static T GetRouteVariable<T>(IHttpRouteData routeData, string name) {
			object result = null;
			if (routeData.Values.TryGetValue(name, out result)) {
				return (T)result;
			}
			object subroutes = null;
			if (routeData.Values.TryGetValue("MS_SubRoutes", out subroutes) && subroutes.GetType().IsArray && typeof(IHttpRouteData).IsAssignableFrom(subroutes.GetType().GetElementType())) {
				if (((IHttpRouteData[])subroutes).Length > 0 && ((IHttpRouteData[])subroutes)[0].Values.TryGetValue(name, out result)) {
					return (T)result;
				}
			}

			return default(T);
		}

		public HttpControllerDescriptor SelectController(HttpRequestMessage request) {
			var routeData = request.GetRouteData();
			if (routeData == null) {
				throw new HttpResponseException(HttpStatusCode.NotFound);
			}

			// Get the namespace and controller variables from the route data.
			var namespaceName = GetRouteVariable<string>(routeData, NamespaceKey);
			if (namespaceName == null) {
				throw new HttpResponseException(HttpStatusCode.NotFound);
			}

			var controllerName = GetRouteVariable<string>(routeData, ControllerKey);
			if (controllerName == null) {
				throw new HttpResponseException(HttpStatusCode.NotFound);
			}

			// Find a matching controller.
			var key = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", namespaceName, controllerName);

			HttpControllerDescriptor controllerDescriptor;
			if (_controllers.Value.TryGetValue(key, out controllerDescriptor)) {
				return controllerDescriptor;
			} else if (_duplicates.Contains(key)) {
				throw new HttpResponseException(
					request.CreateErrorResponse(HttpStatusCode.InternalServerError,
					"Multiple controllers were found that match this request."));
			} else {
				throw new HttpResponseException(HttpStatusCode.NotFound);
			}
		}

		public IDictionary<string, HttpControllerDescriptor> GetControllerMapping() {
			return _controllers.Value;
		}
	}
}
