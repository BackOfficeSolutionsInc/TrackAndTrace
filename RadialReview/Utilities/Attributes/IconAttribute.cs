using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Attributes
{
    [AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class IconAttribute : DisplayNameAttribute
    {
        public BootstrapGlyphs Glyph;
        public double Version;

        public IconAttribute(BootstrapGlyphs glyph)
        {
            Glyph = glyph;
        }

        public HtmlString AsHtml(string title="")
        {
            var clss=Glyph.ToString().Replace("_", "-").Replace("@","");
            return new HtmlString("<span title=\"" + title + "\" class=\"icon glyphicon glyphicon-" + clss + "\"></span>");
        }
    }
}