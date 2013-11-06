using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace System.Web
{
    public static class HtmlExtensions
    {
        public static HtmlString Badge<T>(this HtmlHelper<T> html, Func<T, int> count)
        {
            var c = count(html.ViewData.Model);
            if (c != 0)
                return new HtmlString(@"<span class=""badge"">" + c + "</span>");
            return new HtmlString("");
        }

        public static HtmlString AlertBoxDismissableJavascript(this HtmlHelper html, String messageVariableName, String alertType = "alert-danger")
        {
            return new HtmlString("\"<div class=\\\"alert " + alertType + " alert-dismissable\\\"><button type=\\\"button\\\" class=\\\"close\\\" data-dismiss=\\\"alert\\\" aria-hidden=\\\"true\\\">&times;</button><strong>"+MessageStrings.Warning+"</strong> <span class=\\\"message\\\">\" + " + messageVariableName + " + \"</span></div>\"");

        }
        public static HtmlString AlertBoxDismissable(this HtmlHelper html, String message, String alertType = "alert-danger")
        {
            if (!String.IsNullOrWhiteSpace(message))
                return new HtmlString("<div class=\"alert " + alertType + " alert-dismissable\"><button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-hidden=\"true\">&times;</button><strong>" + MessageStrings.Warning + "</strong> <span class=\"message\">" + message + "</span></div>");
            return new HtmlString("");
        }


        public static HtmlString BootstrapValidationSummary(this HtmlHelper html, Boolean excludePropertyErrors)
        {
            var errors = html.ViewData.ModelState.Values.SelectMany(x => x.Errors);
            var output = @"<div class=""validation-summary-" + (errors.Any() ? "errors" : "valid") + @" alert alert-error"" data-valmsg-summary=""true"">";
            output += @"<button type=""button"" class=""close"" data-dismiss=""alert"">&times;</button><table><tr><td><strong>Error:</strong></td><td><ul>";
            if (errors.Any())
            {
                foreach (var li in errors)
                {
                    output += @"<li>" + li.ErrorMessage + "</li>";
                }
            }
            else
            {
                output += @"<li style=""display:none""></li>";
            }
            output += "</ul></td></tr></table></div>";
            return new HtmlString(output);
        }

        private class ScriptBlock : IDisposable
        {
            private const string scriptsKey = "scripts";
            public static List<string> pageScripts
            {
                get
                {
                    if (HttpContext.Current.Items[scriptsKey] == null)
                        HttpContext.Current.Items[scriptsKey] = new List<string>();
                    return (List<string>)HttpContext.Current.Items[scriptsKey];
                }
            }

            WebViewPage webPageBase;

            public ScriptBlock(WebViewPage webPageBase)
            {
                this.webPageBase = webPageBase;
                this.webPageBase.OutputStack.Push(new StringWriter());
            }

            public void Dispose()
            {
                pageScripts.Add(((StringWriter)this.webPageBase.OutputStack.Pop()).ToString());
            }
        }

        public static IDisposable BeginScripts(this HtmlHelper helper)
        {
            return new ScriptBlock((WebViewPage)helper.ViewDataContainer);
        }

        public static MvcHtmlString PageScripts(this HtmlHelper helper)
        {
            return MvcHtmlString.Create(string.Join(Environment.NewLine, ScriptBlock.pageScripts.Select(s => s.ToString())));
        }
    }


}