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

        /// <summary>
        /// Useful when pairing up two concurrent requests
        /// Ex:
        ///     A========B
        ///         C============D
        ///  = > ACBD
        ///  
        ///     A==========B
        ///         C==D
        ///  
        ///  = > ACDB
        /// 
        /// A is always before B
        /// C always before D
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="orderedSequence1"></param>
        /// <param name="orderedSequence2"></param>
        /// <returns></returns>
        public static IEnumerable<IList<T>> DualOrderdLists<T>(IList<T> orderedSequence1, IList<T> orderedSequence2) {

            var combine = orderedSequence1.Union(orderedSequence2).Select((x,i)=>new { x, i }).ToList();
            var o1 = combine.Take(orderedSequence1.Count()).Select(x=>x.i).ToList();
            var o2 = combine.Skip(orderedSequence1.Count()).Select(x=>x.i).ToList();

            return Permutate(combine).Where(ii => {
                var iter = ii.Select(x => x.i).ToList();
                var idx = 0;
                foreach (var o in o1) {
                    var temp = iter.IndexOf(o);
                    if (temp < idx)
                        return false;
                    idx = temp;
                }
                idx = 0;
                foreach (var o in o2) {
                    var temp = iter.IndexOf(o);
                    if (temp < idx)
                        return false;
                    idx = temp;
                }
                return true;
            }).Select(x=>x.Select(y=>y.x).ToList());


        }
	}
}
