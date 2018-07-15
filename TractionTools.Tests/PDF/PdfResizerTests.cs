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
using static RadialReview.Accessors.PdfAccessor;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Rocks;
using System.Diagnostics;

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class PdfResizerTests : BasePermissionsTest {
		[TestMethod]
		[TestCategory("PDF")]
		public void TestSectionOptimzer_Element() {


			var r = new ResizableElement(c=> {
				c.Container.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
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

				var item = new ResizableElement(c => {
					c.Container.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
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
			var item = new ResizableElement(c => {
				c.Container.Format.Font.Size = c.Variables.Get("FontSize");
				c.Container.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
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
				var item = new ResizableElement(c => {
					c.Container.Format.Font.Size = c.Variables.Get("FontSize" + ii);
					c.Container.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
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

			items.Add(new ResizableElement(c=> {
				c.Container.Format.Font.Size = c.Variables.Get("FontSize");
				var p = c.Container.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg");
				c.Container.Format.SpaceBefore = c.Variables.Get("Space") - 1;
				c.Container.Format.SpaceAfter = c.Variables.Get("Space") - 1;

			}));

			items.Add(new ResizableElement(c => {
				c.Container.Format.Font.Size = c.Variables.Get("FontSize");
				var p = c.Container.AddParagraph("asdg asdg asdg asdg asdg ");
				c.Container.Format.SpaceBefore = c.Variables.Get("Space");
				c.Container.Format.SpaceAfter = c.Variables.Get("Space") - 1;
			}));


			items.Add(new ResizableElement(c => {
				c.Container.Format.Font.Size = c.Variables.Get("FontSize");
				var p = c.Container.AddParagraph("asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asasdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg asdg as");
				c.Container.Format.SpaceBefore = c.Variables.Get("Space");
				c.Container.Format.SpaceAfter = c.Variables.Get("Space") - 1;
			}));

			items.Add(new ResizableElement(c => {
				c.Container.Format.Font.Size = c.Variables.Get("FontSize");
				var p = c.Container.AddParagraph("asdg asdg");
				c.Container.Format.SpaceBefore = c.Variables.Get("Space");
				c.Container.Format.SpaceAfter = c.Variables.Get("Space") - 1;
				c.Container.Borders.Bottom.Width = 1;
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


				public async Task Draw(MigraDoc.DocumentObjectModel.Section section, IEnumerable<string> forNamedSections, IEnumerable<IDrawInstruction> instructions) {
					var hasA = forNamedSections.Contains("A");
					var hasB = forNamedSections.Contains("B");
					var instructionLookup = instructions.ToDictionary(x => x.ViewBox.GetName(), x => x);
					if (hasA && hasB) {
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
						return;
					}
					if (hasA) {
						var table = section.AddTable();
						table.AddColumn(Unit.FromInch(1));
						table.AddColumn(Unit.FromInch(WIDTH * 2));

						var r = table.AddRow();
						var aContent = instructionLookup["A"];
						r.Cells[0].AddParagraph("'A' Block");
						r.Cells[1].Elements.Add(aContent.Contents);
						return;
					}
					if (hasB) {
						var table = section.AddTable();
						table.AddColumn(Unit.FromInch(WIDTH * 2));
						table.AddColumn(Unit.FromInch(1));
						var r = table.AddRow();
						var bContent = instructionLookup["B"];
						r.Cells[0].Elements.Add(bContent.Contents);
						r.Cells[1].AddParagraph("'B' Block");
						return;
					}
					return;

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

		private IEnumerable<IElement> HintElements(string Section, int Length) {
			for (var i = 0; i < Length; i++)
				yield return new ResizableElement(c => {
					c.Container.Borders.Width = 1;
					if (Section == "A") {
						c.Container.AddParagraph("asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf asdf");
					} else {
						c.Container.AddParagraph("qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer qwer");
					}
					c.Container.Format.SpaceBefore = c.Variables.Get("Space");
					c.Container.Format.SpaceAfter = c.Variables.Get("Space");
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
		public void TestSectionOptimzer_Vto_Vision() {

			var vto = new AngularVTO();
			var j = 0;

			var pageDir = new[] {
				new { cv = 4,pic = 40, strat = 3, colors= false },
				new { cv = 16,pic = 40, strat = 3, colors= false },
				new { cv = 4,pic = 40, strat = 2, colors= false },
				new { cv = 4,pic = 10, strat = 2, colors= false },
				new { cv = 4,pic = 10, strat = 3, colors= true  },
			};
			foreach (var pd in pageDir) {
				vto.Values = new List<AngularCompanyValue>() {
					new AngularCompanyValue() {CompanyValue ="First" },
					new AngularCompanyValue() {CompanyValue ="Second" },
					new AngularCompanyValue() {CompanyValue ="Third" },
					new AngularCompanyValue() {CompanyValue ="Fourth" },
					new AngularCompanyValue() {CompanyValue ="Fifth" },
					new AngularCompanyValue() {CompanyValue ="Sixth" },
					new AngularCompanyValue() {CompanyValue ="Seventh" },
					new AngularCompanyValue() {CompanyValue ="Eighth" },
					new AngularCompanyValue() {CompanyValue ="Ninth" },
					new AngularCompanyValue() {CompanyValue ="Tenth" },
					new AngularCompanyValue() {CompanyValue ="Eleventh" },
					new AngularCompanyValue() {CompanyValue ="Twelvth" },
					new AngularCompanyValue() {CompanyValue ="Thirteenth" },
					new AngularCompanyValue() {CompanyValue ="Fourteenth" },
					new AngularCompanyValue() {CompanyValue ="Fifteenth" },
					new AngularCompanyValue() {CompanyValue ="Sixteenth" },
				};

				vto.Values = vto.Values.Take(pd.cv).ToList();

				vto.TenYearTarget = "An amazing ten year goal";

				var strats = new List<AngularStrategy>() {
					new AngularStrategy() { Title="Strat1", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys, lets make it a longer title to test it out and see if things wrap correctly..." },
					new AngularStrategy() { Title="Strat2", Guarantee ="i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i ",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
					new AngularStrategy() { Title="Strat3", Guarantee ="Love it or your money back",ProvenProcess ="Our Process",TargetMarket ="All those companies with people in them that pay us moneys" },
				};

				vto.Strategies = strats.Take(pd.strat).ToList();

				vto.CoreFocus = new AngularCoreFocus() {
					Purpose = "Create Freedom & Simplify the Lives of Leaders.",
					Niche = "The Best Software for EOS."
				};

				vto.ThreeYearPicture = new AngularThreeYearPicture();

				var ll = new List<AngularVtoString>();
				for (var i = 0; i < 10; i++) {
					ll.Add(new AngularVtoString() { Data = "It looks like a big company with lots of employees and benefits for the workers", });
					ll.Add(new AngularVtoString() { Data = "There are lots of people", });
					ll.Add(new AngularVtoString() { Data = "and customers", });
					ll.Add(new AngularVtoString() { Data = "i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i ", });
				}





				vto.ThreeYearPicture.LooksLike = ll.Take(pd.pic);
				vto.Name = "Awesome VTO";

				var issuesList = new List<AngularVtoString>();
				for (var i = 0; i < 10; i++) {
					issuesList.Add(new AngularVtoString() { Data = "It looks like a big company with lots of employees and benefits for the workers", });
					issuesList.Add(new AngularVtoString() { Data = "There are lots of people", });
					issuesList.Add(new AngularVtoString() { Data = "and customers", });
					issuesList.Add(new AngularVtoString() { Data = "i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i ", });
				}

				vto.Issues = issuesList.Take(pd.pic); 

				//PdfAccessor.AddVtoVision_Intermediate(doc, vto, null);

				VtoPdfSettings[] settings = new VtoPdfSettings[] { null };

				if (pd.colors) {
					settings = new[] {
						null,
						new VtoPdfSettings() {
							FillColor = Color.FromArgb(100, 239, 118, 34),
							BorderColor = Color.FromArgb(255, 239, 118, 34),
							ImageUrl = "https://s3.amazonaws.com/Radial/base/Logos/TRACTION-TOOLS_Signature.png"
						}, new VtoPdfSettings() {
							FillTextColor = Colors.Red,
							FillColor = Color.FromArgb(255, 118, 239, 34),
							ImageUrl = "https://s3.amazonaws.com/Radial/base/Charts/All.png"
						}, new VtoPdfSettings() {
							FillColor = Color.FromArgb(255, 34, 118, 239),
							ImageUrl = "https://cataas.com/cat?height=150&width=100"
						}
					};
				}

				foreach (var s in settings) {
					var doc = new Document();
					PdfAccessor.AddVtoVision(doc, vto, null, s);
					PdfAccessor.AddVtoTraction(doc, vto, null, s);
					Save(doc, "TestSectionOptimzer_Vto_" + j + "+TYP"+pd.pic+"+Strat"+pd.strat+(pd.colors?"_colors":"")+".pdf");
					j++;
				}
			}

		}
		[TestMethod]
		[TestCategory("PDF")]
		public void TestSectionOptimzer_Vto_Traction() {

			var vto = new AngularVTO();
			var j = 0;

			var pageDir = new[] {
				new { cv = 4,   pic = 40, strat = 3, colors= true  , rocks =40 , goals=40 , includeLong = false},
				new { cv = 4,	pic = 40, strat = 3, colors= false , rocks =40 , goals=10 , includeLong = true},
				new { cv = 16,	pic = 40, strat = 3, colors= false , rocks =10 , goals=40 , includeLong = true},
				new { cv = 4,	pic = 10, strat = 2, colors= false , rocks =10 , goals=10 , includeLong = true},
				new { cv = 4,	pic = 10, strat = 2, colors= false , rocks =40 , goals=10 , includeLong = true},
				new { cv = 4,   pic = 10, strat = 3, colors= true  , rocks =10 , goals=40 , includeLong = true},
			};
			foreach (var pd in pageDir) {
				
				vto.Name = "Awesome VTO";

				var issuesList = new List<AngularVtoString>();
				for (var i = 0; i < 10; i++) {
					if (pd.includeLong)
						issuesList.Add(new AngularVtoString() { Data = "It looks like a big company with lots of employees and benefits for the workers", });
					issuesList.Add(new AngularVtoString() { Data = "There are lots of people", });
					issuesList.Add(new AngularVtoString() { Data = "and customers", });
					if (pd.includeLong)
						issuesList.Add(new AngularVtoString() { Data = "i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i ", });
				}

				var rockList = new List<AngularVtoRock>();
				for (var i = 0; i < 10; i++) {
					if (pd.includeLong)
						rockList.Add(new AngularVtoRock() {Rock = new AngularRock() { Name = "It looks like a big company with lots of employees and benefits for the workers", } });
					rockList.Add(new AngularVtoRock() { Rock = new AngularRock() { Name = "There are lots of people", } });
					rockList.Add(new AngularVtoRock() { Rock = new AngularRock() { Name = "and customers", } });
					if (pd.includeLong)
						rockList.Add(new AngularVtoRock() { Rock = new AngularRock() { Name = "i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i ", } });
				}


				var goalsList = new List<AngularVtoString>();
				for (var i = 0; i < 10; i++) {
					if (pd.includeLong)
						goalsList.Add(new AngularVtoString() { Data = "It looks like a big company with lots of employees and benefits for the workers", });
					goalsList.Add(new AngularVtoString() { Data = "There are lots of people", });
					goalsList.Add(new AngularVtoString() { Data = "and customers", });
					if (pd.includeLong)
						goalsList.Add(new AngularVtoString() { Data = "i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i i ", });
				}

				vto.Issues = issuesList.Take(pd.pic);
				vto.QuarterlyRocks = new AngularQuarterlyRocks() {
					Rocks = rockList.Take(pd.rocks)
				};
				vto.OneYearPlan = new AngularOneYearPlan() {
					GoalsForYear = goalsList.Take(pd.goals)
				};

				//PdfAccessor.AddVtoVision_Intermediate(doc, vto, null);

				VtoPdfSettings[] settings = new VtoPdfSettings[] { null };

				if (pd.colors) {
					settings = new[] {
						null,
						new VtoPdfSettings() {
							FillColor = Color.FromArgb(100, 239, 118, 34),
							BorderColor = Color.FromArgb(255, 239, 118, 34),
							ImageUrl = "https://s3.amazonaws.com/Radial/base/Logos/TRACTION-TOOLS_Signature.png"
						}, new VtoPdfSettings() {
							FillTextColor = Colors.Red,
							FillColor = Color.FromArgb(255, 118, 239, 34),
							ImageUrl = "https://s3.amazonaws.com/Radial/base/Charts/All.png"
						}, new VtoPdfSettings() {
							FillColor = Color.FromArgb(255, 34, 118, 239),
							ImageUrl = "https://cataas.com/cat?height=150&width=100"
						}
					};
				}

				foreach (var s in settings) {
					var doc = new Document();
					PdfAccessor.AddVtoTraction(doc, vto, null, s);
					Save(doc, "TestSectionOptimzer_Vto_Traction_" + j + "+TYP" + pd.pic + "+Strat" + pd.strat + (pd.colors ? "_colors" : "") + ".pdf");
					j++;
				}
			}

		}


		[TestMethod]
		[TestCategory("PDF")]
		public async Task TestSlowVto() {
			var vto = new AngularVTO();
			vto.Values = new List<AngularCompanyValue>() {
				new AngularCompanyValue() { CompanyValue = "Ownership Thinking" },
				new AngularCompanyValue() { CompanyValue = "Passionate Problem Solver" },
				new AngularCompanyValue() { CompanyValue = "Client Success Is Our Success" },
			};

			vto.CoreFocus = new AngularCoreFocus() {
				Purpose = "Focus On Technology So Our Clients Can Focus On Business",
				Niche = "Microsoft, FileMaker and Networking Technology"
			};

			vto.TenYearTarget = @"•	December 31, 2023  
•	Revenue: $20M 
•	Net Margin: 25% 
•	90 Employees 
•	Expand Office Space in Main Office 
•	2nd Office Based on Client Geography  
•	80/20 Revenue split between Managed Services and Development";

			vto.Strategies = new List<AngularStrategy>() {
				new AngularStrategy() {
					Guarantee = @"Network Services: Evergreen contracts. Freedom to leave us every 90 days if not satisfied.  

App Dev: All work has a 90-day warranty against defects. If defect is reported, we fix it for free.",
					TargetMarket =@"Network Services
•	Avoid large capital expenditures
•	Can't hire skillset in one person
•	Lack capacity for support in-house
•	Unhappy with IT performance
•	Cloud expertise
•	Data center hosting and support
•	Expertise in the prospect's vertical
•	Compliance issues
•	Security concerns

Application Development
•	Inside IT
•	Product to resell
•	Want to franchise
•	Multi-location
•	Build efficiency
•	Can’t buy what they want off-shelf
•	Systematize a unique process
•	Bottomless pocket
•	Integration of off-shelf products
•	Inefficient paper processes
•	Vision

Based on interviews with us and competitive research, our new marketing agency, Creative MMS, put together a list of personas of the types of contacts we are targeting with our marketing efforts. They are also going to do further research of our existing “hero” clients through interviews to zero in on the ITS traits that they value most so we can incorporate this into our messaging and content.",
					ProvenProcess = @"We are working with CreativeMMS to update and maintain the following to attract new prospects through the following:

Website: Recommended UX/UI, site map changes to site for easier contact process and landing pages for proper recognition by prospects when they search for us

Google AdWords: Revising our campaigns, ad copy, budgets, and corresponding landing pages

Social: Daily output of messaging via LinkedIn, Twitter, Facebook that links back to our site and industry news.  

Email: Starting with a series of quick hit weekly messaging (as opposed to once/monthly) to reset the list of contacts and try to zero in on what contacts are interested in via our automation tool (eTrigue) so we can target messaging going forward and get prospects to respond when they’re ready to talk to us.  Also, provide clients with more practical information so they see us as thought leaders and educators to add value to the partnership.

SEO: Tight coordination with PPC, social campaigns, email, our site content to raise ranking of our profile. Will also recommend a backlink strategy with well-regarded sites to improve ranking.

In addition to the marketing efforts, we are doing more targeted networking through the ALA and other organizations, as well as planning in person seminars for clients, and doing more client one-on-ones outside of the office to boost the personal relationships and encourage referrals.",
					Uniques = new List<AngularVtoString>() {
						new AngularVtoString() {Data="Employee Owned" },
						new AngularVtoString() {Data="No Long Term Contracts" },
						new AngularVtoString() {Data="White Glove/Boutique Client Service" },
						new AngularVtoString() {Data="Employee-owned" },
						new AngularVtoString() {Data="White glove/boutique service" },
						new AngularVtoString() {Data="Comprehensive strategic planning with emphasis on security first" },
					}
				},
				new AngularStrategy()
			};

			vto.ThreeYearPicture = new AngularThreeYearPicture() {
				FutureDate = DateTime.UtcNow,
				Revenue = "$17M",
				Profit = "25% Net",
				Measurables = "$ Revenue per Employee",
				LooksLike = new List<AngularVtoString> {
					new AngularVtoString() {Data= "85 Employees" },
					new AngularVtoString() {Data= "Expanded main office" },
					new AngularVtoString() {Data= "Possible 2nd office based on geography" },
					new AngularVtoString() {Data= "80/20 revenue split between managed services and development" },
					new AngularVtoString() {Data= "A strong seasoned acquisition team" },
					new AngularVtoString() {Data= "Kaseya may have to be replaced" },
					new AngularVtoString() {Data= "IT Solutions Stock Price is $29.50" },
					new AngularVtoString() {Data= "Dedicated 3 person sales team" },
					new AngularVtoString() {Data= "2 new Technical Account Managers" },
					new AngularVtoString() {Data= "<4% yearly client attrition" },
					new AngularVtoString() {Data= "•Well-honed Marketing Machine" },
					new AngularVtoString() {Data= "Managed Services Team Pod Structures" },
					new AngularVtoString() {Data= "•Trained Application Support Specialists" },
				}
			};
			vto.IncludeVision = true;
			var doc = PdfAccessor.CreateDoc(null, "Quarterly Printout1");
			var settings = new VtoPdfSettings();
			var sw = new Stopwatch();
			sw.Start();
			await PdfAccessor.AddVTO(doc, vto, "yyyy-MM-dd", settings);
			var duration = sw.Elapsed;
			Console.WriteLine(duration.TotalMilliseconds + "ms");

			Save(doc, "TestSlowVto.pdf");

			Assert.IsTrue(duration < TimeSpan.FromSeconds(30));

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
