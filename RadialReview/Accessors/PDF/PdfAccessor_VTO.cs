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

namespace RadialReview.Accessors {
	public partial class PdfAccessor {
		public static void AddVTO(Document doc, AngularVTO vto, string dateformat) {
			if (vto.IncludeVision)
				AddVtoVision(doc, vto, dateformat);
			AddVtoTraction(doc, vto, dateformat);
		}

		private static Section AddVtoPage(Document doc, string docName, string pageName) {
			Section section;

			section = doc.AddSection();
			section.PageSetup.Orientation = Orientation.Landscape;
			section.PageSetup.PageFormat = PageFormat.Letter;


			var paragraph = new Paragraph();
			var p = section.Footers.Primary.AddParagraph("© 2003 - " + DateTime.UtcNow.AddMonths(3).Year + " EOS. All Rights Reserved.");
			p.Format.LeftIndent = Unit.FromPoint(14);

			section.Footers.Primary.Format.Font.Size = 10;
			section.Footers.Primary.Format.Font.Name = "Arial Narrow";
			section.Footers.Primary.Format.Font.Size = 8;
			section.Footers.Primary.Format.Font.Color = TableGray;

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
				var imageFilename = HttpContext.Current.Server.MapPath("~/Content/img/EOS_Model.png");

				var img = titleRow.Cells[1].AddImage(imageFilename);
				//img.Height = Unit.FromInch(2.13);
				img.Width = Unit.FromInch(1.95);
			} catch (Exception e) {
				titleRow.Height = Unit.FromInch(1.787);
			}

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
			box.Borders.Color = TableBlack;
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



			return section;
		}


		private static Cell FormatParagraph(Cell cell, ResizeVariables vars) {
			cell.Format.Font.Size = vars.Get("FontSize");
			cell.Borders.Top.Width = vars.Get("Spacer");
			cell.Borders.Bottom.Width = vars.Get("Spacer");
			cell.Borders.Color = Colors.Transparent;

			return cell;
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

		private static void AppendMarketStrategies(Cell cell, AngularVTO vto, ResizeVariables vars) {
			//cell.AddParagraph("MarketStrategy");
			//var marketingStrategyPanel = new ResizableElement(leftContentWidth, (c, v) => { AppendMarketStrategy(FormatParagraph(c, v), vto); });
			var strats = vto.NotNull(x => x.Strategies.ToList()) ?? new List<AngularStrategy>();
			var addSpaceBefore = true;

			var stratCount = Math.Round(vars.Get("NumberOfStrategies"));

			for (var i = 0; i < Math.Min(strats.Count(),stratCount); i++) {
				var strat = strats[i];
				var isFirst = i == 0;
				var isLast = i == strats.Count() - 1;
				var isOnly = strats.Count() == 1;

				AppendMarketStrategy(cell,strat, isFirst,isLast, isOnly, ref addSpaceBefore);
			}

		}

		private static void AppendMarketStrategy(Cell cell, AngularStrategy strat, bool isFirst, bool isLast, bool isOnly, ref bool addSpaceBefore) {
			
			
			var fs = 7;
			var paragraphs = new List<Paragraph>();
			//Add spacer
			if (!isFirst) {
				var spacer = new Paragraph();
				spacer.Format.Borders.Top.Color = TableGray;
				spacer.Format.SpaceBefore = fs * 1.5;
				spacer.Format.SpaceAfter = fs * .75;
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
			var theList = new Paragraph();
			theList.Format.Font.Name = "Arial Narrow";
			theList.AddFormattedText("Target Market/\"The List\": ", TextFormat.Bold);
			theList.AddText(strat.TargetMarket ?? "");
			paragraphs.Add(theList);

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
				paragraphs.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44)));
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

