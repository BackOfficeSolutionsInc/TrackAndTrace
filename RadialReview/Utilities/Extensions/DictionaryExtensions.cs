using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class DictionaryExtensions
    {
        public static V GetOrDefault<K, V>(this Dictionary<K, V> dictionary, K key,V defaultValue)
        {
            V value;
            if (dictionary.TryGetValue(key, out value))
                return value;
            return defaultValue;
        }

        public static void Update<K, V>(this Dictionary<K, V> dictionary, K key, V defaultValue, Func<V, V> updateTo)
        {
            var old = GetOrDefault(dictionary, key, defaultValue);
            var updated = updateTo(old);
            dictionary[key] = updated;
        }
        public static void Update<K, V>(this Dictionary<K, V> dictionary, K key, V defaultValue, Action<V> updateTo)
        {
            if (!dictionary.ContainsKey(key))
                dictionary[key] = defaultValue;
            updateTo(dictionary[key]);
        }
    }
}