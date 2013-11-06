using System;
using System.Collections.Generic;
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
    }
}