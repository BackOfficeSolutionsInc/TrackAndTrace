using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.DocumentObjectModel;
using RadialReview.Models.Angular.VTO;
using Table = MigraDoc.DocumentObjectModel.Tables.Table;
using VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment;
using RadialReview.Utilities;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Pdf;

namespace RadialReview.Accessors {


	public partial class PdfAccessor {
		public static bool DEBUGGER = false && Config.IsLocal();
		


		public static async Task AddVTO(Document doc, AngularVTO vto, string dateformat, VtoPdfSettings settings) {
            if (vto.IncludeVision) {
                //await AddVtoVision_Intermediate(doc, vto, dateformat, settings);
                await AddVtoVision(doc, vto, dateformat, settings);
            }
            await AddVtoTraction(doc, vto, dateformat, settings);            
		}

		private static async Task<Section> AddVtoPage(Document doc, string docName, string pageName, VtoPdfSettings settings) {
			Section section;

			section = doc.AddSection();
			await AddVtoPageToSection(section, docName, pageName, settings);

			return section;
		}

		private static async Task AddVtoPageToSection(Section section, string docName, string pageName, VtoPdfSettings settings) {
			section.PageSetup.Orientation = Orientation.Landscape;
			section.PageSetup.PageFormat = PageFormat.Letter;


			var paragraph = new Paragraph();
			var p = section.Footers.Primary.AddParagraph("© 2003 - " + DateTime.UtcNow.AddMonths(3).Year + " EOS. All Rights Reserved.");
			p.Format.LeftIndent = Unit.FromPoint(14);

			section.Footers.Primary.Format.Font.Size = 10;
			section.Footers.Primary.Format.Font.Name = "Arial Narrow";
			section.Footers.Primary.Format.Font.Size = 8;
			section.Footers.Primary.Format.Font.Color = settings.LightTextColor;

			section.PageSetup.LeftMargin = Unit.FromInch(.3);
			section.PageSetup.RightMargin = Unit.FromInch(.3);
			section.PageSetup.TopMargin = Unit.FromInch(.2);
			section.PageSetup.BottomMargin = Unit.FromInch(.5);

			var title = section.AddTable();
			title.AddColumn(Unit.FromInch(0.05));
			title.AddColumn(Unit.FromInch(2.22));
			title.AddColumn(Unit.FromInch(10.07 - 2.22));

			var titleRow = title.AddRow();
			try {
				//var imageFilename = HttpContext.Current.Server.MapPath("~/Content/img/EOS_Model.png");
				var image = await settings.GetImage();

				var img = titleRow.Cells[1].AddImage(image.Base64);
				//img.Height = Unit.FromInch(2.13);
				if (image.Width > image.Height) {
					img.Width = Unit.FromInch(1.95);
				} else {
					img.Height = Unit.FromInch(1.75);
				}

			} catch (Exception e) {
			}
			titleRow.Cells[1].VerticalAlignment = VerticalAlignment.Center;
			titleRow.Cells[1].Format.Alignment = ParagraphAlignment.Left;
			titleRow.Height = Unit.FromInch(1.787);

			var titleTable = titleRow.Cells[2].Elements.AddTable();
			titleTable.AddColumn(Unit.FromInch(10.07 - 2.22));
			var trow = titleTable.AddRow();
			trow.TopPadding = Unit.FromInch(.1);
			trow.BottomPadding = Unit.FromInch(.14);

			paragraph = trow.Cells[0].AddParagraph("THE VISION/TRACTION ORGANIZER™");
			paragraph.Format.Font.Size = 32;
			paragraph.Format.Alignment = ParagraphAlignment.Center;
			paragraph.Format.Font.Name = "Arial Narrow";

			trow = titleTable.AddRow();

			var frame = trow.Cells[0].AddTextFrame();
			frame.Height = Unit.FromInch(0.38);
			frame.Width = Unit.FromInch(5.63);

			frame.MarginRight = Unit.FromInch(1);
			frame.MarginLeft = Unit.FromInch(1.15);
			frame.MarginTop = Unit.FromInch(.05);


			var box = frame.AddTable();
			box.Borders.Color = settings.BorderColor;
			box.Borders.Width = Unit.FromPoint(.75);
			box.LeftPadding = Unit.FromInch(.1);

			var size = Unit.FromInch(5.63);
			var c = box.AddColumn(size);
			c.Format.Alignment = ParagraphAlignment.Left;
			var rr = box.AddRow();
			rr.Cells[0].AddParagraph(docName);
			rr.Format.Font.Size = 16;
			rr.Format.Font.Bold = true;
			rr.Format.Font.Name = "Arial Narrow";
			rr.HeightRule = RowHeightRule.Exactly;
			rr.VerticalAlignment = VerticalAlignment.Center;
			rr.Height = Unit.FromInch(0.38);

			frame = trow.Cells[0].AddTextFrame();
			frame.Height = Unit.FromInch(0.38);
			frame.Width = Unit.FromInch(5.63);

			frame.MarginTop = Unit.FromInch(.05);

			p = frame.AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Center;
			p.Format.LeftIndent = Unit.FromInch(2);
			p.Format.SpaceBefore = Unit.FromInch(.11);
			var ft = p.AddFormattedText(pageName, TextFormat.Bold | TextFormat.Underline);
			ft.Font.Size = 20;
			ft.Font.Name = "Arial Narrow";
		}

		private static Cell FormatParagraph(string title, ResizeContext ctx, VtoPdfSettings settings) {
			var container = ctx.Container;
			var vars = ctx.Variables;
			var table = new Table();
			container.Add(table);
			table.AddColumn(VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);
			table.AddColumn(VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH);
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding = 0;
			table.Borders.Bottom.Width = 1;
			table.Borders.Bottom.Color = settings.BorderColor;

			var row = table.AddRow();
			var titleCell = row.Cells[0];
			titleCell.Borders.Bottom.Color = settings.BorderColor;
			titleCell.Borders.Right.Color = settings.BorderColor;
			titleCell.Borders.Bottom.Width = 1;
			titleCell.Format.Font.Bold = true;
			titleCell.Format.Font.Size = 14;
			titleCell.Shading.Color = settings.FillColor;
			titleCell.Format.Font.Color = settings.FillTextColor;

			titleCell.AddParagraph(title);

			titleCell.VerticalAlignment = VerticalAlignment.Center;
			titleCell.Format.Alignment = ParagraphAlignment.Center;


			var contentsTable = new Table();

			//contentsTable.Rows.LeftIndent = 0;
			//contentsTable.LeftPadding = 0;
			//contentsTable.RightPadding = 0;

			contentsTable.AddColumn(VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH - VtoVisionDocumentGenerator.CELL_PADDING);
			var contentCell = contentsTable.AddRow().Cells[0];
			row.Cells[1].Elements.Add(contentsTable);
			AddPadding(vars, contentCell);

			return contentCell;
		}

		private static void AddPadding(ResizeContext ctx,bool left) {
			AddPadding(ctx.Variables, ctx.Container, false, false);
		}

		private static void AddPadding(RangedVariables vars, Cell contentCell, bool top = true, bool bottom = true) {
			contentCell.Format.Font.Size = vars.Get("FontSize");

			contentCell.Borders.Left.Width = VtoVisionDocumentGenerator.CELL_PADDING;
			contentCell.Borders.DistanceFromRight = VtoVisionDocumentGenerator.CELL_PADDING;
			contentCell.Borders.Right.Width = VtoVisionDocumentGenerator.CELL_PADDING;


			if (top)
				contentCell.Borders.Top.Width = vars.Get("Spacer");// "Spacer");
			if (bottom)
				contentCell.Borders.Bottom.Width = vars.Get("Spacer");
			contentCell.Borders.Color = Colors.Transparent;

			if (PdfAccessor.DEBUGGER) {
				contentCell.Borders.Color = Colors.Blue;
			}
		}

		private static void AppendCoreValues(Cell cell, AngularVTO vto) {
			//cell.AddParagraph("CoreValue");
			var values = vto.Values.Select(x => x.CompanyValue).ToList();
			foreach (var l in OrderedList(values, ListType.NumberList1)) {
				cell.Add(l);
			}
		}

		private static void AppendCoreFocus(Cell cell, AngularVTO vto) {
			//cell.AddParagraph("CoreFocus");
			var paragraphs = new List<Paragraph>();
			var purpose = new Paragraph();
			var text = (vto.NotNull(x => x.CoreFocus.PurposeTitle) ?? "Purpose/Cause/Passion").Trim().TrimEnd(':') + ": ";
			purpose.AddFormattedText(text, TextFormat.Bold);
			purpose.Format.Font.Name = "Arial Narrow";
			purpose.AddText(vto.NotNull(x => x.CoreFocus.Purpose) ?? "");
			paragraphs.Add(purpose);

			purpose.Format.SpaceAfter = 7 * 1.5;

			var niche = new Paragraph();
			niche.AddFormattedText("Our Niche: ", TextFormat.Bold);
			niche.AddText(vto.NotNull(x => x.CoreFocus.Niche) ?? "");
			niche.Format.Font.Name = "Arial Narrow";
			paragraphs.Add(niche);

			foreach (var p in paragraphs) {
				cell.Add(p);
			}
		}

		private static void AppendTenYear(Cell cell, AngularVTO vto) {
			//cell.AddParagraph("TenYear");
			var tenYear = new Paragraph();
			tenYear.Format.Font.Name = "Arial Narrow";
			tenYear.AddText(vto.NotNull(x => x.TenYearTarget) ?? "");
			cell.Add(tenYear);
		}
		
