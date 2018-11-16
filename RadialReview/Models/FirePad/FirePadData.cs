using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Common.Logging;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;


namespace RadialReview.Models.FirePad {
	public class FirePadData {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public string padID { get; set; }
		public string initialText { get; set; }
		public string html { get; set; }
		public string text { get; set; }
		int baseLength = 0;
		Dictionary<int, string> positionOccupied = new Dictionary<int, string>();
		string TODO_STYLE = "<style>ul.firepad-todo { list-style: none; margin-left: 0; padding-left: 0; } ul.firepad-todo > li { padding-left: 1em; text-indent: -1em; } ul.firepad-todo > li:before { content: \"\\2610\"; padding-right: 5px; } ul.firepad-todo > li.firepad-checked:before { content: \"\\2611\"; padding-right: 5px; }</style>\n";
		public void setHtml(Dictionary<string, Dictionary<string, object>> items) {

			
			TextOperation to = new TextOperation();
			var str= "";
			TextOperation doc = new TextOperation();
			List<Dictionary<string, object>> oldAttributes = new List<Dictionary<string, object>>();
			List<Dictionary<string, object>> newAttributes=null;
			foreach (var item in items) {

				str=to.fromJSON(item).apply(str, oldAttributes, out newAttributes);
				
			}
			
			var i = 0;
			foreach(var attr in newAttributes) {
				var a = attr.Count > 0 ? attr.Values : new object();
				doc.ops.Add(new TextOp("insert", str.Substring(i++, 1), a));
			}
			
			EntityManager entityManager=null;
			
			html = serializeHtml(doc, entityManager);
			
		}

