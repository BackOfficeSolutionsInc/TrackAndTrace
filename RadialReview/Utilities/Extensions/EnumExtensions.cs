using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class EnumExtensions
    {
        public static T Parse<T>(this string enumStr) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum){
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
    }
}