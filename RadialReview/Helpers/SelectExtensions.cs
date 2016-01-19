using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using RadialReview;
using System.ComponentModel.DataAnnotations;
using System.Resources;

namespace System.Web
{
    public static class SelectExtensions
    {

        public static string GetInputName<TModel, TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Call)
            {
                MethodCallExpression methodCallExpression = (MethodCallExpression)expression.Body;
                string name = GetInputName(methodCallExpression);
                return name.Substring(expression.Parameters[0].Name.Length + 1);

            }
            return expression.Body.ToString().Substring(expression.Parameters[0].Name.Length + 1);
        }

        private static string GetInputName(MethodCallExpression expression)
        {
            // p => p.Foo.Bar().Baz.ToString() => p.Foo OR throw...
            MethodCallExpression methodCallExpression = expression.Object as MethodCallExpression;
            if (methodCallExpression != null)
            {
                return GetInputName(methodCallExpression);
            }
            return expression.Object.ToString();
        }

        public static MvcHtmlString EnumDropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes = null) where TModel : class
        {
            if (htmlAttributes == null)
                htmlAttributes = new { };

            string inputName = GetInputName(expression);
            var value = htmlHelper.ViewData.Model == null
                ? default(TProperty)
                : expression.Compile()(htmlHelper.ViewData.Model);
            //return htmlHelper.DropDownListFor(expression,ToSelectList(typeof(TProperty), value.ToString()));
            return htmlHelper.DropDownList(inputName, ToSelectList(typeof(TProperty), value.ToString()), htmlAttributes);
        }
		public static MvcHtmlString EnumDropDownList<TModel, TEnum>(this HtmlHelper<TModel> htmlHelper, String name, TEnum selected, object htmlAttributes = null)
			where TModel : class
			where TEnum : struct, IConvertible
        {
            return htmlHelper.DropDownList(name, ToSelectList(typeof(TEnum), selected.ToString()),htmlAttributes);
        }

        public static List<SelectListItem> ToSelectList(Type enumType, string selectedItem)
        {
            var items = new List<SelectListItem>();
            foreach (var item in Enum.GetValues(enumType))
            {
                var fi = enumType.GetField(item.ToString());
                var attribute = fi.GetCustomAttributes(typeof(DisplayAttribute), true).FirstOrDefault() as DisplayAttribute;
	            string title;
	            if (attribute != null){
		            title = attribute.ResourceType == null ? attribute.Name : new ResourceManager(attribute.ResourceType).GetString(attribute.Name);
	            }else{
		            title = item.ToString();
	            }
	            if (item.ToString().ToLower() == "invalid")
                    continue;
                
                var listItem = new SelectListItem
                {
                    Value = item.ToString(),
                    Text = title,
                    Selected = selectedItem == (item).ToString()
                };
                items.Add(listItem);
            }

            return items;
        }
    }
}