		private static void AppendRowTitle(Row row, string title) {
			var cvTitle = row.Cells[0];
			row.Borders.Bottom.Color = TableBlack;
			row.Borders.Right.Color = TableBlack;
			cvTitle.Shading.Color = TableGray;
			cvTitle.Format.Font.Bold = true;
			cvTitle.Format.Font.Size = 14;
			cvTitle.Format.Font.Name = "Arial Narrow";
			cvTitle.AddParagraph(title ?? "");
			cvTitle.Format.Alignment = ParagraphAlignment.Center;
			row.VerticalAlignment = VerticalAlignment.Center;
		}

		[Obsolete("Only for private or test use")]
		public static void AddVtoVision(Document doc, AngularVTO vto, string dateformat) {
			var TableGray = new Color(100, 100, 100, 100);
			var TableBlack = new Color(0, 0, 0);

			var section = AddVtoPage(doc, vto.Name ?? "", "VISION");
			var vision = section.AddTable();
			vision.Style = "Table";
			vision.Borders.Color = TableBlack;
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

			coreValuesPanel = new ResizableElement(leftContentWidth, (c, v) => { AppendCoreValues(FormatParagraph(c, v), vto); });
			coreFocusPanel = new ResizableElement(leftContentWidth, (c, v) => { AppendCoreFocus(FormatParagraph(c, v), vto); });
			tenYearPanel = new ResizableElement(leftContentWidth, (c, v) => { AppendTenYear(FormatParagraph(c, v), vto); });
			marketingStrategyPanel = new ResizableElement(leftContentWidth, (c, v) => { AppendMarketStrategies(FormatParagraph(c, v), vto, v); });
			//var marketingStrategyElements = new List<ResizableElement>();

			var stratCount = vto.NotNull(x => x.Strategies.Count());

			var vars = new ResizeVariables();
			vars.Add("FontSize", 10, 6, 10);
			vars.Add("Spacer", Unit.FromInch(.25), Unit.FromInch(.05), Unit.FromInch(5));
			vars.Add("NumberOfStrategies", stratCount, 0, stratCount+1.5);

			if (showCoreValue)
				elements.Add(coreValuesPanel);
			if (showCoreFocus)
				elements.Add(coreFocusPanel);
			if (showTenYearPanel)
				elements.Add(tenYearPanel);
			if (showMarketingStrategy)
				elements.Add(marketingStrategyPanel);


			var optimized = PdfOptimzer.OptimizeHeights(Unit.FromInch(5.5), elements, vars);

			var f = optimized.Fit.Fits;

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
					AppendRowTitle(r, vto.NotNull(x => x.CoreValueTitle) ?? "CORE VALUES");
					r.Cells[1].Elements.Add(t);
				}, optimized.Variables);
			}

			if (showCoreValue) {
				coreFocusPanel.AddToDocument(t => {
					var r = left.AddRow();
					AppendRowTitle(r, vto.NotNull(x => x.CoreFocus.CoreFocusTitle) ?? "CORE FOCUS™");
					r.Cells[1].Elements.Add(t);
				}, optimized.Variables);
			}

			if (showTenYearPanel) {
				tenYearPanel.AddToDocument(t => {
					var r = left.AddRow();
					AppendRowTitle(r, vto.NotNull(x => x.TenYearTargetTitle) ?? "10-YEAR TARGET™");
					r.Cells[1].Elements.Add(t);
				}, optimized.Variables);
			}

			if (showMarketingStrategy) {
				marketingStrategyPanel.AddToDocument(t => {
					var r = left.AddRow();
					AppendRowTitle(r, vto.NotNull(x => x.Strategy.MarketingStrategyTitle) ?? "MARKETING STRATEGY");
					var c = r.Cells[1];
					c.Borders.Bottom.Width = 0;
					c.Elements.Add(t);

				}, optimized.Variables);
			}

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

			}*/

		}


		#region ignore
		private static void AddVtoVision_old(Document doc, AngularVTO vto, string dateformat) {

			Cell coreValuesPanel, coreFocusPanel, tenYearPanel, marketingStrategyPanel, threeYearPanel;
			//Table issueTable, rockTable, goalTable;
			Unit baseHeight = Unit.FromInch(5.1);//5.15


			var TableGray = new Color(100, 100, 100, 100);
			var TableBlack = new Color(0, 0, 0);

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

			}
		}

		private static void AddVtoTraction(Document doc, AngularVTO vto, string dateformat) {
			Cell oneYear, quarterlyRocks, issuesList;
			Table issueTable, rockTable, goalTable;
			Unit baseHeight = Unit.FromInch(5.1);//5.15
			AddPage_VtoTraction(doc, vto, baseHeight, out oneYear, out quarterlyRocks, out issuesList, out issueTable, out rockTable, out goalTable);

			Unit fs = 10;
			var goalObjects = new List<DocumentObject>();
			var goalsSplits = new List<Page>();
			var goalRows = new List<Row>();
			var goalParagraphs = new List<Paragraph>();
			{
				var goals = vto.OneYearPlan.GoalsForYear.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();

				//ResizeToFit(oneYear, Unit.FromInch(3.47), Unit.FromInch(5.15), (cell, fs) => {
				goalObjects.AddRange(AddVtoSectionHeader(vto.OneYearPlan, fs, dateformat));
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


				for (var i = 0; i < goals.Count; i++) {
					var r = new Row();
					//rockTable.AddRow();
					r.Height = Unit.FromInch(0.2444 * fs.Point / 10);
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
				Unit pg1Height = baseHeight - headerSize.Height + Unit.FromInch(0.51);
				goalsSplits = SplitHeights(Unit.FromInch(3), new[] { pg1Height, (baseHeight) }, goalParagraphs);


				goalObjects.Add(goalTable);
			}

			var rockObjects = new List<DocumentObject>();
			var rockSplits = new List<Page>();
			var rockRows = new List<Row>();
			var rockParagraphs = new List<Paragraph>();
			{
				var rocks = vto.QuarterlyRocks.Rocks.Where(x => !String.IsNullOrWhiteSpace(x.Rock.Name)).ToList();
				quarterlyRocks.Format.LeftIndent = Unit.FromInch(.095);
				//ResizeToFit(quarterlyRocks, Unit.FromInch(3.47), Unit.FromInch(5.15), (cell, fs) => {
				rockObjects.AddRange(AddVtoSectionHeader(vto.QuarterlyRocks, fs, dateformat));
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



				for (var i = 0; i < rocks.Count; i++) {
					var r = new Row();
					r.Height = Unit.FromInch(0.2444 * fs.Point / 10);
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
				rockSplits = SplitHeights(Unit.FromInch(2.6), new[] { pg1Height, (baseHeight) }, rockParagraphs);
				rockObjects.Add(rockTable);

			}
			var issuesObjects = new List<DocumentObject>();
			var issueSplits = new List<Page>();
			var issueRows = new List<Row>();
			var issueParagraph = new List<Paragraph>();
			{
				var issues = vto.Issues.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();

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

					for (var i = 0; i < issues.Count; i++) {
						var r = new Row();
						r.Height = Unit.FromInch(0.2444 * fs.Point / 10);
						r.HeightRule = RowHeightRule.AtLeast;
						var p = r.Cells[0].AddParagraph("" + (i + 1) + ".");
						p.Format.SpaceBefore = Unit.FromPoint(2);
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						p.Format.Alignment = ParagraphAlignment.Right;
						p = r.Cells[1].AddParagraph(issues[i] ?? "");
						issueParagraph.Add(p);
						p.Format.SpaceBefore = Unit.FromPoint(2);
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						issueRows.Add(r);
					}

					//var rowHeights = GetRowHeights(issueRows, Unit.FromInch(3));
					var extraHeight = 0.51;

					issueSplits = SplitHeights(Unit.FromInch(3), new[] { (baseHeight), (baseHeight) }, issueParagraph, null /*x => x.Cells[1]*/, extraHeight);
					issuesObjects.Add(issueTable);
				}

			}

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
					AddPage_VtoTraction(doc, vto, baseHeight, out oneYear, out quarterlyRocks, out issuesList, out issueTable, out rockTable, out goalTable);
					AppendAll(oneYear, new DocumentObject[] { goalTable }.ToList());
					AppendAll(quarterlyRocks, new DocumentObject[] { rockTable }.ToList());
					AppendAll(issuesList, new DocumentObject[] { issueTable }.ToList());

				}
			}
		}


		private static void AddPage_VtoTraction(Document doc, AngularVTO vto, Unit height, out Cell oneYear, out Cell quarterlyRocks, out Cell issuesList, out Table issueTable, out Table rockTable, out Table goalTable) {
			var section = AddVtoPage(doc, vto._TractionPageName ?? vto.Name ?? "", "TRACTION");

			var table = section.AddTable();
			table.AddColumn(Unit.FromInch(3.47));
			table.AddColumn(Unit.FromInch(3.47));
			table.AddColumn(Unit.FromInch(3.47));
			table.Borders.Color = TableBlack;

			var tractionHeader = table.AddRow();
			tractionHeader.Shading.Color = TableGray;
			tractionHeader.Height = Unit.FromInch(0.55);
			var paragraph = tractionHeader.Cells[0].AddParagraph(vto.OneYearPlan.OneYearPlanTitle ?? "1-YEAR PLAN");
			paragraph.Format.Font.Name = "Arial Narrow";
			paragraph.Format.Font.Size = 14;
			paragraph.Format.Font.Bold = true;
			paragraph.Format.Alignment = ParagraphAlignment.Center;

			tractionHeader.Cells[0].VerticalAlignment = VerticalAlignment.Center;
			//tractionHeader.Cells[0].Format.Shading.Color = TableGray;


			paragraph = tractionHeader.Cells[1].AddParagraph(vto.QuarterlyRocks.RocksTitle ?? "ROCKS");
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
			oneYear = tractionData.Cells[0];
			quarterlyRocks = tractionData.Cells[1];
			issuesList = tractionData.Cells[2];
			issueTable = new Table();
			issueTable.Borders.Color = TableGray;
			issueTable.AddColumn(Unit.FromInch(.28));
			issueTable.AddColumn(Unit.FromInch(3));

			rockTable = new Table();
			rockTable.Borders.Color = TableGray;
			rockTable.AddColumn(Unit.FromInch(.28));
			rockTable.AddColumn(Unit.FromInch(2.6));
			rockTable.AddColumn(Unit.FromInch(.4));

			goalTable = new Table();
			goalTable.Borders.Color = TableGray;
			goalTable.AddColumn(Unit.FromInch(.28));
			goalTable.AddColumn(Unit.FromInch(3));
		}


		private static void AddPage_VtoVision(Document doc, AngularVTO vto, Unit height,
			out Cell coreValuePanel, out Cell coreFocusPanel, out Cell tenYearPanel, out Cell marketingStrategyPanel, out Cell threeYearPanel,
			bool showCoreValue = true, bool showCoreFocus = true, bool showTenYear = true, bool showMarketingStrategy = true, bool showThreeYear = true) {
			var section = AddVtoPage(doc, vto.Name ?? "", "VISION");
			var vision = section.AddTable();
			vision.Style = "Table";
			vision.Borders.Color = TableBlack;
			vision.Borders.Width = 1;
			vision.Rows.LeftIndent = 0;
			vision.LeftPadding = 0;
			vision.RightPadding = 0;

			var anyCellsOnLeft = showMarketingStrategy || showCoreValue || showCoreFocus || showTenYear;

			if (anyCellsOnLeft && showThreeYear) {
				vision.AddColumn(Unit.FromInch(1.66 + 5.33));
				vision.AddColumn(Unit.FromInch(3.4));
			} else {
				vision.AddColumn(Unit.FromInch(1.66 + 5.33 + 3.4));
			}

			var vrow = vision.AddRow();
			var vtoLeft = vrow.Cells[0].Elements.AddTable();


			Column column;
			if (anyCellsOnLeft) {
				column = vtoLeft.AddColumn(Unit.FromInch(1.66));
				if (showThreeYear) {
					column = vtoLeft.AddColumn(Unit.FromInch(5.33));
				} else {
					column = vtoLeft.AddColumn(Unit.FromInch(5.33 + 3.4));
				}
			} else {
				column = vtoLeft.AddColumn(Unit.FromInch(0));
				column = vtoLeft.AddColumn(Unit.FromInch(0));
				//column = vtoLeft.AddColumn(Unit.FromInch(0));
			}


			Row row;

			var extraHeight = Unit.FromInch(0);

			//core values
			if (showCoreValue) {
				row = vtoLeft.AddRow();
				var cvTitle = row.Cells[0];
				row.Height = Unit.FromInch(1.2);
				row.Borders.Bottom.Color = TableBlack;
				row.Borders.Right.Color = TableBlack;
				cvTitle.Shading.Color = TableGray;
				cvTitle.Format.Font.Bold = true;
				cvTitle.Format.Font.Size = 14;
				cvTitle.Format.Font.Name = "Arial Narrow";
				cvTitle.AddParagraph(vto.CoreValueTitle ?? "CORE VALUES");
				cvTitle.Format.Alignment = ParagraphAlignment.Center;
				row.VerticalAlignment = VerticalAlignment.Center;
				coreValuePanel = row.Cells[1];
			} else {
				coreValuePanel = null;
				extraHeight += Unit.FromInch(1.2);
			}

			//corefocus
			if (showCoreFocus) {
				row = vtoLeft.AddRow();
				row.Borders.Bottom.Color = TableBlack;
				row.Borders.Right.Color = TableBlack;
				row.Height = Unit.FromInch(1.2);
				var cfTitle = row.Cells[0];
				cfTitle.Shading.Color = TableGray;
				cfTitle.AddParagraph(vto.CoreFocus.CoreFocusTitle ?? "CORE FOCUS™");
				cfTitle.Format.Font.Name = "Arial Narrow";
				cfTitle.Format.Font.Bold = true;
				cfTitle.Format.Font.Size = 14;
				coreFocusPanel = row.Cells[1];
				cfTitle.Format.Alignment = ParagraphAlignment.Center;
				row.VerticalAlignment = VerticalAlignment.Center;
			} else {
				coreFocusPanel = null;
				extraHeight += Unit.FromInch(1.2);
			}

			//ten year target
			if (showTenYear) {
				row = vtoLeft.AddRow();
				row.Borders.Bottom.Color = TableBlack;
				row.Borders.Right.Color = TableBlack;
				var tyTitle = row.Cells[0];
				row.Height = Unit.FromInch(0.6);
				tyTitle.Shading.Color = TableGray;
				tyTitle.AddParagraph(vto.TenYearTargetTitle ?? "10-YEAR TARGET™");
				tyTitle.Format.Font.Name = "Arial Narrow";
				tyTitle.Format.Font.Bold = true;
				tyTitle.Format.Font.Size = 13.5;
				tenYearPanel = row.Cells[1];
				tyTitle.Format.Alignment = ParagraphAlignment.Center;
				row.VerticalAlignment = VerticalAlignment.Center;
			} else {
				tenYearPanel = null;
				extraHeight += Unit.FromInch(0.6);
			}

			//marketing strategy
			if (showMarketingStrategy) {
				row = vtoLeft.AddRow();
				var msTitle = row.Cells[0];
				msTitle.Shading.Color = TableGray;
				msTitle.AddParagraph(vto.Strategy.MarketingStrategyTitle ?? "MARKETING STRATEGY");
				msTitle.Format.Font.Name = "Arial Narrow";
				marketingStrategyPanel = row.Cells[1];
				msTitle.Format.Font.Bold = true;
				msTitle.Format.Font.Size = 14;
				row.Height = Unit.FromInch(2.7) + extraHeight;
				msTitle.Format.Alignment = ParagraphAlignment.Center;
				row.VerticalAlignment = VerticalAlignment.Center;
				row.Borders.Right.Color = TableBlack;
			} else {
				marketingStrategyPanel = null;
			}

			//three year picture
			if (showThreeYear) {
				var cellNum = 1;
				if (!anyCellsOnLeft)
					cellNum = 0;

				var vtoRight = vrow.Cells[cellNum].Elements.AddTable();
				if (anyCellsOnLeft) {
					column = vtoRight.AddColumn(Unit.FromInch(3.4));
				} else {
					column = vtoRight.AddColumn(Unit.FromInch(5.33 + 3.4 + 1.66));
				}
				row = vtoRight.AddRow();
				row.Height = Unit.FromInch(.55);
				var typTitle = row.Cells[0];
				typTitle.Shading.Color = TableGray;
				typTitle.AddParagraph(vto.ThreeYearPicture.ThreeYearPictureTitle ?? "3-YEAR-PICTURE™");
				typTitle.Format.Font.Name = "Arial Narrow";
				typTitle.Format.Font.Bold = true;
				typTitle.Format.Font.Size = 14;
				typTitle.Format.Alignment = ParagraphAlignment.Center;
				row.VerticalAlignment = VerticalAlignment.Center;
				row.Borders.Bottom.Color = TableBlack;
				row = vtoRight.AddRow();
				threeYearPanel = row.Cells[0];
			} else {
				threeYearPanel = null;
			}

			//bullet points
			Style style = doc.AddStyle("NumberList1", "Normal");
			style.ParagraphFormat.RightIndent = 12;
			style.ParagraphFormat.TabStops.ClearAll();
			style.ParagraphFormat.TabStops.AddTabStop(Unit.FromInch(.15), TabAlignment.Left);
			style.ParagraphFormat.LeftIndent = Unit.FromInch(.15);
			style.ParagraphFormat.FirstLineIndent = Unit.FromInch(-.15);
			style.ParagraphFormat.SpaceBefore = 0;
			style.ParagraphFormat.SpaceAfter = 0;

			style = doc.AddStyle("BulletList1", "Normal");
			style.ParagraphFormat.RightIndent = 12;
			style.ParagraphFormat.TabStops.ClearAll();
			style.ParagraphFormat.TabStops.AddTabStop(Unit.FromInch(.15), TabAlignment.Left);
			style.ParagraphFormat.LeftIndent = Unit.FromInch(.15);
			style.ParagraphFormat.FirstLineIndent = Unit.FromInch(-.15);
			style.ParagraphFormat.SpaceBefore = 0;
			style.ParagraphFormat.SpaceAfter = 0;
		}

		private static List<Paragraph> OrderedList(IEnumerable<string> items, ListType type, Unit? leftIndent = null) {
			var o = new List<Paragraph>();
			var res = items.Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
			for (int idx = 0; idx < res.Count(); ++idx) {
				ListInfo listinfo = new ListInfo();
				listinfo.ContinuePreviousList = idx > 0;
				listinfo.ListType = type;
				var paragraph = new Paragraph();
				paragraph.AddText((res[idx] ?? "").Trim());
				paragraph.Format.Font.Name = "Arial Narrow";
				paragraph.Format.Font.Size = 10;
				paragraph.Style = "" + type;
				paragraph.Format.ListInfo = listinfo;

				paragraph.Format.SpaceAfter = 0;
				paragraph.Format.SpaceBefore = 0;

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
	}
}