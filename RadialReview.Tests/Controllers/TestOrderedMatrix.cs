using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Tests.Controllers
{
    [TestClass]
    public class TestOrderedMatrix
    {
        [TestMethod]
        public void Reorder()
        {
            var s = new Stopwatch();
            s.Start();
            var array = new double[][]{
                new double[]{0.59693792,	0.624181094,	0.215714337,	0.419517023},
                new double[]{0.899579521,	0.083985372,	0.37399041,     0.70148705 },
                new double[]{0.459407606,	0.855726372,	0.61093908,     0.210967056},
                new double[]{0.303880622,	0.950343473,	0.757398219,    0.889234764},
                new double[]{0.948084877,	0.69541774 ,    0.053049697,    0.092166723},
                new double[]{0.478198069,	0.117605517,	0.163803729,    0.833526534},
                new double[]{0.208276637,	0.462139148,	0.3051785,      0.449521523},
                new double[]{0.547907048,	0.142367685,	0.581945779,    0.646428363},
                new double[]{0.660661726,	0.901626709,	0.741915494,    0.918144575},
                new double[]{0.377205106,	0.933071831,	0.616662266,    0.079392429},
            };

            var cols = array.Count();
            var rows = array[0].Count();

            var bestMatch = Double.NegativeInfinity;
            var bestX = new int[rows];
            var bestY = new int[cols];
            
            var curX=Enumerable.Range(0, rows);
            var curY=Enumerable.Range(0, cols);

            var r = new Random(12345);

            for (int i = 0; i < 100; i++)
            {
                var score=Score(array,curX.ToArray(),curY.ToArray());
                if (score>bestMatch){
                    bestMatch=score;
                    bestX = curX.ToArray();
                    bestY = curY.ToArray();
                }
                curX = curX.Shuffle(r);
                curY = curY.Shuffle(r);
            }

            var end = s.ElapsedMilliseconds;

            Console.WriteLine(String.Join("\n", bestX));
            Console.WriteLine();
            Console.WriteLine(String.Join("\n", bestY));
            Console.WriteLine();
            Console.WriteLine(bestMatch);
            Console.WriteLine(end);
        }

        public double Score(double[][] data, int[] xOrder, int[] yOrder)
        {
            double score = 0;

            //row-wise
            var listX = new List<double>();//data.Select((d,i)=>data[i].Average()).ToArray();
            for (int r = 0; r < yOrder.Length; r++)
            {
                double sum = 0;
                double avg = 0;
                var len = xOrder.Length;
                for (int c = 0; c < len; c++)
                {
                    sum += data[yOrder[r]][xOrder[c]];
                }
                if (len > 0)
                    avg = sum / len;
                listX.Add(avg);
            }

            for (int i = 0; i < listX.Count() - 1; i++)
            {
                score += Math.Sign(listX[i] - listX[i + 1]);
            }

            //col-wise
            var listY=new List<double>();//data.Select((d,i)=>data[i].Average()).ToArray();
            for(int c=0;c<xOrder.Length;c++)
            {
                double sum=0;    
                double avg=0;
                var len=yOrder.Length;
                for(int r=0;r<len;r++)
                {
                    sum += data[yOrder[r]][xOrder[c]];
                }
                if (len>0)
                    avg=sum/len;
                listY.Add(avg);
            }

            for (int i = 0; i < listY.Count() - 1; i++)
            {
                score += Math.Sign(listY[i] - listY[i + 1]);
            }
            
            return score;
        }
    }
}
