using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common.Logging;
using Newtonsoft.Json.Linq;
using System.Collections;
namespace RadialReview.Models.FirePad {
	
	// Converted from UnityScript to C# at http://www.M2H.nl/files/js_to_c.php - by Mike Hergaarden
	// Do test the code! You usually need to change a few small bits.


	

	public class TextOperation  {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public List<object> ops;
		int baseLength;
		int targetLength;
		TextOp textOp =new TextOp();
		Utils utils= new Utils();

		// Constructor for new operations.
		public TextOperation (){
			

			// When an operation is applied to an input string, you can think of this as
			// if an imaginary cursor runs over the entire string and skips over some
			// parts, deletes some parts and inserts characters at some positions. These
			// actions (skip/delete/insert) are stored as an array in the "ops" property.
			this.ops= new List<object>();
			// An operation's baseLength is the length of every string the operation
			// can be applied to.
			this.baseLength = 0;
			// The targetLength is the length of every string that results from applying
			// the operation on a valid input string.
			this.targetLength = 0;
		}
		// After an operation is constructed, the user of the library can specify the
		// actions of an operation (skip/insert/delete) with these three builder
		// methods. They all return the operation for convenient chaining.

		// Skip over a given number of characters.
		public void retain (object n,object attributes) {
			if (utils.TokenType(n) != "number") {
				throw new Exception("retain expects a positive integer.");
			}else if((int)n<0) {
				throw new Exception("retain expects a positive integer.");
			}
			if ((int)n == 0) { return ; }
			this.baseLength += (int)n;
			this.targetLength += (int)n;
			var prevOp = (this.ops.Count > 0) ? (TextOp)this.ops[this.ops.Count - 1] : null;
			if (prevOp!=null && prevOp.isRetain() && prevOp.AttributesEqual(attributes)) {
				// The last op is a retain op with the same attributes => we can merge them into one op.
				var chars = (int)prevOp.chars;
				chars += (int)n;
				prevOp.chars = chars;
			} else {
				// Create a new op.
				this.ops.Add(new TextOp("retain", n, attributes));
			}
			
		}

		// Insert a string at the current position.
		public void insert(object str, object attributes) {
			if (utils.TokenType(str) != "string") {
				throw new Exception("insert expects a string");
			}
			if (str.ToString() == "") { return; }
			attributes = attributes ?? null;
			this.targetLength += str.ToString().Length;
			var prevOp = (this.ops.Count > 0) ? (TextOp)this.ops[this.ops.Count - 1] : null;
			var prevPrevOp = (this.ops.Count > 1) ? (TextOp)this.ops[this.ops.Count - 2] : null;
			if (prevOp!=null && prevOp.isInsert() && prevOp.AttributesEqual(attributes)) {
				// Merge insert op.
				string a = (string)prevOp.text;
				a += ((JValue)str).ToObject<string>();
				prevOp.text = a;
			} else if (prevOp!=null && prevOp.isDelete()) {
				// It doesn't matter when an operation is applied whether the operation
				// is delete(3), insert("something") or insert("something"), delete(3).
				// Here we enforce that in this case, the insert op always comes first.
				// This makes all operations that have the same effect when applied to
				// a document of the right length equal in respect to the `equals` method.
				if (prevPrevOp!=null && prevPrevOp.isInsert() && prevPrevOp.AttributesEqual(attributes)) {
					string a = (string)prevPrevOp.text;
					 a += str;
					prevPrevOp.text = a;
				} else {
					this.ops[this.ops.Count - 1] = new TextOp("insert", str, attributes);
					this.ops.Add(prevOp);
				}
			} else {
				TextOp tp = new TextOp("insert", str, attributes);
				this.ops.Add(tp);
			}
			
		}

		// Delete a string at the current position.
		public void delete (object n) {
			int x;
			if (utils.TokenType(n) == "string") { x = ((string)n).Length; }
			else if (utils.TokenType(n) != "number" ) {
				throw new Exception("delete expects a positive integer or a string");
			}else if((int)n<0) {
				throw new Exception("delete expects a positive integer or a string");
			} else {
				x = (int)n;
			}
			if (x == 0) { return; }
			this.baseLength += x;
			var prevOp = (this.ops.Count > 0) ? (TextOp)this.ops[this.ops.Count - 1] : null;
			if (prevOp.isDelete()) {
				var a = (int)prevOp.chars;
				 a += x;
				prevOp.chars = a;
			} else {
				this.ops.Add(new TextOp("delete", x));
			}
		
		}

