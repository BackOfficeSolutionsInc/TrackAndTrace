using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class Point2D{
        public double X { get; set; }
        public double Y { get; set; }

    }

    public class HeatMapBinner
    {

        public static double[][] Bin(List<Point2D> points, int xBins, int yBins, Point2D bottomLeft, Point2D topRight)
        {
            double[][] bins = new double[xBins][];
            for (int i = 0; i < xBins; i++)
            {
                bins[i] = new double[yBins];
            }


            foreach (var p in points)
            {
                int xBin = (int)((p.X - bottomLeft.X) / (topRight.X - bottomLeft.X) * xBins);
                int yBin = (int)((p.Y - bottomLeft.Y) / (topRight.Y - bottomLeft.Y) * yBins);

                bins[xBin][yBin] += 1;
            }

            return bins;
        }

    }
}