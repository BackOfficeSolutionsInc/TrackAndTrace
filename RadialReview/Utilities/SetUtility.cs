using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
	public class SetUtility
	{
		public class AddedRemoved<T>
		{
			public IEnumerable<T> OldValues { get; set; }
			public IEnumerable<T> NewValues { get; set; }
			public IEnumerable<T> AddedValues { get; set; }
			public IEnumerable<T> RemovedValues { get; set; }
		}

		public static AddedRemoved<T> AddRemove<T>(IEnumerable<T> oldValues, IEnumerable<T> newValues)
		{
			return AddRemove(oldValues, newValues, x => x);
		}

		public static AddedRemoved<object> AddRemoveBase(IEnumerable oldValues, IEnumerable newValues, Func<object, object> comparison)
		{
			var newEnum = newValues as object[] ?? newValues.Cast<object>().ToArray();
			var oldEnum = oldValues as object[] ?? oldValues.Cast<object>().ToArray();

			var removed = oldEnum.Where(o => !newEnum.Any(n => comparison(o).Equals(comparison(n)))).ToList();
			var added   = newEnum.Where(n => !oldEnum.Any(o => comparison(o).Equals(comparison(n)))).ToList();


			return new AddedRemoved<object>()
			{
				AddedValues = added,
				RemovedValues = removed,
				OldValues = oldEnum,
				NewValues = newEnum
			};
		}


		public static AddedRemoved<T> AddRemove<T, E>(IEnumerable<T> oldValues, IEnumerable<T> newValues, Func<T, E> comparison)
		{
			var oldEnum = oldValues as IList<T> ?? oldValues.ToList();
			var newEnum = newValues as IList<T> ?? newValues.ToList();

			var removed = oldEnum.Where(o => !newEnum.Any(n => comparison(o).Equals(comparison(n)))).ToList();
			var added   = newEnum.Where(n => !oldEnum.Any(o => comparison(o).Equals(comparison(n)))).ToList();


			return new AddedRemoved<T>()
			{
				AddedValues = added,
				RemovedValues = removed,
				OldValues = oldEnum,
				NewValues = newEnum
			};
		}

	}
}