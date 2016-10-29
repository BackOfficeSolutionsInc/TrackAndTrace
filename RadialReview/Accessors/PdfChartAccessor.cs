using MigraDoc.DocumentObjectModel;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors {
	public class PdfChartAccessor {
		private static XFont _Font = new XFont("Verdana", 10, XFontStyle.Regular);
		private static XBrush _BlackText = new XSolidBrush(XColor.FromArgb(255, 51, 51, 51));


		private static double GetTextHeight(XGraphics gfx, string text, double rectWidth, XFont font) {
			double height1 = font.GetHeight();
			double height2 = gfx.MeasureString(text, font).Height;
			double width = gfx.MeasureString(text, font).Width;
			if (width <= rectWidth)
				return height2;
			int num = (int)Math.Ceiling(width / 290.0) - 1;
			return height2 + (double)num * height1;
		}

		private static XPoint[] Cross(double x, double y, double size = 1.0) {
			var xpointArray = new[] {
				new XPoint(-0.223606797749978 * size + x, 0.0 * size + y),
				new XPoint(-0.447213595499958 * size + x, -0.223606797749978 * size + y),
				new XPoint(-0.223606797749978 * size + x, -0.447213595499958 * size + y),
				new XPoint(0.0 * size + x, -0.223606797749978 * size + y),
				new XPoint(0.223606797749978 * size + x, -0.447213595499958 * size + y),
				new XPoint(0.447213595499958 * size + x, -0.223606797749978 * size + y),
				new XPoint(0.223606797749978 * size + x, 0.0 * size + y),
				new XPoint(0.447213595499958 * size + x, 0.223606797749978 * size + y),
				new XPoint(0.223606797749978 * size + x, 0.447213595499958 * size + y),
				new XPoint(0.0 * size + x, 0.223606797749978 * size + y),
				new XPoint(-0.223606797749978 * size + x, 0.447213595499958 * size + y),
				new XPoint(-0.447213595499958 * size + x, 0.223606797749978 * size + y),
			};

			return xpointArray;
		}

		private static XPoint[] Diamond(double x, double y, double size = 1.0) {
			return new[] {
				new XPoint(0.5 * size + x, 0.0 * size + y),
				new XPoint(0.0 * size + x, 0.5 * size + y),
				new XPoint(-0.5 * size + x, 0.0 * size + y),
				new XPoint(0.0 * size + x, -0.5 * size + y),
			};
		}

		private static void DrawShape(XGraphics gfx, XPen pen, XBrush brush, double x, double y, string markerType, double size = 1.0) {

			switch (markerType) {
				case "cross":	gfx.DrawPolygon(pen, brush, Cross(x, y, size), XFillMode.Winding);break;
				case "circle":	gfx.DrawEllipse(pen, brush, x - size * 0.5, y - size * 0.5, size, size);break;
				case "square":	gfx.DrawRectangle(pen, brush, x - size * 0.5, y - size * 0.5, size, size);break;
				case "diamond":	gfx.DrawPolygon(pen, brush, Diamond(x, y, size), XFillMode.Winding);break;
				default:
					throw new ArgumentOutOfRangeException("No marker type:" + markerType);
			}			
		}

		private static void DrawPieChart(XGraphics gfx, XRect placement, List<PieSlice<FiveState>> slices) {
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated method
			//double num = Enumerable.Sum<PdfChartAccessor.PieSlice>((IEnumerable<PdfChartAccessor.PieSlice>)slices, PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__7_0 ?? (PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__7_0 = new Func<PdfChartAccessor.PieSlice, double>(PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9.\u003CDrawPieChart\u003Eb__7_0)));

			double num = slices.Sum(x => x.Data);

			double startAngle = 270.0;
			foreach (var pieSlice in slices) {
				var xsolidBrush = new XSolidBrush(pieSlice.Brush);
				var sweepAngle = pieSlice.Data / num * 360.0;
				gfx.DrawPie((XBrush)xsolidBrush, placement, startAngle, sweepAngle);
				startAngle += sweepAngle;
			}
		}

		private static List<PieSlice<FiveState>> GeneratePieSlices(List<GetWantCapacityAnswer> answers, string gwc) {
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated method
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated method
			return answers.Select(x => {
				if (gwc == "g")
					return x.GetIt;
				if (gwc == "w")
					return x.WantIt;
				if (gwc == "c")
					return x.HasCapacity;
				else
					throw new ArgumentOutOfRangeException("gwc type:" + gwc);
			}).GroupBy(x => x)
			.Select(x => new PieSlice<FiveState>() {
				Data = x.Count(),
				Brush = x.Key.GetColor(),
				Item = x.Key
			}).ToList();


			//return Enumerable.ToList<PdfChartAccessor.PieSlice>(Enumerable.Select<IGrouping<FiveState, FiveState>, PdfChartAccessor.PieSlice>(Enumerable.GroupBy<FiveState, FiveState>(Enumerable.Select<GetWantCapacityAnswer, FiveState>((IEnumerable<GetWantCapacityAnswer>)answers, (Func<GetWantCapacityAnswer, FiveState>)(x => {
			//	string local_0 = gwc;

			//})), PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__8_1 ?? (PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__8_1 = new Func<FiveState, FiveState>(PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9.\u003CGeneratePieSlices\u003Eb__8_1))), PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__8_2 ?? (PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__8_2 = new Func<IGrouping<FiveState, FiveState>, PdfChartAccessor.PieSlice>(PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9.\u003CGeneratePieSlices\u003Eb__8_2))));
		}

		public static void DrawRolesTable(XGraphics gfx, XRect placement, List<GetWantCapacityAnswer> answers) {
			var state = gfx.Save();
			var textWidthPercentage = 0.5;
			var textWidth = Math.Max(1.0, placement.Width * textWidthPercentage);
			var pieMargin = 2.0;
			var pieWidth = Math.Max(1.0, placement.Width * (1.0 - textWidthPercentage) / 3.0 - pieMargin * 2.0);
			var top = placement.Top;
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated field
			// ISSUE: reference to a compiler-generated method
			//foreach (IGrouping<Askable, GetWantCapacityAnswer> grouping in Enumerable.GroupBy<GetWantCapacityAnswer, Askable>((IEnumerable<GetWantCapacityAnswer>)answers, PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__9_0 ?? (PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9__9_0 = new Func<GetWantCapacityAnswer, Askable>(PdfChartAccessor.\u003C\u003Ec.\u003C\u003E9.\u003CDrawRolesTable\u003Eb__9_0))))
			//{
			var order = new Dictionary<FiveState, int>();

			order.Add(FiveState.Always, 4);
			order.Add(FiveState.Mostly, 3);
			order.Add(FiveState.Rarely, 2);
			order.Add(FiveState.Never, 1);
			order.Add(FiveState.Indeterminate, 0);

			foreach (var grouping in answers.GroupBy(x => x.Askable)) {
				var gwc = new[] { "g", "w", "c" };
				//double val1 = pieWidth;
				for (int i = 0; i < gwc.Length; ++i) {
					var slices = GeneratePieSlices(grouping.ToList(), gwc[i]).OrderByDescending(x => order[x.Item]).ToList();
					

					var xx = placement.Left + textWidth + (i * 2.0 + 1.0) * pieMargin + i * pieWidth;
					var yy = top + pieMargin;
					var pieLoc = new XRect(xx, yy, pieWidth, pieWidth);
					DrawPieChart(gfx, pieLoc, slices);
				}
				var question = grouping.Key.GetQuestion();
				var font = _Font;
				var height = Math.Max(pieWidth, GetTextHeight(gfx, question, textWidth, font));
				var layoutRectangle = new XRect(placement.Left, top + pieMargin, textWidth, height);

				var tf = new XTextFormatter(gfx) {
					Alignment = XParagraphAlignment.Right
				};

				tf.DrawString(question, font, _BlackText, layoutRectangle, XStringFormats.TopLeft);
				top += pieWidth + 2.0 * pieMargin;
			}
			gfx.Restore(state);
		}

		private static void DrawPoint(XGraphics gfx, Scatter.ScatterPoint point, Scatter chartData, XRect innerChart) {
			var x = (double)((point.cx - chartData.xMin) / (chartData.xMax - chartData.xMin)) * (innerChart.Right - innerChart.Left) + innerChart.Left;
			var y = (double)(1 - (point.cy - chartData.yMin) / (chartData.xMax - chartData.yMin)) * (innerChart.Bottom - innerChart.Top) + innerChart.Top;
			if (point.ox.HasValue && point.oy.HasValue) {
				var ox = (double)((point.ox - chartData.xMin) / (chartData.xMax - chartData.xMin)) * (innerChart.Right - innerChart.Left) + innerChart.Left;
				var oy = (double)(1 - (point.oy - chartData.yMin) / (chartData.xMax - chartData.yMin)) * (innerChart.Bottom - innerChart.Top) + innerChart.Top;
				gfx.DrawLine(XPens.Gray, new XPoint(ox, ox), new XPoint(x, y));
			}
			var alpha = 200;
			var num = 6;

			var selfBrush = new XSolidBrush(XColor.FromArgb(alpha, 255, 255, 0));
			var selfPen = new XPen(XColor.FromArgb(alpha, 184, 134, 11));

			var managerBrush = new XSolidBrush(XColor.FromArgb(alpha, 173, 216, 230));
			var managerPen = new XPen(XColor.FromArgb(alpha, 0, 0, 255));

			var peerBrush = new XSolidBrush(XColor.FromArgb(alpha, 122, 209, 122));
			var peerPen = new XPen(XColor.FromArgb(alpha, 48, 89, 48));

			var drBrush = new XSolidBrush(XColor.FromArgb(alpha, 255, 0, 0));
			var drPen = new XPen(XColor.FromArgb(alpha, 139, 0, 0));

			var nrBrush = new XSolidBrush(XColor.FromArgb(alpha, 128, 128, 128));
			var nrPen = new XPen(XColor.FromArgb(alpha, 0, 0, 0));

			if (point.@class.Contains("about-Self"))
				DrawShape(gfx, selfPen, selfBrush, x, y, "cross", num);
			else if (point.@class.Contains("about-Manager"))
				DrawShape(gfx, managerPen, (XBrush)managerBrush, x, y, "square", num);
			else if (point.@class.Contains("about-Peer"))
				DrawShape(gfx, peerPen, (XBrush)peerBrush, x, y, "triangle", num);
			else if (point.@class.Contains("about-Subordinate"))
				DrawShape(gfx, drPen, (XBrush)drBrush, x, y, "diamond", num);
			else if (point.@class.Contains("about-NoRelationship"))
				DrawShape(gfx, nrPen, (XBrush)nrBrush, x, y, "circle", num);
			else
				DrawShape(gfx, nrPen, (XBrush)nrBrush, x, y, "circle", num);
		}

		public static void DrawQuadrant(Scatter plot, XGraphics gfx, XRect placement, Unit? margin = null, bool centerHeight = true) {
			var state = gfx.Save();
			var marg = margin ?? Unit.FromInch(0.3);

			var tx = placement.Left + marg;
			var ty = placement.Top + marg;
			var bx = placement.Right - marg;
			var by = placement.Bottom - marg;

			var dx = Math.Abs(bx - tx);
			var dy = Math.Abs(by - ty);
			var w = Math.Min(dx, dy);

			tx = tx + (dx - w) / 2.0;
			bx = bx - (dx - w) / 2.0;

			if (centerHeight) {
				ty += (dy - w) / 2.0;
				by -= (dy - w) / 2.0;
			} else {
				by -= (dy - w);
			}

			var mx = (tx + bx) * 0.5;
			var my = (ty + by) * 0.5;
			var chartInner = new XRect(tx, ty, w, w);

			var borderColor = XPens.LightGray;
			gfx.DrawRectangle(borderColor, chartInner);
			gfx.DrawLine(borderColor, mx, ty, mx, by);
			gfx.DrawLine(borderColor, tx, my, bx, my);

			var yaxis = new XRect(chartInner.Left - (double)(float)marg * 4.0 / 5.0, chartInner.Top, (double)marg * 3.0 / 5.0, chartInner.Height);
			var xaxis = new XRect(chartInner.Left, chartInner.Bottom + (double)marg / 5.0, chartInner.Width, (double)marg * 3.0 / 5.0);
			var xAxisColor = new XSolidBrush(XColor.FromArgb(255, 212, 238, 159));
			var yAxisColor = new XSolidBrush(XColor.FromArgb(255, 163, 213, 213));

			gfx.DrawRectangle(yAxisColor, yaxis);
			gfx.DrawRectangle(xAxisColor, xaxis);

			XSolidBrush xAxisColor_Title = new XSolidBrush(XColor.FromArgb(255, 89, 118, 34));
			XSolidBrush yAxisColor_Title = new XSolidBrush(XColor.FromArgb(255, 34, 102, 102));
			XFont font = _Font;
			gfx.RotateTransform(-90.0);
			yaxis.Transform(new XMatrix(0, 1, -1, 0, 0, 0));
			gfx.DrawString(plot.yAxis, font, yAxisColor_Title, yaxis, XStringFormats.Center);
			yaxis.Transform(new XMatrix(0, -1, 1, 0, 0, 0));
			gfx.RotateTransform(90.0);

			gfx.DrawString(plot.xAxis, font, xAxisColor_Title, xaxis, XStringFormats.Center);
			foreach (Scatter.ScatterPoint point in plot.Points)
				DrawPoint(gfx, point, plot, chartInner);
			gfx.Restore(state);
		}

		private class PieSlice<T> {
			public double Data { get; set; }
			public XColor Brush { get; set; }
			public T Item { get; set; }
		}
	}
}