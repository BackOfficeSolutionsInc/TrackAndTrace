using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
	public class DefaultDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>, IDictionary<K,V> {
		public IEnumerable<K> Keys {
			get { return Backing.Keys.ToList(); }
		}

		public Dictionary<K, V> Backing { get; private set; }

		//Key,Value,=DefaultValue
		public Func<K, V> DefaultFunction { get; private set; }
		/// <summary>
		/// Key,OldValue,AddedValue,=NewValue
		/// </summary>
		public Func<K, V, V, V> MergeFunction { get; private set; }


        /// <summary>
        /// Creates a default dictionary with mergeFunction as an overwrite
        /// </summary>
        /// <param name="defaultFunc"></param>
        public DefaultDictionary(Func<K, V> defaultFunc) : this(defaultFunc, (k, old, add) => add) {
		}

		public DefaultDictionary(Func<K, V> defaultFunc, Func<K, V, V, V> mergeFunc) {
			DefaultFunction = defaultFunc?? new Func<K,V>(x=> default(V));
			MergeFunction = mergeFunc;
			Backing = new Dictionary<K, V>();
		}

		public V this[K key] {
			get {
				if (Backing.ContainsKey(key)) {
					return Backing[key];
				} else {
					var defaultValue = DefaultFunction(key);
					Backing[key] = defaultValue;
					return defaultValue;
				}
			}
			set {
				Backing[key] = value;
			}
		}

		public V Merge(K key, V value) {
			var merged = MergeFunction(key, this[key], value);
			this[key] = merged;
			return merged;
		}


		public IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
			return Backing.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return Backing.GetEnumerator();
		}


        ICollection<K> IDictionary<K, V>.Keys { get { return ((IDictionary<K, V>)Backing).Keys; } }
        public ICollection<V> Values { get { return ((IDictionary<K, V>)Backing).Values; } }
        public int Count { get { return ((IDictionary<K, V>)Backing).Count; } }
        public bool IsReadOnly { get { return ((IDictionary<K, V>)Backing).IsReadOnly; } }
        public bool ContainsKey(K key) {
            return ((IDictionary<K, V>)Backing).ContainsKey(key);
        }

        public void Add(K key, V value) {
            ((IDictionary<K, V>)Backing).Add(key, value);
        }

        public bool Remove(K key) {
            return ((IDictionary<K, V>)Backing).Remove(key);
        }

        public bool TryGetValue(K key, out V value) {
            return ((IDictionary<K, V>)Backing).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<K, V> item) {
            ((IDictionary<K, V>)Backing).Add(item);
        }

        public void Clear() {
            ((IDictionary<K, V>)Backing).Clear();
        }

        public bool Contains(KeyValuePair<K, V> item) {
            return ((IDictionary<K, V>)Backing).Contains(item);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) {
            ((IDictionary<K, V>)Backing).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<K, V> item) {
            return ((IDictionary<K, V>)Backing).Remove(item);
        }
    }
}