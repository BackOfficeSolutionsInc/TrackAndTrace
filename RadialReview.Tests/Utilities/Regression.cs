using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Tests.Utilities
{
    public class Regression
    {
        public double a { get; set; }
        public double b { get; set; }

        public static Regression Linear(IEnumerable<double> xs, IEnumerable<double> ys)
        {
            // data points

            // build matrices
            var X = DenseMatrix.OfColumns(xs.Count(),2,Enumerable.Repeat(1.0,xs.Count()).AsList(xs));
            /*  new[] { new DenseVector(xs.Count(), 1), new DenseVector(xs) });*/
            var y = DenseVector.OfEnumerable(ys);

            // solve
            var p = X.QR().Solve(y);
            var a = p[0];
            var b = p[1];
            return new Regression{ a=a, b=b };
        }

        public bool IsBounded(double p1, double p2)
        {
            Console.WriteLine(a + "," + b);
            return a < p1 && b < p2;
        }
    }
}
