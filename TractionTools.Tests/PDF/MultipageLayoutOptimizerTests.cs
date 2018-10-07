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
using static RadialReview.Utilities.Pdf.MultipageLayoutOptimizer;

namespace TractionTools.Tests.PDF {
    [TestClass]
    public class MultipageLayoutOptimizerTests : BaseTest {
        [TestMethod]
        public void TestLayoutTesters() {
            var s = Stopwatch.StartNew();
            var layoutTesters = Enumerable.Range(0, 4).Select(i => {
                var breakout = new TimeoutCheck(TimeSpan.FromSeconds(6));
                var res = MultipageLayoutOptimizer.GetTestableLayouts(new Settings(new XSize(1000, 1000)), breakout, i);
                return res;
            }).ToList();
            var elapse = s.ElapsedMilliseconds;
            int a = 0;

            Assert.AreEqual(1, layoutTesters[0].Count);
            Assert.AreEqual(3, layoutTesters[1].Count);
            Assert.AreEqual(37, layoutTesters[2].Count);
            Assert.AreEqual(5477, layoutTesters[3].Count);
            //Assert.AreEqual(123, layoutTesters[4].Count);
            //Assert.AreEqual(37449, layoutTesters[5].Count);
            //Assert.AreEqual(299593, layoutTesters[6].Count);

            Assert.IsTrue(elapse < 5000);

            Throws<ArgumentOutOfRangeException>(() => {
                var breakout = new TimeoutCheck(TimeSpan.FromSeconds(6));
                MultipageLayoutOptimizer.GetTestableLayouts(new Settings(new XSize(1, 1)), breakout, 4);
            });
        }

        private PdfDocumentAndStats CreateRectPage(int x, int y, int w, int h, double scale, int i) {
            var doc = new PdfDocument();
            var p = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(p);
            var ux = Unit.FromInch(x);
            var uy = Unit.FromInch(y);
            var uw = Unit.FromInch(w * scale);
            var uh = Unit.FromInch(h * scale);

            var rect = new XRect(ux, uy, uw, uh);
            p.CropBox = new PdfRectangle(rect);

            var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Purple, Brushes.DarkOrange, Brushes.Pink };
            gfx.DrawString("" + i, new XFont("arial", 12 * scale), Brushes.Blue, new XPoint(ux, uy));
            gfx.DrawRectangle(colors[i % colors.Length], rect);
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
            var timeout = TimeSpan.FromSeconds(6);
            var sw = Stopwatch.StartNew();
            var layout = MultipageLayoutOptimizer.GetBestLayout(docs, new Settings(), timeout);
            Console.WriteLine(sw.ElapsedMilliseconds + "ms");
            Assert.IsTrue(sw.Elapsed < timeout.Add(TimeSpan.FromMilliseconds(10)), "Over by " + (sw.Elapsed - timeout).TotalSeconds + "s");
            var builder = new MultiPageDocument(layout);
            Assert.IsFalse(layout.IsFallback, "Should not have been the fallback");

            Save(layout.DrawDebug(), "TestLayoutOptimizer_BestFit_Rect.pdf");
            Save(builder.Flatten().Document, "TestLayoutOptimizer_BestFit.pdf");

            int a = 0;
        }

        [TestMethod]
        public void TestLayoutOptimizer_BestFit_ManyPages() {
            var docs = new[] {
                CreateRectPage(1, 1, 1, 1, 1, 1 ),
                CreateRectPage(2, 1, 2, 1, 1, 2 ),
                CreateRectPage(3, 1, 3, 1, 1, 3 ),
                CreateRectPage(1, 1, 3, 2, 1, 4 ),
                CreateRectPage(1, 2, 2, 3, 1, 5 ),
                CreateRectPage(1, 3, 1, 3, 1, 6 ),
                CreateRectPage(2, 2, 1, 2, 1, 7 ),
                CreateRectPage(1, 1,10, 2, 1, 8 ),
                CreateRectPage(1, 1,10,10, 1, 9 ),
                CreateRectPage(1, 1, 2,10, 1, 10),
                CreateRectPage(1, 1, 2, 2, 1, 11),
                CreateRectPage(1, 1,10, 6, 1, 12),
                CreateRectPage(1, 1,10,10, 1, 13),
                CreateRectPage(1, 1, 6,10, 1, 14),
                CreateRectPage(1, 1, 6, 6, 1, 15),
                CreateRectPage(1, 1,10, 8, 1, 16),
                CreateRectPage(1, 1,10,10, 1, 17),
                CreateRectPage(1, 1, 8,10, 1, 18),
                CreateRectPage(1, 1, 8, 8, 1, 19),
                CreateRectPage(1, 1,10, 9, 1, 20),
                CreateRectPage(1, 1,10,10, 1, 21),
                CreateRectPage(1, 1, 9,10, 1, 22),
                CreateRectPage(1, 1, 9, 9, 1, 23),
                CreateRectPage(1, 1, 8, 9, 1, 24),
                CreateRectPage(1, 1, 8, 8, 1, 25),
                CreateRectPage(1, 1, 9, 8, 1, 26),
                CreateRectPage(1, 1, 9, 9, 1, 27),

            };
            var timeout = TimeSpan.FromSeconds(5);
            var paper = new XSize(Unit.FromInch(8.5), Unit.FromInch(11));

            var sw = Stopwatch.StartNew();
            var layout = MultipageLayoutOptimizer.GetBestLayout(docs, new Settings(), timeout);
            Console.WriteLine(sw.ElapsedMilliseconds + "ms");
            Assert.IsTrue(sw.Elapsed < timeout.Add(TimeSpan.FromMilliseconds(10)),"Over by "+(sw.Elapsed-timeout).TotalSeconds+"s");
            Assert.IsTrue(layout.IsFallback,"Should have been the fallback");
            var builder = new MultiPageDocument(layout);

            Save(layout.DrawDebug(), "TestLayoutOptimizer_BestFit_Rect_Fallback.pdf");
            Save(builder.Flatten().Document, "TestLayoutOptimizer_BestFit_Fallback.pdf");

            int a = 0;
        }

        [TestMethod]
        public void TestDrawLayoutOptimizer() {
            var one = XUnit.FromInch(1);
            var breakout = new TimeoutCheck(TimeSpan.FromSeconds(6));
            var layouts = MultipageLayoutOptimizer.GetTestableLayouts(new Settings(new XSize(one, one)), breakout, 3);
            var doc = new PdfDocument();

            var colors = new[] { XPens.Red, XPens.Blue, XPens.Green, XPens.Purple, XPens.DarkOrange };

            var j = 0;
            foreach (var l in layouts.OrderBy(s => s.GetOrderingString())) {
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
                    gfx.DrawRectangle(colors[i % colors.Length], rect);
                    i += 1;
                }
                j += 1;
            }

            Save(doc, "TestDrawLayoutOptimizer.pdf");



        }
    }
}
