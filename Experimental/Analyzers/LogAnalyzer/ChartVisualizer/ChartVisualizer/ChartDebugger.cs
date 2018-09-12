using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

[assembly: System.Diagnostics.DebuggerVisualizer(typeof(ChartVisualizer.ChartDebugger),typeof(VisualizerObjectSource),Target = typeof(System.String),Description = "Chart Visualizer")]

namespace ChartVisualizer
{
    public class ChartDebugger : DialogDebuggerVisualizer {
		override protected void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider) {
			var chart = (XYSeries)objectProvider.GetObject());

			var chart1 = new Chart();

			var series1 = new Series {
				Name = "Series1",
				Color = System.Drawing.Color.Green,
				IsVisibleInLegend = false,
				IsXValueIndexed = true,
				ChartType = SeriesChartType.Line
			};

			chart1.Series.Add(series1);

			for (int i = 0; i < 100; i++) {
				series1.Points.AddXY(i, f(i));
			}
			chart1.Invalidate();

		}

		public static void TestShowVisualizer(object objectToVisualize) {
			VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(ChartDebugger));
			visualizerHost.ShowVisualizer();
		}

	}
}
