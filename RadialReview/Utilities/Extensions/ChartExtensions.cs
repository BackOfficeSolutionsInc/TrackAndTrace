using RadialReview.Models.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview
{
    public static class ChartExtensions
    {
        public static String[] GetClasses(this ScatterData self)
        {
            return Regex.Split(self.Class, "\\s+");
        }
        public static String[] GetClasses(this ScatterDatum self)
        {
            return Regex.Split(self.Class, "\\s+");
        }

    }
}