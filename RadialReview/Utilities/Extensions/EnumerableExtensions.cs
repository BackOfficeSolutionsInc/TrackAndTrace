using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public class AliveEnumerable<TSource> : IEnumerable<TSource>
    {
        public IEnumerable<TSource> Wrapped { get; set; }

        public AliveEnumerable(IEnumerable<TSource> source)
        {
            Wrapped = source;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            return Wrapped.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Wrapped.GetEnumerator();
        }
    }

    public static class EnumerableExtensions
    {
        public static List<T> AsList<T>(this T first,params T[] after){
            var output=new List<T>{first};
            output.AddRange(after);
            return output;
        }

        public static IEnumerable<TResult> SelectAlive<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector) where TSource : IDeletable
        {
            return source.Alive().Select(selector);
        }
        public static IEnumerable<TResult> SelectAlive<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TSource : IDeletable
        {
            return source.Alive().Select(selector);
        }

        public static IEnumerable<T> Alive<T>(this IEnumerable<T> source) where T :IDeletable
        {
            if (source == null)
                return null;

            if (source is AliveEnumerable<T>)
                return source;
            return new AliveEnumerable<T>(source.Where(x=>x.DeleteTime==null));
        }

        public static List<TSource> ToListAlive<TSource>(this IEnumerable<TSource> source) where TSource : IDeletable
        {
            return source.Alive().ToList();
        }
            

        /*public static Boolean Contains<T>(this IEnumerable<T> enumerable, Func<T, Boolean> contains)
        {
            return enumerable.FirstOrDefault(contains) != null;
        }*/

    }
}