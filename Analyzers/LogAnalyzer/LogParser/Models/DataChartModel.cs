using ParserUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Models {

    [DebuggerDisplay("({X}, {Y})")]
    [Serializable]
    public class Point {
        /// <summary>
        /// Use utc time
        /// </summary>
        public DateTime X { get; set; }
        public decimal? Y { get; set; }

        public Point() {
        }

        public Point(DateTime x, decimal? y) {
            X = x;
            Y = y;
        }
        public Point(long utc, decimal? y) : this(utc.ToDateTime(), y) {
        }

    }

    public class DataChartModel {
        public string Name { get; set; }
        public List<Point> Datapoints { get; set; }
        public Stat Statistic { get; set; }
    }
}
