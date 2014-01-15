using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace System.Web
{
    public static class HtmlExtensions
    {

        public static String ViewOrEdit(this HtmlHelper html, bool edit)
        {
            return edit ? "Edit" : "View";
        }

        public static HtmlString Color(this HtmlHelper html, double value, double neg, double zero, double pos,double alpha)
        {
            double v = 0;
            var redValue = 0.0;
            var greenValue = 0.0;
            // value is a value between 0 and 511; 
            // 0 = red, 255 = yellow, 511 = green.
            if (value > zero)
            {
                if (pos - zero == 0)
                    v = 255;
                else
                    v = (int)((value - zero) / (pos - zero) * 255.0 + 255.0);
            }
            else
            {
                if (zero - neg == 0)
                    v = 0;
                else
                    v = (int)((value - neg) / (zero - neg) * 255.0);

            }

            v = Math.Max(0, Math.Min(511, v));


            if (v < 255)
            {
                redValue = 255;
                greenValue = Math.Sqrt(v) * 16;
                greenValue = Math.Round(greenValue);
            }
            else
            {
                greenValue = 255;
                v = v - 255;
                redValue = 256 - (v * v / 255);
                redValue = Math.Round(redValue);

            }

            int red = Math.Min(255, Math.Max(0, (int)redValue));
            int green = Math.Min(255, Math.Max(0, (int)greenValue));

            var hexColor = String.Format("rgba({0},{1},{2},{3})",red,green,0,alpha);

            return new HtmlString(hexColor);
        }

        public static HtmlString EditFirstButton(this HtmlHelper html, List<string> items, bool edit = true)
        {
            var count = items.Count();
            var name = "" + count;
            var joined = String.Join(", ", items);
            if (count == 1)
                name = items.First();
            return new HtmlString(ViewOrEdit(html, edit) + " (<span title='" + joined + "'>" + name + "</span>)");
        }

        public static HtmlString Badge<T>(this HtmlHelper<T> html, Func<T, int> count)
        {
            var c = count(html.ViewData.Model);
            if (c != 0)
                return new HtmlString(@"<span class=""badge"">" + c + "</span>");
            return new HtmlString("");
        }

        public static HtmlString ShowModal(this HtmlHelper html, String title, String pullUrl, String pushUrl, String callbackFunction = null, String preSubmitCheck = null, String onComplete = null)
        {
            if (onComplete != null)
                return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "','" + preSubmitCheck + "','" + onComplete + "')");
            if (preSubmitCheck != null)
                return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "','" + preSubmitCheck + "')");
            else if (callbackFunction != null)
                return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "')");
            else
                return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "')");
        }

        public static HtmlString AlertBoxDismissableJavascript(this HtmlHelper html, String messageVariableName, String alertType = "alert-danger")
        {
            return new HtmlString("\"<div class=\\\"alert " + alertType + " alert-dismissable\\\"><button type=\\\"button\\\" class=\\\"close\\\" data-dismiss=\\\"alert\\\" aria-hidden=\\\"true\\\">&times;</button><strong>" + MessageStrings.Warning + "</strong> <span class=\\\"message\\\">\" + " + messageVariableName + " + \"</span></div>\"");

        }
        public static HtmlString AlertBoxDismissable(this HtmlHelper html, String message, String alertType = null, String alertMessage = null)
        {
            if (String.IsNullOrWhiteSpace(alertType))
                alertType = "alert-danger";
            if (String.IsNullOrWhiteSpace(alertMessage))
                alertMessage = MessageStrings.Warning;

            if (!String.IsNullOrWhiteSpace(message))
                return new HtmlString("<div class=\"alert " + alertType + " alert-dismissable\"><button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-hidden=\"true\">&times;</button><strong>" + alertMessage + "</strong> <span class=\"message\">" + message + "</span></div>");
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

        #region Blocks
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

        private class StyleBlock : IDisposable
        {
            private const string styleKey = "styles";
            public static List<string> pageStyles
            {
                get
                {
                    if (HttpContext.Current.Items[styleKey] == null)
                        HttpContext.Current.Items[styleKey] = new List<string>();
                    return (List<string>)HttpContext.Current.Items[styleKey];
                }
            }

            WebViewPage webPageBase;

            public StyleBlock(WebViewPage webPageBase)
            {
                this.webPageBase = webPageBase;
                this.webPageBase.OutputStack.Push(new StringWriter());
            }

            public void Dispose()
            {
                pageStyles.Add(((StringWriter)this.webPageBase.OutputStack.Pop()).ToString());
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
        public static IDisposable BeginStyles(this HtmlHelper helper)
        {
            return new StyleBlock((WebViewPage)helper.ViewDataContainer);
        }

        public static MvcHtmlString PageStyles(this HtmlHelper helper)
        {
            return MvcHtmlString.Create(string.Join(Environment.NewLine, StyleBlock.pageStyles.Select(s => s.ToString())));
        }
        #endregion



    }


}