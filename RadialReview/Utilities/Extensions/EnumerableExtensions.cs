using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
        public static IEnumerable<SelectListItem> ToSelectList<T, TId>(this IEnumerable<T> self, Func<T, String> textSelector, Func<T, TId> idSelector, TId selected = default(TId))
        {
            return self.Select(x=>{
                var id=idSelector(x);
                var text=textSelector(x);
                return new SelectListItem(){Selected=(id.Equals(selected)),Text=text,Value=id.ToString()};
            });
        }

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
            
        public static IEnumerable<TSource> UnionBy<TSource,TProp>(this IEnumerable<TSource> first,Func<TSource,TProp> keySelector,params IEnumerable<TSource>[] remaining)
        {
            var enumerables=first;
            foreach(var r in remaining)
            {
                first = first.Concat(r);
            }
            return first.GroupBy(keySelector).Select(x => x.First());
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var rng = new Random();
            return source.Shuffle(rng);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (rng == null) throw new ArgumentNullException("rng");

            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(
            this IEnumerable<T> source, Random rng)
        {
            var buffer = source.ToList();

            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }

        /*public static Boolean Contains<T>(this IEnumerable<T> enumerable, Func<T, Boolean> contains)
        {
            return enumerable.FirstOrDefault(contains) != null;
        }*/

    }
}