using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.Utilities {
	public static class PermutationUtil {

		private static void RotateRight<T>(IList<T> sequence, int count) {
			T tmp = sequence[count - 1];
			sequence.RemoveAt(count - 1);
			sequence.Insert(0, tmp);
		}

		public static IEnumerable<IList<T>> Permutate<T>(this IList<T> sequence) {
			return Permutate(sequence, sequence.Count);
		}

		private static IEnumerable<IList<T>> Permutate<T>(this IList<T> sequence, int count) {
			if (count == 1)
				yield return sequence.ToList();
			else {
				for (int i = 0; i < count; i++) {
					foreach (var perm in Permutate(sequence, count - 1))
						yield return perm.ToList();
					RotateRight(sequence, count);
				}
			}
		}
	}
}
