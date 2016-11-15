using MigraDoc.DocumentObjectModel;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using RadialReview.Engines;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors {
	public class PdfChartAccessor {
		private static bool DEBUG = false;

		private static XFont _FontLargeBold = new XFont("Verdana", 20, XFontStyle.Bold);
		private static XFont _Font = new XFont("Verdana", 10, XFontStyle.Regular);
		private static XFont _FontBold = new XFont("Verdana", 10, XFontStyle.Bold);
		public static XFont _Font8 = new XFont("Verdana", 8, XFontStyle.Regular);
		private static XFont _Font8Bold = new XFont("Verdana", 8, XFontStyle.Bold);
		private static XFont _Font7 = new XFont("Verdana", 7, XFontStyle.Regular);
		private static XBrush _BlackText = new XSolidBrush(XColor.FromArgb(255, 51, 51, 51));
		private static Unit _DefaultMargin = Unit.FromInch(0.3);
		

		private static XRect FromMargin(XRect placement, Unit? margin = null) {
			var marg = margin ?? _DefaultMargin;
			return new XRect(placement.Left + marg, placement.Top + marg, Math.Max(1, placement.Width - 2 * marg), Math.Max(1, placement.Height - 2 * marg));
		}

		private static void DrawDebug(XGraphics gfx, XRect placement, XPen color = null) {
			if (DEBUG)
				gfx.DrawRectangle(color ?? XPens.Red, placement);
		}

		private static double GetTextHeight(XGraphics gfx, string text, double rectWidth, XFont font) {
			double height1 = font.GetHeight();
			var maxWidth = 0.0;
			var num = 0;
			var height2 = 0.0;
			foreach (var row in text.Replace("\r","").Split('\n')) {
				var measure = gfx.MeasureString(row, font);
				height2 += measure.Height;
				maxWidth = Math.Max(maxWidth, measure.Width);
				num += (int)Math.Ceiling(maxWidth / rectWidth) - 1;
			}
			if (maxWidth <= rectWidth && num == 0)
				return height2;
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

		private static XPoint[] Check(double x, double y, double size = 1.0) {
			return new[] {
				new XPoint(-0.134259259259258*size+x,0.402777777777779*size+y),
				new XPoint(0.449074074074075*size+x,-0.180555555555555*size+y),
				new XPoint(0.282407407407408*size+x,-0.347222222222223*size+y),
				new XPoint(-0.134259259259258*size+x,0.0694444444444446*size+y),
				new XPoint(-0.300925925925925*size+x,-0.097222222222223*size+y),
				new XPoint(-0.467592592592592*size+x,0.0694444444444446*size+y),

			};
		}

		private static XPoint[] Diamond(double x, double y, double size = 1.0) {
			return new[] {
				new XPoint(0.5 * size + x, 0.0 * size + y),
				new XPoint(0.0 * size + x, 0.5 * size + y),
				new XPoint(-0.5 * size + x, 0.0 * size + y),
				new XPoint(0.0 * size + x, -0.5 * size + y),
			};
		}

		private static XPoint[] Triangle(double x, double y, double size = 1.0) {
			return new[] {
				new XPoint(x, -0.5*size+y),
				new XPoint(-0.433012702*size+x,    0.25*size+y),
				new XPoint(0.433012702*size+x, 0.25*size+y),
			};
		}

		private static void DrawShape(XGraphics gfx, XPen pen, XBrush brush, double x, double y, string markerType, double size = 1.0) {

			switch (markerType) {
				case "cross":
					gfx.DrawPolygon(pen, brush, Cross(x, y, size), XFillMode.Winding);
					break;
				case "circle":
					gfx.DrawEllipse(pen, brush, x - size * 0.5, y - size * 0.5, size, size);
					break;
				case "square":
					gfx.DrawRectangle(pen, brush, x - size * 0.5, y - size * 0.5, size, size);
					break;
				case "diamond":
					gfx.DrawPolygon(pen, brush, Diamond(x, y, size), XFillMode.Winding);
					break;
				case "triangle":
					gfx.DrawPolygon(pen, brush, Triangle(x, y, size), XFillMode.Winding);
					break;
				default:
					throw new ArgumentOutOfRangeException("No marker type:" + markerType);
			}
		}

		private static void DrawPoint(XGraphics gfx, Scatter.ScatterPoint point, Scatter chartData, XRect innerChart) {
			var x = (double)((point.cx - chartData.xMin) / (chartData.xMax - chartData.xMin)) * (innerChart.Right - innerChart.Left) + innerChart.Left;
			var y = (double)(1 - (point.cy - chartData.yMin) / (chartData.xMax - chartData.yMin)) * (innerChart.Bottom - innerChart.Top) + innerChart.Top;
			if (point.ox.HasValue && point.oy.HasValue) {
				var ox = (double)((point.ox - chartData.xMin) / (chartData.xMax - chartData.xMin)) * (innerChart.Right - innerChart.Left) + innerChart.Left;
				var oy = (double)(1 - (point.oy - chartData.yMin) / (chartData.xMax - chartData.yMin)) * (innerChart.Bottom - innerChart.Top) + innerChart.Top;
				gfx.DrawLine(XPens.Gray, new XPoint(ox, oy), new XPoint(x, y));
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

		private static void DrawPieChart(XGraphics gfx, XRect placement, List<PieSlice> slices) {
			double num = slices.Sum(x => x.Data);
			double startAngle = 270.0;
			foreach (var pieSlice in slices) {
				var xsolidBrush = new XSolidBrush(pieSlice.Brush);
				var sweepAngle = pieSlice.Data / num * 360.0;
				gfx.DrawPie(xsolidBrush, placement, startAngle, sweepAngle);
				startAngle += sweepAngle;
			}
		}

		private static List<PieSlice<FiveState>> GeneratePieSlices(List<GetWantCapacityAnswer> answers, string gwc) {
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
				Brush = x.Key.GetXColor(),
				Item = x.Key
			}).ToList();
		}
		
		private static List<PieSlice<PositiveNegativeNeutral>> GeneratePieSlices(List<CompanyValueAnswer> answers) {
			return answers.Select(x => x.Exhibits).GroupBy(x => x)
			.Select(x => new PieSlice<PositiveNegativeNeutral>() {
				Data = x.Count(),
				Brush = x.Key.GetXColor(),
				Item = x.Key
			}).ToList();
		}

		private static void _DrawFeedbacks(XGraphics gfx, List<FeedbackRow> feedbackRows, XRect placement, ref double totalHeight, ref double top) {
			var feedbackMargin = 6.0;
			if (feedbackRows.Any()) {
				top += feedbackMargin * 2;
				var title = "Feedback:";
				var titleFont = _Font8Bold;
				var h = GetTextHeight(gfx, title, placement.Width, titleFont);
				gfx.DrawString(title, titleFont, XBrushes.Gray, new XRect(placement.Left, top, placement.Width, h), XStringFormats.TopLeft);
				top += h;
				totalHeight += h;
				gfx.DrawLine(XPens.LightGray, placement.Left, top, placement.Right, top);
				top += feedbackMargin * 2;
				totalHeight += feedbackMargin * 2;

				var font = _Font7;
				var iconWidth = 7;
				var feedbackWidth = placement.Width - iconWidth - 2 * feedbackMargin;

				foreach (var included in feedbackRows) {
					var fb = included.Feedback;
					var tf = new XTextFormatter(gfx) { Alignment = XParagraphAlignment.Left };

					h = GetTextHeight(gfx, fb, feedbackWidth, font);
					tf.DrawString(fb, font, XBrushes.Gray, new XRect(placement.Left + iconWidth + feedbackMargin, top, feedbackWidth, h), XStringFormats.TopLeft);
					var myH = Math.Max(iconWidth, h) + feedbackMargin;
					var iconRect = new XRect(placement.Left /*+ 1*/, top /*+ 1*/, iconWidth, h+1.5 /*- 2*/);
					gfx.DrawRectangle(new XSolidBrush(included.Color), iconRect);

					if (included.IconName != null) {
						gfx.DrawString(included.IconName, _Font7, new XSolidBrush(XColor.FromArgb(200,255,255,255)), iconRect,XStringFormats.TopCenter);
					}

					top += myH;
					totalHeight += myH;
				}
			}
		}

		public static XRect DrawQuadrant(Scatter plot, XGraphics gfx, XRect placement, Unit? margin = null, bool centerHeight = true) {
			var state = gfx.Save();
			var marg = margin ?? Unit.FromInch(0.3);

			DrawDebug(gfx, new XRect(placement.Left, placement.Top, placement.Width, 1));


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

			var yaxis = new XRect(chartInner.Left - marg * 4.0 / 5.0, chartInner.Top, marg * 3.0 / 5.0, chartInner.Height);
			var xaxis = new XRect(chartInner.Left, chartInner.Bottom + marg / 5.0, chartInner.Width, marg * 3.0 / 5.0);
			var xAxisColor = new XSolidBrush(XColor.FromArgb(255, 212, 238, 159));
			var yAxisColor = new XSolidBrush(XColor.FromArgb(255, 163, 213, 213));

			gfx.DrawRectangle(yAxisColor, yaxis);
			gfx.DrawRectangle(xAxisColor, xaxis);

			var xAxisColor_Title = new XSolidBrush(XColor.FromArgb(255, 89, 118, 34));
			var yAxisColor_Title = new XSolidBrush(XColor.FromArgb(255, 34, 102, 102));
			var font = _Font;
			gfx.RotateTransform(-90.0);
			yaxis.Transform(new XMatrix(0, 1, -1, 0, 0, 0));
			gfx.DrawString(plot.yAxis, font, yAxisColor_Title, yaxis, XStringFormats.Center);
			yaxis.Transform(new XMatrix(0, -1, 1, 0, 0, 0));
			gfx.RotateTransform(90.0);

			gfx.DrawString(plot.xAxis, font, xAxisColor_Title, xaxis, XStringFormats.Center);

			plot.Points = plot.Points.OrderBy(x => {
				if (x.@class.Contains("about-Peer"))
					return 5;
				if (x.@class.Contains("about-Self"))
					return 4;
				if (x.@class.Contains("about-Subordinate"))
					return 3;
				if (x.@class.Contains("about-NoRelationship"))
					return 2;
				if (x.@class.Contains("about-Manager"))
					return 1;
				return 3;
			}).ToList();

			foreach (Scatter.ScatterPoint point in plot.Points)
				DrawPoint(gfx, point, plot, chartInner);

			var totalHeight = chartInner.Height + (double)marg * 2;

			if (centerHeight)
				totalHeight = placement.Height;

			var actualSize = new XRect(placement.Left, placement.Y, placement.Width, totalHeight);
			DrawDebug(gfx, actualSize, new XPen(XColors.Blue) { DashStyle = XDashStyle.Dot });
			gfx.Restore(state);
			return actualSize;
		}

		public static XRect DrawRocksTable(XGraphics gfx, XRect location, List<RockAnswer> answers, Unit? margin = null) {
			var state = gfx.Save();

			DrawDebug(gfx, new XRect(location.Left, location.Top, location.Width, 1), XPens.Red);
			var placement = FromMargin(location, margin);
			//DrawDebug(gfx, placement);


			var rockWidthPercentage = 0.25;
			var rockCompletionBoxW = _Font.GetHeight() * 4;
			var textAreaWidth = (placement.Width - rockCompletionBoxW);
			var rockTextWidth = Math.Max(1.0, textAreaWidth * rockWidthPercentage);
			//	var feedbackWidth = Math.Max(1.0, textAreaWidth * (1.0 - rockWidthPercentage));
			var completionMargin = 4.0;
			var reasonMargin = 2.0;

			var totalHeight = 0.0;

			////////////////////////////

			var curH = placement.Top;

			var minRowHeight = _Font.GetHeight() * 4 + completionMargin * 3;

			foreach (var rock in answers.GroupBy(x => x.Askable)) {
				var question = rock.Key.GetQuestion();

				var managerRockState = rock.Where(x => x.ManagerOverride != RockState.Indeterminate).FirstOrDefault().NotNull(x => (RockState?)x.ManagerOverride) ?? RockState.Indeterminate;
				
				var completionBoxOuter = new XRect(placement.Left, curH, rockCompletionBoxW, rockCompletionBoxW);
				var completionBox = FromMargin(completionBoxOuter, completionMargin);

				gfx.DrawRectangle(XPens.Transparent, new XSolidBrush(managerRockState.GetColor()), completionBox);

				var bottomComplete = new XRect(completionBox.Left, completionBox.Top + completionBox.Height * 0.7, completionBox.Width, completionBox.Height * 0.3);

				var complete = managerRockState.IsComplete();
				if (complete != null) {
					if (complete.Value) {
						gfx.DrawPolygon(XBrushes.White, Check(completionBox.Center.X, completionBox.Center.Y, rockCompletionBoxW * 0.6), XFillMode.Winding);
					} else {
						gfx.DrawPolygon(XBrushes.White, Cross(completionBox.Center.X, completionBox.Center.Y, rockCompletionBoxW * 0.6), XFillMode.Winding);
					}
				} else {					
					gfx.DrawString("Not Set", _Font8Bold, XBrushes.White, completionBox, XStringFormats.Center);
				}

				var tf = new XTextFormatter(gfx) {
					Alignment = XParagraphAlignment.Left,
				};

				var selfAnswer = rock.Where(x => x.ByUserId == x.AboutUserId).FirstOrDefault();
				var selfAnswerCompletion = RockState.Indeterminate;
				if (selfAnswer != null)
					selfAnswerCompletion = selfAnswer.Completion;


				var userCompletionText = "Marked as " + selfAnswerCompletion.ToString();

				var rockFont = _Font8Bold;
				var userCompletionFont = _Font7;
				var reasonsFont = _Font7;

				var rockNameHeight = GetTextHeight(gfx, question, textAreaWidth, rockFont);

				//Rock Name
				var rockNameContainer = new XRect(completionBoxOuter.Right, completionBox.Top, textAreaWidth, rockNameHeight);
				tf.DrawString(question, rockFont, _BlackText, rockNameContainer, XStringFormats.TopLeft);

				//Reasons
				var reasons = rock.SelectMany(x => {
					var list = new List<String>();
					if (!string.IsNullOrWhiteSpace(x.Reason))
						list.Add("\"" + x.Reason + "\" - " + x.AboutUser.GetName());
					if (!string.IsNullOrWhiteSpace(x.OverrideReason))
						list.Add("\"" + x.OverrideReason + "\" - Supervisor");
					return list;
				}).Where(x => x != null).Distinct().ToList();

				var ch = rockNameContainer.Bottom + reasonMargin;
				foreach (var r in reasons) {
					var hh = GetTextHeight(gfx, r, textAreaWidth, reasonsFont);
					tf.DrawString(r, reasonsFont, XBrushes.DarkGray, new XRect(completionBoxOuter.Right, ch, textAreaWidth, hh), XStringFormats.TopLeft);
					ch += hh + reasonMargin;

				}
				ch += reasonMargin;

				var userSubmittedCompletionHeight = GetTextHeight(gfx, userCompletionText, textAreaWidth, userCompletionFont);
				//var calculatedHeight = rockNameHeight + userSubmittedCompletionHeight + completionMargin * 3;
				var userStatusContainer = new XRect(completionBoxOuter.Right, ch, textAreaWidth, userSubmittedCompletionHeight);
				tf.DrawString(userCompletionText, userCompletionFont, XBrushes.LightGray, userStatusContainer, XStringFormats.TopLeft);

				var calculatedHeight = ch + userSubmittedCompletionHeight + reasonMargin - curH;

				//XRect userStatusContainer;

				//if (calculatedHeight > minRowHeight) {
				//	userStatusContainer = new XRect(completionBoxOuter.Right, completionBox.Top + rockNameHeight + completionMargin, textAreaWidth, userSubmittedCompletionHeight);
				//} else {
				//	userStatusContainer = new XRect(completionBoxOuter.Right, completionBox.Bottom - (userSubmittedCompletionHeight + completionMargin), textAreaWidth, userSubmittedCompletionHeight);
				//}


				var curRowHeight = Math.Max(minRowHeight, calculatedHeight);
				totalHeight += curRowHeight;
				curH += curRowHeight;
			}

			var actualSize = new XRect(location.Left, location.Top, location.Width, totalHeight + 2 * (margin ?? _DefaultMargin));
			DrawDebug(gfx, actualSize, new XPen(XColors.Blue) { DashStyle = XDashStyle.Dot });
			gfx.Restore(state);
			return actualSize;
		}

		public static XRect DrawRolesTable(XGraphics gfx, XRect location, List<GetWantCapacityAnswer> answers, Unit? margin = null) {
			var state = gfx.Save();

			DrawDebug(gfx, new XRect(location.Left, location.Top, location.Width, 1), XPens.Red);
			var placement = FromMargin(location, margin);
			//DrawDebug(gfx, placement);

			var totalHeight = 0.0;

			var textWidthPercentage = 0.5;
			var textWidth = Math.Max(1.0, placement.Width * textWidthPercentage);
			var pieMargin = 2.0;
			var pieWidth = Math.Max(1.0, placement.Width * (1.0 - textWidthPercentage) / 3.0 - pieMargin * 2.0);
			var gwcNames = new[] { "Get It", "Want It", "Capacity to Do It" };

			var mh = 0.0;
			var gwcFont = _Font8;
			for (var i = 0; i < gwcNames.Length; i++) {
				var h = GetTextHeight(gfx, gwcNames[i], pieWidth, gwcFont);
				mh = Math.Max(mh, h);
				var rect = new XRect(placement.X + textWidth + pieMargin * (i * 2 + 1) + pieWidth * i, placement.Y, pieWidth, h);
				var tf = new XTextFormatter(gfx) {
					Alignment = XParagraphAlignment.Center,
				};
				tf.DrawString(gwcNames[i], gwcFont, _BlackText, rect, XStringFormats.TopLeft);
			}

			var top = placement.Top + mh;
			var order = new Dictionary<FiveState, int>();

			totalHeight += mh;

			order.Add(FiveState.Always, 4);
			order.Add(FiveState.Mostly, 3);
			order.Add(FiveState.Rarely, 2);
			order.Add(FiveState.Never, 1);
			order.Add(FiveState.Indeterminate, 0);

			foreach (var grouping in answers.GroupBy(x => x.Askable)) {
				var gwc = new[] { "g", "w", "c" };

				var question = grouping.Key.GetQuestion();
				var font = _Font8Bold;
				var h = GetTextHeight(gfx, question, textWidth, font);

				var extraH = 0.0;
				if (h > pieWidth) {
					extraH = (h - (pieWidth + pieMargin));
				}

				for (int i = 0; i < gwc.Length; ++i) {
					var slices = GeneratePieSlices(grouping.ToList(), gwc[i]).OrderByDescending(x => order[x.Item]).Select(x => x.Simple()).ToList();
					var xx = placement.Left + textWidth + (i * 2.0 + 1.0) * pieMargin + i * pieWidth;
					var yy = top + pieMargin+extraH;
					var pieLoc = new XRect(xx, yy, pieWidth, pieWidth);
					DrawPieChart(gfx, pieLoc, slices);
				}
				var height = Math.Max(pieWidth, GetTextHeight(gfx, question, textWidth, font));

				var tf = new XTextFormatter(gfx) {
					Alignment = XParagraphAlignment.Right
				};

				//var h = GetTextHeight(gfx, question, textWidth, font);
				if (h > pieWidth) {
					var layoutRectangle = new XRect(placement.Left, top + pieMargin * 2, textWidth, height);
					tf.DrawString(question, font, _BlackText, layoutRectangle, XStringFormats.TopLeft);
					top += h + 4.0 * pieMargin;
					totalHeight += h + 4.0 * pieMargin;

				} else {
					var layoutRectangle = new XRect(placement.Left, top + pieMargin * 2 + (pieWidth - h - font.GetHeight() / 2.0) / 2.0, textWidth, h);
					tf.DrawString(question, font, _BlackText, layoutRectangle, XStringFormats.TopLeft);
					top += pieWidth + 2.0 * pieMargin;
					totalHeight += pieWidth + 2.0 * pieMargin;
				}
				
				//totalHeight += Math.Max(h + 2.0 * pieMargin, pieWidth + 2.0 * pieMargin);
			}


			var feedbacksG = answers.Where(x => x.IncludeGetItReason).Select(x => new FeedbackRow() { Color = x.GetIt.GetXColor(), Feedback = x.GetItReason, IconName="G" }).ToList();
			var feedbacksW = answers.Where(x => x.IncludeWantItReason).Select(x => new FeedbackRow() { Color = x.WantIt.GetXColor(), Feedback = x.WantItReason, IconName = "W" }).ToList();
			var feedbacksC = answers.Where(x => x.IncludeHasCapacityReason).Select(x => new FeedbackRow() { Color = x.HasCapacity.GetXColor(), Feedback = x.HasCapacityReason, IconName = "C" }).ToList();

			var feedbacks = new List<FeedbackRow>();
			feedbacks.AddRange(feedbacksG);
			feedbacks.AddRange(feedbacksW);
			feedbacks.AddRange(feedbacksC);
			
			_DrawFeedbacks(gfx, feedbacks, placement, ref totalHeight, ref top);

			var actualSize = new XRect(location.Left, location.Top, location.Width, totalHeight + 2 * (margin ?? _DefaultMargin));
			DrawDebug(gfx, actualSize, new XPen(XColors.Blue) { DashStyle = XDashStyle.Dot });
			gfx.Restore(state);
			return actualSize;
		}

		public static XRect DrawValueTable(XGraphics gfx, XRect location, UserOrganizationModel aboutUser, List<CompanyValueAnswer> answers, List<UserOrganizationModel> supervisors, Unit? margin = null) {
			var state = gfx.Save();

			DrawDebug(gfx, new XRect(location.Left, location.Top, location.Width, 1), XPens.Red);
			var placement = FromMargin(location, margin);
			//DrawDebug(gfx, placement);

			var totalHeight = 0.0;

			var textWidthPercentage = 0.35;
			var rightPadPercentage = 0.20;
			var textWidth = Math.Max(1.0, placement.Width * textWidthPercentage);
			var pieMargin = 2.0;

			var values = answers.GroupBy(x => x.Askable).Select(x => x.Key).ToList();
			var valueCount = Math.Max(1, values.Count());

			var pieWidth = Math.Max(1.0, placement.Width * (1.0 - textWidthPercentage - rightPadPercentage) / valueCount - pieMargin * 2.0);

			var allValues = new List<ValueRow>();


			var managers = answers.Where(x => supervisors.Any(y => y.Id == x.ByUserId)).GroupBy(x => x.ByUserId).Select(x => new ValueRow() {
				Answers = x.ToList(),
				Name = x.First().ByUser.GetName()
			});
			var self = new ValueRow() {
				Answers = answers.Where(x => x.ByUserId == aboutUser.Id).ToList(),
				Name = aboutUser.GetName()
			};
			var others = new ValueRow() {
				Answers = answers.Where(x => !supervisors.Any(y => y.Id == x.ByUserId) && x.ByUserId != aboutUser.Id).ToList(),
				Name = "Others"
			};

			allValues.AddRange(managers);
			allValues.Add(self);
			allValues.Add(others);

			var order = new Dictionary<PositiveNegativeNeutral, int>();
			order.Add(PositiveNegativeNeutral.Positive, 3);
			order.Add(PositiveNegativeNeutral.Neutral, 2);
			order.Add(PositiveNegativeNeutral.Negative, 1);
			order.Add(PositiveNegativeNeutral.Indeterminate, 0);
			var mh = 0;
			var top = placement.Top + mh;


			var theValues = answers.GroupBy(x => x.Askable).Select(x => x.Key).ToList();

			var maxWidth_Value = 0.0;
			var angle = 45;

			var valueFont = _Font8Bold;

			foreach (var v in theValues) {
				var value = v.GetQuestion();
				maxWidth_Value = Math.Max(maxWidth_Value, gfx.MeasureString(value, valueFont).Width);
			}
			var titleHeight = Math.Sin(angle * Math.PI / 180) * maxWidth_Value;

			for (var i = 0; i < theValues.Count; i++) {
				var xx = (placement.Left + textWidth) + (i * 2.0 + 1.0) * pieMargin + (i + 0.5) * pieWidth;
				var yy = top + pieMargin + titleHeight;
				var pt = new XPoint(xx, yy);
				gfx.RotateAtTransform(-angle, pt);
				var value = theValues[i].GetQuestion();
				gfx.DrawString(value, valueFont, XBrushes.Black, pt);
				gfx.RotateAtTransform(angle, pt);
			}

			top += titleHeight + pieMargin;
			totalHeight += titleHeight + pieMargin;

			var valueNameFont = _Font;

			foreach (var valueRow in allValues) {
				//double val1 = pieWidth;
				for (int i = 0; i < values.Count; ++i) {
					var a = valueRow.Answers.Where(x => x.Askable.Id == values[i].Id).ToList();

					var slices = GeneratePieSlices(a).OrderByDescending(x => order[x.Item]).Select(x => x.Simple()).ToList();
					var xx = placement.Left + textWidth + (i * 2.0 + 1.0) * pieMargin + i * pieWidth;
					var yy = top + pieMargin;
					var pieLoc = new XRect(xx, yy, pieWidth, pieWidth);
					DrawPieChart(gfx, pieLoc, slices);
				}
				//var font = _Font;
				var height = Math.Max(pieWidth, GetTextHeight(gfx, valueRow.Name, textWidth, valueNameFont));

				var tf = new XTextFormatter(gfx) { Alignment = XParagraphAlignment.Right };
				var h = GetTextHeight(gfx, valueRow.Name, textWidth, valueNameFont);

				if (h > pieWidth) {
					var layoutRectangle = new XRect(placement.Left, top + pieMargin * 2, textWidth, height);
					tf.DrawString(valueRow.Name, valueNameFont, _BlackText, layoutRectangle, XStringFormats.TopLeft);
					totalHeight += h + 2.0 * pieMargin;
					top += h + 2.0 * pieMargin;
				} else {
					var layoutRectangle = new XRect(placement.Left, top + pieMargin * 2 + (pieWidth - h - valueNameFont.GetHeight() / 2.0) / 2.0, textWidth, h);
					tf.DrawString(valueRow.Name, valueNameFont, _BlackText, layoutRectangle, XStringFormats.TopLeft);
					totalHeight += pieWidth + 2.0 * pieMargin;
					top += pieWidth + 2.0 * pieMargin;
				}
			}

			//The bar
			top += pieMargin;
			totalHeight += pieMargin;
			gfx.DrawLine(XPens.LightGray, placement.Left + textWidth, top, placement.Left + textWidth+(valueCount * 2.0 + 1.0) * pieMargin + valueCount * pieWidth, top);
			top += pieMargin;
			totalHeight += pieMargin;


			var barHeight = pieWidth / 3.0;
			var theBarRect = new XRect(placement.Left, top, textWidth, barHeight);
			gfx.DrawString("The Bar", valueNameFont, _BlackText, theBarRect, XStringFormats.CenterRight);
		

			for (int i = 0; i < values.Count; ++i) {
				var valueAnswers = answers.Where(x => x.Askable.Id == values[i].Id).ToList();
				var coreValue = values[i];
				var score = ChartsEngine.ScatterScorer.MergeValueScores(valueAnswers,(CompanyValueModel) coreValue);

				var xx = placement.Left + textWidth + (i * 2.0 + 1.0) * pieMargin + i * pieWidth;
				var yy = top + pieMargin;
				var recLoc = new XRect(xx, yy, pieWidth, barHeight);

				var tiny = values.Count > 3;

				gfx.DrawRectangle(new XSolidBrush(score.Merged.GetXColor()), recLoc);
				var above = "";
				if (score.Above == true)
					above = tiny?"+":"above";
				else if (score.Above == false)
					above = tiny?"-":"below";
				gfx.DrawString(above, _Font7, XBrushes.White, recLoc,XStringFormats.Center);

			}
			top += barHeight;
			totalHeight += barHeight;



			//Feedback
			var feedbacks = answers.Where(x => x.IncludeReason).Select(x=>new FeedbackRow() { Color = x.Exhibits.GetXColor(), Feedback=x.Reason}).ToList();
			_DrawFeedbacks(gfx, feedbacks, placement, ref totalHeight, ref top);

			var actualSize = new XRect(location.Left, location.Top, location.Width, totalHeight + 2 * (margin ?? _DefaultMargin));
			DrawDebug(gfx, actualSize, new XPen(XColors.Blue) { DashStyle = XDashStyle.Dot });
			gfx.Restore(state);
			return actualSize;
		}

		public static XRect DrawHeader(XGraphics gfx, XRect location, ReviewModel review, Unit? margin = null) {
			var state = gfx.Save();

			DrawDebug(gfx, new XRect(location.Left, location.Top, location.Width, 1), XPens.Red);
			var placement = FromMargin(location, margin);

			var lineMargin = 6.0;

			var forUser = review.ForUser;

			var nameFont = _FontLargeBold;
			var name = forUser.GetName();
			var h = GetTextHeight(gfx, name, placement.Width, nameFont);

			//Employee Name
			var top = new XRect(placement.Left, placement.Top, placement.Width, h);
			gfx.DrawString(forUser.GetName(), nameFont, XBrushes.DarkGray, top, XStringFormats.TopCenter);

			var extraFont = _Font8Bold;
			//Review Name
			h = GetTextHeight(gfx, review.Name, placement.Width / 2.0, extraFont);
			var left = new XRect(placement.Left, top.Bottom, placement.Width / 2.0, h);
			gfx.DrawString(review.Name, extraFont, XBrushes.DarkGray, left, XStringFormats.BottomLeft);

			//Titles
			h = GetTextHeight(gfx, review.Name, placement.Width / 2.0, extraFont);
			var right = new XRect(left.Right, top.Bottom, placement.Width / 2.0, h);
			gfx.DrawString(forUser.GetTitles(), extraFont, XBrushes.DarkGray, right, XStringFormats.BottomRight);

			var h2 = Math.Max(left.Height, right.Height);
			
			var totalHeight = top.Height + h2+ lineMargin;

			gfx.DrawLine(XPens.LightGray, placement.Left, placement.Top + totalHeight, placement.Right, placement.Top + totalHeight);

			//totalHeight +=lineMargin;

			var jd = review.ForUser.JobDescription;
			if (!string.IsNullOrWhiteSpace(jd)) {
				var jdW = Unit.FromInch(1.1);
				var jdh = GetTextHeight(gfx, jd, placement.Width- jdW, _Font8);
				gfx.DrawString("Job Description:", _Font8Bold, _BlackText, new XRect(placement.Left, placement.Top + totalHeight, jdW, jdh),XStringFormats.TopLeft);
				var tf = new XTextFormatter(gfx) { Alignment = XParagraphAlignment.Left };
				tf.DrawString(jd, _Font8, _BlackText, new XRect(placement.Left+ jdW, placement.Top + totalHeight, placement.Width - jdW, jdh), XStringFormats.TopLeft);

				totalHeight += jdh;
			}


			var actualSize = new XRect(location.Left, location.Top, location.Width, totalHeight + 2*(margin ?? _DefaultMargin));
			DrawDebug(gfx, actualSize, new XPen(XColors.Blue) { DashStyle = XDashStyle.Dot });
			gfx.Restore(state);
			return actualSize;
		}

		public static XRect DrawFeedback(XGraphics gfx, XRect location, List<FeedbackAnswer> answers,bool anon, Unit? margin = null) {
			var state = gfx.Save();

			DrawDebug(gfx, new XRect(location.Left, location.Top, location.Width, 1), XPens.Red);
			var placement = FromMargin(location, margin);

			var curH = placement.Top;
			var totalHeight = 0.0;

			var titleMargin = 3;
			var feedbackMargin = 9;

			var font = _FontBold;
			var tf = new XTextFormatter(gfx) { Alignment = XParagraphAlignment.Left };
			var feedbackTitle = "Feedback:";
			var h = GetTextHeight(gfx, feedbackTitle, placement.Width, font);
			var pos = new XRect(placement.Left, curH, placement.Width, h);
			tf.DrawString(feedbackTitle, font, _BlackText, pos, XStringFormats.TopLeft);

			totalHeight += h + titleMargin;
			curH += h + titleMargin;

			gfx.DrawLine(XPens.LightGray, placement.Left, curH + titleMargin, placement.Right, curH+ titleMargin);

			totalHeight += feedbackMargin * 2;
			curH += feedbackMargin * 2;


			foreach (var questionAnswers in answers.GroupBy(x => x.Askable)) {

				font = _Font8Bold;
				var question = questionAnswers.Key.GetQuestion();

				tf = new XTextFormatter(gfx) { Alignment = XParagraphAlignment.Left };
				h = GetTextHeight(gfx, question, placement.Width, font);
				pos = new XRect(placement.Left, curH, placement.Width, h);
				tf.DrawString(question, font, _BlackText, pos, XStringFormats.TopLeft);

				curH += h + feedbackMargin;
				totalHeight += h + feedbackMargin;

				font = _Font8;
				foreach (var q in questionAnswers) {
					var feedback = q.Feedback;
					if (!anon) {
						feedback = "\"" + feedback + "\" - " + q.ByUser.GetName();
					}

					var fh = GetTextHeight(gfx, feedback, placement.Width, font);
					var fpos = new XRect(placement.Left + 10, curH, placement.Width - 10, fh);
					tf.DrawString(feedback, font, _BlackText, fpos, XStringFormats.TopLeft);
					curH += fh+ feedbackMargin;
					totalHeight += fh+ feedbackMargin;
				}

				curH += feedbackMargin*2;
				totalHeight += feedbackMargin*2;
			}

			///

			//top.Height + h2;

			var actualSize = new XRect(location.Left, location.Top, location.Width, totalHeight + 2 * (margin ?? _DefaultMargin));
			DrawDebug(gfx, actualSize, new XPen(XColors.Blue) { DashStyle = XDashStyle.Dot });
			gfx.Restore(state);
			return actualSize;
		}

		public static XRect DrawNotes(XGraphics gfx, XRect location,string notes, Unit? margin = null) {
			var state = gfx.Save();

			DrawDebug(gfx, new XRect(location.Left, location.Top, location.Width, 1), XPens.Red);
			var placement = FromMargin(location, margin);

			var curH = placement.Top;
			var totalHeight = 0.0;

			var titleMargin = 3;
			var feedbackMargin = 9;

			var font = _FontBold;
			var tf = new XTextFormatter(gfx) { Alignment = XParagraphAlignment.Left };
			var feedbackTitle = "Supervisor Notes:";
			var h = GetTextHeight(gfx, feedbackTitle, placement.Width, font);
			var pos = new XRect(placement.Left, curH, placement.Width, h);
			tf.DrawString(feedbackTitle, font, _BlackText, pos, XStringFormats.TopLeft);

			totalHeight += h + titleMargin;
			curH += h + titleMargin;

			gfx.DrawLine(XPens.LightGray, placement.Left, curH + titleMargin, placement.Right, curH + titleMargin);

			totalHeight += feedbackMargin * 2;
			curH += feedbackMargin * 2;

			tf = new XTextFormatter(gfx) { Alignment = XParagraphAlignment.Left };
			
			font = _Font8;
			var feedback = notes;
			var fh = GetTextHeight(gfx, feedback, placement.Width - 10, font);
			var fpos = new XRect(placement.Left + 10, curH, placement.Width - 10, fh);
			tf.DrawString(feedback, font, _BlackText, fpos, XStringFormats.TopLeft);
			curH += fh + feedbackMargin;
			totalHeight += fh + feedbackMargin;
				

			curH += feedbackMargin * 2;
			totalHeight += feedbackMargin * 2;

			///

			//top.Height + h2;

			var actualSize = new XRect(location.Left, location.Top, location.Width, totalHeight + 2 * (margin ?? _DefaultMargin));
			DrawDebug(gfx, actualSize, new XPen(XColors.Blue) { DashStyle = XDashStyle.Dot });
			gfx.Restore(state);
			return actualSize;
		}

		private class ValueRow {
			public string Name { get; set; }
			public List<CompanyValueAnswer> Answers { get; set; }
		}
		private class PieSlice<T> {
			public double Data { get; set; }
			public XColor Brush { get; set; }
			public T Item { get; set; }

			public PieSlice Simple() {
				return new PieSlice() {
					Data = Data,
					Brush = Brush,
				};
			}
		}
		private class PieSlice {
			public double Data { get; set; }
			public XColor Brush { get; set; }
		}
		private class FeedbackRow {
			public string Feedback { get; set; }
			public XColor Color { get; set; }
			public string IconName { get; set; }
		}




	}
}