		private static IEnumerable<IHint> GenerateMarketingStrategyHints_New_NotWorking(string viewBoxName, string title, AngularVTO vto, VtoPdfSettings setting) {
			if (vto.NotNull(x => x.Strategies) != null) {
				var stratCount = vto.Strategies.Count();
				var spaceBefore = true;
				//var group = new MarketingStrategyHint.MSHintGroups(title);
				for (var i = 0; i < stratCount; i++) {
					var strat = vto.Strategies.ElementAt(i);
					//var isFirst = i == 0;
					//var isLast = i == stratCount - 1;
					var isOnly = stratCount == 1;
					var paddOnlyOnce = i == 0;


					
					var fs = 7;
					//Strategy title & Target market
					var targetMarketHints = new PageSplitter(strat.TargetMarket, 350).GenerateStaticHints(viewBoxName, ctx => {
						var c = ctx.Container;
						var v = ctx.Variables;

						AddPadding(v, c, true, false);

						var paragraphs = new List<Paragraph>();

						//Add strategy title
						if (!isOnly && !string.IsNullOrWhiteSpace(strat.Title)) {
							var stratP = ctx.AddParagraph();
							if (spaceBefore) {
								stratP.Format.SpaceBefore = fs * 1.5;
							}
							stratP.Format.SpaceAfter = fs * 1;
							stratP.AddFormattedText(strat.Title ?? "", TextFormat.Bold | TextFormat.Underline);
							stratP.Format.Font.Name = "Arial Narrow";
						}

						//Target Market List
						var theList = ctx.AddParagraph();
						theList.Format.SpaceAfter = 0;

						theList.Format.Font.Name = "Arial Narrow";
						theList.AddFormattedText("Target Market/\"The List\": \n", TextFormat.Bold);
					},(ctx,str)=> {
						AddPadding(ctx,true);
						var theList = ctx.AddParagraph();
						theList.Format.Font.Name = "Arial Narrow";
						theList.AddText(str ?? "");
					},widthOverride: VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH);
					foreach (var h in targetMarketHints)
						yield return h;


					//Three Uniques
					yield return new Hint(viewBoxName, new ResizableElement((ctx) => {
						var c = ctx.Container;
						var v = ctx.Variables;
						AddPadding(ctx, true);
						var paragraphs = new List<Paragraph>();
						var uniques = strat.NotNull(x => x.Uniques.ToList()) ?? new List<AngularVtoString>();
						if (uniques.Any(x => !string.IsNullOrWhiteSpace(x.Data))) {
							var uniquePara = new Paragraph();
							uniquePara.Format.SpaceBefore = fs * 1.25;
							var uniquesTitle = "Uniques: ";
							if (uniques.Count == 3)
								uniquesTitle = "Three " + uniquesTitle;
							uniquePara.AddFormattedText(uniquesTitle, TextFormat.Bold);
							uniquePara.Format.Font.Name = "Arial Narrow";
							paragraphs.Add(uniquePara);
							paragraphs.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44)));
						}
						paragraphs.ForEach(p => c.Add(p));
					}, widthOverride: VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH));


					//Proven Process
					var ppHints = new PageSplitter(strat.ProvenProcess, 350).GenerateStaticHints(viewBoxName, ctx => {
						AddPadding(ctx, true);
						var provenProcess = ctx.AddParagraph();
						if (!isOnly && string.IsNullOrEmpty(strat.Guarantee)) {
							provenProcess.Format.SpaceBefore = fs * 1.25;
						}
						provenProcess.Format.Font.Name = "Arial Narrow";
						provenProcess.AddFormattedText("Proven Process: ", TextFormat.Bold);
						provenProcess.Format.SpaceAfter = 0;
					}, (ctx, str) => {
						AddPadding(ctx, true);
						var p = ctx.AddParagraph();
						p.Format.LineSpacing = fs;
						ctx.Container.Contents.VerticalAlignment = VerticalAlignment.Top;
						p.Format.SpaceBefore = 0;
						p.Format.Font.Name = "Arial Narrow";
						p.AddText(str ?? "");
					}, widthOverride: VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH);
					foreach (var h in ppHints)
						yield return h;

					//Guarentees
					var guarenteeHints = new PageSplitter(strat.Guarantee, 350).GenerateStaticHints(viewBoxName, ctx => {
						AddPadding(ctx, true);
						var guarantee = ctx.AddParagraph();
						guarantee.Format.SpaceBefore = fs * 1.25;
						if (!isOnly) {
							guarantee.Format.SpaceAfter = fs * 1.25;
						}
						guarantee.AddFormattedText("Guarantee: ", TextFormat.Bold);
						guarantee.Format.Font.Name = "Arial Narrow";
						guarantee.Format.SpaceAfter = 0;
					}, (ctx,str) => {
						AddPadding(ctx, true);
						var guarantee = ctx.AddParagraph();
						guarantee.Format.Font.Name = "Arial Narrow";
						guarantee.AddText(str ?? "");
					}, widthOverride: VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH);

					foreach (var h in guarenteeHints)
						yield return h;

					
					//yield return new Hint(viewBoxName, new ResizableElement((ctx) => {
					//	var c = ctx.Container;
					//	var v = ctx.Variables;
					//	AddPadding(v, c, false, true);
					//}));

					////Guarantee
					//foreach (var g in guarenteeTextSplits) {
					//	yield return new Hint(viewBoxName, new StaticElement((ctx) => {
					//		var c = ctx.Container;
					//		var v = ctx.Variables;
					//		var paragraphs = new List<Paragraph>();
					//		if (!string.IsNullOrEmpty(g)) {
					//			var guarantee = new Paragraph();
					//			guarantee.Format.SpaceBefore = fs * 1.25;

					//			if (!isOnly) {
					//				guarantee.Format.SpaceAfter = fs * 1.25;
					//			}

					//			guarantee.AddFormattedText("Guarantee: ", TextFormat.Bold);
					//			guarantee.Format.Font.Name = "Arial Narrow";
					//			guarantee.AddText(g ?? "");
					//			paragraphs.Add(guarantee);
					//		}
					//		paragraphs.ForEach(p => c.Add(p));

