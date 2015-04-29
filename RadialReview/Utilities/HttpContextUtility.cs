using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace RadialReview
{
    public static class HttpContextUtility
    {
        //protected static Dictionary<object, object> DeepStore = new Dictionary<object, object>();

        public static V Push<K,V>(this HttpContextBase context,K key,V value)
        {
            context.Items[key] = value;
            return value;
        }

        public static V Get<V>(this HttpContextBase context, String key, Func<String, V> generator, Predicate<V> forceUpdate, bool deepSave=false)
        {
            if (context == null)
                throw new ArgumentException("Requires Context");

            //var generated = false;
            if (deepSave) {
                if (context.Cache.Get(key) == null || forceUpdate((V)context.Cache.Get(key))){
                    context.Cache.Insert(key,generator(key),null);
                }
                return (V)context.Cache.Get(key);
            }else{
                if (context.Items[key] == null || forceUpdate((V)context.Items[key])){
	                context.Push(key, generator(key));
                }
                return (V)context.Items[key];
            }
        }
        public static V Get<V>(this HttpContextBase context, String key, Func<String, V> generator, bool forceUpdate, bool deepSave = false)
        {
            return Get(context, key, generator, x => forceUpdate, deepSave);
        }

        public static void CacheAdd<V>(this HttpContextBase context, String key, V value, DateTime expires)
        {
            context.Cache.Add(key, value, null, expires, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }

        public static V CacheGet<V>(this HttpContextBase context, String key)
        {
            return (V)context.Cache.Get(key);
        }

        public static bool CacheContains(this HttpContextBase context, String key)
        {
            return context.Cache.Get(key) != null;
        }
        public static bool CacheInvalidate(this HttpContextBase context, String key)
        {
            return context.Cache.Remove(key) != null;
        }
    }
}