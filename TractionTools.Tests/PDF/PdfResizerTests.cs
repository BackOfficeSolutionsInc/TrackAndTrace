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
using System.Linq;
using MigraDoc.DocumentObjectModel.Tables;

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class PdfResizerTests : BasePermissionsTest {
		[TestMethod]
		[TestCategory("PDF")]
		public void TestSectionOptimzer_Element() {


			var r = new ResizableElement((c, v) => {
				c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
			});

			var w = Unit.FromInch(1.5);
			var h = r.CalcHeight(w, null);

			var doc = new Document();
			r.AddToDocument(t => {
				doc.AddSection().Add(t);
			}, w, null);

			Save(doc, "TestSectionOptimzer_Element.pdf");
			var a = 0;
		}


		[TestMethod]
		[TestCategory("PDF")]
		public void TestSectionOptimzer_FitHeight() {
			var items = new List<ResizableElement>();
			var w = Unit.FromInch(1.5);
			for (var i = 0; i < 5; i++) {

				var item = new ResizableElement((c, v) => {
					c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
				});
				items.Add(item);
			}

			{
				var result = ViewBoxOptimzer.FitHeights(Unit.FromInch(3), w, new[] { items[0] }, null);
				Assert.IsTrue(result.Fits);
				Assert.AreEqual(items[0].CalcHeight(w, null).Inch, result.TotalHeight.Inch);
			}

			{
				var result = ViewBoxOptimzer.FitHeights(Unit.FromInch(3), w, new[] { items[0], items[1] }, null);
				Assert.IsTrue(result.Fits);
				Assert.AreEqual(items[0].CalcHeight(w, null).Inch * 2, result.TotalHeight.Inch);
			}

			{
				var result = ViewBoxOptimzer.FitHeights(Unit.FromInch(3), w, new[] { items[0], items[1], items[2], items[3] }, null);
				Assert.IsFalse(result.Fits);
				Assert.AreEqual(items[0].CalcHeight(w, null).Inch * 4, result.TotalHeight.Inch);
			}
		}

		[TestMethod]
		[TestCategory("PDF")]
		public void TestSectionOptimzer() {
			var w = Unit.FromInch(1.5);
			var item = new ResizableElement((c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
			});

			var vars = new RangedVariables();
			vars.Add("FontSize", 15, 7, 20);


			var optimal = ViewBoxOptimzer.Optimize(Unit.FromInch(.5), w, new[] { item }, vars);

			var doc = new Document();
			var section = doc.AddSection();
			item.AddToDocument(t => {
				section.Add(t);
			}, w, optimal.Variables);
			item.AddToDocument(t => {
				section.Add(t);
			}, w, vars);

			Save(doc, "TestSectionOptimzer.pdf");
		}


		[TestMethod]
		[TestCategory("PDF")]
		public void TestResizeMultiple() {
			var items = new List<ResizableElement>();
			var vars = new RangedVariables();
			var w = Unit.FromInch(1.5);
			for (var i = 0; i < 3; i++) {
				vars.Add("FontSize" + i, 7, 5, 20);
				var ii = i;
				var item = new ResizableElement((c, v) => {
					c.Format.Font.Size = v.Get("FontSize" + ii);
					c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
				});
				items.Add(item);
			}


			var optimal = ViewBoxOptimzer.Optimize(Unit.FromInch(1.5), w, items, vars);

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
				}, w, optimal.Variables);
			}

			Save(doc, "TestResizeMultiple.pdf");
		}



		[TestMethod]
		[TestCategory("PDF")]
		public void TestSectionOptimzer_Boxes() {

			var items = new List<ResizableElement>();

			var WIDTH = Unit.FromInch(1);

			items.Add(new ResizableElement((c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
				c.Format.SpaceBefore = v.Get("Space") - 1;
				c.Format.SpaceAfter = v.Get("Space") - 1;

			}));

			items.Add(new ResizableElement((c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg asdg asdg asdg ");
				c.Format.SpaceBefore = v.Get("Space");
				c.Format.SpaceAfter = v.Get("Space") - 1;
			}));


			items.Add(new ResizableElement((c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg as");
				c.Format.SpaceBefore = v.Get("Space");
				c.Format.SpaceAfter = v.Get("Space") - 1;
			}));

			items.Add(new ResizableElement((c, v) => {
				c.Format.Font.Size = v.Get("FontSize");
				var p = c.AddParagraph("asdg asdg");
				c.Format.SpaceBefore = v.Get("Space");
				c.Format.SpaceAfter = v.Get("Space") - 1;
				c.Borders.Bottom.Width = 1;
			}));


			var vars = new RangedVariables();
			vars.Add("Space", Unit.FromInch(.25), Unit.FromInch(.025), Unit.FromInch(5));
			vars.Add("FontSize", 8, 6, 20);

			var heights = new[] { Unit.FromInch(7), Unit.FromInch(5), Unit.FromInch(4), Unit.FromInch(3), Unit.FromInch(2), Unit.FromInch(1) };

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

			for (var j = 0; j < heights.Length; j++) {
				var container = mainRow.Cells[j];
				var stats = statRow.Cells[j];
				var h = heights[j];

				var optimal = ViewBoxOptimzer.Optimize(h, WIDTH, items, vars);

				var table = container.Elements.AddTable();
				table.AddColumn(WIDTH);

				foreach (var i in items) {
					var r = table.AddRow();
					i.AddToDocument(t => {
						r.Cells[0].Elements.Add(t);
					}, WIDTH, optimal.Variables);
				}

				stats.Format.Font.Size = 7;
				stats.AddParagraph("Result:" + optimal.Result);
				stats.AddParagraph("Fit:" + optimal.FitResults.Fits);
				stats.AddParagraph("Err:" + optimal.FitResults.SqrError);

			}
			Save(doc, "TestSectionOptimzer_Boxes.pdf");

		}


		public class LayoutGen : IDocumentGenerator {

			public class Section : INamedViewBox {

				public Section(string name, Unit height, Unit width) {
					Name = name;
					Height = height;
					Width = width;
				}

				public string Name { get; set; }
				public Unit Height { get; set; }
				public Unit Width { get; set; }

				public Unit GetHeight() { return Height; }

				public string GetName() { return Name; }
				public Unit GetWidth() { return Width; }
			}

			public class PageLayout : IPageGenerator {


				public const double WIDTH = 4.5;
				public const double HEIGHT = 3.25;


				public Action<MigraDoc.DocumentObjectModel.Section> GetDrawer(IEnumerable<string> forNamedSections,IEnumerable<IDrawInstruction> instructions) {
					var hasA = forNamedSections.Contains("A");
					var hasB = forNamedSections.Contains("B");
					var instructionLookup = instructions.ToDictionary(x=>x.ViewBox.GetName(),x=>x);
					if (hasA && hasB) {
						return new Action<MigraDoc.DocumentObjectModel.Section>(section => {
							var table = section.AddTable();
							table.AddColumn(Unit.FromInch(1));
							table.AddColumn(Unit.FromInch(WIDTH));
							table.AddColumn(Unit.FromInch(WIDTH));
							table.AddColumn(Unit.FromInch(1));

							var r = table.AddRow();
							r.Cells[0].AddParagraph("'A' Block");
							var aContent = instructionLookup["A"];
							r.Cells[1].Elements.Add(aContent.Contents);
							var bContent = instructionLookup["B"];
							r.Cells[2].Elements.Add(bContent.Contents);
							r.Cells[3].AddParagraph("'B' Block");
						});
					}
					if (hasA) {
						return new Action<MigraDoc.DocumentObjectModel.Section>(section => {
							var table = section.AddTable();
							table.AddColumn(Unit.FromInch(1));
							table.AddColumn(Unit.FromInch(WIDTH * 2));

							var r = table.AddRow();
							var aContent = instructionLookup["A"];
							r.Cells[0].AddParagraph("'A' Block");
							r.Cells[1].Elements.Add(aContent.Contents);
						});
					}
					if (hasB) {
						return new Action<MigraDoc.DocumentObjectModel.Section>(section => {
							var table = section.AddTable();
							table.AddColumn(Unit.FromInch(WIDTH * 2));
							table.AddColumn(Unit.FromInch(1));
							var r = table.AddRow();
							var bContent = instructionLookup["B"];
							r.Cells[0].Elements.Add(bContent.Contents);
							r.Cells[1].AddParagraph("'B' Block");
						});
					}

					return new Action<MigraDoc.DocumentObjectModel.Section>(section => { });

				}

				public IEnumerable<INamedViewBox> GetViewBoxes(IEnumerable<string> forNamedSections) {
					var hasA = forNamedSections.Contains("A");
					var hasB = forNamedSections.Contains("B");
					if (hasA && hasB) {
						return new List<INamedViewBox>() {
							new Section("A",Unit.FromInch(HEIGHT),Unit.FromInch(WIDTH)),
							new Section("B",Unit.FromInch(HEIGHT),Unit.FromInch(WIDTH)),
						};
					}

					if (hasA) {
						return new List<INamedViewBox>() {
							new Section("A",Unit.FromInch(HEIGHT),Unit.FromInch(WIDTH*2)),
						};
					}
					if (hasB) {
						return new List<INamedViewBox>() {
							new Section("B",Unit.FromInch(HEIGHT),Unit.FromInch(WIDTH*2)),
						};
					}
					return new List<INamedViewBox>();
				}
			}

			public IPageGenerator GetPageLayout(int page) {
				return new PageLayout();
			}

		}

		//public class Hint : IHint {
		//	public int Length { get; set; }
		//	public string Section{ get; set; }

		//	public Hint(int length,string section) {
		//		Length = length;
		//		Section = section;
		//	}

		//	public IEnumerable<IElement> GetElements() {
		//		for (var i = 0; i < Length; i++)
		//			yield return new ResizableElement((c,v) => {
		//				c.Borders.Width = 1;
		//				if (Section == "A") {
		//					c.AddParagraph("asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf");
		//				} else {
		//					c.AddParagraph("qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer");
		//				}
		//				c.Format.SpaceBefore = v.Get("Space");
		//				c.Format.SpaceAfter = v.Get("Space");
		//			});
		//	}

		//	public string ForViewBox() {
		//		return Section;
		//	}

		//}

		private IEnumerable<IElement> HintElements(string Section,int Length) {
			for (var i = 0; i < Length; i++)
				yield return new ResizableElement((c, v) => {
					c.Borders.Width = 1;
					if (Section == "A") {
						c.AddParagraph("asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf");
					} else {
						c.AddParagraph("qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer");
					}
					c.Format.SpaceBefore = v.Get("Space");
					c.Format.SpaceAfter = v.Get("Space");
				});
		}


		[TestMethod]
		[TestCategory("PDF")]
		public void TestLayoutOptimizer() {
			var layoutGen = new LayoutGen();

			var aHints = Enumerable.Range(0, 2).Select(x => new Hint("A", HintElements("A", 2))).ToList();
			var bHints = Enumerable.Range(0, 2).Select(x => new Hint("B", HintElements("B", 3))).ToList();
			var allHints = aHints.Union(bHints).ToList();

			var vars = new RangedVariables();
			vars.Add("Space", Unit.FromInch(.2), Unit.FromInch(.0125), Unit.FromInch(6));

			var result = LayoutOptimizer.Optimize(layoutGen, allHints, vars);

			var doc = new Document();
			doc.DefaultPageSetup.PageWidth = Unit.FromInch(11);
			doc.DefaultPageSetup.PageHeight = Unit.FromInch(8.5);
			doc.DefaultPageSetup.TopMargin = Unit.FromInch(0);
			doc.DefaultPageSetup.BottomMargin = Unit.FromInch(0);
			doc.DefaultPageSetup.LeftMargin = Unit.FromInch(0);
			doc.DefaultPageSetup.RightMargin = Unit.FromInch(0);
			LayoutOptimizer.Draw(doc, result);

			Save(doc, "TestLayoutOptimizer.pdf");
		}


		[TestMethod]
		[TestCategory("PDF")]
		public void TestSectionOptimzer_Vto() {

			var vto = new AngularVTO();

			vto.Values = new List<AngularCompanyValue>() {
				new AngularCompanyValue() {CompanyValue ="First" },
				new AngularCompanyValue() {CompanyValue ="Second" },
				new AngularCompanyValue() {CompanyValue ="Third" },
				new AngularCompanyValue() {CompanyValue ="Fourth" },
			};

			vto.TenYearTarget = "An amazing ten year goal";

			vto.Strategies = new List<AngularStrategy>() {
				new AngularStrategy() { Title="Strat1", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys, lets make it a longer title to test it out and see if things wrap correctly..." },
				new AngularStrategy() { Title="Strat2", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				//new AngularStrategy() { Title="Strat3", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				//new AngularStrategy() { Title="Strat4", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				//new AngularStrategy() { Title="Strat5", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" }
			};


			vto.ThreeYearPicture = new AngularThreeYearPicture();

			var ll = new List<AngularVtoString>();
			for (var i = 0; i < 10; i++) {
				ll.Add(new AngularVtoString() { Data = "It looks like a big company", });
				ll.Add(new AngularVtoString() { Data = "There are lots of people", });
				ll.Add(new AngularVtoString() { Data = "and customers", });
			}

			vto.ThreeYearPicture.LooksLike = ll;


			var doc = new Document();
			//PdfAccessor.AddVtoVision_Intermediate(doc, vto, null);
			PdfAccessor.AddVtoVision(doc, vto, null);

			Save(doc, "TestSectionOptimzer_Vto.pdf");

		}


		[TestMethod]
		[TestCategory("PDF")]
		public void EnsurePDFDebuggerIsOff() {
			Assert.IsFalse(PdfAccessor.DEBUGGER);
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
