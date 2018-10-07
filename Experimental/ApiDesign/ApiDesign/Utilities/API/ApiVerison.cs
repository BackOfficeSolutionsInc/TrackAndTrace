using ApiDesign.Utilites.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using System.Web.Mvc;

namespace ApiDesign.Utilities.API {

	//public class ApiVerison : ActionFilterAttribute, IRoutePrefix {

	//	public int Version { get; set; }
	//	public string Prefix { get; private set; }

	//	public ApiVerison(int version) : base(){
	//		Prefix = "api/v" + version;
	//		Version = version;
	//	}

	//	//public override bool Match(object obj) {			
	//	//	return base.Match(obj);
	//	//}
	//	public override void OnActionExecuting(HttpActionContext actionContext) {

	//		//while (true) {
	//		//	var formatters=actionContext.RequestContext.Configuration.Formatters.JsonFormatter.SerializerSettings.Converters;
	//		//	var found = formatters.FirstOrDefault(x => x is DtoSerializer && ((DtoSerializer)x).Version != Version);
	//		//	if (found == null)
	//		//		break;
	//		//	actionContext.RequestContext.Configuration.Formatters.JsonFormatter.SerializerSettings.Converters.Remove(found);
	//		//}

	//		base.OnActionExecuting(actionContext);
	//	}
	//	//
	//	// Summary:
	//	//     Occurs after the action method is invoked.
	//	//
	//	// Parameters:
	//	//   actionExecutedContext:
	//	//     The action executed context.
	//	public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext) {

	//		actionExecutedContext.Response.Content.

	//		base.OnActionExecuted(actionExecutedContext);
	//	}
	//
	//[AttributeUsage(AttributeTargets.Class)]
	//public class ApiVersion : Attribute,IResultFilter{
	//	public ApiVersion(int version) {
	//		Version = version;
	//	}
	//	public int Version { get; set; }

	//	public void OnResultExecuted(ResultExecutedContext filterContext) {
	//		int a = 0;
	//	}

	//	public void OnResultExecuting(ResultExecutingContext filterContext) {
	//		int a = 0;
	//	}
	//}
	public class ApiVersionAttribute : Attribute, IControllerConfiguration {
		public ApiVersionAttribute(int version) {
			Version = version; 
		}

		public int Version { get; set; }
		public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor) {
			var formatter = controllerSettings.Formatters.JsonFormatter;

			controllerSettings.Formatters.Remove(formatter);

			//formatter = new JsonMediaTypeFormatter {
			//	SerializerSettings =
			//	{
			//	ContractResolver = new CamelCasePropertyNamesContractResolver()
			//}
			//};
			//JsonConvert.SerializeObject(value, new[] { serializer });

			controllerSettings.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new DtoSerializer(Version));
		//	ad.Formatters.Add(new ApiVersionFormatter(Version));
		}
	}



	public class ApiVersionFormatter : MediaTypeFormatter {
		public ApiVersionFormatter() {
			//You can add any other supported types here.
			this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
		}

		public ApiVersionFormatter(int version) {
			Version = version;
			this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
		}

		public int Version { get; set; }

		public override bool CanReadType(Type type) {
			//you can just return false if you don't want to read any differently than your default way
			//if you return true here, you should override the ReadFromStreamAsync method to do custom deserialize
			//return type.IsDefined(typeof(ApiVersion), true);
			return true;
		}

		public override bool CanWriteType(Type type) {
			//return type.IsDefined(typeof(ApiVersion), true);
			return true;
		}

		public override async Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext) {

			//var attrs = type.GetCustomAttributes(typeof(ApiVersion),true);
			//var version = ((ApiVersion)attrs.First()).Version;

			var serializer = new DtoSerializer(Version);

			//var serializers = transportContext.Formatters.JsonFormatter.SerializerSettings.Converters;

			string json = JsonConvert.SerializeObject(value, new[] { serializer });
			//if (serializer.CanConvert(value.GetType())) {
			//	value = 
			//	json = JsonConvert.SerializeObject(value);
			//} else {
			//	json = JsonConvert.SerializeObject(value);
			//}

			//value will be your object that you want to serialize

			//add any custom serialize settings here

			//Use the right encoding for your application here
			var byteArray = Encoding.UTF8.GetBytes(json);
			await writeStream.WriteAsync(byteArray, 0, byteArray.Length);
		}
	}

}