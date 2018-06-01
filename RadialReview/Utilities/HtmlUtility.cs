using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Utilities {

    public class CellInfo<T> {
        public int Row { get; set; }
        public int Col { get; set; }
        public T Cell { get; set; }
    }

    public class TableOptions<T> {
        public bool Responsive { get; set; }
        public Func<CellInfo<T>, string> CellClass { get; set; }
        public Func<CellInfo<T>, Dictionary<string, string>> CellProperties { get; set; }
        public string TableClass { get; set; }
        public Func<CellInfo<T>, string> CellText { get; set; }
    }

    public static class HtmlUtility {

		//public static string RenderRazorViewToString(this Controller controller, string viewName, object model) {
		//	controller.ViewData.Model = model;
		//	using (var sw = new StringWriter()) {
		//		var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
		//		var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);
		//		viewResult.View.Render(viewContext, sw);
		//		viewResult.ViewEngine.ReleaseView(controller.ControllerContext, viewResult.View);
		//		return sw.GetStringBuilder().ToString();
		//	}
		//}

		public static String Table<T>(List<List<T>> rowData, TableOptions<T> options = null)
        {
            var sb = new StringBuilder();
            //Defaults
            options = options?? new TableOptions<T>();
            options.CellClass=options.CellClass?? new Func<CellInfo<T>,string>(x=>"");
            options.CellProperties = options.CellProperties ?? new Func<CellInfo<T>, Dictionary<string, string>>(x => new Dictionary<string, string>());
            options.TableClass = options.TableClass??"";
            options.CellText = options.CellText ?? new Func<CellInfo<T>,string>(x=>x.Cell.ToString());
            if (options.Responsive)
                sb.Append("<div class='table-responsive'>");
            sb.Append("<table class=\"").Append(options.TableClass).Append("\" >");
            var i=0;
            foreach (var row in rowData) {
                sb.Append("<tr>");
                var j=0;
                foreach(var cell in row){
                    var c = new CellInfo<T>(){Cell=cell,Row=i,Col=j};
                    sb.Append("<td class=\"").Append(options.CellClass(c)).Append("\" ");
                    var props = options.CellProperties(c);
                    foreach (var prop in props) {
                        sb.Append(prop.Key).Append("=\"").Append(prop.Value).Append("\" ");
                    }
                    sb.Append(">").Append(options.CellText(c)).Append("</td>");
                    j++;
                }
                sb.Append("</tr>");
                i++;
            }
            sb.Append("</table>");
            if (options.Responsive)
                sb.Append("</div>");
            return sb.ToString();
        }
    }
}