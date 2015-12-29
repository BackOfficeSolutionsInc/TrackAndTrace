using System.Web.Helpers;
using RadialReview.Models;
using RadialReview.Properties;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using RadialReview.Utilities;


namespace System.Web
{
    public static class HtmlExtensions
	{
		public static string VideoConferenceUrl(this HtmlHelper html, string resource = null)
		{
			return Config.VideoConferenceUrl(resource);
		}

		public static string GetBaseUrl(this HtmlHelper html, string resource = null)
		{
			var server =   Config.BaseUrl((OrganizationModel)html.ViewBag.Organization).TrimEnd('/');
			if (resource != null){
				server = server + "/" + resource.TrimStart('/');
			}

			return server;
		}
		public static UserOrganizationModel UserOrganization(this HtmlHelper html)
		{
			return (UserOrganizationModel)html.ViewBag.UserOrganization;
		}

		public static OrganizationModel Organization(this HtmlHelper html){
			return (OrganizationModel)html.ViewBag.Organization;
		}

		public static string ProductName(this HtmlHelper html){
			return Config.ProductName(html.Organization());
		}

	    public static string ReviewName(this HtmlHelper html)
		{
			return Config.ReviewName(html.Organization());
	    }

        public static MvcHtmlString CollapseSection(this HtmlHelper html,String title,String viewName,object model,string checkboxClass=null)
        {
			html.ViewData["PartialViewName"] = viewName;
			html.ViewData["SectionTitle"] = title;
			html.ViewData["CheckboxClass"] = checkboxClass;
            return html.Partial("Partial/Collapsable",model,html.ViewData);
        }

        public static HtmlString ViewOrEdit(this HtmlHelper html, bool edit,bool icon=true)
        {
            if(icon)
                return new HtmlString(edit ? "<span class='glyphicon glyphicon-pencil viewEdit edit'></span>" : "<span class='glyphicon glyphicon-eye-open viewEdit view'></span>");
            return new HtmlString(edit ? "Edit" : "View");
        }

        public static HtmlString GrayScale(this HtmlHelper html, double value, double neg, double pos, double alpha)
        {
            double scale=0;
            if (pos-neg!=0)
                scale=(value - neg) / (pos - neg)*255.0;

            int coerced = (int)(255-Math.Max(0, Math.Min(scale, 255.0)));

            return new HtmlString(String.Format("rgba({0},{0},{0},{1})", coerced, alpha));

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
            var after = "";
            var joined = String.Join(", ", items);
            if (count == 1)
                name = items.First();
            else if (count == 0)
            {
                name = "<i>None</i>";
                joined = "None";
            }
            else
            {
                name = items.First()+"<span class='hidden'>"+String.Join(",",items.Skip(1))+"</span>";
                after = "(+" + (count - 1) + ")";
                joined = String.Join(",", items);
            }

			//return new HtmlString("<span class='editFirst'><span class='icon'>" + ViewOrEdit(html, edit).ToHtmlString() + "</span><span title='" + joined + "' class='text'><span class='uncollapsable'>" + after + "</span><span class='collapsable'>" + name + "</span></span></span>");
			return new HtmlString("<span class='editFirst'><span title='" + joined + "' class='text'><span class='uncollapsable'>" + after + "</span><span class='collapsable'>" + name + "</span></span></span>");
		}

        public static HtmlString Badge<T>(this HtmlHelper<T> html, Func<T, int> count)
        {
            var c = count(html.ViewData.Model);
            if (c != 0)
                return new HtmlString(@"<span class=""badge"">" + c + "</span>");
            return new HtmlString("");
        }

		public static HtmlString ShowModal(this HtmlHelper html, String title, String pullUrl, String pushUrl, String callbackFunction = null, String preSubmitCheck = null, String onComplete = null, String onCompleteFunction = null)
        {
            title = title.Replace("'", "\\'");
			if (onComplete != null || onCompleteFunction!=null){
				var c = "";
				if (onComplete != null)
					c = "'" + onComplete + "'";
				else
					c = onCompleteFunction;
				
				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "','" + preSubmitCheck + "'," + c + ")");
			}
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
		public static HtmlString ValidationSummaryMin(this HtmlHelper html, Boolean excludePropertyErrors=false)
		{

			/*
			 *
			 * <div class="validation-summary-errors" data-valmsg-summary="true">
			 * <ul><li>Value must be between 1 and 10.</li>
				<li>Value must be between 1 and 10.</li>
				<li>Value must be between 1 and 10.</li>
				<li>Value must be between 1 and 10.</li>
				<li>Value must be between 1 and 10.</li>
				</ul></div> 
			 * 
			 */

			var errors = html.ViewData.ModelState.Values.SelectMany(x => x.Errors);
			var output = @"<div class=""validation-summary-" + (errors.Any() ? "errors" : "valid") + @" alert alert-error"" data-valmsg-summary=""true"">";
			//output += @"<button type=""button"" class=""close"" data-dismiss=""alert"">&times;</button><table><tr><td><strong>Error:</strong></td><td>";
			output += "<ul>";
			if (errors.Any())
			{
				foreach (var li in errors.GroupBy(x=>x.ErrorMessage))
				{
					output += @"<li>" + li.First().ErrorMessage + "</li>";
				}
			}
			else
			{
				output += @"<li style=""display:none""></li>";
			}
			output += "</ul>"+/*"</td></tr></table>"*/ "</div>";
			return new HtmlString(output);
		}

