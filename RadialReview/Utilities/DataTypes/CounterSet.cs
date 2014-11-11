using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
	public class CounterSet<T> : IEnumerable<KeyValuePair<T,int>>
	{
		protected DefaultDictionary<T,int> Backing = new DefaultDictionary<T, int>(x=>0);

		public int Add(T item)
		{
			var count = Backing[item]+1;
			Backing[item] = count;
			return count;
		}

		public int GetCount(T item)
		{
			return Backing.Keys.Contains(item) ? Backing[item] : 0;
		}


		#region IEnumerable<KeyValuePair<T,int>> Members

		public IEnumerator<KeyValuePair<T, int>> GetEnumerator()
		{
			return Backing.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return Backing.GetEnumerator();
		}

		#endregion
	}
}