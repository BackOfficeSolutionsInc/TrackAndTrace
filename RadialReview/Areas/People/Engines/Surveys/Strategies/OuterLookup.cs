using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Impl {
	public class OuterLookup : IOuterLookup {
		protected DefaultDictionary<Type, DefaultDictionary<Type, ICollection<object>>> Backing { get; set; }

		public OuterLookup() {
			Backing = new DefaultDictionary<Type, DefaultDictionary<Type, ICollection<object>>>(x => new DefaultDictionary<Type, ICollection<object>>(y => new List<object>()));
		}

		public void AddList<U>(Type classType, IEnumerable<U> objects) {
			var Utype = typeof(U);
			foreach (var o in objects.Cast<object>()) {
				Backing[classType][Utype].Add(o);
			}
		}

		public IDictionary<Type, ICollection<object>> GetLookups(Type Ttype) {
			var dict = Backing[Ttype];
			return dict;
		}

		public void AddItem<U>(Type classType, string key, U item) where U : class {
			var list = Backing[classType][typeof(string)];
			var toRemove = list.Where(x => ((KeyValuePair<string, object>)x).Key == key).ToList();
			foreach (var remove in toRemove) {
				list.Remove(remove);
			}
			list.Add(new KeyValuePair<string, object>(key, item));
		}

		public U GetItem<U>(Type classType, string key) where U : class {
			var found = Backing[classType][typeof(string)].LastOrDefault(x => ((KeyValuePair<string, object>)x).Key == key);
			if (found is KeyValuePair<string, object>) {
				var keyVal = (KeyValuePair<string, object>)found;
				return keyVal.Value as U;
			}
			return null;
		}

		public IInnerLookup GetInnerLookup<T>() {
			return GetInnerLookup(typeof(T));
		}

		public IInnerLookup GetInnerLookup(Type classType) {
			return new InnerLookup(classType, this);
		}

	}

	public class InnerLookup : IInnerLookup {
		protected OuterLookup Parent { get; set; }
		protected Type ClassType { get; set; }
		public InnerLookup(Type classType, OuterLookup parent) {
			Parent = parent;
			ClassType = classType;
		}

		public void AddList<U>(IEnumerable<U> objects) {
			Parent.AddList(ClassType, objects);
		}

		public IReadOnlyCollection<U> GetList<U>() {
			var type = typeof(U);
			return Parent.GetLookups(ClassType)[type].Cast<U>().ToList();
		}

		public void Add<U>(string key, U value) where U : class {
			Parent.AddItem(ClassType, key, value);
		}

		public U Get<U>(string key) where U : class {
			return Parent.GetItem<U>(ClassType, key);
		}

		public U GetOrAdd<U>(string key, Func<string, U> defltValue) where U : class {
			var found = Get<U>(key);
			if (found == null) {
				found = defltValue(key);
				Add(key, found);
			}
			return found;
		}
	}
}