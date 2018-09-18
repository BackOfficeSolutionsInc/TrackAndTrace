using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace ApiDesign.Utilities.API {
	public class ApiSelector : DefaultHttpControllerSelector {
		private readonly HttpConfiguration _configuration;

		public ApiSelector(HttpConfiguration configuration) : base(configuration) {
			_configuration = configuration;
		}

		public override HttpControllerDescriptor SelectController(HttpRequestMessage request) {
			var rd = request.GetRouteData();
			if (rd != null && rd.Values != null && rd.Values["version"] != null) {
				var version = (string)rd.Values["version"];
				var controller = (string)rd.Values["controller"];

				return GetController(controller, version);

			}
			return base.SelectController(request);
		}

		

		public HttpControllerDescriptor GetController(string calledController, string version) {
			var assembliesResolver = _configuration.Services.GetAssembliesResolver();
			var controllersResolver = _configuration.Services.GetHttpControllerTypeResolver();
			var controllerTypes = controllersResolver.GetControllerTypes(assembliesResolver);

			//var calledController = name.Substring(0, name.Length - ControllerSuffix.Length);

			var groupedByName = controllerTypes.GroupBy(
				t => t.Name.Substring(0, t.Name.Length - ControllerSuffix.Length),
				StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1);
			

			var duplicateControllers = groupedByName.ToDictionary(
				g => g.Key,
				g => g.ToDictionary(t => {
					var ns = t.Namespace.ToUpper();
					var versionLoc = ns.LastIndexOf(".V") + 2;
					var nextDotLoc = ns.IndexOf('.', versionLoc);
					if (nextDotLoc == -1)
						nextDotLoc = ns.Length;
					return ns.Substring(versionLoc) ?? String.Empty;
				}, StringComparer.OrdinalIgnoreCase)
				, StringComparer.OrdinalIgnoreCase);

			var controllerTypeGroup = duplicateControllers[calledController][version];
			var controllerType = controllerTypeGroup;
			var c = new HttpControllerDescriptor(_configuration, calledController, controllerType);
			return c;

			//foreach (var controllerTypeGroup in duplicateControllers) {
			//	foreach (var controllerType in controllerTypeGroup.Value.SelectMany(controllerTypesGrouping => controllerTypesGrouping)) {
			//		var c= 
			//		return c;
			//		//var desc = new HttpControllerDescriptor(_configuration, controllerTypeGroup.Key, controllerType);
			//		//result.Add(new NamespacedHttpControllerMetadata(controllerTypeGroup.Key, controllerType.Namespace, desc));
			//	}
			//}
			//return null;
		}
	}
}