		// Converts a plain JS object into an operation and validates it.
		public TextOperation fromJSON(KeyValuePair<string, Dictionary<string, object>> item) {

			var o = new TextOperation();
			
				var itm = item.Value;
				object obj = itm["o"];
				IEnumerable<object> collection = (IEnumerable<object>)obj;
				List<object> ops = new List<object>();
				foreach (object elem in collection) {
					ops.Add(elem);
				}
				
				for (var i = 0; i < ops.Count; i++) {
					object op = CastFromNewton(ops[i]);
					object attributes = null;
					if (utils.TokenType(op) == "object") {
						attributes = op;
						i++;
						op = CastFromNewton(ops[i]);


					}
					if (utils.TokenType(op) == "number") {
						if ((int)op > 0) {
							o.retain(op, attributes);
						} else {
							o.delete(-(int)op);
						}
					} else {
						utils.assert(utils.TokenType(op) == "string");
						o.insert(op, attributes);
					}
				}
			
			return o;
		}

		public object CastFromNewton(object op) {
			switch (utils.TokenType(op)) {
				case "object":
					op = ((JObject)op).ToObject<object>();
					break;
				case "string":
					op = ((JValue)op).ToObject<string>();
					break;
				case "number":
					op = ((JValue)op).ToObject<int>();
					break;
				case "boolean":
					op = ((JValue)op).ToObject<bool>();
					break;
				default:
					op = null;
					break;
			}
			return op;
		}


		public string apply (string str, List<Dictionary<string, object>> oldAttributes, out List<Dictionary<string, object>> newAttributes ) {
			var operation = this;
			newAttributes = null;
			oldAttributes = oldAttributes ?? new List<Dictionary<string,object>>();
			newAttributes = newAttributes ?? new List<Dictionary<string, object>>();
			if (str.Length != operation.baseLength) {
				throw new Exception("The operation's base length must be equal to the string's length.");
			}
			var newStringParts = new List<string>();
			var j = 0;
			//var k=0;
			//object attr;
			var oldIndex = 0;
			var ops = this.ops;
			for (var i = 0; i < ops.Count; i++) {
				var op = (TextOp)ops[i];
				if (op.isRetain()) {
					if (oldIndex + (int)op.chars > str.Length) {
						throw new Exception("Operation can't retain more characters than are left in the string.");
					}
					// Copy skipped part of the retained string.

					//var s = str.Substring(oldIndex, oldIndex + (int)op.chars);
					//s = Slice(str, oldIndex, oldIndex + (int)op.chars);
					newStringParts.Add(str.Substring(oldIndex, (int)op.chars));

					// Copy (and potentially update) attributes for each char in retained string.
					for (var k = 0; k < (int)op.chars; k++) {
						var currAttributes = new Dictionary<string, object>();
						if (oldIndex + 1 < oldAttributes.Count) {
							currAttributes = oldAttributes[oldIndex + k] ;
						} 
						var updatedAttributes = new Dictionary<string, object>();
						foreach (var attr in currAttributes) {
							updatedAttributes.Add(attr.Key,attr.Value);
							utils.assert((bool)updatedAttributes[attr.Key] != false);
						}
						var obj = new Dictionary<string, object>();
						if (op.attributes.GetType() == typeof(object)) {
							if (utils.IsDictionary(op.attributes)) {
								obj = (Dictionary<string, object>)op.attributes;
							}
						} else {
							obj = ((JObject)op.attributes).ToObject<Dictionary<string, object>>();
						}
						foreach (var attr in obj) {
							bool a=true;
							if (attr.Value.GetType() == typeof(bool)) {
								a = (bool)attr.Value;
							}
							if (a == false) {
								 updatedAttributes.Remove(attr.Key);
							} else {
								updatedAttributes[attr.Key] = attr;
							}
							if (updatedAttributes[attr.Key].GetType()==typeof(bool)) {
								utils.assert((bool)updatedAttributes[attr.Key] != false);
							}
						}
						newAttributes.Add(updatedAttributes);
					}

					oldIndex += (int)op.chars;
				} else if (op.isInsert()) {
					// Insert string.
					newStringParts.Add( (string)op.text);

					// Insert attributes for each char.
					var a = (string)op.text;
					for (var k = 0; k < a.Length; k++) {
						var insertedAttributes = new Dictionary<string, object>();
						var obj=new Dictionary<string, object>() ;

						if(op.attributes.GetType()==typeof(object)) {
							if (utils.IsDictionary(op.attributes)) {
								obj = (Dictionary<string, object>)op.attributes;
							}
						}else {
							obj = ((JObject)op.attributes).ToObject<Dictionary<string, object>>();
						}
						foreach (var attr in obj) {
							insertedAttributes[attr.Key] = attr;
							if (attr.Value.GetType() == typeof(bool)) {
								utils.assert((bool)attr.Value != false);
							}
						}
						newAttributes.Add(insertedAttributes);
					}
				} else { // delete op
					oldIndex += (int)op.chars;
				}
			}
			if (oldIndex != str.Length) {
				throw new Exception("The operation didn't operate on the whole string.");
			}
			var newString = string.Join("", newStringParts);
			utils.assert(newString.Length == newAttributes.Count);

			return newString;
		}
	}

}