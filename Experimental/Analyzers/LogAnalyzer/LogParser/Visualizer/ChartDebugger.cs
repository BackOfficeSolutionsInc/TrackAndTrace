using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using LogParser.Downloaders;
using LogParser;
using System.Drawing;

[assembly: System.Diagnostics.DebuggerVisualizer(typeof(ChartVisualizer.SeriesDebugger), typeof(VisualizerObjectSource), Target = typeof(XYSeries), Description = "Series Visualizer")]
[assembly: System.Diagnostics.DebuggerVisualizer(typeof(ChartVisualizer.ChartDebugger), typeof(VisualizerObjectSource), Target = typeof(LogParser.Downloaders.Chart), Description = "Chart Visualizer")]
[assembly: System.Diagnostics.DebuggerVisualizer(typeof(ChartVisualizer.ScaledChartDebugger), typeof(VisualizerObjectSource), Target = typeof(LogParser.Downloaders.Chart), Description = "Chart Visualizer (Scaled)")]
namespace ChartVisualizer {
	public class SeriesDebugger : DialogDebuggerVisualizer {
		override protected void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider) {
			var series = (XYSeries)objectProvider.GetObject();
			var chart = ChartDebugger.BuildChart(false);
			ChartDebugger.AddSeries(chart, series, false);
			windowService.ShowDialog(chart);
		}
		public static void TestShowVisualizer(object objectToVisualize) {
			VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(SeriesDebugger));
			visualizerHost.ShowVisualizer();
		}
	}

	public class ScaledChartDebugger : ChartDebugger {
		public ScaledChartDebugger() {
			Scale = true;
		}
	}

	public class ChartDebugger : DialogDebuggerVisualizer {

		public bool Scale { get; set; }

		public static System.Windows.Forms.DataVisualization.Charting.Chart BuildChart(bool scaled) {
			var chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			chart.BeginInit();
			var ca = new ChartArea();
			chart.ChartAreas.Add(ca);
			chart.Dock = DockStyle.Fill;

			ca.AxisX.MajorGrid.LineColor = Color.LightGray;
			ca.AxisY.MajorGrid.LineColor = Color.LightGray;

			ca.CursorX.IsUserSelectionEnabled = true;
			ca.CursorY.IsUserSelectionEnabled = true;

			ca.AxisX.ScaleView.Zoomable = true;
			ca.AxisY.ScaleView.Zoomable = true;
			ca.CursorX.AutoScroll = true;
			ca.CursorY.AutoScroll = true;

			var ct = chart.ContextMenu = new ContextMenu();
			ct.MenuItems.Add("Switch Type");
		//	ct.MenuItems.Add("Scale");

			if (scaled) {
				ca.AxisY.LabelStyle.Format = "##%";
				ca.AxisY.Maximum = 1;
				ca.AxisY.Interval = .05;
			}

			ca.AxisX.IntervalOffsetType = DateTimeIntervalType.Hours;
			ca.AxisX.IntervalType = DateTimeIntervalType.Hours;
			ca.AxisX.Interval = 1;

			ca.AxisX.LabelStyle.Format = "MMM dd (HH:mm)";
			ca.AxisX.Name = "UTC";

			//ca.Area3DStyle.Enable3D = true;
			chart.MouseWheel += chart1_MouseWheel;

			chart.ApplyPaletteColors();
			foreach (Series series in chart.Series)
				series.Color = Color.FromArgb(127, series.Color);

			//ca.AxisX.CustomLabels

			//ca. = new Size(1000, 400);			
			return chart;
		}
		private static void chart1_MouseWheel(object sender, MouseEventArgs e) {
			var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)sender;
			var xAxis = chart.ChartAreas[0].AxisX;
			var yAxis = chart.ChartAreas[0].AxisY;

			try {
				if (e.Delta < 0) // Scrolled down.
				{
					xAxis.ScaleView.ZoomReset();
					yAxis.ScaleView.ZoomReset();
				} else if (e.Delta > 0) // Scrolled up.
				  {
					var xMin = xAxis.ScaleView.ViewMinimum;
					var xMax = xAxis.ScaleView.ViewMaximum;
					var yMin = yAxis.ScaleView.ViewMinimum;
					var yMax = yAxis.ScaleView.ViewMaximum;

					var posXStart = xAxis.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
					var posXFinish = xAxis.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;
					var posYStart = yAxis.PixelPositionToValue(e.Location.Y) - (yMax - yMin) / 4;
					var posYFinish = yAxis.PixelPositionToValue(e.Location.Y) + (yMax - yMin) / 4;

					xAxis.ScaleView.Zoom(posXStart, posXFinish);
					yAxis.ScaleView.Zoom(posYStart, posYFinish);
				}
			} catch { }
		}

		public static void AddSeries(System.Windows.Forms.DataVisualization.Charting.Chart chart, XYSeries series, bool shouldScale) {
			var series1 = new Series {
				Name = series.Name,
				//Color = color,
				//IsVisibleInLegend = false,
				IsXValueIndexed = false,
				ChartType = SeriesChartType.StepLine,
				XValueType = ChartValueType.DateTime,	
				
								
				
			};
			chart.Series.Add(series1);
			decimal? max = null;

			chart.ContextMenu.MenuItems[0].Click += (a, b) => {
				series1.ChartType = (series1.ChartType == SeriesChartType.Area) ? SeriesChartType.StepLine : SeriesChartType.Area;
			};

			foreach (var p in series.Points) {
				max = !shouldScale ? 1 : max ?? (series.Points.Where(x => x.Y.HasValue).Max(x => x.Y.Value));
				if (p.Y != null) {
					series1.Points.AddXY(p.X.ToOADate(), p.Y.Value/max);
				}
			}
		}

		override protected void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider) {
			var chart = (LogParser.Downloaders.Chart)objectProvider.GetObject();
			var scaled = chart.Series.Count > 1 && Scale;
			var c = BuildChart(scaled);
			foreach (var s in chart.Series) {
				AddSeries(c, s, scaled);
			}
			windowService.ShowDialog(c);
		}

		public static void TestShowVisualizer(object objectToVisualize) {
			VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(ChartDebugger));
			visualizerHost.ShowVisualizer();
		}
	}
}
