using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDesign.Tests.TestUtilities {
	public class SetUtility {
		public class AddedRemoved<T> {
			public IEnumerable<T> OldValues { get; set; }
			public IEnumerable<T> NewValues { get; set; }
			public IEnumerable<T> AddedValues { get; set; }
			public IEnumerable<T> RemovedValues { get; set; }

			public bool AreSame() {
				return !AddedValues.Any() && !RemovedValues.Any();
			}

			public void PrintDifference() {
				if (AddedValues.Any()) {
					Console.WriteLine("Added:");
					foreach (var a in AddedValues) {
						Console.WriteLine("  - " + a.ToString());
					}
				}
				if (RemovedValues.Any()) {
					Console.WriteLine("Removed:");
					foreach (var a in RemovedValues) {
						Console.WriteLine("  - " + a.ToString());
					}
				}
			}
		}


		public static void AssertEqual<T>(IEnumerable<T> expected, IEnumerable<T> found, string additionaErrorInfo = null, bool inconclusive = false) {
			var res = AddRemove(found, expected);
			var finalErrors = new List<string>();
			if (res.AddedValues.Any()) {
				Console.WriteLine("Were Added:");
				foreach (var i in res.AddedValues) {
					Console.WriteLine("\t" + i.ToString());
				}
				finalErrors.Add("Found " + res.AddedValues.Count() + " additional items. " + additionaErrorInfo ?? "");

			}
			if (res.RemovedValues.Any()) {
				Console.WriteLine("Were Removed:");
				foreach (var i in res.RemovedValues) {
					Console.WriteLine("\t" + i.ToString());
				}
				finalErrors.Add("Expected " + res.RemovedValues.Count() + " additional items. " + additionaErrorInfo ?? "");
			}
			if (finalErrors.Any()) {
				var err = string.Join("\n", finalErrors);
				if (inconclusive)
					throw new AssertInconclusiveException(err);
				throw new Exception(err);
			}
		}



		public static AddedRemoved<T> AddRemove<T>(IEnumerable<T> oldValues, IEnumerable<T> newValues) {
			return AddRemove(oldValues, newValues, x => x);
		}

		public static AddedRemoved<object> AddRemoveBase(IEnumerable oldValues, IEnumerable newValues, Func<object, object> comparison) {
			var newEnum = newValues as object[] ?? newValues.Cast<object>().ToArray();
			var oldEnum = oldValues as object[] ?? oldValues.Cast<object>().ToArray();

			var removed = oldEnum.Where(o => !newEnum.Any(n => comparison(o).Equals(comparison(n)))).ToList();
			var added = newEnum.Where(n => !oldEnum.Any(o => comparison(o).Equals(comparison(n)))).ToList();


			return new AddedRemoved<object>() {
				AddedValues = added,
				RemovedValues = removed,
				OldValues = oldEnum,
				NewValues = newEnum
			};
		}


		public static AddedRemoved<T> AddRemove<T, E>(IEnumerable<T> oldValues, IEnumerable<T> newValues, Func<T, E> comparison) {
			var oldEnum = oldValues as IList<T> ?? oldValues.ToList();
			var newEnum = newValues as IList<T> ?? newValues.ToList();

			var removed = oldEnum.Where(o => !newEnum.Any(n => comparison(o).Equals(comparison(n)))).ToList();
			var added = newEnum.Where(n => !oldEnum.Any(o => comparison(o).Equals(comparison(n)))).ToList();


			return new AddedRemoved<T>() {
				AddedValues = added,
				RemovedValues = removed,
				OldValues = oldEnum,
				NewValues = newEnum
			};
		}

	}
}
