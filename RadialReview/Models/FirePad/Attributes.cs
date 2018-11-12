using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	public class Attributes {
		public static Dictionary<string, TagHtml> attribute = new Dictionary<string, TagHtml>()
		{
			{ "b",new TagHtml { key="b", tag="<b>",endTag="</b>"} },
			{ "i",new TagHtml {key="i" ,tag="<i>",endTag="</i>"} },
			{ "u",new TagHtml {key="u",tag="<u>",endTag="</u>" } },
			{ "l",new TagHtml () },
			{ "s",new TagHtml {key="s", tag="<s>", endTag="</s>" } },
			{ "f",new TagHtml ()  },
			{ "fs",new TagHtml () },
			{ "c",new TagHtml () },
			{ "lt",new TagHtml () },
			{ "li",new TagHtml {key="li", tag="<li>", endTag="</li>" } },
			{ "ol",new TagHtml { key="ol",tag="<ol>", endTag="</ol>" } },
			{ "ul",new TagHtml { key="ul",tag="<ul>", endTag="</ul>" } },
			{ "div",new TagHtml { key="div",tag="<div>", endTag="</div>" }  }


		};
		public static Dictionary<string, TagHtml> forRemobal = new Dictionary<string, TagHtml>()
		{
			{ "ul",new TagHtml { tag="<ul>", endTag="</ul>" } },
			 { "ol",new TagHtml { tag="<ol>", endTag="</ol>" } },
			{ "div",new TagHtml { tag="<div>", endTag="</div>" } } };
	}
}