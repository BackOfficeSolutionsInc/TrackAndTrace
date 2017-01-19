using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
	public class DefaultDictionary<K, V> : IEnumerable<KeyValuePair<K, V>> {
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
			DefaultFunction = defaultFunc;
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
	}
}