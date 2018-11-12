using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	public class Options {
		Utils utils = new Utils();

		string[] attrs = new string[] { "src", "alt", "width", "height", "style", "class" };
		public string render(Dictionary<string, object> info = null) {
			utils.assert(info != null/*.src*/, "image entity should have 'src'!");

			var html = "<img ";
			for (var i = 0; i < attrs.Length; i++) {
				var attr = attrs[i];

				if (info.ContainsKey(attr)) {
					html += " " + attr + "=\"" + info[attr] + '"';
					break;
				}
			}
			html += ">";
			return html;
		}

		public Dictionary<string, object> fromElement(Dictionary<string, object> element = null) {
			Dictionary<string, object> info = null;
			for (var i = 0; i < attrs.Length; i++) {
				var attr = attrs[i];

				if (element.ContainsKey(attr)) {
					info.Add(attr, element[attr]);
				}
			}
			return info;
		}
	}
}