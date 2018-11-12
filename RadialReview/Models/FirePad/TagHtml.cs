using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	public class TagHtml {
		public string tag { get; set; }
		public string endTag { get; set; }
		public string size { get; set; }
		public bool status { get; set; }
		public string code { get; set; }
		public string key { get; set; }
		public int tagLoc { get; set; }
		public int endTagLoc { get; set; }
		public void createTag(string code, string val) {
			switch (code) {
				case "f":
					tag = "<span style=\"font-family: " + val + "\">";
					endTag = "</span>";
					break;
				case "c":
					tag = "<span style=\"color: " + val + "\">";
					endTag = "</span>";
					break;
				case "fs":
					tag = "<span style=\"font-size: " + val + "\">";
					endTag = "</span>";
					break;
				case "lt":
					switch (val) {
						case "":
							tag = "<li>";
							endTag = "</li>";
							break;
						case "u":
							tag = "<ul>";
							endTag = "</ul>";
							break;
					}

					break;

			}

		}
	}
}