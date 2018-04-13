using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using System.IO;
using TractionTools.Tests.Permissions;
using RadialReview.Utilities;
using System.Collections.Generic;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Accessors;

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class PdfResizerTests : BasePermissionsTest {
		[TestMethod]
		public void TestResizableElement() {


			var r = new ResizableElement(Unit.FromInch(1.5), (c, v) => {
				c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
			});


			var h = r.CalcHeight(null);

			var doc = new Document();
			r.AddToDocument(t => {
				doc.AddSection().Add(t);
			}, null);

			Save(doc, "TestResizableElement.pdf");
			var a = 0;
		}


		[TestMethod]
		public void TestFit() {
			var items = new List<ResizableElement>();
			for (var i = 0; i < 5; i++) {
				var item = new ResizableElement(Unit.FromInch(1.5), (c, v) => {
					c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
				});
				items.Add(item);
			}

			{
				var result = PdfOptimzer.FitHeights(Unit.FromInch(3), new[] { items[0] }, null);
				Assert.IsTrue(result.Fits);
				Assert.AreEqual(items[0].CalcHeight(null).Inch, result.TotalHeight.Inch);
			}

			{
				var result = PdfOptimzer.FitHeights(Unit.FromInch(3), new[] { items[0], items[1] }, null);
				Assert.IsTrue(result.Fits);
				Assert.AreEqual(items[0].CalcHeight(null).Inch * 2, result.TotalHeight.Inch);
			}

			{
				var result = PdfOptimzer.FitHeights(Unit.FromInch(3), new[] { items[0], items[1], items[2], items[3] }, null);
				Assert.IsFalse(result.Fits);
				Assert.AreEqual(items[0].CalcHeight(null).Inch * 4, result.TotalHeight.Inch);
			}
		}

		[TestMethod]
		public void TestResize() {

			var item = new ResizableElement(Unit.FromInch(1.5), (c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
			});

			var vars = new ResizeVariables();
			vars.Add("FontSize", 15, 7, 20);


			var optimal = PdfOptimzer.OptimizeHeights(Unit.FromInch(.5), new[] { item }, vars);

			var doc = new Document();
			var section = doc.AddSection();
			item.AddToDocument(t => {
				section.Add(t);
			}, optimal.Variables);
			item.AddToDocument(t => {
				section.Add(t);
			}, vars);

			Save(doc, "TestResize.pdf");
		}


		[TestMethod]
		public void TestResizeMultiple() {
			var items = new List<ResizableElement>();
			var vars = new ResizeVariables();

			for (var i = 0; i < 3; i++) {
				vars.Add("FontSize" + i, 7, 5, 20);
				var ii = i;
				var item = new ResizableElement(Unit.FromInch(1.5), (c, v) => {
					c.Format.Font.Size = v.Get("FontSize" + ii);
					c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
				});
				items.Add(item);
			}


			var optimal = PdfOptimzer.OptimizeHeights(Unit.FromInch(1.5), items, vars);

			Console.WriteLine("variables:");
			foreach (var o in optimal.Variables.GetValues()) {
				Console.WriteLine(o);
			}

			Console.WriteLine("==========");
			Console.WriteLine("obj:" + optimal.Error);
			Console.WriteLine("found:" + optimal.SolverCompleted);
			Console.WriteLine("result:" + optimal.Result);


			var doc = new Document();

			var section = doc.AddSection();
			section.PageSetup.TopMargin = Unit.FromInch(0);
			section.PageSetup.BottomMargin = Unit.FromInch(0);
			section.PageSetup.LeftMargin = Unit.FromInch(0);
			section.PageSetup.RightMargin = Unit.FromInch(0);

			foreach (var i in items) {
				i.AddToDocument(t => {
					section.Add(t);
				}, optimal.Variables);
			}

			Save(doc, "TestResizeMultiple.pdf");
		}



		[TestMethod]
		public void TestResizeBoxes() {

			var items = new List<ResizableElement>();

			var WIDTH = Unit.FromInch(1);

			items.Add(new ResizableElement(WIDTH, (c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
				c.Format.SpaceBefore = v.Get("Space") - 1;
				c.Format.SpaceAfter = v.Get("Space") - 1;

			}));

			items.Add(new ResizableElement(WIDTH, (c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg asdg asdg asdg ");
				c.Format.SpaceBefore = v.Get("Space");
				c.Format.SpaceAfter = v.Get("Space") - 1;
			}));


			items.Add(new ResizableElement(WIDTH, (c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg as");
				c.Format.SpaceBefore = v.Get("Space");
				c.Format.SpaceAfter = v.Get("Space") - 1;
			}));

			items.Add(new ResizableElement(WIDTH, (c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg");
				c.Format.SpaceBefore = v.Get("Space");
				c.Format.SpaceAfter = v.Get("Space") - 1;
				c.Borders.Bottom.Width = 1;
			}));


			var vars = new ResizeVariables();
			vars.Add("Space", Unit.FromInch(.25), Unit.FromInch(.025), Unit.FromInch(5));
			vars.Add("FontSize", 8, 6, 20);

			var heights = new[] {  Unit.FromInch(7), Unit.FromInch(5), Unit.FromInch(4), Unit.FromInch(3), Unit.FromInch(2), Unit.FromInch(1) };

			var doc = new Document();
			var section = doc.AddSection();
			section.PageSetup.TopMargin = Unit.FromInch(0);
			section.PageSetup.BottomMargin = Unit.FromInch(0);
			section.PageSetup.LeftMargin = Unit.FromInch(0);
			section.PageSetup.RightMargin = Unit.FromInch(0);

			var mainTable = section.AddTable();
			//mainTable.Borders.Width = 1;

			foreach (var h in heights) {
				mainTable.AddColumn(WIDTH);
			}

			var mainRow = mainTable.AddRow();
			var statRow = mainTable.AddRow();

			for (var j=0;j<heights.Length;j++) {
				var container = mainRow.Cells[j];
				var stats = statRow.Cells[j];
				var h = heights[j];

				var optimal = PdfOptimzer.OptimizeHeights(h, items, vars);
				
				var table = container.Elements.AddTable();
				table.AddColumn(WIDTH);

				foreach (var i in items) {
					var r = table.AddRow();
					i.AddToDocument(t => {
						r.Cells[0].Elements.Add(t);
					}, optimal.Variables);
				}

				stats.Format.Font.Size = 7;
				stats.AddParagraph("Result:" + optimal.Result);
				stats.AddParagraph("Fit:" + optimal.Fit.Fits);
				stats.AddParagraph("Err:" + optimal.Fit.SqrError);

			}
			Save(doc, "TestResizeBoxes.pdf");

		}

		[TestMethod]
		public void TestResizeVto() {

			var vto = new AngularVTO();

			vto.Values = new List<AngularCompanyValue>() {
				new AngularCompanyValue() {CompanyValue ="First" },
				new AngularCompanyValue() {CompanyValue ="Second" },
				new AngularCompanyValue() {CompanyValue ="Third" },
				new AngularCompanyValue() {CompanyValue ="Fourth" },
			};

			vto.TenYearTarget = "An amazing ten year goal";

			vto.Strategies = new List<AngularStrategy>() {
				new AngularStrategy() { Title="Strat1", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				new AngularStrategy() { Title="Strat2", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				new AngularStrategy() { Title="Strat3", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				//new AngularStrategy() { Title="Strat4", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				//new AngularStrategy() { Title="Strat5", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" }
			};
			

			var doc = new Document();
			PdfAccessor.AddVtoVision(doc, vto, null);

			Save(doc, "TestResizeVto.pdf");

		}


		private void Save(Document doc, string name) {
			PdfDocumentRenderer renderer = new PdfDocumentRenderer(true);
			renderer.Document = doc;
			renderer.RenderDocument();
			renderer.PdfDocument.Save(Path.Combine(GetCurrentPdfFolder(), name));
			renderer.PdfDocument.Save(Path.Combine(GetPdfFolder(), name));
		}
	}
}
