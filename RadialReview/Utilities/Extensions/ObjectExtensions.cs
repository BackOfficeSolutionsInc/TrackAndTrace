using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class ObjectExtensions
    {
        //NotNull
        public static R NotNull<T, R>(this T obj, Func<T, R> f) where T : class
        {
            return obj != null ? f(obj) : default(R);
        }

        public static int ToInt(this Boolean b)
        {
            return b ? 1 : 0;
        }

        public static long ToLong(this String s)
        {
            return long.Parse(s);
        }
        public static int ToInt(this String s)
        {
            return int.Parse(s);
        }
        public static double ToDouble(this String s)
        {
            return double.Parse(s);
        }

        public static bool ToBoolean(this String s)
        {
            return bool.Parse(s);
        }
        public static bool ToBooleanJS(this String s)
        {
            return s.ToLower().Contains("true");
        }

        public static DateTime ToDateTime(this String s,String format,double offset=0.0)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;
            return DateTime.ParseExact(s, format, provider).AddHours(offset);
        }

        public static bool Alive(this object obj)
        {
            if (obj is IDeletable)
                return ((IDeletable)obj).DeleteTime == null;
            return true;
        }
    }

}