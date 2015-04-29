﻿using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Ajax.Utilities;
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
		public static int? TryParse(this string str)
		{
			int o;
			if (int.TryParse(str, out o))
				return o;
			return null;
		}

        //NotNull
        public static R NotNull<T, R>(this T obj, Func<T, R> f) 
        {
            return obj != null ? f(obj) : default(R);
        }

        public static int ToInt(this Boolean b)
        {
            return b ? 1 : 0;
        }
		public static int ToLong(this Boolean b)
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
            var provider = CultureInfo.InvariantCulture;
            return DateTime.ParseExact(s, format, provider).AddHours(offset);
        }

        public static bool Alive(this object obj)
        {
            if (obj is IDeletable)
                return ((IDeletable)obj).DeleteTime == null;
            return true;
        }

		public static TRef Get<T, TRef>(this T obj, Expression<Func<T, TRef>> selector)
		{
			return selector.Compile()(obj);
		}
		public static void Set<T, TRef>(this T obj, Expression<Func<T, TRef>> selector,TRef value)
		{
			var prop = (PropertyInfo)((MemberExpression)selector.Body).Member;
			prop.SetValue(obj, value, null);
		}

		public static T Touch<T>(this T self) where T : IEnumerable
	    {
			foreach (var o in self){
				if (o is IEnumerable){
					((IEnumerable)o).Touch();
				}
			}
			return self;
	    }
    }

}