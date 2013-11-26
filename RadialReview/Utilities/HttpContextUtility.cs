using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class HttpContextUtility
    {

        public static V Push<K,V>(this HttpContextBase context,K key,V value)
        {
            context.Items[key] = value;
            return value;
        }

        public static V Get<K, V>(this HttpContextBase context, K key, Func<K, V> generator, Predicate<V> forceUpdate)
        {
            if (context == null)
                throw new ArgumentException("Requires Context");

            var generated = false;
            if (context.Items[key] == null || forceUpdate((V)context.Items[key]))
            {
                context.Items[key] = generator(key);
            }
            return (V)context.Items[key];
        }
        public static V Get<K, V>(this HttpContextBase context, K key, Func<K, V> generator, bool forceUpdate)
        {
            return Get(context, key, generator, x => forceUpdate);
        }
    }
}