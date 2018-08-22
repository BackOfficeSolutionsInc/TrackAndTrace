using LogParser.Downloaders;
using LogParser.Models;
using ParserUtilities.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Output {

    public class MetricChart {
        public MetricChart(DataChartModel chart, DateTime start,DateTime end, string width, string height) {
            Chart = chart;
            Start = start;
            End = end;
            Width = width;
            Height = height;
        }

        public DataChartModel Chart { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }

        public void AppendChart(StringBuilder sb,StringBuilder legend,string color,string legendKey="",int num=0) {
            CreateChart(Width, Height,sb,Start,End,legend,legendKey,Chart,color,num);
        }


        public static void SaveOverlay(string file,params DataChartModel[] charts) {
            var sb = new StringBuilder();
            var legend = new StringBuilder();
            sb.Append("<html>");
            sb.Append(@"
<style>
    svg{
        position:fixed;
        top:0;
        left:0;
    }
    svg polyline{
        fill:transparent;
    }
</style>");
            var i = 0;
            var min = charts.Min(x => x.Datapoints.Min(y => y.X));
            var max = charts.Max(x => x.Datapoints.Max(y => y.X));
            foreach (var c in charts) {
                var mc = new MetricChart(c,min,max,"90%","50%");
                mc.AppendChart(sb, legend, "red",num:i);
                i += 1;
            }
            sb.Append("</html>");
            File.WriteAllText(file,sb.ToString());
        }
        public static void CreateChart(string width,string height,StringBuilder sb, DateTime start, DateTime end, StringBuilder legend, string chartKey, DataChartModel c, string color,int num) {
            var dp = c.Datapoints.OrderBy(x => x.X); 
            var min = c.Statistic.Min ?? dp.Select(x => (double?)x.Y).Min();
            var max = c.Statistic.Max ?? dp.Select(x => (double?)x.Y).Max();
            if (max - min != 0) {
                var pts = dp.Select(x => Tuple.Create(((x.X - start).TotalSeconds / (end - start).TotalSeconds * 100), (((max - min) - ((double?)x.Y - min)) / (max - min) * 100)));
                var points = string.Join(" ", pts.Select(x => x.Item1 + "," + x.Item2));
                var circles = string.Join("", pts.Select(x => "<ellipse cx='" + x.Item1 + "' cy='" + x.Item2 + "' rx='0.1' ry='1' fill='" + color + "'/>"));
                sb.Append("<div class='chart chart-" + c.Name + " hidden'>");
                sb.Append(@"<svg width='"+width+"' height='"+height+@"' viewBox=""0 0 100 104"" preserveAspectRatio=""none"">");
                sb.Append(@"<polyline stroke=""" + color + @""" points=""" + points + @""" vector-effect=""non-scaling-stroke"" />");
                sb.Append(circles);
                sb.Append("</svg>");
                sb.Append("</div>");
                legend.Append("<div><span class='legend-dot' style='background-color:" + color + ";'>" + (chartKey) + "</span><input data-chart-num='" + (num) + "' id='cb_" + c.Name + "' type='checkbox' name='chart' value='" + c.Name + "'><label for='cb_" + c.Name + "'>" + c.Name + "</label><span class='chart-max'>[" + (Math.Round((min ?? 0) * 1000.0) / 1000) + ", " + (Math.Round((max ?? 0) * 1000.0) / 1000) + "]</span></div>");

            } else {
                legend.Append("<div>No data for " + c.Name + "</div>");
            }
        }
    }
}
