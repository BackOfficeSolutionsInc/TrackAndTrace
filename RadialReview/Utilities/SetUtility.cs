using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
	public class SetUtility
	{

		public static void AddRemove<T>(IEnumerable<T> oldValues, IEnumerable<T> newValues, out IEnumerable<T> added, out IEnumerable<T> removed)
		{
			var add = new List<T>();
			var remove = new List<T>();

			foreach (var o in oldValues)
			{
				if (!newValues.Any(n => o.Equals(n)))
				{
					remove.Add(o);
				}
			}

			foreach (var n in newValues)
			{
				if (!oldValues.Any(o => o.Equals(n)))
				{
					add.Add(n);
				}
			}

			added = add;
			removed = remove;

		}
	}
}