		public string serializeHtml(TextOperation doc, EntityManager entityManager) {
			Utils utils = new Utils();
			var html = "";
			var newLine = true;
			var listTypeStack = new Stack<object>();
			var inListItem = false;
			var firstLine = true;
			var emptyLine = true;
			string afterInsert = null;
			
		
			
			var usesTodo = false;
			AttributeConstants ATTR = new AttributeConstants();
			
				var i = 0;
				
				var op = (TextOp)doc.ops[i];
				while (op!=null) {

				var text1 = Regex.Replace(html, "<.*?>", string.Empty);
				if (op.isRetain()) {
					//var op1= (TextOp)doc.ops[i + 1];
					//if (afterInsert == null && op.chars.GetType() == typeof(int) && (int)op.chars - 1 < baseLength && op1.isInsert()) {
					//	html = insertText((int)op.chars, html, out afterInsert);
					//}
					if (doc.ops.Count - 1 == i) {
						op = null;
					} else {
						op = (TextOp)doc.ops[++i];
					}
					continue;
				} else if (op.isDelete()) {

					if (doc.ops.Count - 1 == i) {
						op = null;
					} else {
						op = (TextOp)doc.ops[++i];
					}
					continue;
				}
				utils.assert(op.isInsert());
					Dictionary<String, object> attrs;
					if (op.attributes.GetType() == typeof(object)) {
						if (utils.IsDictionary(op.attributes)) {
							attrs = (Dictionary<String, object>)op.attributes;
						} else {
							attrs = new Dictionary<String, object>();
						}
					} else {
						if (utils.IsDictionary(op.attributes)) {
							attrs = attrs = (Dictionary<String, object>)op.attributes;
						} else {
							attrs = ((JObject)op.attributes).ToObject<Dictionary<String, object>>();
						}
					}
					if (newLine) {
					newLine = false;

					var indent = 0;
					object listType = null;
					var lineAlign = "left";
					if (attrs.ContainsKey(AttributeConstants.LINE_SENTINEL)) {

						if (attrs.ContainsKey(AttributeConstants.LINE_INDENT)) {
							indent = (int)attrs[AttributeConstants.LINE_INDENT];
						} else {
							indent = 0;
						}
						if (attrs.ContainsKey(AttributeConstants.LIST_TYPE)) {
							listType = (string)attrs[AttributeConstants.LIST_TYPE];
						} else {
							listType = null;
						}
						if (attrs.ContainsKey(AttributeConstants.LINE_ALIGN)) {
							lineAlign = (string)attrs[AttributeConstants.LINE_ALIGN];
						} else {
							lineAlign = "left";
						}

					}
					if (listType != null) {
						indent = indent == 0 ? 1 : indent; // lists are automatically indented at least 1.
					}

					if (inListItem) {
						html += "</li>";
						inListItem = false;
					} else if (!firstLine) {
						if (emptyLine) {
							html += "<br/>";
						}
						html += "</div>";
					}
					firstLine = false;

					// Close any extra lists.
					utils.assert(indent >= 0, "Indent must not be negative.");
					while (listTypeStack.Count > indent ||
						(indent == listTypeStack.Count && listType != null && !compatibleListType((string)listType, (string)listTypeStack.ElementAt(listTypeStack.Count - 1)))) {
						html += close((string)listTypeStack.Pop());
					}

					// Open any needed lists.
					while (listTypeStack.Count < indent) {
						var toOpen = (string)listType ?? LIST_TYPE.UNORDERED; // default to unordered lists for indenting non-list-item lines.
						usesTodo = (string)listType == LIST_TYPE.TODO ? (string)listType == LIST_TYPE.TODOCHECKED : usesTodo;
						html += open(toOpen);
						listTypeStack.Push(toOpen);
					}

					var style = (lineAlign != "left") ? " style=\"text-align:" + lineAlign + "\"" : "";
					if (listType != null) {
						var clazz = "";
						switch ((string)listType) {
							case LIST_TYPE.TODOCHECKED:
								clazz = " class=\"firepad-checked\"";
								break;
							case LIST_TYPE.TODO:
								clazz = " class=\"firepad-unchecked\"";
								break;
						}
						html += "<li" + clazz + style + ">";
						inListItem = true;
					} else {
						// start line div.
						html += "<div" + style + ">";
					}
					emptyLine = true;
				}

				if (attrs.ContainsKey(AttributeConstants.LINE_SENTINEL)) {
						if (doc.ops.Count - 1 == i) {
							op = null;
						} else {
							op = (TextOp)doc.ops[++i];
						}
						continue;
				}

				if (attrs.ContainsKey(AttributeConstants.ENTITY_SENTINEL)) {
					Entity entt = new Entity();
					string txt = (string)op.text;
					for (var j = 0; j < txt.Length; j++) {
						var entity = entt.fromAttributes(attrs);
						//var element = entityManager.exportToElement(entity);
						//html += element.outerHTML;
					}
						if (doc.ops.Count - 1 == i) {
							op = null;
						} else {
							op = (TextOp)doc.ops[++i];
						}
					continue;
				}

				var prefix = "";
				var suffix = "";
				foreach (var attr in attrs) {
					var value = attr.Value;
					object start = null;
					object end = null;
					if (attr.Key == AttributeConstants.BOLD || attr.Key == AttributeConstants.ITALIC || attr.Key == AttributeConstants.UNDERLINE || attr.Key == AttributeConstants.STRIKE) {
						utils.assert((bool)value == true);
						start = end = attr.Key;
					} else if (attr.Key == AttributeConstants.FONT_SIZE) {
						start = "span style=\"font-size: " + value;
						start += (utils.TokenType(value) != "string" || value.ToString().IndexOf("px", value.ToString().Length - 2) == -1) ? "px\"" : "\"";
						end = "span";
					} else if (attr.Key == AttributeConstants.FONT) {
						start = "span style=\"font-family: " + value + "\"";
						end = "span";
					} else if (attr.Key == AttributeConstants.COLOR) {
						start = "span style=\"color: " + value + "\"";
						end = "span";
					} else if (attr.Key == AttributeConstants.BACKGROUND_COLOR) {
						start = "span style=\"background-color: " + value + "\"";
						end = "span";
					} else {
						log.Error("Encountered unknown attribute while rendering html: " + attr);
					}
					if (start != null)
						prefix += "<" + start + ">";
					if (end != null)
						suffix = "</" + end + ">" + suffix;
				}

				var text = (string)op.text;
				//added for insertion
				//if (afterInsert != null) {
				//	text += afterInsert;
				//	afterInsert = null;
				//}
				
				//added for insertion
				
				
				var newLineIndex = text.IndexOf("\n");
				if (newLineIndex >= 0) {
					//baseLength++;
					newLine = true;
					
					if (newLineIndex < text.Length - 1) {
						// split op.
						op = new TextOp("insert", text.Substring(newLineIndex + 1), attrs);
					} else {
							if (doc.ops.Count - 1 == i) {
								op = null;
							} else {
								op = (TextOp)doc.ops[++i];
							}
						}
					text = text.Substring(0, newLineIndex);
				} else {
						if (doc.ops.Count - 1 == i) {
							op = null;
						} else {
							op = (TextOp)doc.ops[++i];
						}

					}

				// Replace leading, trailing, and consecutive spaces with nbsp's to make sure they're preserved.
				//var str = new Array(text.Length + 1).join("\u00a0");

				text = text.Replace(" +", "\u00a0"
				).Replace("^ ", "\u00a0").Replace(" $", "\u00a0");
				if (text.Length > 0) {
					emptyLine = false;
				}

				html += prefix + textToHtml(text) + suffix;
				baseLength += text.Length;
			}

			if (inListItem) {
				html += "</li>";
			} else if (!firstLine) {
				if (emptyLine) {
					html += "&nbsp;";
				}
				html += "</div>";
			}

			// Close any extra lists.
			while (listTypeStack.Count > 0) {
				html += close((string)listTypeStack.Pop());
			}

			if (usesTodo) {
				html = TODO_STYLE + html;
			}
		
			return html;
		}
		public string insertText(int chars,string htmlText,out string afterInsert) {
			string html = "";
			int letterCounter = 0;

			afterInsert = "";
			bool validchar = true;
			string text="";
			Dictionary<int, char> validInText = new Dictionary<int, char>();

			text = Regex.Replace(htmlText, "<.*?>", string.Empty);
			for (int i = text.IndexOf('>'); i > -1; i = text.IndexOf('>', i + 1)) {
				validInText.Add(i,'>');
			}
			for (int i = text.IndexOf('<'); i > -1; i = text.IndexOf('<', i + 1)) {
				validInText.Add(i, '<');
			}
			foreach (char c in htmlText) {
				switch (c) {
					case '<':				
						if (!(validInText.ContainsKey(letterCounter) && validInText[letterCounter] == c)) {
							validchar = false;
						} 
						break;
					case '>':
						if (!(validInText.ContainsKey(letterCounter) && validInText[letterCounter] == c)) {
							validchar = true;
						} 
						break;
					default:
						if (validchar) {
							letterCounter++;
						}
						break;
				
			}
				html += c;
				if (letterCounter == chars-1) {
					afterInsert = htmlText.Substring(html.Length);
					baseLength -= afterInsert.Length;
					return html;
				}
			}
			return html;
		}
		public string close(string listType) {
			return (listType == LIST_TYPE.ORDERED) ? "</ol>" : "</ul>";
		}
		public string open(string listType) {
			return (listType == LIST_TYPE.ORDERED) ? "<ol>" : 
				   (listType == LIST_TYPE.UNORDERED) ? "<ul>" :
				   "<ul class=\"firepad-todo\">";
		}
		public bool compatibleListType(string l1, string l2) {
		return (l1 == l2) ||
			(l1 == LIST_TYPE.TODO && l2 == LIST_TYPE.TODOCHECKED) ||
			(l1 == LIST_TYPE.TODOCHECKED && l2 == LIST_TYPE.TODO);
	}
		public string textToHtml(string text) {
			return text.Replace(" &", "&amp;")
				.Replace(" \"", "&quot;")
				.Replace(" '", " &#39;")
		.Replace("<", "&lt;")
				.Replace(">", "&gt;")
				.Replace("\u00a0", "&nbsp;");
  }
	}
}