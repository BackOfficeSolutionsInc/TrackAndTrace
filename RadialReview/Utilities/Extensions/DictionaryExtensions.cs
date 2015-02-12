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

        public static V GetOrAddDefault<K, V>(this Dictionary<K, V> dictionary, K key, Func<K, V> defaultValue)
        {
            V value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            value =defaultValue(key);
            dictionary[key] = value;
            return dictionary[key];
        }

		public static void AddRange<T, K, V>(this Dictionary<K, V> dictionary, IEnumerable<T> range, Func<T, K> keySelector, Func<T, V> valueSelector)
		{
			foreach (var r in range)
			{
				dictionary[keySelector(r)] = valueSelector(r);
			}
		}
		public static void AddRangeNoReplace<T, K, V>(this Dictionary<K, V> dictionary, IEnumerable<T> range, Func<T, K> keySelector, Func<T, V> valueSelector)
		{
			foreach (var r in range)
			{
				var k = keySelector(r);
				if (!dictionary.ContainsKey(k))
					dictionary[k] = valueSelector(r);
			}
		}

		public static void AddRange<T, K>(this Dictionary<K, T> dictionary, IEnumerable<T> range, Func<T, K> keySelector){
			AddRange(dictionary,range,keySelector,x=>x);
		}
		public static void AddRangeNoReplace<T, K>(this Dictionary<K, T> dictionary, IEnumerable<T> range, Func<T, K> keySelector)
		{
			AddRangeNoReplace(dictionary, range, keySelector, x => x);
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