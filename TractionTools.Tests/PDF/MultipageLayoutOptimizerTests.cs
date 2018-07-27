using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities;
using RadialReview.Utilities.Pdf;
using PdfSharp.Drawing;
using System.Linq;
using System.Diagnostics;
using TractionTools.Tests.TestUtils;
using PdfSharp.Pdf;
using MigraDoc.DocumentObjectModel;
using System.Drawing;
using static RadialReview.Utilities.Pdf.MultiPageDocument;

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class MultipageLayoutOptimizerTests : BaseTest {
		[TestMethod]
		public void TestLayoutTesters() {

			var s = Stopwatch.StartNew();
			var layoutTesters = Enumerable.Range(0, 3).Select(i => {
				var res = MultipageLayoutOptimizer.GetTestableLayouts(new Settings(new XSize(1, 1)),i);
				return res;
			}).ToList();
			var elapse = s.ElapsedMilliseconds;
			int a = 0;

			Assert.AreEqual(1, layoutTesters[0].Count);
			Assert.AreEqual(9, layoutTesters[1].Count);
			Assert.AreEqual(73, layoutTesters[2].Count);
			Assert.AreEqual(585, layoutTesters[3].Count);
			Assert.AreEqual(4681, layoutTesters[4].Count);
			Assert.AreEqual(37449, layoutTesters[5].Count);
			Assert.AreEqual(299593, layoutTesters[6].Count);

			Assert.IsTrue(elapse < 2000);

			Throws<ArgumentOutOfRangeException>(() => MultipageLayoutOptimizer.GetTestableLayouts(new Settings(new XSize(1, 1)), 7));
		}

		private PdfDocumentAndStats CreateRectPage(int x, int y, int w, int h, double scale,int i) {
			var  doc = new PdfDocument();
			var p = doc.AddPage();
			var gfx = XGraphics.FromPdfPage(p);
			var ux = Unit.FromInch(x);
			var uy = Unit.FromInch(y);
			var uw = Unit.FromInch(w * scale);
			var uh = Unit.FromInch(h * scale);

			var rect = new XRect(ux, uy, uw, uh);
			p.CropBox = new PdfRectangle(rect);

			var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Purple, Brushes.DarkOrange, Brushes.Pink };
			gfx.DrawString(""+i, new XFont("arial", 12 * scale), Brushes.Blue, new XPoint(ux, uy));
			gfx.DrawRectangle(colors[i% colors.Length], rect);
			return new PdfDocumentAndStats(doc, new DocStats(rect, scale));
		}

		[TestMethod]
		public void TestLayoutOptimizer_BestFit() {
			var docs = new[] {
				CreateRectPage(1, 1, 1, 1, 1, 1),
				CreateRectPage(2, 1, 2, 1, 1, 2),
				CreateRectPage(3, 1, 3, 1, 1, 3),
				CreateRectPage(1, 1, 3, 2, 1, 4),
				CreateRectPage(1, 2, 2, 3, 1, 5),
				CreateRectPage(1, 3, 1, 3, 1, 6),
				CreateRectPage(2, 2, 1, 2, 1, 7)
			};
			var paper = new XSize(Unit.FromInch(8.5), Unit.FromInch(11));
			var layout = MultipageLayoutOptimizer.GetBestLayout(docs, new Settings());
			var builder = new MultiPageDocument( layout);

			Save(layout.DrawDebug(), "TestLayoutOptimizer_BestFit_Rect.pdf");
			Save(builder.Flatten().Document, "TestLayoutOptimizer_BestFit.pdf");


			int a = 0;
		}

		[TestMethod]
		public void TestDrawLayoutOptimizer() {
			var one = XUnit.FromInch(1);
			var layouts = MultipageLayoutOptimizer.GetTestableLayouts(new Settings(new XSize(one, one)), 3);
			var doc = new PdfDocument();

			var colors = new[] { XPens.Red, XPens.Blue, XPens.Green, XPens.Purple, XPens.DarkOrange };

			var j = 0;
			foreach (var l in layouts.OrderBy(s=>s.GetOrderingString())) {
				var p = doc.AddPage();
				p.Width = one;
				p.Height = one;
				var gfx = XGraphics.FromPdfPage(p);
				var i = 0;
				foreach (var c in l.GetGenerators()) {
					var pageCell = c(p, p);
					var col = new SolidBrush(System.Drawing.Color.FromArgb(64, (byte)0, (byte)255, (byte)0));
					gfx.DrawRectangle(col, pageCell.Resize);
				}
				foreach (var rect in l.GetRectangles()) {
					rect.Inflate(-1, -1);
					gfx.DrawRectangle(colors[i%colors.Length], rect);
					i += 1;
				}
				j += 1;
			}

			Save(doc, "TestDrawLayoutOptimizer.pdf");



		}
	}
}
