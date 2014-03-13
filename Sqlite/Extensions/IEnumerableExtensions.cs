using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlite
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> NoIntersect<T>(this IEnumerable<T> array1, IEnumerable<T> array2, IEqualityComparer<T> comparer=null)
        {
            if (comparer == null)
                return array1.Except(array2).Union(array2.Except(array1));               

            return array1.Except(array2, comparer).Union(array2.Except(array1, comparer), comparer);

        }
    }
}