					//	}));
					//}


				}
			}
		}
		
		private static void AppendMarketStrategy(ResizeContext ctx, Cell cell, AngularStrategy strat, VtoPdfSettings setting, bool isFirst, bool isLast, bool isOnly, ref bool addSpaceBefore) {
			var fs = ctx.Variables.Get("FontSize");

			var paragraphs = new List<Paragraph>();
			//Add spacer
			if (!isFirst) {
				var spacer = new Paragraph();
				spacer.Format.Borders.Top.Color = setting.LightBorderColor;
				//spacer.Format.SpaceBefore = fs * 1.5;
				//spacer.Format.SpaceAfter = fs * .75;
				paragraphs.Add(spacer);
			}

			//Add strategy title
			if (!isOnly && !string.IsNullOrWhiteSpace(strat.Title)) {
				var title = new Paragraph();
				if (!isFirst && addSpaceBefore) {
					title.Format.SpaceBefore = fs * 1.5;
				}
				title.Format.SpaceAfter = fs * 1;
				title.AddFormattedText(strat.Title ?? "", TextFormat.Bold | TextFormat.Underline);
				title.Format.Font.Name = "Arial Narrow";
				paragraphs.Add(title);
			}

			//Target Market List
			if (!string.IsNullOrWhiteSpace(strat.TargetMarket)) {
				var theList = new Paragraph();
				theList.Format.Font.Name = "Arial Narrow";
				theList.AddFormattedText("Target Market/\"The List\": ", TextFormat.Bold);
				theList.AddText(strat.TargetMarket ?? "");
				paragraphs.Add(theList);
			}

			//Three Uniques
			var uniques = strat.NotNull(x => x.Uniques.ToList()) ?? new List<AngularVtoString>();
			if (uniques.Any(x => !string.IsNullOrWhiteSpace(x.Data))) {
				var uniquePara = new Paragraph();
				uniquePara.Format.SpaceBefore = fs * 1.25;
				var uniquesTitle = "Uniques: ";
				if (uniques.Count == 3)
					uniquesTitle = "Three " + uniquesTitle;
				uniquePara.AddFormattedText(uniquesTitle, TextFormat.Bold);
				uniquePara.Format.Font.Name = "Arial Narrow";
				paragraphs.Add(uniquePara);
				paragraphs.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44),setFontSize:false));
			}

			//Proven Process
			if (!string.IsNullOrEmpty(strat.ProvenProcess)) {
				var provenProcess = new Paragraph();
				provenProcess.Format.SpaceBefore = fs * 1.25;

				if (!isOnly && string.IsNullOrEmpty(strat.Guarantee)) {
					provenProcess.Format.SpaceAfter = fs * 1.25;
				}
				provenProcess.AddFormattedText("Proven Process: ", TextFormat.Bold);
				provenProcess.Format.Font.Name = "Arial Narrow";
				provenProcess.AddText(strat.ProvenProcess ?? "");
				paragraphs.Add(provenProcess);
			}

			//Guarantee
			if (!string.IsNullOrEmpty(strat.Guarantee)) {
				var guarantee = new Paragraph();
				guarantee.Format.SpaceBefore = fs * 1.25;

				if (!isOnly) {
					guarantee.Format.SpaceAfter = fs * 1.25;
				}

				guarantee.AddFormattedText("Guarantee: ", TextFormat.Bold);
				guarantee.Format.Font.Name = "Arial Narrow";
				guarantee.AddText(strat.Guarantee ?? "");
				paragraphs.Add(guarantee);
			}

			addSpaceBefore = false;
			if (string.IsNullOrEmpty(strat.ProvenProcess) && string.IsNullOrEmpty(strat.ProvenProcess)) {
				addSpaceBefore = true;
			}

			foreach (var p in paragraphs) {
				cell.Add(p);
			}
		}

		private static IEnumerable<IHint> GenerateThreeYearHints(AngularVTO vto, string dateFormat) {
			//cell.AddParagraph("TenYear");

			var header = new ResizableElement((ctx) => {
				var c = ctx.Container;
				var v = ctx.Variables;
				c.Borders.Left.Width = VtoVisionDocumentGenerator.CELL_PADDING;
				c.Borders.Right.Width = VtoVisionDocumentGenerator.CELL_PADDING;
				c.Borders.Color = Colors.Transparent;

				var fs = v.Get("FontSize");
				var paragraphs = AddVtoSectionHeader(vto.ThreeYearPicture, fs, dateFormat);
				foreach (var a in paragraphs)
					c.Add(a);

				var p = new Paragraph();
				p.AddFormattedText("What does it look like?", TextFormat.Bold | TextFormat.Underline);
				p.Format.Font.Name = "Arial Narrow";
				p.Format.Font.Size = fs;
				c.Add(p);
			});

			yield return new Hint(VtoVisionDocumentGenerator.RIGHT_COLUMN, header);

			var looksLike = vto.ThreeYearPicture.LooksLike.Where(x => !string.IsNullOrWhiteSpace(x.Data)).Select(x => x.Data).ToList();
			var listing = OrderedList(looksLike, ListType.BulletList1);

			var looksLikeHints = listing.Select((ll, i) => {
				return new StaticElement(c => {
					c.Container.Add(ll.Clone());
				});
			}).Select(x => {
				return new Hint(VtoVisionDocumentGenerator.RIGHT_COLUMN, x);
			});

			foreach (var h in looksLikeHints)
				yield return h;
		}

		private static void AppendRowTitle(Row row, string title, VtoPdfSettings settings) {
			var cvTitle = row.Cells[0];
			row.Borders.Bottom.Color = settings.BorderColor;
			row.Borders.Right.Color = settings.BorderColor;
			cvTitle.Shading.Color = settings.FillColor;
			cvTitle.Format.Font.Bold = true;
			cvTitle.Format.Font.Size = 14;
			cvTitle.Format.Font.Name = "Arial Narrow";
			cvTitle.AddParagraph(title ?? "");
			cvTitle.Format.Alignment = ParagraphAlignment.Center;
			row.VerticalAlignment = VerticalAlignment.Center;
		}

		public static async Task AddVtoVision(Document doc, AngularVTO vto, string dateFormat, VtoPdfSettings settings) {
            var timeout = new TimeoutCheck(TimeSpan.FromSeconds(settings.MaxSeconds ?? 20));
			settings = settings ?? new VtoPdfSettings();
			var visionLayoutGenerator = new VtoVisionDocumentGenerator(vto, settings);

			var vars = new RangedVariables();
			vars.Add("FontSize", 10, 6, 10);
			vars.Add("Spacer", Unit.FromInch(.25), Unit.FromInch(.05), Unit.FromInch(8));
			//vars.Add("MS_Spacer", Unit.FromInch(.25), Unit.FromInch(.05), Unit.FromInch(8));

			var coreValueTitle = vto.NotNull(x => x.CoreValueTitle) ?? "CORE VALUES";
			var coreFocusTitle = vto.NotNull(x => x.CoreFocus.CoreFocusTitle) ?? "CORE FOCUS™";
			var tenYearTitle = vto.NotNull(x => x.TenYearTargetTitle) ?? "10-YEAR TARGET™";
			var marketingStrategyTitle = vto.NotNull(x => x.Strategy.MarketingStrategyTitle) ?? "MARKETING STRATEGY";

			var coreValuesPanel = new ResizableElement((c) => { AppendCoreValues(FormatParagraph(coreValueTitle, c, settings), vto); });
			var coreFocusPanel = new ResizableElement((c) => { AppendCoreFocus(FormatParagraph(coreFocusTitle, c, settings), vto); });
			var tenYearPanel = new ResizableElement((c) => { AppendTenYear(FormatParagraph(tenYearTitle, c, settings), vto); });
			var marketingStrategyPanel = GenerateMarketingStrategyHints_Old(VtoVisionDocumentGenerator.LEFT_COLUMN, marketingStrategyTitle, vto, settings);

			var hints = new List<IHint>();
			hints.Add(new Hint(VtoVisionDocumentGenerator.LEFT_COLUMN, coreValuesPanel));
			hints.Add(new Hint(VtoVisionDocumentGenerator.LEFT_COLUMN, coreFocusPanel));
			hints.Add(new Hint(VtoVisionDocumentGenerator.LEFT_COLUMN, tenYearPanel));
			hints.AddRange(marketingStrategyPanel);

			hints.AddRange(GenerateThreeYearHints(vto, dateFormat));
			var result = LayoutOptimizer.Optimize(visionLayoutGenerator, hints, vars, timeout);

			LayoutOptimizer.Draw(doc, result);
		}

		#region ignore

		public static async Task AddVtoTraction(Document doc, AngularVTO vto, string dateformat, VtoPdfSettings settings) {
			settings = settings ?? new VtoPdfSettings();
			Unit baseHeight = Unit.FromInch(5.0);//5.15
			var vt = await AddPage_VtoTraction(doc, vto, settings, baseHeight);
			Cell oneYear = vt.oneYear, quarterlyRocks = vt.quarterlyRocks, issuesList = vt.issuesList;
			Table issueTable = vt.issueTable, rockTable = vt.rockTable, goalTable = vt.goalTable;


			#region One Year Plan
			Unit fs = 10;
			var goalObjects = new List<DocumentObject>();
			var goalsSplits = new List<Page>();
			var goalRows = new List<Row>();
			var goalParagraphs = new List<Paragraph>();
			{
				var oneYearPlan = vto.OneYearPlan ?? new AngularOneYearPlan();
				oneYearPlan.GoalsForYear = oneYearPlan.GoalsForYear ?? new List<AngularVtoString>();

				var goals = oneYearPlan.GoalsForYear.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();

				//ResizeToFit(oneYear, Unit.FromInch(3.47), Unit.FromInch(5.15), (cell, fs) => {
				goalObjects.AddRange(AddVtoSectionHeader(oneYearPlan, fs, dateformat));
				var gfy = new Paragraph();

				gfy.Format.Font.Size = fs;
				gfy.Format.Font.Name = "Arial Narrow";
				gfy.AddFormattedText("Goals for the Year:", TextFormat.Bold);
				goalObjects.Add(gfy);


				//var pt = new Paragraph();
				//pt.Elements.Add(t);

				//var goalParagraphs = goals.Select(x => {
				//	var c = new Cell();
				//	var p = c.AddParagraph(x);
				//	p.Format.SpaceBefore = Unit.FromPoint(2);
				//	p.Format.Font.Size = fs;
				//	p.Format.Font.Name = "Arial Narrow";
				//	return p;
				//});

				var minRowHeight = Unit.FromInch(0.2444 * fs.Point / 10);

				for (var i = 0; i < goals.Count; i++) {
					var r = new Row();
					//rockTable.AddRow();
					r.Height = minRowHeight;
					r.HeightRule = RowHeightRule.AtLeast;
					var p = r.Cells[0].AddParagraph("" + (i + 1) + ".");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					p.Format.Alignment = ParagraphAlignment.Right;
					p = r.Cells[1].AddParagraph(goals[i] ?? "");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					goalRows.Add(r);
					goalParagraphs.Add(p);
				}

				var headerSize = GetSize(goalObjects, Unit.FromInch(3.47));
				Unit pg1Height = baseHeight - headerSize.Height;//+ Unit.FromInch(0.51);
				goalsSplits = SplitHeights(Unit.FromInch(3), new[] { pg1Height, (baseHeight) }, goalParagraphs, elementAtLeast: minRowHeight);


				goalObjects.Add(goalTable);
			}
			#endregion
			#region Rocks
			var rockObjects = new List<DocumentObject>();
			var rockSplits = new List<Page>();
			var rockRows = new List<Row>();
			var rockParagraphs = new List<Paragraph>();
			{
				var vtoQuarterlyRocks = vto.QuarterlyRocks ?? new AngularQuarterlyRocks();
				vtoQuarterlyRocks.Rocks = vtoQuarterlyRocks.Rocks ?? new List<AngularVtoRock>();

				var rocks = vtoQuarterlyRocks.Rocks.Where(x => !String.IsNullOrWhiteSpace(x.Rock.Name)).ToList();
				quarterlyRocks.Format.LeftIndent = Unit.FromInch(.095);
				//ResizeToFit(quarterlyRocks, Unit.FromInch(3.47), Unit.FromInch(5.15), (cell, fs) => {
				rockObjects.AddRange(AddVtoSectionHeader(vtoQuarterlyRocks, fs, dateformat));
				var gfy = new Paragraph();

				gfy.Format.Font.Size = fs;
				gfy.Format.Font.Name = "Arial Narrow";
				gfy.AddFormattedText("Rocks for the Quarter:", TextFormat.Bold);
				rockObjects.Add(gfy);


				//var rockParagraphs = rocks.Select(x => {
				//	var c = new Cell();
				//	var p = c.AddParagraph(x.Rock.Name);
				//	p.Format.SpaceBefore = Unit.FromPoint(2);
				//	p.Format.Font.Size = fs;
				//	p.Format.Font.Name = "Arial Narrow";
				//	return p;
				//});


				var minRowHeight = Unit.FromInch(0.2444 * fs.Point / 10);

				for (var i = 0; i < rocks.Count; i++) {
					var r = new Row();
					r.Height = minRowHeight;
					r.HeightRule = RowHeightRule.AtLeast;
					var p = r.Cells[0].AddParagraph("" + (i + 1) + ".");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					p.Format.Alignment = ParagraphAlignment.Right;
					p = r.Cells[1].AddParagraph(rocks[i].Rock.Name ?? "");
					rockParagraphs.Add(p);
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					p = r.Cells[2].AddParagraph(rocks[i].Rock.Owner.NotNull(X => X.Initials) ?? "");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Alignment = ParagraphAlignment.Center;
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					rockRows.Add(r);

				}
				//var headerSize = GetSize(gfy, Unit.FromInch(3.47));

				var headerSize = GetSize(rockObjects, Unit.FromInch(3.47));
				//Unit pg1Height = Unit.FromInch(baseHeight - Unit.FromPoint(headerSize.Height*.166).Inch);
				Unit pg1Height = baseHeight - headerSize.Height;
				rockSplits = SplitHeights(Unit.FromInch(2.6), new[] { pg1Height, (baseHeight) }, rockParagraphs, elementAtLeast: minRowHeight);
				rockObjects.Add(rockTable);

			}
			#endregion
			#region Issues
			var issuesObjects = new List<DocumentObject>();
			var issueSplits = new List<Page>();
			var issueRows = new List<Row>();
			var issueParagraph = new List<Paragraph>();
			{
				var vtoIssues = vto.Issues ?? new List<AngularVtoString>();

				var issues = vtoIssues.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();

				//issuesList.Elements.AddParagraph(" ").SpaceBefore = Unit.FromInch(0.095);
				//ResizeToFit(issuesList, Unit.FromInch(3.47), Unit.FromInch(5.15), (cell, fs) => {

				if (issues.Any()) {
					//var issueParagraphs = issues.Select(x => {
					//	var c = new Cell();
					//	var p = c.AddParagraph(x);
					//	p.Format.SpaceBefore = Unit.FromPoint(2);
					//	p.Format.Font.Size = fs;
					//	p.Format.Font.Name = "Arial Narrow";
					//	return p;
					//});


					var rspace = issueTable.AddRow();
					rspace.Height = Unit.FromInch(0.095);
					rspace.HeightRule = RowHeightRule.Exactly;
					rspace.Borders.Left.Visible = false;
					rspace.Borders.Right.Visible = false;
					rspace.Borders.Top.Visible = false;

					var minRowHeight = Unit.FromInch(0.2444 * fs.Point / 10);
					for (var i = 0; i < issues.Count; i++) {
						var r = new Row();
						r.Height = minRowHeight;
						r.HeightRule = RowHeightRule.AtLeast;
						var p = r.Cells[0].AddParagraph("" + (i + 1) + ".");
						//p.Format.SpaceBefore = Unit.FromPoint(2);
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						p.Format.Alignment = ParagraphAlignment.Right;
						p = r.Cells[1].AddParagraph(issues[i] ?? "");
						issueParagraph.Add(p);
						//p.Format.SpaceBefore = Unit.FromPoint(2);
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						issueRows.Add(r);
					}

					//var rowHeights = GetRowHeights(issueRows, Unit.FromInch(3));
					var extraHeight = 0.51;
					//Unit issueHeight1 = baseHeight - Unit.FromInch(0.095 * 3);
					Unit issueHeight = baseHeight - Unit.FromInch(0.095 * 2);

					issueSplits = SplitHeights(Unit.FromInch(3.0), new[] { (issueHeight), (issueHeight) }, issueParagraph, null /*x => x.Cells[1]*/, extraHeight, elementAtLeast: minRowHeight);
					issuesObjects.Add(issueTable);
				}

			}
			#endregion

			AppendAll(oneYear, goalObjects);
			AppendAll(quarterlyRocks, rockObjects);
			AppendAll(issuesList, issuesObjects);

			var maxPage = Math.Max(Math.Max(issueSplits.Count(), goalsSplits.Count()), rockSplits.Count());

			var curGoalI = 0;
			var curRockI = 0;
			var curIssueI = 0;

			for (var p = 0; p < maxPage; p++) {

				if (p < goalsSplits.Count()) {
					foreach (var r in goalsSplits[p]) {
						goalTable.Rows.Add(goalRows[curGoalI]);
						curGoalI++;
					}
				}

				if (p < rockSplits.Count()) {
					foreach (var r in rockSplits[p]) {
						rockTable.Rows.Add(rockRows[curRockI]);
						curRockI++;
					}
				}

				if (p < issueSplits.Count()) {
					foreach (var r in issueSplits[p]) {
						issueTable.Rows.Add(issueRows[curIssueI]);
						curIssueI++;
					}
				}

				if (p + 1 < maxPage) {
					var vt2 = await AddPage_VtoTraction(doc, vto, settings, baseHeight);
					oneYear = vt2.oneYear;
					quarterlyRocks = vt2.quarterlyRocks;
					issuesList = vt2.issuesList;
					issueTable = vt2.issueTable;
					rockTable = vt2.rockTable;
					goalTable = vt2.goalTable;
					AppendAll(oneYear, new DocumentObject[] { goalTable }.ToList());
					AppendAll(quarterlyRocks, new DocumentObject[] { rockTable }.ToList());
					AppendAll(issuesList, new DocumentObject[] { issueTable }.ToList());

				}
			}
		}

		public class VtoTractionFrame {
			public Cell oneYear { get; set; }
			public Cell quarterlyRocks { get; set; }
			public Cell issuesList { get; set; }
			public Table issueTable { get; set; }
			public Table rockTable { get; set; }
			public Table goalTable { get; set; }
		}

		private static async Task<VtoTractionFrame> AddPage_VtoTraction(Document doc, AngularVTO vto, VtoPdfSettings settings, Unit height) {
			var o = new VtoTractionFrame();

			var section = await AddVtoPage(doc, vto._TractionPageName ?? vto.Name ?? "", "TRACTION", settings);


			var oneYearPlan = vto.OneYearPlan ?? new AngularOneYearPlan();
			var quarterlyRocks = vto.QuarterlyRocks ?? new AngularQuarterlyRocks();

			var table = section.AddTable();
			table.AddColumn(Unit.FromInch(3.47));
			table.AddColumn(Unit.FromInch(3.47));
			table.AddColumn(Unit.FromInch(3.47));
			table.Borders.Color = settings.BorderColor;

			var tractionHeader = table.AddRow();
			tractionHeader.KeepWith = 1;
			tractionHeader.Shading.Color = settings.FillColor;
			//tractionHeader.Borders.Bottom.Color = settings.BorderColor;
			//tractionHeader.Borders.Right.Color = settings.BorderColor;
			tractionHeader.Height = Unit.FromInch(0.55);
			var paragraph = tractionHeader.Cells[0].AddParagraph(oneYearPlan.OneYearPlanTitle ?? "1-YEAR PLAN");
			paragraph.Format.Font.Name = "Arial Narrow";
			paragraph.Format.Font.Size = 14;
			paragraph.Format.Font.Bold = true;
			paragraph.Format.Alignment = ParagraphAlignment.Center;

			tractionHeader.Cells[0].VerticalAlignment = VerticalAlignment.Center;
			//tractionHeader.Cells[0].Format.Shading.Color = TableGray;


			paragraph = tractionHeader.Cells[1].AddParagraph(quarterlyRocks.RocksTitle ?? "ROCKS");
			paragraph.Format.Font.Name = "Arial Narrow";
			paragraph.Format.Font.Size = 14;
			paragraph.Format.Font.Bold = true;
			paragraph.Format.Alignment = ParagraphAlignment.Center;
			tractionHeader.Cells[1].VerticalAlignment = VerticalAlignment.Center;
			//tractionHeader.Cells[1].Format.Shading.Color = TableGray;

			paragraph = tractionHeader.Cells[2].AddParagraph(vto.IssuesListTitle ?? "ISSUES LIST");
			paragraph.Format.Font.Name = "Arial Narrow";
			paragraph.Format.Font.Size = 14;
			paragraph.Format.Font.Bold = true;
			paragraph.Format.Alignment = ParagraphAlignment.Center;
			tractionHeader.Cells[2].VerticalAlignment = VerticalAlignment.Center;
			//tractionHeader.Cells[2].Format.Shading.Color = TableGray;

			var tractionData = table.AddRow();

			tractionData.Height = height;

			o.oneYear = tractionData.Cells[0];
			o.quarterlyRocks = tractionData.Cells[1];
			o.issuesList = tractionData.Cells[2];
			o.issueTable = new Table();
			o.issueTable.Borders.Color = settings.LightBorderColor;
			o.issueTable.AddColumn(Unit.FromInch(.28));
			o.issueTable.AddColumn(Unit.FromInch(3));

			o.rockTable = new Table();
			o.rockTable.Borders.Color = settings.LightBorderColor;
			o.rockTable.AddColumn(Unit.FromInch(.28));
			o.rockTable.AddColumn(Unit.FromInch(2.6));
			o.rockTable.AddColumn(Unit.FromInch(.4));

			o.goalTable = new Table();
			o.goalTable.Borders.Color = settings.LightBorderColor;
			o.goalTable.AddColumn(Unit.FromInch(.28));
			o.goalTable.AddColumn(Unit.FromInch(3));

			return o;
		}


		private static List<Paragraph> OrderedList(IEnumerable<string> items, ListType type, Unit? leftIndent = null,bool setFontSize=true) {
			var o = new List<Paragraph>();
			var res = items.Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
			for (int idx = 0; idx < res.Count(); ++idx) {
				ListInfo listinfo = new ListInfo();
				listinfo.ContinuePreviousList = idx > 0;
				listinfo.ListType = type;
				var paragraph = new Paragraph();
				paragraph.AddText((res[idx] ?? "").Trim());
				paragraph.Format.Font.Name = "Arial Narrow";
				if (setFontSize) {
					paragraph.Format.Font.Size = 10;
				}
				paragraph.Style = "" + type;
				paragraph.Format.ListInfo = listinfo;

				paragraph.Format.SpaceAfter = 0;
				paragraph.Format.SpaceBefore = 0;

				leftIndent = leftIndent ?? Unit.FromInch(0.05);

				if (leftIndent != null) {
					var tabStopDist = Unit.FromInch(.15);
					paragraph.Format.TabStops.ClearAll();
					paragraph.Format.TabStops.AddTabStop(Unit.FromInch(leftIndent.Value.Inch + tabStopDist));
					paragraph.Format.FirstLineIndent = -1 * tabStopDist;
					paragraph.Format.LeftIndent = leftIndent.Value + tabStopDist;
				}
				o.Add(paragraph);
			}
			return o;
		}

		#endregion
		#region Old
		/*[Obsolete("Only for private or test use", false)]
		public static async Task AddVtoVision_Intermediate(Document doc, AngularVTO vto, string dateformat, VtoPdfSettings settings) {
			//var TableGray = new Color(100, 100, 100, 100);
			//var TableBlack = new Color(0, 0, 0);

			var section = await AddVtoPage(doc, vto.Name ?? "", "VISION", settings);
			var vision = section.AddTable();
			vision.Style = "Table";
			vision.Borders.Color = settings.BorderColor;
			vision.Borders.Width = 1;
			vision.Rows.LeftIndent = 0;
			vision.LeftPadding = 0;
			vision.RightPadding = 0;

			var leftTitleWidth = Unit.FromInch(1.66);
			var leftContentWidth = Unit.FromInch(5.33);

			var leftWidth = leftTitleWidth + leftContentWidth;
			var rightWidth = Unit.FromInch(3.4);


			var showCoreValue = true;
			var showCoreFocus = true;
			var showTenYearPanel = true;
			var showMarketingStrategy = true;


			var elements = new List<ResizableElement>();
			ResizableElement coreValuesPanel = null, coreFocusPanel = null, tenYearPanel = null, marketingStrategyPanel = null;

			var coreValueTitle = vto.NotNull(x => x.CoreValueTitle) ?? "CORE VALUES";
			var coreFocusTitle = vto.NotNull(x => x.CoreFocus.CoreFocusTitle) ?? "CORE FOCUS™";
			var tenYearTitle = vto.NotNull(x => x.TenYearTargetTitle) ?? "10-YEAR TARGET™";
			var marketingStrategyTitle = vto.NotNull(x => x.Strategy.MarketingStrategyTitle) ?? "MARKETING STRATEGY";

			coreValuesPanel = new ResizableElement((ctx) => { AppendCoreValues(FormatParagraph(coreValueTitle, ctx, settings), vto); });
			coreFocusPanel = new ResizableElement((c) => { AppendCoreFocus(FormatParagraph(coreFocusTitle, c, settings), vto); });
			tenYearPanel = new ResizableElement((c) => { AppendTenYear(FormatParagraph(tenYearTitle, c, settings), vto); });
			marketingStrategyPanel = new ResizableElement((c) => { AppendMarketStrategies(FormatParagraph(marketingStrategyTitle, c, settings), vto, c, settings); });
			//var marketingStrategyElements = new List<ResizableElement>();

			var stratCount = vto.NotNull(x => x.Strategies.Count());

			var vars = new RangedVariables();
			vars.Add("FontSize", 10, 6, 10);
			vars.Add("Spacer", Unit.FromInch(.25), Unit.FromInch(.05), Unit.FromInch(8));
			//vars.Add("MS_Spacer", Unit.FromInch(.25), Unit.FromInch(.05), Unit.FromInch(8));

			if (showCoreValue)
				elements.Add(coreValuesPanel);
			if (showCoreFocus)
				elements.Add(coreFocusPanel);
			if (showTenYearPanel)
				elements.Add(tenYearPanel);
			if (showMarketingStrategy)
				elements.Add(marketingStrategyPanel);


			var optimized = ViewBoxOptimzer.Optimize(leftContentWidth, Unit.FromInch(5.5), elements, vars);

			var f = optimized.FitResults.Fits;

			vision.AddColumn(leftWidth);
			vision.AddColumn(rightWidth);

			var mainRow = vision.AddRow();

			var left = mainRow.Cells[0].Elements.AddTable();
			left.AddColumn(leftTitleWidth);
			left.AddColumn(leftContentWidth);
			var right = mainRow.Cells[1];

			if (showCoreValue) {
				coreValuesPanel.AddToDocument(t => {
					var r = left.AddRow();
					AppendRowTitle(r, vto.NotNull(x => x.CoreValueTitle) ?? "CORE VALUES", settings);
					r.Cells[1].Elements.Add(t);
				}, leftContentWidth, optimized.Variables);
			}

			if (showCoreValue) {
				coreFocusPanel.AddToDocument(t => {
					var r = left.AddRow();
					AppendRowTitle(r, vto.NotNull(x => x.CoreFocus.CoreFocusTitle) ?? "CORE FOCUS™", settings);
					r.Cells[1].Elements.Add(t);
				}, leftContentWidth, optimized.Variables);
			}

			if (showTenYearPanel) {
				tenYearPanel.AddToDocument(t => {
					var r = left.AddRow();
					AppendRowTitle(r, vto.NotNull(x => x.TenYearTargetTitle) ?? "10-YEAR TARGET™", settings);
					r.Cells[1].Elements.Add(t);
				}, leftContentWidth, optimized.Variables);
			}

			if (showMarketingStrategy) {
				marketingStrategyPanel.AddToDocument(t => {
					var r = left.AddRow();
					AppendRowTitle(r, vto.NotNull(x => x.Strategy.MarketingStrategyTitle) ?? "MARKETING STRATEGY", settings);
					var c = r.Cells[1];
					c.Borders.Bottom.Width = 0;
					c.Elements.Add(t);

				}, leftContentWidth, optimized.Variables);
			}
			#region hide
			/*
			AddPage_VtoVision(doc, vto, baseHeight, out coreValuesPanel, out coreFocusPanel, out tenYearPanel, out marketingStrategyPanel, out threeYearPanel);

			var values = vto.Values.ToList();
			ResizeToFit(coreValuesPanel, Unit.FromInch(5.33), Unit.FromInch(1.2), (cell, fs) => {
				var o = new List<Paragraph>();
				return OrderedList(values.Select(x => x.CompanyValue), ListType.NumberList1);
			}, maxFontSize: Unit.FromPoint(10), isCoreValues: true);


			ResizeToFit(coreFocusPanel, Unit.FromInch(5.33), Unit.FromInch(1.2), (cell, fs) => {
				var o = new List<Paragraph>();
				var p1 = new Paragraph();
				var txt = p1.AddFormattedText((vto.CoreFocus.PurposeTitle ?? "Purpose/Cause/Passion").Trim().TrimEnd(':') + ": ", TextFormat.Bold);
				p1.Format.Font.Name = "Arial Narrow";
				p1.AddText(vto.CoreFocus.Purpose ?? "");
				o.Add(p1);
				p1.Format.SpaceAfter = fs * 1.5;
				var p2 = new Paragraph();
				p2.AddFormattedText("Our Niche: ", TextFormat.Bold);
				p2.AddText(vto.CoreFocus.Niche ?? "");
				p2.Format.Font.Name = "Arial Narrow";
				o.Add(p2);
				return o;
			}, maxFontSize: 10);


			ResizeToFit(tenYearPanel, Unit.FromInch(5.33), Unit.FromInch(.6), (cell, fs1) => {
				var o = new List<Paragraph>();
				var p11 = new Paragraph();
				p11.Format.Font.Name = "Arial Narrow";
				p11.AddText(vto.TenYearTarget ?? "");
				o.Add(p11);
				return o;
			}, maxFontSize: 10);

			var marketingParagraphs = new List<Paragraph>();
			bool addBeforeSpace = false;
			//////
			{
				var count = -1;
				var strats = vto.Strategies.ToList();
				var includeTitle = strats.Count > 1;

				foreach (var item in strats.ToList()) {
					count += 1;
					var fs = 10;


					if (count > 0) {
						var spacer = new Paragraph();
						spacer.Format.Borders.Top.Color = TableGray;
						//spacer.Format.Borders.Bottom.Color = Colors.Red;
						spacer.Format.SpaceBefore = fs * 1.5;
						spacer.Format.SpaceAfter = fs * .75;
						marketingParagraphs.Add(spacer);
					}

					if (includeTitle && !string.IsNullOrWhiteSpace(item.Title)) {
						var p0 = new Paragraph();
						p0.Format.Font.Size = fs;
						if (count > 0) {
							if (addBeforeSpace) {
								p0.Format.SpaceBefore = fs * 1.5;
							}
						}
						p0.Format.SpaceAfter = fs * 1;
						var txt0 = p0.AddFormattedText(item.Title ?? "", TextFormat.Bold | TextFormat.Underline);
						p0.Format.Font.Name = "Arial Narrow";
						//p0.AddText(item.Title ?? "");
						marketingParagraphs.Add(p0);
					}

					var p1 = new Paragraph();
					p1.Format.Font.Size = fs;
					//p1.Format.SpaceBefore = fs * 1.5;
					var txt = p1.AddFormattedText("Target Market/\"The List\": ", TextFormat.Bold);
					p1.Format.Font.Name = "Arial Narrow";
					p1.AddText(item.TargetMarket ?? "");
					marketingParagraphs.Add(p1);

					var p2 = new Paragraph();
					p2.Format.Font.Size = fs;
					var uniques = item.Uniques.ToList();
					p2.Format.SpaceBefore = fs * 1.25;
					var uniquesTitle = "Uniques: ";
					if (uniques.Count == 3)
						uniquesTitle = "Three " + uniquesTitle;
					p2.AddFormattedText(uniquesTitle, TextFormat.Bold);
					p2.Format.Font.Name = "Arial Narrow";
					marketingParagraphs.Add(p2);
					marketingParagraphs.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44)));

					if (!string.IsNullOrEmpty(item.ProvenProcess)) {
						var p3 = new Paragraph();
						p3.Format.Font.Size = fs;
						p3.Format.SpaceBefore = fs * 1.25;

						if (strats.Count > 1 && string.IsNullOrEmpty(item.Guarantee)) {
							p3.Format.SpaceAfter = fs * 1.25;
						}

						p3.AddFormattedText("Proven Process: ", TextFormat.Bold);
						p3.Format.Font.Name = "Arial Narrow";
						p3.AddText(item.ProvenProcess ?? "");
						marketingParagraphs.Add(p3);
					}

					if (!string.IsNullOrEmpty(item.Guarantee)) {
						var p4 = new Paragraph();
						p4.Format.Font.Size = fs;
						p4.Format.SpaceBefore = fs * 1.25;

						if (strats.Count > 1) {
							p4.Format.SpaceAfter = fs * 1.25;
						}

						p4.AddFormattedText("Guarantee: ", TextFormat.Bold);
						p4.Format.Font.Name = "Arial Narrow";
						p4.AddText(item.Guarantee ?? "");
						marketingParagraphs.Add(p4);
					}

					addBeforeSpace = false;
					if (string.IsNullOrEmpty(item.ProvenProcess) && string.IsNullOrEmpty(item.ProvenProcess)) {
						addBeforeSpace = true;
					}
				}
			}

			var marketingPages = SplitHeights(Unit.FromInch(5.33), new[] { Unit.FromInch(2.7), Unit.FromInch(5.7) }, marketingParagraphs);

			var j = 0;
			for (var i = 0; i < marketingPages.Count; i++) {
				Page m = marketingPages[i];
				var f = m.FirstOrDefault();
				if (f != null && f.Item is Paragraph) {
					var p = (Paragraph)f.Item;
					if (p.Format.Borders.Top.Color == TableGray) {
						m.Items = m.Items.Skip(1).ToList();
						//marketingParagraphs.RemoveAt(j);
						marketingParagraphs.Remove(p);
					}
				}
				f = m.LastOrDefault();
				if (f != null && f.Item is Paragraph) {
					var p = (Paragraph)f.Item;
					if (p.Format.Borders.Top.Color == TableGray) {
						m.Items = m.Items.Take(m.Items.Count - 1).ToList();
						marketingParagraphs.Remove(p);
					}
				}
			}



			var looksList = vto.ThreeYearPicture.LooksLike.Where(x => !string.IsNullOrWhiteSpace(x.Data)).Select(x => x.Data).ToList();

			var threeYearParagraphs = new List<Paragraph>();
			{
				var fs = 10;
				threeYearParagraphs.AddRange(AddVtoSectionHeader(vto.ThreeYearPicture, fs, dateformat));
				var p = new Paragraph();
				p.AddFormattedText("What does it look like?", TextFormat.Bold | TextFormat.Underline);
				p.Format.Font.Name = "Arial Narrow";
				p.Format.Font.Size = fs;
				threeYearParagraphs.Add(p);
				threeYearParagraphs.AddRange(OrderedList(looksList, ListType.BulletList1));
			}

			var threeYearPages = SplitHeights(Unit.FromInch(3.4), new[] { Unit.FromInch(5.15), Unit.FromInch(5.7) }, threeYearParagraphs, null, null, stillTooBig: x => {
				var i = (Paragraph)x.Unmodified.Item;
				var baseSize = i.Format.Font.Size;
				var o = new Paragraph();
				var size = ResizeToFit(o, x.MaximumWidth, x.MaximumHeight, (d, s) => {
					i.Format.Font.Size = s;
					return i.AsList();
				}, 5, baseSize);

				o.Format.Font.Size = size;

				return new ItemHeight() {
					Item = o,
					Height = GetSize(o, x.MaximumWidth).Height
				};
			});

			var curMSI = 0;
			var curTYPI = 0;
			//Page 1
			if (marketingPages.Any()) {
				foreach (var mPara in marketingPages[0]) {
					marketingStrategyPanel.Add(marketingParagraphs[curMSI].Clone());
					curMSI++;
				}
			}
			if (threeYearPages.Any()) {
				foreach (var tyPara in threeYearPages[0]) {
					threeYearPanel.Add(threeYearParagraphs[curTYPI].Clone());
					curTYPI++;
				}
			}


			var maxPage = Math.Max(marketingPages.Count(), threeYearPages.Count());
			for (var p = 1; p < maxPage; p++) {
				if (p < maxPage) {
					var showMarketing = p < marketingPages.Count();
					var showThreeYear = p < threeYearPages.Count();

					AddPage_VtoVision(doc, vto, baseHeight, out coreValuesPanel, out coreFocusPanel, out tenYearPanel, out marketingStrategyPanel, out threeYearPanel, false, false, false, showMarketing, showThreeYear);
				}
				if (p < marketingPages.Count()) {
					foreach (var r in marketingPages[p]) {
						marketingStrategyPanel.Add(marketingParagraphs[curMSI].Clone());
						curMSI++;
					}
				}

				if (p < threeYearPages.Count()) {
					foreach (var r in threeYearPages[p]) {
						threeYearPanel.Add(threeYearParagraphs[curTYPI].Clone());
						curTYPI++;
					}
				}

			}*
			#endregion
		}*/

		//private static async Task AddPage_VtoVision(Document doc, AngularVTO vto,VtoPdfSettings settings, Unit height,
		//	out Cell coreValuePanel, out Cell coreFocusPanel, out Cell tenYearPanel, out Cell marketingStrategyPanel, out Cell threeYearPanel,
		//	bool showCoreValue = true, bool showCoreFocus = true, bool showTenYear = true, bool showMarketingStrategy = true, bool showThreeYear = true) {
		//	var section = await AddVtoPage(doc, vto.Name ?? "", "VISION", settings);
		//	var vision = section.AddTable();
		//	vision.Style = "Table";
		//	vision.Borders.Color = settings.BorderColor;
		//	vision.Borders.Width = 1;
		//	vision.Rows.LeftIndent = 0;
		//	vision.LeftPadding = 0;
		//	vision.RightPadding = 0;

		//	var anyCellsOnLeft = showMarketingStrategy || showCoreValue || showCoreFocus || showTenYear;

		//	if (anyCellsOnLeft && showThreeYear) {
		//		vision.AddColumn(Unit.FromInch(1.66 + 5.33));
		//		vision.AddColumn(Unit.FromInch(3.4));
		//	} else {
		//		vision.AddColumn(Unit.FromInch(1.66 + 5.33 + 3.4));
		//	}

		//	var vrow = vision.AddRow();
		//	var vtoLeft = vrow.Cells[0].Elements.AddTable();


		//	Column column;
		//	if (anyCellsOnLeft) {
		//		column = vtoLeft.AddColumn(Unit.FromInch(1.66));
		//		if (showThreeYear) {
		//			column = vtoLeft.AddColumn(Unit.FromInch(5.33));
		//		} else {
		//			column = vtoLeft.AddColumn(Unit.FromInch(5.33 + 3.4));
		//		}
		//	} else {
		//		column = vtoLeft.AddColumn(Unit.FromInch(0));
		//		column = vtoLeft.AddColumn(Unit.FromInch(0));
		//		//column = vtoLeft.AddColumn(Unit.FromInch(0));
		//	}


		//	Row row;

		//	var extraHeight = Unit.FromInch(0);

		//	//core values
		//	if (showCoreValue) {
		//		row = vtoLeft.AddRow();
		//		var cvTitle = row.Cells[0];
		//		row.Height = Unit.FromInch(1.2);
		//		row.Borders.Bottom.Color = settings.BorderColor;
		//		row.Borders.Right.Color = settings.BorderColor;
		//		cvTitle.Shading.Color = settings.FillColor;
		//		cvTitle.Format.Font.Bold = true;
		//		cvTitle.Format.Font.Size = 14;
		//		cvTitle.Format.Font.Name = "Arial Narrow";
		//		cvTitle.AddParagraph(vto.CoreValueTitle ?? "CORE VALUES");
		//		cvTitle.Format.Alignment = ParagraphAlignment.Center;
		//		row.VerticalAlignment = VerticalAlignment.Center;
		//		coreValuePanel = row.Cells[1];
		//	} else {
		//		coreValuePanel = null;
		//		extraHeight += Unit.FromInch(1.2);
		//	}

		//	//corefocus
		//	if (showCoreFocus) {
		//		row = vtoLeft.AddRow();
		//		row.Borders.Bottom.Color = settings.BorderColor;
		//		row.Borders.Right.Color = settings.BorderColor;
		//		row.Height = Unit.FromInch(1.2);
		//		var cfTitle = row.Cells[0];
		//		cfTitle.Shading.Color = settings.FillColor;
		//		cfTitle.AddParagraph(vto.CoreFocus.CoreFocusTitle ?? "CORE FOCUS™");
		//		cfTitle.Format.Font.Name = "Arial Narrow";
		//		cfTitle.Format.Font.Bold = true;
		//		cfTitle.Format.Font.Size = 14;
		//		coreFocusPanel = row.Cells[1];
		//		cfTitle.Format.Alignment = ParagraphAlignment.Center;
		//		row.VerticalAlignment = VerticalAlignment.Center;
		//	} else {
		//		coreFocusPanel = null;
		//		extraHeight += Unit.FromInch(1.2);
		//	}

		//	//ten year target
		//	if (showTenYear) {
		//		row = vtoLeft.AddRow();
		//		row.Borders.Bottom.Color = TableBlack;
		//		row.Borders.Right.Color = TableBlack;
		//		var tyTitle = row.Cells[0];
		//		row.Height = Unit.FromInch(0.6);
		//		tyTitle.Shading.Color = TableGray;
		//		tyTitle.AddParagraph(vto.TenYearTargetTitle ?? "10-YEAR TARGET™");
		//		tyTitle.Format.Font.Name = "Arial Narrow";
		//		tyTitle.Format.Font.Bold = true;
		//		tyTitle.Format.Font.Size = 13.5;
		//		tenYearPanel = row.Cells[1];
		//		tyTitle.Format.Alignment = ParagraphAlignment.Center;
		//		row.VerticalAlignment = VerticalAlignment.Center;
		//	} else {
		//		tenYearPanel = null;
		//		extraHeight += Unit.FromInch(0.6);
		//	}

		//	//marketing strategy
		//	if (showMarketingStrategy) {
		//		row = vtoLeft.AddRow();
		//		var msTitle = row.Cells[0];
		//		msTitle.Shading.Color = settings.FillColor;
		//		msTitle.AddParagraph(vto.Strategy.MarketingStrategyTitle ?? "MARKETING STRATEGY");
		//		msTitle.Format.Font.Name = "Arial Narrow";
		//		marketingStrategyPanel = row.Cells[1];
		//		msTitle.Format.Font.Bold = true;
		//		msTitle.Format.Font.Size = 14;
		//		row.Height = Unit.FromInch(2.7) + extraHeight;
		//		msTitle.Format.Alignment = ParagraphAlignment.Center;
		//		row.VerticalAlignment = VerticalAlignment.Center;
		//		row.Borders.Right.Color = settings.BorderColor;
		//	} else {
		//		marketingStrategyPanel = null;
		//	}

		//	//three year picture
		//	if (showThreeYear) {
		//		var cellNum = 1;
		//		if (!anyCellsOnLeft)
		//			cellNum = 0;

		//		var vtoRight = vrow.Cells[cellNum].Elements.AddTable();
		//		if (anyCellsOnLeft) {
		//			column = vtoRight.AddColumn(Unit.FromInch(3.4));
		//		} else {
		//			column = vtoRight.AddColumn(Unit.FromInch(5.33 + 3.4 + 1.66));
		//		}
		//		row = vtoRight.AddRow();
		//		row.Height = Unit.FromInch(.55);
		//		var typTitle = row.Cells[0];
		//		typTitle.Shading.Color = settings.FillColor;
		//		typTitle.AddParagraph(vto.ThreeYearPicture.ThreeYearPictureTitle ?? "3-YEAR-PICTURE™");
		//		typTitle.Format.Font.Name = "Arial Narrow";
		//		typTitle.Format.Font.Bold = true;
		//		typTitle.Format.Font.Size = 14;
		//		typTitle.Format.Alignment = ParagraphAlignment.Center;
		//		row.VerticalAlignment = VerticalAlignment.Center;
		//		row.Borders.Bottom.Color = settings.BorderColor;
		//		row = vtoRight.AddRow();
		//		threeYearPanel = row.Cells[0];
		//	} else {
		//		threeYearPanel = null;
		//	}

		//	//bullet points
		//	Style style = doc.AddStyle("NumberList1", "Normal");
		//	style.ParagraphFormat.RightIndent = 12;
		//	style.ParagraphFormat.TabStops.ClearAll();
		//	style.ParagraphFormat.TabStops.AddTabStop(Unit.FromInch(.15), TabAlignment.Left);
		//	style.ParagraphFormat.LeftIndent = Unit.FromInch(.15);
		//	style.ParagraphFormat.FirstLineIndent = Unit.FromInch(-.15);
		//	style.ParagraphFormat.SpaceBefore = 0;
		//	style.ParagraphFormat.SpaceAfter = 0;

		//	style = doc.AddStyle("BulletList1", "Normal");
		//	style.ParagraphFormat.RightIndent = 12;
		//	style.ParagraphFormat.TabStops.ClearAll();
		//	style.ParagraphFormat.TabStops.AddTabStop(Unit.FromInch(.15), TabAlignment.Left);
		//	style.ParagraphFormat.LeftIndent = Unit.FromInch(.15);
		//	style.ParagraphFormat.FirstLineIndent = Unit.FromInch(-.15);
		//	style.ParagraphFormat.SpaceBefore = 0;
		//	style.ParagraphFormat.SpaceAfter = 0;
		//}
		//private static async Task AddVtoVision_old(Document doc, AngularVTO vto, string dateformat, VtoPdfSettings settings) {

		//	Cell coreValuesPanel, coreFocusPanel, tenYearPanel, marketingStrategyPanel, threeYearPanel;
		//	//Table issueTable, rockTable, goalTable;
		//	Unit baseHeight = Unit.FromInch(5.1);//5.15


		//	var TableGray = new Color(100, 100, 100, 100);
		//	var TableBlack = new Color(0, 0, 0);

		//	await AddPage_VtoVision(doc, vto, settings, baseHeight, out coreValuesPanel, out coreFocusPanel, out tenYearPanel, out marketingStrategyPanel, out threeYearPanel);

		//	var values = vto.Values.ToList();
		//	ResizeToFit(coreValuesPanel, Unit.FromInch(5.33), Unit.FromInch(1.2), (cell, fs) => {
		//		var o = new List<Paragraph>();
		//		return OrderedList(values.Select(x => x.CompanyValue), ListType.NumberList1);
		//	}, maxFontSize: Unit.FromPoint(10), isCoreValues: true);


		//	ResizeToFit(coreFocusPanel, Unit.FromInch(5.33), Unit.FromInch(1.2), (cell, fs) => {
		//		var o = new List<Paragraph>();
		//		var p1 = new Paragraph();
		//		var txt = p1.AddFormattedText((vto.CoreFocus.PurposeTitle ?? "Purpose/Cause/Passion").Trim().TrimEnd(':') + ": ", TextFormat.Bold);
		//		p1.Format.Font.Name = "Arial Narrow";
		//		p1.AddText(vto.CoreFocus.Purpose ?? "");
		//		o.Add(p1);
		//		p1.Format.SpaceAfter = fs * 1.5;
		//		var p2 = new Paragraph();
		//		p2.AddFormattedText("Our Niche: ", TextFormat.Bold);
		//		p2.AddText(vto.CoreFocus.Niche ?? "");
		//		p2.Format.Font.Name = "Arial Narrow";
		//		o.Add(p2);
		//		return o;
		//	}, maxFontSize: 10);


		//	ResizeToFit(tenYearPanel, Unit.FromInch(5.33), Unit.FromInch(.6), (cell, fs1) => {
		//		var o = new List<Paragraph>();
		//		var p11 = new Paragraph();
		//		p11.Format.Font.Name = "Arial Narrow";
		//		p11.AddText(vto.TenYearTarget ?? "");
		//		o.Add(p11);
		//		return o;
		//	}, maxFontSize: 10);

		//	var marketingParagraphs = new List<Paragraph>();
		//	bool addBeforeSpace = false;
		//	//////
		//	{
		//		var count = -1;
		//		var strats = vto.Strategies.ToList();
		//		var includeTitle = strats.Count > 1;

		//		foreach (var item in strats.ToList()) {
		//			count += 1;
		//			var fs = 10;


		//			if (count > 0) {
		//				var spacer = new Paragraph();
		//				spacer.Format.Borders.Top.Color = settings.LightBorderColor;
		//				//spacer.Format.Borders.Bottom.Color = Colors.Red;
		//				spacer.Format.SpaceBefore = fs * 1.5;
		//				spacer.Format.SpaceAfter = fs * .75;
		//				marketingParagraphs.Add(spacer);
		//			}

		//			if (includeTitle && !string.IsNullOrWhiteSpace(item.Title)) {
		//				var p0 = new Paragraph();
		//				p0.Format.Font.Size = fs;
		//				if (count > 0) {
		//					if (addBeforeSpace) {
		//						p0.Format.SpaceBefore = fs * 1.5;
		//					}
		//				}
		//				p0.Format.SpaceAfter = fs * 1;
		//				var txt0 = p0.AddFormattedText(item.Title ?? "", TextFormat.Bold | TextFormat.Underline);
		//				p0.Format.Font.Name = "Arial Narrow";
		//				//p0.AddText(item.Title ?? "");
		//				marketingParagraphs.Add(p0);
		//			}

		//			var p1 = new Paragraph();
		//			p1.Format.Font.Size = fs;
		//			//p1.Format.SpaceBefore = fs * 1.5;
		//			var txt = p1.AddFormattedText("Target Market/\"The List\": ", TextFormat.Bold);
		//			p1.Format.Font.Name = "Arial Narrow";
		//			p1.AddText(item.TargetMarket ?? "");
		//			marketingParagraphs.Add(p1);

		//			var p2 = new Paragraph();
		//			p2.Format.Font.Size = fs;
		//			var uniques = item.Uniques.ToList();
		//			p2.Format.SpaceBefore = fs * 1.25;
		//			var uniquesTitle = "Uniques: ";
		//			if (uniques.Count == 3)
		//				uniquesTitle = "Three " + uniquesTitle;
		//			p2.AddFormattedText(uniquesTitle, TextFormat.Bold);
		//			p2.Format.Font.Name = "Arial Narrow";
		//			marketingParagraphs.Add(p2);
		//			marketingParagraphs.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44)));

		//			if (!string.IsNullOrEmpty(item.ProvenProcess)) {
		//				var p3 = new Paragraph();
		//				p3.Format.Font.Size = fs;
		//				p3.Format.SpaceBefore = fs * 1.25;

		//				if (strats.Count > 1 && string.IsNullOrEmpty(item.Guarantee)) {
		//					p3.Format.SpaceAfter = fs * 1.25;
		//				}

		//				p3.AddFormattedText("Proven Process: ", TextFormat.Bold);
		//				p3.Format.Font.Name = "Arial Narrow";
		//				p3.AddText(item.ProvenProcess ?? "");
		//				marketingParagraphs.Add(p3);
		//			}

		//			if (!string.IsNullOrEmpty(item.Guarantee)) {
		//				var p4 = new Paragraph();
		//				p4.Format.Font.Size = fs;
		//				p4.Format.SpaceBefore = fs * 1.25;

		//				if (strats.Count > 1) {
		//					p4.Format.SpaceAfter = fs * 1.25;
		//				}

		//				p4.AddFormattedText("Guarantee: ", TextFormat.Bold);
		//				p4.Format.Font.Name = "Arial Narrow";
		//				p4.AddText(item.Guarantee ?? "");
		//				marketingParagraphs.Add(p4);
		//			}

		//			addBeforeSpace = false;
		//			if (string.IsNullOrEmpty(item.ProvenProcess) && string.IsNullOrEmpty(item.ProvenProcess)) {
		//				addBeforeSpace = true;
		//			}
		//		}
		//	}

		//	var marketingPages = SplitHeights(Unit.FromInch(5.33), new[] { Unit.FromInch(2.7), Unit.FromInch(5.7) }, marketingParagraphs);

		//	var j = 0;
		//	for (var i = 0; i < marketingPages.Count; i++) {
		//		Page m = marketingPages[i];
		//		var f = m.FirstOrDefault();
		//		if (f != null && f.Item is Paragraph) {
		//			var p = (Paragraph)f.Item;
		//			if (p.Format.Borders.Top.Color == TableGray) {
		//				m.Items = m.Items.Skip(1).ToList();
		//				//marketingParagraphs.RemoveAt(j);
		//				marketingParagraphs.Remove(p);
		//			}
		//		}
		//		f = m.LastOrDefault();
		//		if (f != null && f.Item is Paragraph) {
		//			var p = (Paragraph)f.Item;
		//			if (p.Format.Borders.Top.Color == TableGray) {
		//				m.Items = m.Items.Take(m.Items.Count - 1).ToList();
		//				marketingParagraphs.Remove(p);
		//			}
		//		}
		//	}



		//	var looksList = vto.ThreeYearPicture.LooksLike.Where(x => !string.IsNullOrWhiteSpace(x.Data)).Select(x => x.Data).ToList();

		//	var threeYearParagraphs = new List<Paragraph>();
		//	{
		//		var fs = 10;
		//		threeYearParagraphs.AddRange(AddVtoSectionHeader(vto.ThreeYearPicture, fs, dateformat));
		//		var p = new Paragraph();
		//		p.AddFormattedText("What does it look like?", TextFormat.Bold | TextFormat.Underline);
		//		p.Format.Font.Name = "Arial Narrow";
		//		p.Format.Font.Size = fs;
		//		threeYearParagraphs.Add(p);
		//		threeYearParagraphs.AddRange(OrderedList(looksList, ListType.BulletList1));
		//	}

		//	var threeYearPages = SplitHeights(Unit.FromInch(3.4), new[] { Unit.FromInch(5.15), Unit.FromInch(5.7) }, threeYearParagraphs, null, null, stillTooBig: x => {
		//		var i = (Paragraph)x.Unmodified.Item;
		//		var baseSize = i.Format.Font.Size;
		//		var o = new Paragraph();
		//		var size = ResizeToFit(o, x.MaximumWidth, x.MaximumHeight, (d, s) => {
		//			i.Format.Font.Size = s;
		//			return i.AsList();
		//		}, 5, baseSize);

		//		o.Format.Font.Size = size;

		//		return new ItemHeight() {
		//			Item = o,
		//			Height = GetSize(o, x.MaximumWidth).Height
		//		};
		//	});

		//	var curMSI = 0;
		//	var curTYPI = 0;
		//	//Page 1
		//	if (marketingPages.Any()) {
		//		foreach (var mPara in marketingPages[0]) {
		//			marketingStrategyPanel.Add(marketingParagraphs[curMSI].Clone());
		//			curMSI++;
		//		}
		//	}
		//	if (threeYearPages.Any()) {
		//		foreach (var tyPara in threeYearPages[0]) {
		//			threeYearPanel.Add(threeYearParagraphs[curTYPI].Clone());
		//			curTYPI++;
		//		}
		//	}


		//	var maxPage = Math.Max(marketingPages.Count(), threeYearPages.Count());
		//	for (var p = 1; p < maxPage; p++) {
		//		if (p < maxPage) {
		//			var showMarketing = p < marketingPages.Count();
		//			var showThreeYear = p < threeYearPages.Count();

		//			AddPage_VtoVision(doc, vto, settings, baseHeight, out coreValuesPanel, out coreFocusPanel, out tenYearPanel, out marketingStrategyPanel, out threeYearPanel, false, false, false, showMarketing, showThreeYear);
		//		}
		//		if (p < marketingPages.Count()) {
		//			foreach (var r in marketingPages[p]) {
		//				marketingStrategyPanel.Add(marketingParagraphs[curMSI].Clone());
		//				curMSI++;
		//			}
		//		}

		//		if (p < threeYearPages.Count()) {
		//			foreach (var r in threeYearPages[p]) {
		//				threeYearPanel.Add(threeYearParagraphs[curTYPI].Clone());
		//				curTYPI++;
		//			}
		//		}

		//	}
		//}
		/*private static void AppendMarketStrategies(Cell cell, AngularVTO vto, ResizeContext ctx, VtoPdfSettings setting) {
			//cell.AddParagraph("MarketStrategy");
			//var marketingStrategyPanel = new ResizableElement(leftContentWidth, (c, v) => { AppendMarketStrategy(FormatParagraph(c, v), vto); });
			var strats = vto.NotNull(x => x.Strategies.ToList()) ?? new List<AngularStrategy>();
			var addSpaceBefore = true;

			var stratCount = strats.Count();//Math.Min(strats.Count(),Math.Round(vars.Get("NumberOfStrategies")));

			for (var i = 0; i < stratCount; i++) {
				var strat = strats[i];
				var isFirst = i == 0;
				var isLast = i == strats.Count() - 1;
				var isOnly = strats.Count() == 1;

				AppendMarketStrategy(cell, strat, setting, isFirst, isLast, isOnly, ref addSpaceBefore);
			}
		}*/


		private class MarketingStrategyHint : Hint {

			public class MSHintGroups {
				public string Title { get; set; }
				public DefaultDictionary<Cell, MSHintGroup> Groups { get; set; }

				public MSHintGroups(string title) {
					Title = title;
					Groups = new DefaultDictionary<Cell, MSHintGroup>(x => new MSHintGroup(Title));

				}
			}

			public class MSHintGroup {
				public Table OuterContainer { get; set; }
				public Table InnerContainer { get; set; }
				public Cell TitleCell { get; set; }
				public string Title { get; set; }
				public int Rows = 0;

				public MSHintGroup(string title) {
					Title = title;
				}

				public MSHintGroup Clone() {
					return new MSHintGroup(this.Title) {
						OuterContainer = null,
						TitleCell = null,
						Rows = 0
					};
				}
			}

			public MarketingStrategyHint(string viewBox, MSHintGroups groups, VtoPdfSettings settings, params IElement[] elements) : base(viewBox, elements) {
				Groups = groups;
				Settings = settings;
			}

			public MSHintGroups Groups { get; set; }
			public VtoPdfSettings Settings { get; set; }

			//private void Dive(DocumentObjectCollection element, Action<dynamic> action) {
			//	action(element);
			//	foreach (var child in element) {
			//		Dive(child, action);
			//	}
			//}

			public override void DrawElement(Container elementContents, Cell viewBoxContainer,int page) {

				var G = Groups.Groups[viewBoxContainer];

				Column resizeableColumn1 = null;
				Column resizeableColumn2 = null;


				if (G.OuterContainer == null) {
					G.OuterContainer = new Table();
					var t = G.OuterContainer;
					t.Rows.LeftIndent = 0;
					t.LeftPadding = 0;
					t.RightPadding = 0;

					t.AddColumn(VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);
					resizeableColumn1=t.AddColumn(viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);
					viewBoxContainer.Elements.Add(t);
					var row = t.AddRow();
					AppendRowTitle(row, G.Title, Settings);
					row.Borders.Bottom.Width = 0;
					G.TitleCell = row.Cells[0];
					G.TitleCell.Format.Font.Color = Settings.FillTextColor;

					G.InnerContainer = new Table();
					G.InnerContainer.Rows.LeftIndent = 0;
					G.InnerContainer.LeftPadding = 0;
					G.InnerContainer.RightPadding = 0;
					resizeableColumn2=G.InnerContainer.AddColumn(viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);

					row.Cells[1].Elements.Add(G.InnerContainer);

					//G.InnerContainer.Borders.Color = Colors.Blue;
					//G.InnerContainer.Borders.Width= 2;

					row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

				} /*else {
					row = G.OuterContainer.AddRow();
				}
				G.TitleCell.MergeDown = G.Rows;*/

				G.Rows += 1;
				var r = G.InnerContainer.AddRow();

				if (resizeableColumn1 != null && resizeableColumn2 != null && (viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH) != resizeableColumn1.Width) {
					resizeableColumn1.Width = viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH;
					resizeableColumn2.Width = viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH;
				}

				base.DrawElement(elementContents, r.Cells[0], page);
				if (HasError) {
					try {
						elementContents.Rows[0].Cells[0].Format.Font.Size = 6;
					} catch (Exception e) {
						int i = 0;
					}
				}
			}

			//public override void Draw(Table viewBoxContainer, INamedViewBox viewBox, RangedVariables pageVariables) {
			//	if (Row == null) {
			//	}
			//	base.Draw(viewBoxContainer, viewBox, pageVariables);
			//}
		}
		private static IEnumerable<IHint> GenerateMarketingStrategyHints_Old(string viewBoxName, string title, AngularVTO vto, VtoPdfSettings setting) {
			if (vto.NotNull(x => x.Strategies) != null) {
				var stratCount = vto.Strategies.Count();
				var spaceBefore = true;
				var group = new MarketingStrategyHint.MSHintGroups(title);
				for (var i = 0; i < stratCount; i++) {
					var strat = vto.Strategies.ElementAt(i);
					//var isFirst = i == 0;
					//var isLast = i == stratCount - 1;
					var isOnly = stratCount == 1;
					var paddOnlyOnce = i == 0;


					yield return new MarketingStrategyHint(viewBoxName, group, setting, new ResizableElement((ctx) => {
						var c = ctx.Container;
						var v = ctx.Variables;
						
			

						//var isFirst = ctx.This == ctx.Hint.GetElements().FirstOrDefault();
						//var isLast = ctx.This == ctx.Hint.GetElements().LastOrDefault();
						//var isOnly = isFirst && isLast;

						var isFirst = true;
						var isLast = true;
						//var isOnly = false;

						//var a = ((MarketingStrategyHint)ctx.Hint).Groups.Groups[ctx.Container];
						//if (paddOnlyOnce)
						AddPadding(v, c, isFirst, isLast);
						AppendMarketStrategy(ctx, c, strat, setting, isFirst, isLast, isOnly, ref spaceBefore);
					}, widthOverride: VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH));
				}
			}
		}
		#endregion
	}
}