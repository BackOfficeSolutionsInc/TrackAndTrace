﻿using System.IO;
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
using RadialReview.Utilities.Productivity;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using RadialReview.App_Start;
using System.Reflection;
using PdfSharp.Drawing;
using RadialReview.Utilities.NHibernate;
using RadialReview.Controllers;
using static RadialReview.Utilities.ViewUtility;

namespace RadialReview {

	public class MvcApplication : System.Web.HttpApplication {
		// protected async void App

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		protected async void Application_Start() {

			ChromeExtensionComms.SendCommand("appStart");
			//GlobalConfiguration.Configure(WebApiConfig.Register);
			//AntiForgeryConfig.RequireSsl = true;
			AntiForgeryConfig.SuppressXFrameOptionsHeader = true;

			//AreaRegistration.RegisterAllAreas();
			AreaRegistration.RegisterAllAreas();

			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			//SwaggerConfig.Register(GlobalConfiguration.Configuration);
			GlobalConfiguration.Configure(WebApiConfig.Register);

			BundleConfig.RegisterBundles(BundleTable.Bundles);

			HookConfig.RegisterHooks();

			// ValueProviderFactories.Factories.Add(new JsonValueProviderFactory());

			//ServerUtility.RegisterCacheEntry();
			//ServerUtility.Reschedule();

			//Add Angular serializer to SignalR
			var serializerSettings = new JsonSerializerSettings();
			serializerSettings.Converters.Add(new AngularSerialization(true));
			var serializer = JsonSerializer.Create(serializerSettings);
			GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);

			//NHibernate ignore proxy
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
				Converters = new List<JsonConverter> { new NHibernateProxyJsonConvert() }
			};

			ApplicationAccessor.EnsureApplicationExists();

			ViewEngines.Engines.Clear();
			IViewEngine razorEngine = new RazorViewEngine() { FileExtensions = new[] { "cshtml" } };
			ViewEngines.Engines.Add(razorEngine);

			ModelBinders.Binders.Add(typeof(DateTime), new DateTimeModelBinder());
			ModelBinders.Binders.Add(typeof(DateTime?), new DateTimeModelBinder());

			//install fonts
			InstallFonts();
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		protected void Application_Error(object sender, EventArgs e) {
			Exception ex = Server.GetLastError();
			string message = null;
			if (ex is HttpException) {
				var ee = (HttpException)ex;
				if (ee.Message.StartsWith("A potentially dangerous Request.Path value was detected from the client (:)")) {
					HttpContext.Current.Server.ClearError();
					HttpContext.Current.Response.Redirect("~/Home/Index");
				}
				if (ee.Message.StartsWith("A public action method '")) {
					HttpContext.Current.Server.ClearError();
					message = "Page does not exist.";
				}
			}



			// Create an arbitrary controller instance
			try {
				var view = ViewUtility.RenderView("~/views/Error/Index.cshtml", ex);
				view.ViewData["HasBaseController"] = true;
				view.ViewData["NoAccessCode"] = true;
				if (!string.IsNullOrWhiteSpace(message)) {
					view.ViewData["Message"] = message;
				}
				var html = view.Execute();
				HttpContext.Current.Server.ClearError();
				var response = HttpContext.Current.Response;
				response.TrySkipIisCustomErrors = true;
				response.ClearContent();
				response.StatusCode = 500;
				response.Write(html);
			} catch (Exception ee) {
			}

		}

		protected async Task Application_End() {
			var wasKilled = await ChromeExtensionComms.SendCommandAndWait("appEnd");
			var inte = 0;
			inte += 1;
		}
		[DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
		public static extern int AddFontResource([In][MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
		protected int InstallFonts() {
			if (!Config.IsLocal()) {
				var fonts = new[] { "Arial Narrow Bold.TTF", "Arial Narrow.TTF", "arial.ttf" };
				var installed = 0;
				foreach (var f in fonts) {
					try {
						var result = AddFontResource(@"c:\\Windows\\Fonts\\" + f);
						var error = Marshal.GetLastWin32Error();
						installed = installed + (error == 0 ? 1 : 0);
					} catch (Exception) {
					}
				}

				var assembly = Assembly.GetExecutingAssembly();

				foreach (var resourceName in assembly.GetManifestResourceNames()) {
					try {
						if (resourceName.ToLower().EndsWith(".ttf")) {
							using (var resourceStream = assembly.GetManifestResourceStream(resourceName)) {
								XPrivateFontCollection.Add(resourceStream);//, resourceName.Substring(0, resourceName.Length - 4).Split('.').Last());
							}
						}
					} catch (Exception e) {
						throw e;
					}
				}

				return installed;
			}
			return 0;
		}


		void Application_EndRequest(Object Sender, EventArgs e) {
			if ("POST" == Request.HttpMethod) {
				try {
					Request.InputStream.Seek(0, SeekOrigin.Begin);
					var bytes = Request.BinaryRead(Request.TotalBytes);
					var s = Encoding.UTF8.GetString(bytes);
					if (!String.IsNullOrEmpty(s) && !s.ToLower().Contains("password")) {
						var QueryStringLength = 0;
						if (0 < Request.QueryString.Count) {
							QueryStringLength = Request.ServerVariables["QUERY_STRING"].Length;
							Response.AppendToLog("&");
						}

						if (4100 > (QueryStringLength + s.Length)) {
							Response.AppendToLog(s);
						} else {
							// append only the first 4090 the limit is a total of 4100 char.
							Response.AppendToLog(s.Substring(0, (4090 - QueryStringLength)));
							// indicate buffer exceeded
							Response.AppendToLog("|||...|||");
							// TODO: if s.Length >; 4000 then log to separate file
						}
					}
					Request.InputStream.Seek(0, SeekOrigin.Begin);
				} catch (Exception) {
					Response.AppendToLog("~Error~");
				}
			}
		}

	}
}
