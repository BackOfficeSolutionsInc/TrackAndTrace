using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	public class Entity {
		public object type;
		public Dictionary<string, object> info;
		//AttributeConstants ATTR = new AttributeConstants();
		string SENTINEL = AttributeConstants.ENTITY_SENTINEL;
		string PREFIX = AttributeConstants.ENTITY_SENTINEL + "_";


		public Entity() { }
		public Entity(object type, Dictionary<string, object> info) {
			// Allow calling without new.


			this.type = type;
			this.info = info ?? null;
		}

		//Entity.prototype.toAttributes = function() {
		//	var attrs = { };
		//	attrs[SENTINEL] = this.type;

		//   for(var attr in this.info) {
		//		attrs[PREFIX + attr] = this.info[attr];
		//	}

		//	return attrs;
		//};

		public Entity fromAttributes(Dictionary<string, object> attributes) {
			var type = attributes[SENTINEL];
			Dictionary<string, object> info = null;
			foreach (var attr in attributes) {

				if (attr.Value.ToString().IndexOf(PREFIX) == 0) {
					info[attr.Value.ToString().Substring(PREFIX.Length)] = attributes[attr.Key];
				}
			}

			return new Entity(type, info);
		}
	}
}