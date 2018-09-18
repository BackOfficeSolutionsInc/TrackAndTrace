﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;


namespace ApiDesign.Utilities.API {
	//originally created for Umbraco https://github.com/umbraco/Umbraco-CMS/blob/7.2.0/src/Umbraco.Web/WebApi/NamespaceHttpControllerSelector.cs
	//adapted from there, does not recreate HttpControllerDescriptors, instead caches them
	public class NamespaceHttpControllerSelector : DefaultHttpControllerSelector {
		private const string ControllerKey = "controller";
		private readonly HttpConfiguration _configuration;
		private readonly Lazy<HashSet<NamespacedHttpControllerMetadata>> _duplicateControllerTypes;
		private List<string> AvailableNamespaces { get; set; }

		public NamespaceHttpControllerSelector(HttpConfiguration configuration, List<string> availableNameSpaces) : base(configuration) {
			_configuration = configuration;
			_duplicateControllerTypes = new Lazy<HashSet<NamespacedHttpControllerMetadata>>(InitializeNamespacedHttpControllerMetadata);
			AvailableNamespaces = availableNameSpaces;
		}

		public override HttpControllerDescriptor SelectController(HttpRequestMessage request) {
			var routeData = request.GetRouteData();
			//if (routeData == null || routeData.Route == null || routeData.Route.DataTokens["Namespaces"] == null)
			if (AvailableNamespaces == null)
				return base.SelectController(request);

			// Look up controller in route data
			object controllerName;
			routeData.Values.TryGetValue(ControllerKey, out controllerName);
			var controllerNameAsString = controllerName as string;
			if (controllerNameAsString == null)
				return base.SelectController(request);

			//get the currently cached default controllers - this will not contain duplicate controllers found so if
			// this controller is found in the underlying cache we don't need to do anything
			var map = base.GetControllerMapping();
			if (map.ContainsKey(controllerNameAsString))
				return base.SelectController(request);

			//the cache does not contain this controller because it's most likely a duplicate, 
			// so we need to sort this out ourselves and we can only do that if the namespace token
			// is formatted correctly.
			var namespaces = AvailableNamespaces as IEnumerable<string>;
			if (namespaces == null)
				return base.SelectController(request);

			string version;
			if (routeData != null && routeData.Values != null && routeData.Values["version"] != null) {
				version = (string)routeData.Values["version"];
			} else {
				return base.SelectController(request);
			}
			//see if this is in our cache
			var found = _duplicateControllerTypes.Value.Where(x =>
				string.Equals(x.ControllerName, controllerNameAsString, StringComparison.OrdinalIgnoreCase) &&
				AvailableNamespaces.Contains(x.ControllerNamespace) &&
				string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase)
			).FirstOrDefault();

			if (found==null)
				return base.SelectController(request);

			return found.Descriptor;
		}

		private HashSet<NamespacedHttpControllerMetadata> InitializeNamespacedHttpControllerMetadata() {
			var assembliesResolver = _configuration.Services.GetAssembliesResolver();
			var controllersResolver = _configuration.Services.GetHttpControllerTypeResolver();
			var controllerTypes = controllersResolver.GetControllerTypes(assembliesResolver);

			var groupedByName = controllerTypes.GroupBy(
				t => t.Name.Substring(0, t.Name.Length - ControllerSuffix.Length),
				StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1);

			var duplicateControllers = groupedByName.ToDictionary(
				g => g.Key,
				//g => g.ToLookup(t => t.Namespace ?? String.Empty, StringComparer.OrdinalIgnoreCase),
				g => g.ToLookup(t => {
					var ns = t.Namespace.ToUpper();
					var versionLoc = ns.LastIndexOf(".V") + 2;
					var nextDotLoc = ns.IndexOf('.', versionLoc);
					if (nextDotLoc == -1)
						nextDotLoc = ns.Length;
					return ns.Substring(versionLoc, nextDotLoc - versionLoc) ?? String.Empty;
				}, StringComparer.OrdinalIgnoreCase),
				StringComparer.OrdinalIgnoreCase);

			var result = new HashSet<NamespacedHttpControllerMetadata>();

			foreach (var controllerTypeGroup in duplicateControllers) {
				foreach (var controllersByVersion in controllerTypeGroup.Value) {
					var version = controllersByVersion.Key;//controllerTypeGroup.Value.Key;//controllerType.Key;
					foreach (var controllerType in controllersByVersion) {
						var desc = new HttpControllerDescriptor(_configuration, controllerTypeGroup.Key, controllerType);
						result.Add(new NamespacedHttpControllerMetadata(controllerTypeGroup.Key, version, controllerType.Namespace, desc));
					}
				}
			}

			return result;
		}

		private class NamespacedHttpControllerMetadata {
			private readonly string _version;
			private readonly string _controllerName;
			private readonly string _controllerNamespace;
			private readonly HttpControllerDescriptor _descriptor;

			public NamespacedHttpControllerMetadata(string controllerName, string version, string controllerNamespace, HttpControllerDescriptor descriptor) {
				_controllerName = controllerName;
				_controllerNamespace = controllerNamespace;
				_descriptor = descriptor;
				_version = version;
			}

			public string Version {
				get { return _version; }
			}
			public string ControllerName {
				get { return _controllerName; }
			}

			public string ControllerNamespace {
				get { return _controllerNamespace; }
			}

			public HttpControllerDescriptor Descriptor {
				get { return _descriptor; }
			}
		}
	}

}