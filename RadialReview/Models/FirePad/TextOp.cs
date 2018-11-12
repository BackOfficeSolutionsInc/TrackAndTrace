using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	// Operation are essentially lists of ops. There are three types of ops:
	//
	// * Retain ops: Advance the cursor position by a given number of characters.
	//   Represented by positive ints.
	// * Insert ops: Insert a given string at the current cursor position.
	//   Represented by strings.
	// * Delete ops: Delete the next n characters. Represented by negative ints.
	public class TextOp {
		Utils utils = new Utils();
		public string type = "";
		public object chars = null;
		public object text = null;
		public object attributes = null;

		public TextOp() {

		}
		public TextOp(string type, object arguments1 = null, object arguments2 = null) {
			this.type = type;
			this.chars = null;
			this.text = null;
			this.attributes = null;

			if (type == "insert") {
				this.text = arguments1;
				utils.assert(utils.TokenType(text) == "string");
				this.attributes = arguments2 ?? new object();
				utils.assert(utils.TokenType(this.attributes) == "object");
			} else if (type == "delete") {
				this.chars = arguments1;
				utils.assert(utils.TokenType(chars) == "number");
			} else if (type == "retain") {
				this.chars = arguments1;
				utils.assert(utils.TokenType(chars) == "number");
				this.attributes = arguments2 ?? new object();
				utils.assert(utils.TokenType(attributes) == "object");
			}
		}

		public bool isInsert() { return this.type == "insert"; }
		public bool isDelete() { return this.type == "delete"; }
		public bool isRetain() { return this.type == "retain"; }


		public bool AttributesEqual(object otherAttributes) {
			Dictionary<String, object> othrAttributes = new Dictionary<String, object>();
			if (otherAttributes != null) {
				othrAttributes = ((JObject)otherAttributes).ToObject<Dictionary<String, object>>();
			}
			Dictionary<String, object> Attributes;
			if (this.attributes.GetType() == typeof(object)) {
				if (utils.IsDictionary(this.attributes)) {
					Attributes = (Dictionary<String, object>)this.attributes;
				} else {
					Attributes = new Dictionary<String, object>();
				}
			} else {
				Attributes = ((JObject)this.attributes).ToObject<Dictionary<String, object>>();
			}

			foreach (var a in Attributes) {
				bool isEqual = false;
				foreach (var b in othrAttributes) {
					if (a.Key == b.Key && a.Value == b.Value) {
						isEqual = true;
						break;
					}
				}
				if (!isEqual) {
					return false;
				}
			}
			foreach (var a in othrAttributes) {
				bool isEqual = false;
				foreach (var b in Attributes) {
					if (a.Key == b.Key && a.Value == b.Value) {
						isEqual = true;
						break;
					}
				}
				if (!isEqual) {
					return false;
				}
			}
			return true;
		}
	}
}