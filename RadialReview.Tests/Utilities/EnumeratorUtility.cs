using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Tests.Utilities
{

    public class Enumer<T>
    {
        //func takes the object and transforms it given for the possible tprop
        public static Enumer<T> Create<TProp>(Action<T, TProp> func, params TProp[] possible)
        {
            return new Enumer<T>((x, y) => func(x, (TProp)y), possible.Cast<object>());
        }

        public Action<T, object> Transform { get; set; }
        public object[] Possible { get; set; }

        internal Enumer(Action<T, object> transform, params object[] possible)
        {
            this.Transform = transform;
            this.Possible = possible;
        }

    }

    public class EnumeratorUtility
    {


        public static List<T> Enumerate<T>(Func<T> initialize, params Enumer<T>[] enumers)
        {
            return null;
            //return Recurse(initialize,)
        }

        private static List<T> Recurse<T>(Func<T> initializer, Enumer<T> currentEnumer, params Enumer<T>[] remainingEnumers)
        {
            List<T> possiblities = new List<T>();

            if (remainingEnumers.Count() == 0)
            {
                foreach (var prop in currentEnumer.Possible)
                {
                    var obj =initializer();
                    currentEnumer.Transform(obj, prop);
                    possiblities.Add(obj);
                }
            }
            else
            {
                var next = remainingEnumers.FirstOrDefault();
                var newRemain = remainingEnumers.ToList();
                newRemain.RemoveAt(0);
                possiblities.AddRange(Recurse(initializer, next, newRemain.ToArray()));
            }

            return possiblities;
        }

    }
}