	    public static MvcHtmlString ArrayToString<T>(this HtmlHelper html, IEnumerable<T> items)
	    {
		    return new MvcHtmlString(Json.Encode(items));

	    }



        #region Blocks
        private class ScriptBlock : IDisposable
        {
            private const string scriptsKey = "scripts";

			private string UniqueKey = null;
            public static List<string> pageScripts
            {
                get
                {
                    if (HttpContext.Current.Items[scriptsKey] == null)
                        HttpContext.Current.Items[scriptsKey] = new List<string>();
                    return (List<string>)HttpContext.Current.Items[scriptsKey];
                }
            }

			public static List<string> uniqueKeys
			{
				get
				{
					if (HttpContext.Current.Items[scriptsKey + "_uniqueKeys"] == null)
						HttpContext.Current.Items[scriptsKey + "_uniqueKeys"] = new List<string>();
					return (List<string>)HttpContext.Current.Items[scriptsKey + "_uniqueKeys"];
				}
			}


            WebViewPage webPageBase;

			public ScriptBlock(WebViewPage webPageBase, string uniqueKey)
            {
                this.webPageBase = webPageBase;
				this.webPageBase.OutputStack.Push(new StringWriter());
				this.UniqueKey = uniqueKey;
            }

            public void Dispose()
            {
	            if (UniqueKey == null || !uniqueKeys.Contains(UniqueKey)){
		            if (UniqueKey != null)
			            uniqueKeys.Add(UniqueKey);
		            pageScripts.Add(((StringWriter) this.webPageBase.OutputStack.Pop()).ToString());
	            }
            }
        }

        private class StyleBlock : IDisposable
        {
            private const string styleKey = "styles";
			private string UniqueKey = null;
			public static List<string> pageStyles
			{
				get
				{
					if (HttpContext.Current.Items[styleKey] == null)
						HttpContext.Current.Items[styleKey] = new List<string>();
					return (List<string>)HttpContext.Current.Items[styleKey];
				}
			}

			public static List<string> uniqueKeys
			{
				get
				{
					if (HttpContext.Current.Items[styleKey + "_uniqueKeys"] == null)
						HttpContext.Current.Items[styleKey + "_uniqueKeys"] = new List<string>();
					return (List<string>)HttpContext.Current.Items[styleKey + "_uniqueKeys"];
				}
			}

            WebViewPage webPageBase;

            public StyleBlock(WebViewPage webPageBase,string uniqueKey)
            {
                this.webPageBase = webPageBase;
                this.webPageBase.OutputStack.Push(new StringWriter());
	            this.UniqueKey = uniqueKey;
            }

            public void Dispose()
            {
	            if (UniqueKey == null || !uniqueKeys.Contains(UniqueKey)){
					if (UniqueKey!=null)
						uniqueKeys.Add(UniqueKey);
		            pageStyles.Add(((StringWriter) this.webPageBase.OutputStack.Pop()).ToString());
	            }
            }
        }

		public static IDisposable BeginScripts(this HtmlHelper helper, string uniqueKey = null)
        {
            return new ScriptBlock((WebViewPage)helper.ViewDataContainer, uniqueKey);
        }

        public static MvcHtmlString PageScripts(this HtmlHelper helper)
        {
            return MvcHtmlString.Create(string.Join(Environment.NewLine, ScriptBlock.pageScripts.Select(s => s.ToString())));
        }
        public static IDisposable BeginStyles(this HtmlHelper helper,string uniqueKey=null)
        {
            return new StyleBlock((WebViewPage)helper.ViewDataContainer,uniqueKey);
        }

        public static MvcHtmlString PageStyles(this HtmlHelper helper)
        {
            return MvcHtmlString.Create(string.Join(Environment.NewLine, StyleBlock.pageStyles.Select(s => s.ToString())));
        }
        #endregion



    }


}