using Common.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	public class Utils {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public void assert(bool b, string msg = "") {
			if (!b) {
				throw new Exception(msg + "assertion error");
			}
		}
		public string TokenType(object o) {


			try {
				if (o.GetType() == typeof(int)) {
					return "number";
				} else if (o.GetType() == typeof(string)) {
					return "string";
				} else if (o.GetType() == typeof(object)) {
					return "object";

				} else {
					JValue jv = (JValue)o;
					switch (jv.Type) {
						case JTokenType.Integer:
							return "number";
						case JTokenType.String:
							return "string";
						case JTokenType.Boolean:
							return "boolean";
						default:
							return null;
					}

				}

			} catch (Exception e) {
				if (e.Message == "Unable to cast object of type 'Newtonsoft.Json.Linq.JObject' to type 'Newtonsoft.Json.Linq.JValue'.") {
					JObject jo = (JObject)o;
					if (jo.Type == JTokenType.Object) {
						return "object";
					} else {
						return null;
					}
				} else if (e.Message == "Unable to cast object of type 'System.Collections.Generic.Dictionary`2[System.String,System.Object]' to type 'Newtonsoft.Json.Linq.JValue'.") {
					Type t = o.GetType();
					bool isDict = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
					if (isDict) {
						return "object";
					}else {
						return null;
					}
				} else {
					log.Error("Error PadAccessor.GetHtmlFirepad ", e);
					return null;
				}
			}

		}
		public bool IsList(object o) {
			if (o == null)
				return false;
			return o is IList &&
				   o.GetType().IsGenericType &&
				   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
		}

		public bool IsDictionary(object o) {
			if (o == null)
				return false;
			return o is IDictionary &&
				   o.GetType().IsGenericType &&
				   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
		}
	}
}