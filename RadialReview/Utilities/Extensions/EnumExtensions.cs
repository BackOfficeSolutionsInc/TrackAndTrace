using RadialReview.Models.Enums;
using RadialReview.Utilities.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Resources;
using System.Web;

namespace RadialReview
{
    public static class EnumExtensions
    {
        public static T Parse<T>(this string enumStr) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enum type");
            }

            return (T)Enum.Parse(typeof(T), enumStr);
        }

        public static T Parse<T>(this T e, String toParse) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enum type");
            }

            return (T)Enum.Parse(typeof(T), toParse);
        }

        public static String GetDisplayName<T>(this T value) where T : struct, IConvertible
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var descriptionAttributes = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (descriptionAttributes == null) return string.Empty;
            if (descriptionAttributes.Length > 0){
	            if (descriptionAttributes[0].ResourceType == null)
		            return descriptionAttributes[0].Name;
                return new ResourceManager(descriptionAttributes[0].ResourceType).GetString(descriptionAttributes[0].Name);
            }
            return value.ToString();
        }

        public static IEnumerable<Enum> GetFlags(this Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public static HtmlString GetIcon(this bool value)
        {
            /*var fieldInfo = value.GetType().GetField(value.ToString());
            var iconAttr = fieldInfo.GetCustomAttributes(typeof(IconAttribute), false) as IconAttribute[];
            if (iconAttr == null) return new HtmlString(string.Empty);
            if (iconAttr.Length > 0)
            {
                var name = GetDisplayName(value);
                return iconAttr[0].AsHtml(name);
            }
            return new HtmlString(string.Empty);*/
            if (value)
                return new HtmlString("<span class='glyphicon glyphicon-ok-sign icon greenHover'></span>");
            return new HtmlString("<span class='glyphicon glyphicon-minus-sign icon redHover'></span>");
        }
        public static HtmlString GetIcon(this bool value, string up, string down, String upTitle, String downTitle, String upAttributes = "", String downAttributes = "")
        {
            /*var fieldInfo = value.GetType().GetField(value.ToString());
            var iconAttr = fieldInfo.GetCustomAttributes(typeof(IconAttribute), false) as IconAttribute[];
            if (iconAttr == null) return new HtmlString(string.Empty);
            if (iconAttr.Length > 0)
            {
                var name = GetDisplayName(value);
                return iconAttr[0].AsHtml(name);
            }
            return new HtmlString(string.Empty);*/
            if (value)
                return new HtmlString("<span class='green glyphicon " + up.ToString() + " icon'  title='" + upTitle + "' " + upAttributes + "></span>");
            return new HtmlString("<span class='red glyphicon " + down.ToString() + " icon'  title='" + downTitle + "' " + downAttributes + "></span>");
        }


		public static bool IsSubsetOf<T>(this T a,T b) where T : struct, IConvertible {
			var aa = (long)(object)a;
			var bb = (long)(object)b;

			return (aa & bb) == aa;
		}


        public static HtmlString GetIcon<T>(this T value) where T : struct, IConvertible
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var iconAttr = fieldInfo.GetCustomAttributes(typeof(IconAttribute), false) as IconAttribute[];
            if (iconAttr == null) return new HtmlString(string.Empty);
            if (iconAttr.Length > 0)
            {
                var name = GetDisplayName(value);
                return iconAttr[0].AsHtml(name);
            }
            return new HtmlString(string.Empty);
        }
    }
}