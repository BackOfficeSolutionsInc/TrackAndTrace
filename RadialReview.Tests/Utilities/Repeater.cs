using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Tests.Utilities
{
    public class Repeater
    {
        public static Regression Repeate<T>(int count, IEnumerable<T> vary, Func<T, double> function)
        {
            Stopwatch sw = new Stopwatch();
            var xs = new List<double>();
            var ys = new List<double>();
            foreach (var u in vary)
            {
                for (int i = 0; i < count; i++)
                {
                    sw.Restart();
                    var output = function(u);
                    double time = sw.ElapsedMilliseconds;
                    xs.Add(output);
                    ys.Add(time);
                }
            }

            var r = Regression.Linear(xs, ys);

            return r;
        }
    }
}
