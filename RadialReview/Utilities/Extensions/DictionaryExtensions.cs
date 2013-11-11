using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class DictionaryExtensions
    {
        public static V GetOrDefault<K, V>(this Dictionary<K, V> dictionary, K key)
        {
            V value=default(V);
            dictionary.TryGetValue(key, out value);
            return value;
        }
    }
}