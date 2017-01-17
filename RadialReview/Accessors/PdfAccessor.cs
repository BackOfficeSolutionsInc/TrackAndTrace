using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using MigraDoc.DocumentObjectModel;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.VTO;
using RadialReview.Controllers;
using RadialReview.Engines;
using Table = MigraDoc.DocumentObjectModel.Tables.Table;
using VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment;
using RadialReview.Utilities.DataTypes;
using PdfSharp.Drawing.Layout;
using RadialReview.Models.Reviews;
using MigraDoc.Rendering;
using static RadialReview.Accessors.FastReviewQueries;
using static RadialReview.Engines.ChartsEngine;
using RadialReview.Utilities.Pdf;
using NReco.PhantomJS;
using RadialReview.Properties;
using PdfSharp.Pdf.IO;
using System.Collections;

namespace RadialReview.Accessors {
	public class LayoutHelper {
		private readonly PdfDocument _document;
		private readonly XUnit _topPosition;
		private readonly XUnit _bottomMargin;
		private XUnit _currentPosition;
		public LayoutHelper(PdfDocument document, XUnit topPosition, XUnit bottomMargin) {
			_document = document;
			_topPosition = topPosition;
			_bottomMargin = bottomMargin;
			// Set a value outside the page - a new page will be created on the first request.
			_currentPosition = bottomMargin + 10000;
		}
		public XUnit GetLinePosition(XUnit requestedHeight) {
			return GetLinePosition(requestedHeight, -1f);
		}
		public XUnit GetLinePosition(XUnit requestedHeight, XUnit requiredHeight) {
			XUnit required = requiredHeight == -1f ? requestedHeight : requiredHeight;
			if (_currentPosition + required > _bottomMargin)
				CreatePage();
			XUnit result = _currentPosition;
			_currentPosition += requestedHeight;
			return result;
		}
		public XGraphics Gfx { get; private set; }
		public PdfPage Page { get; private set; }

		void CreatePage() {
			Page = _document.AddPage();
			Page.Size = PdfSharp.PageSize.A4;
			Gfx = XGraphics.FromPdfPage(Page);
			_currentPosition = _topPosition;
		}
	}

	public class DocumentMerger {

		protected List<object> docs { get; set; }

		public void AddDoc(PdfDocument doc) {
			docs.Add(doc);
		}
		public void AddDoc(Document doc) {
			docs.Add(doc);
		}

		public DocumentMerger() {
			docs = new List<object>();
		}

		protected void DrawNumber(XGraphics gfx, XFont font, int number) {

			var wmargin = 35;
			var hmargin = 22;

			var size = new XSize(gfx.PageSize.Width - wmargin * 2, gfx.PageSize.Height - hmargin * 2);

			gfx.DrawString(number.ToString(), font, XBrushes.Black, new XRect(new XPoint(wmargin, hmargin), size), XStringFormats.BottomRight);
		}


		public PdfDocument Flatten(string title, bool includeNumber) {
			DateTime now = DateTime.Now;
			//  filename = filename.ToLower().EndsWith(".pdf")?filename:filename+".pdf";
			PdfDocument document = new PdfDocument();
			document.Info.Title = title;
			document.Info.Author = "Traction Tools";
			document.Info.Keywords = "Traction Tools";
			document.Info.CreationDate = now;

			var pages = 0;
			XFont font = new XFont("Verdana", 10, XFontStyle.Regular);

			foreach (var doc in docs) {
				if (doc is PdfDocument) {
					var pdfDoc = (PdfDocument)doc;
					PdfDocument newPdfDoc;

					using (var stream = new MemoryStream()) {
						pdfDoc.Save(stream, false);
						newPdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
					}

					foreach (var p in newPdfDoc.Pages) {
						var page = document.AddPage(p);
						page.Width = p.Width;
						page.Height = p.Height;
						page.Orientation = p.Orientation;
						XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
						if (includeNumber)
							DrawNumber(gfx, font, pages + 1);

						pages += 1;
					}
				}
				if (doc is Document) {
					var mdoc = (Document)doc;

					DocumentRenderer docRenderer = new DocumentRenderer(mdoc);
					docRenderer.PrepareDocument();

					int pageCount = docRenderer.FormattedDocument.PageCount;
					for (int idx = 0; idx < pageCount; idx++) {
						PdfPage page = document.AddPage();

						var pageInfo = docRenderer.FormattedDocument.GetPageInfo(idx + 1);
						page.Width = pageInfo.Width;
						page.Height = pageInfo.Height;
						page.Orientation = pageInfo.Orientation;

						XGraphics gfx = XGraphics.FromPdfPage(page);
						gfx.MUH = PdfFontEncoding.Unicode;
						docRenderer.RenderPage(gfx, idx + 1);
						if (includeNumber)
							DrawNumber(gfx, font, pages + 1);
						pages += 1;
					}
				}
			}
			return document;

		}

	}


	public class PdfAccessor {

		static XRect GetRect(int index) {
			XRect rect = new XRect(0, 0, A4Width / 2 * 0.9, A4Height / 2 * 0.9);
			rect.X = (index % 2) * A4Width / 2 + A4Width * 0.05 / 2;
			rect.Y = (index / 2) * A4Height / 2 + A4Height * 0.05 / 2;
			return rect;
		}
		static double A4Width = XUnit.FromCentimeter(21).Point;
		static double A4Height = XUnit.FromCentimeter(29.7).Point;
		static double LetterWidth = XUnit.FromInch(8.5).Point;
		static double LetterHeight = XUnit.FromInch(11).Point;

		public static Document CreateDoc(UserOrganizationModel caller, string docTitle) {
			var document = new Document();

			document.Info.Title = docTitle;
			document.Info.Author = caller.GetName();
			document.Info.Comment = "Created with Traction® Tools";


			//document.DefaultPageSetup.PageFormat = PageFormat.Letter;
			document.DefaultPageSetup.Orientation = Orientation.Portrait;

			document.DefaultPageSetup.LeftMargin = Unit.FromInch(.5);
			document.DefaultPageSetup.RightMargin = Unit.FromInch(.5);
			document.DefaultPageSetup.TopMargin = Unit.FromInch(.5);
			document.DefaultPageSetup.BottomMargin = Unit.FromInch(.5);
			document.DefaultPageSetup.PageWidth = Unit.FromInch(8.5);
			document.DefaultPageSetup.PageHeight = Unit.FromInch(11);


			return document;
		}


		public static byte[] LoadImage(MemoryStream st) {
			using (Stream stream = new MemoryStream(st.ToArray())) {
				if (stream == null)
					throw new ArgumentException("No resource with name " + stream);

				int count = (int)stream.Length;
				byte[] data = new byte[count];
				stream.Read(data, 0, count);
				return data;
			}
		}

		public static Document GeneratePeopleAnalyzer(UserOrganizationModel caller, PeopleAnalyzer peopleAnalyzer) {

			var doc = CreateDoc(caller, "People Analyzer");

			//var sec = doc.AddSection();
			AddPeopleAnalyzer(doc, peopleAnalyzer);

			return doc;
		}

		public static void AddPeopleAnalyzer(Document doc, PeopleAnalyzer peopleAnalyzer, bool addPageNumber = true) {

			var section = AddTitledPage(doc, "People Analyzer - " + peopleAnalyzer.ReviewName, addPageNumber: addPageNumber);
			//var section = doc.AddSection();
			var table = section.AddTable();

			table.Style = "Table";
			table.Borders.Color = TableBlack;
			table.Borders.Width = 1;
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding = 0;

			var colCount = Math.Max(1, peopleAnalyzer.Values.Count) + 3;

			var size = Math.Max(.15, (8.5 - (1.5 + 1)) / colCount);


			//Who

			var column = table.AddColumn(Unit.FromInch(1.5));
			column.Format.Alignment = ParagraphAlignment.Left;

			//Values
			foreach (var v in peopleAnalyzer.Values) {
				column = table.AddColumn(Unit.FromInch(size));
				column.Format.Alignment = ParagraphAlignment.Center;
			}

			//G
			column = table.AddColumn(Unit.FromInch(size));
			column.Format.Alignment = ParagraphAlignment.Center;
			//W
			column = table.AddColumn(Unit.FromInch(size));
			column.Format.Alignment = ParagraphAlignment.Center;
			//C
			column = table.AddColumn(Unit.FromInch(size));
			column.Format.Alignment = ParagraphAlignment.Center;


			//rows
			var row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = TableGray;

			row.Height = Unit.FromInch(0.25);

			row.Cells[0].AddParagraph("Name");
			row.Cells[0].VerticalAlignment = VerticalAlignment.Bottom;
			row.Cells[0].Format.Font.Size = 8;


			var i = 0;
			for (i = 0; i < peopleAnalyzer.Values.Count; i++) {
				row.Cells[1 + i].AddParagraph("" + peopleAnalyzer.Values[i].CompanyValue);
				row.Cells[1 + i].VerticalAlignment = VerticalAlignment.Bottom;
				row.Cells[1 + i].Format.Font.Size = 8;
			}

			row.Cells[i + 1].AddParagraph("Gets It");
			row.Cells[i + 1].VerticalAlignment = VerticalAlignment.Bottom;
			row.Cells[i + 1].Format.Font.Size = 8;

			row.Cells[i + 2].AddParagraph("Wants It");
			row.Cells[i + 2].VerticalAlignment = VerticalAlignment.Bottom;
			row.Cells[i + 2].Format.Font.Size = 8;

			row.Cells[i + 3].AddParagraph("Capacity");
			row.Cells[i + 3].VerticalAlignment = VerticalAlignment.Bottom;
			row.Cells[i + 3].Format.Font.Size = 8;


			foreach (var r in peopleAnalyzer.Rows.OrderBy(x => peopleAnalyzer.GetUser(x).NotNull(y => y.GetName()))) {
				var user = peopleAnalyzer.GetUser(r);
				if (user == null)
					continue;

				if (string.IsNullOrWhiteSpace(user.GetName()))
					continue;

				row = table.AddRow();

				row.Cells[0].AddParagraph("" + user.GetName());
				row.Cells[0].Format.RightIndent = Unit.FromInch(.05);
				row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
				i = 1;
				foreach (var val in peopleAnalyzer.Values) {
					var thisValue = r.ValueRatings.Where(x => x.ValueId == val.Id).Select(x => x.Rating).ToList();
					var merge = ScatterScorer.MergeValueScores(thisValue, val);
					var text = merge.Merged.ToShortKey();
					row.Cells[i].AddParagraph("" + text);
					row.Cells[i].Shading.Color = merge.Merged.GetColor();
					i++;
				}

				var stateName = new Func<FiveState, string>(x => {
					if (x == FiveState.Indeterminate)
						return "";
					return (x == FiveState.Always || x == FiveState.Mostly) ? "Y" : "N";
				});

				var gwcKey = "g";
				var gwc = ScatterScorer.MergeRoleScores(r.RoleRatings.Where(x => x.GWC == gwcKey).Select(x => x.Rating).ToList());
				row.Cells[i + 0].AddParagraph("" + stateName(gwc));
				row.Cells[i + 0].Shading.Color = gwc.GetColor();

				gwcKey = "w";
				gwc = ScatterScorer.MergeRoleScores(r.RoleRatings.Where(x => x.GWC == gwcKey).Select(x => x.Rating).ToList());
				row.Cells[i + 1].AddParagraph("" + stateName(gwc));
				row.Cells[i + 1].Shading.Color = gwc.GetColor();

				gwcKey = "c";
				gwc = ScatterScorer.MergeRoleScores(r.RoleRatings.Where(x => x.GWC == gwcKey).Select(x => x.Rating).ToList());
				row.Cells[i + 2].AddParagraph("" + stateName(gwc));
				row.Cells[i + 2].Shading.Color = gwc.GetColor();

			}



		}

		public static void AddReviewPrintout(UserOrganizationModel caller, PdfDocument document, ReviewController.ReviewDetailsViewModel review) {

			var pageNum = 0;
			var AddPageNum = new Action<XGraphics, XRect>((g, pageDim) => {
				pageNum++;
				g.DrawString(review.Review.ReviewerUser.GetName() + "  |  " + pageNum, PdfChartAccessor._Font8, XBrushes.LightGray, new XPoint(pageDim.Width - 20, pageDim.Height - 20), XStringFormats.BottomRight);
			});

			PdfPage page = document.AddPage();

			page.Size = PdfSharp.PageSize.Letter;
			XGraphics gfx = XGraphics.FromPdfPage(page);
			//// HACK²
			gfx.MUH = PdfFontEncoding.Unicode;
			//gfx.MFEH = PdfFontEmbedding.Default;

			var sensitive = true;
			if (caller.Id == review.Review.ReviewerUserId)
				sensitive = false;

			var scatter = new ChartsEngine().ReviewScatter2(caller, review.Review.ReviewerUserId, review.Review.ForReviewContainer.Id, review.Review.ClientReview.ScatterChart.Groups, sensitive, review.Review.ClientReview.ScatterChart.IncludePrevious);

			var pageSize = new XRect(0, 0, Unit.FromInch(8.5), Unit.FromInch(11));
			var margin = Unit.FromInch(0.3);
			AddPageNum(gfx, pageSize);

			var w2 = pageSize.Width / 2.0;

			var gwcAnswers = review.AnswersAbout.Where(x => x is GetWantCapacityAnswer).Cast<GetWantCapacityAnswer>().ToList();
			var valAnswers = review.AnswersAbout.Where(x => x is CompanyValueAnswer).Cast<CompanyValueAnswer>().ToList();
			var rockAnswers = review.AnswersAbout.Where(x => x is RockAnswer).Cast<RockAnswer>().ToList();
			var feedbackIds = review.Review.ClientReview.FeedbackIds.ToListAlive().GroupBy(x => x.Value).Select(x => x.First()).ToList();
			var feedbackAnswers = review.AnswersAbout.Where(x => x is FeedbackAnswer).Cast<FeedbackAnswer>()
				.Where(x => !string.IsNullOrWhiteSpace(x.Feedback) && feedbackIds.Any(y => y.Value == x.Id)).ToList();


			var headerRect = PdfChartAccessor.DrawHeader(gfx, pageSize, review.Review, margin: margin);

			var top = headerRect.Height - margin;
			var leftColumnHeight = top;
			var rightColumnHeight = top;

			if (review.Review.ClientReview.IncludeScatterChart) {
				var r = new XRect(w2, top, w2, w2 * 0.8);
				var q = PdfChartAccessor.DrawQuadrant(scatter, gfx, r, centerHeight: false, margin: margin);
				rightColumnHeight += q.Height;
			}

			var addedPage2 = false;
			PdfPage page2;
			XGraphics gfxPage2 = null;

			if (review.Review.ClientReview.IncludeEvaluation) {
				var testDoc = new PdfDocument();
				var testPage = testDoc.AddPage();
				var tester = XGraphics.FromPdfPage(testPage);
				var r = new XRect(0, top, w2, 1);
				var roleRect = PdfChartAccessor.DrawRolesTable(gfx, r, gwcAnswers, margin: margin);

				leftColumnHeight += roleRect.Height;

				r = new XRect(w2, rightColumnHeight, w2, 1);
				//List<ValueBar> theBar = null;
				var valueRect = PdfChartAccessor.DrawValueTable(tester, r, review.Review.ReviewerUser, valAnswers, review.Supervisers, margin: margin);

				if (r.Top + valueRect.Height > pageSize.Height) {
					if (!addedPage2) {
						page2 = document.AddPage();
						addedPage2 = true;
						gfxPage2 = XGraphics.FromPdfPage(page2);
						AddPageNum(gfxPage2, pageSize);
						r = new XRect(w2, 0, w2, 1);
						rightColumnHeight = 0;
					}
					PdfChartAccessor.DrawValueTable(gfxPage2, r, review.Review.ReviewerUser, valAnswers, review.Supervisers, margin: margin);
				} else {
					PdfChartAccessor.DrawValueTable(gfx, r, review.Review.ReviewerUser, valAnswers, review.Supervisers, margin: margin);
				}

				rightColumnHeight += valueRect.Height;


				r = new XRect(0, leftColumnHeight, w2, 1);
				var rockRect = PdfChartAccessor.DrawRocksTable(gfx, r, rockAnswers, margin: margin);


				if (r.Top + rockRect.Height > pageSize.Height) {
					if (!addedPage2) {
						page2 = document.AddPage();
						addedPage2 = true;
						gfxPage2 = XGraphics.FromPdfPage(page2);
						AddPageNum(gfxPage2, pageSize);

						r = new XRect(0, 0, w2, 1);
						leftColumnHeight = 0;
					}
					PdfChartAccessor.DrawRocksTable(gfxPage2, r, rockAnswers, margin: margin);
				} else {
					PdfChartAccessor.DrawRocksTable(gfx, r, rockAnswers, margin: margin);
				}
				leftColumnHeight += rockRect.Height;
			}

			if (review.Review.ClientReview.IncludeScorecard && review.Review.ClientReview._ScorecardRecur.Scorecard.Measurables.Any()) {
				Document doc = new Document();
				var sc = GenerateScorecard(review.Review.ClientReview._ScorecardRecur, true);
				var sec = doc.AddSection();
				sec.Add(sc);

				var scorePage = document.AddPage();
				scorePage.Height = Math.Min(pageSize.Width, pageSize.Height);
				scorePage.Width = Math.Max(pageSize.Width, pageSize.Height);
				var scoreGfx = XGraphics.FromPdfPage(scorePage);

				DocumentRenderer docRenderer = new DocumentRenderer(doc);
				docRenderer.PrepareDocument();

				docRenderer.RenderObject(scoreGfx, XUnit.FromPoint(margin), XUnit.FromPoint(margin), XUnit.FromPoint(pageSize.Width - 2 * margin), sc);

				AddPageNum(scoreGfx, new XRect(0, 0, scorePage.Width, scorePage.Height));
			}

			var addedPage3 = false;
			PdfPage page3;
			XGraphics gfxPage3 = null;

			var availableRect = new XRect(pageSize.Left, pageSize.Top, pageSize.Width, pageSize.Height);

			if (feedbackAnswers.Any()) {
				if (!addedPage3) {
					addedPage3 = true;
					page3 = document.AddPage();
					gfxPage3 = XGraphics.FromPdfPage(page3);
					AddPageNum(gfxPage3, pageSize);
				}
				var rect = PdfChartAccessor.DrawFeedback(gfxPage3, availableRect, feedbackAnswers, review.ReviewContainer.AnonymousByDefault, margin: margin);
				availableRect = new XRect(pageSize.Left, rect.Bottom, pageSize.Width, Math.Max(pageSize.Height - rect.Bottom,1));
			}

			var addedPage4 = false;
			PdfPage page4;
			XGraphics gfxPage4 = null;

			if (review.Review.ClientReview.IncludeNotes && !string.IsNullOrWhiteSpace(review.Review.ClientReview.ManagerNotes)) {
				if (!addedPage3) {
					addedPage3 = true;
					page3 = document.AddPage();
					gfxPage3 = XGraphics.FromPdfPage(page3);
					AddPageNum(gfxPage3, pageSize);
				}
				var testDoc = new PdfDocument();
				var testPage = testDoc.AddPage();
				var tester = XGraphics.FromPdfPage(testPage);
				var notesSize = PdfChartAccessor.DrawNotes(tester, availableRect, review.Review.ClientReview.ManagerNotes, margin: margin);

				if (notesSize.Bottom > pageSize.Bottom) {
					addedPage4 = true;
					page4 = document.AddPage();
					gfxPage4 = XGraphics.FromPdfPage(page4);
					AddPageNum(gfxPage4, pageSize);
					PdfChartAccessor.DrawNotes(gfxPage4, pageSize, review.Review.ClientReview.ManagerNotes, margin: margin);
				} else {
					PdfChartAccessor.DrawNotes(gfxPage3, availableRect, review.Review.ClientReview.ManagerNotes, margin: margin);
				}
			}

		}

		public static PdfDocument GenerateReviewPrintout(UserOrganizationModel caller, ReviewController.ReviewDetailsViewModel review) {
			var name = review.Review.ReviewerUser.GetName();
			PdfDocument document = new PdfDocument();
			document.Info.Author = "Traction® Tools";
			document.Info.Title = name + " - " + review.ReviewContainer.ReviewName;
			document.Info.Creator = "" + caller.GetName();
			//document.Info.CreationDate = DateTime.UtcNow;
			document.Info.Keywords = "Traction Tools, " + name + ", " + review.ReviewContainer.ReviewName;
			AddReviewPrintout(caller, document, review);
			return document;

		}

		private static Color TableGray = new Color(100, 100, 100, 100);
		private static Color TableDark = new Color(50, 50, 50);
		private static Color TableBlack = new Color(0, 0, 0);

		protected static Section AddTitledPage(Document document, string pageTitle, Orientation orientation = Orientation.Portrait, bool addSection = true, bool addPageNumber = true) {
			Section section;

			if (addSection || document.LastSection == null) {
				section = document.AddSection();
				section.PageSetup.Orientation = orientation;
			} else {
				section = document.LastSection;
			}
			if (addPageNumber) {
				var paragraph = new Paragraph();
				paragraph.AddTab();

				paragraph.AddPageField();
				// Add paragraph to footer for odd pages.
				section.Footers.Primary.Add(paragraph);
				section.Footers.Primary.Format.SpaceBefore = Unit.FromInch(-0.2);
			}


			var frame = section.AddTextFrame();
			frame.Height = Unit.FromInch(.75);
			frame.Width = Unit.FromInch(7.5);
			if (orientation == Orientation.Landscape)
				frame.Width = Unit.FromInch(10);
			// frame.LineFormat.Color = TableGray;
			//frame.Left = ShapePosition.Center;

			frame.MarginRight = Unit.FromInch(1);
			frame.MarginLeft = Unit.FromInch(1);


			var title = frame.AddTable();
			title.Borders.Color = TableBlack;

			var size = Unit.FromInch(5.5);
			if (orientation == Orientation.Landscape)
				size = Unit.FromInch(8);
			var c = title.AddColumn(size);
			c.Format.Alignment = ParagraphAlignment.Center;
			var rr = title.AddRow();
			rr.Cells[0].AddParagraph(pageTitle);
			rr.Format.Font.Bold = true;
			//rr.Format.Font.Size = .4;
			rr.Format.Font.Name = "Arial Narrow";
			rr.Shading.Color = TableGray;
			rr.HeightRule = RowHeightRule.AtLeast;
			rr.VerticalAlignment = VerticalAlignment.Center;
			rr.Height = Unit.FromInch(0.4);
			rr.Format.Font.Size = Unit.FromInch(.2);

			return section;
		}

		public static Document AddL10(Document doc, AngularRecurrence recur, DateTime? lastMeeting, bool addPageNumber = false) {
			//CreateDoc(caller,"THE LEVEL 10 MEETING");
			var section = AddTitledPage(doc, "THE LEVEL 10 MEETING™", addPageNumber: addPageNumber);
			var p = section.Footers.Primary.AddParagraph("© 2003 - " + DateTime.UtcNow.Year + " EOS. All Rights Reserved.");
			p.Format.Font.Size = 8;
			p.Format.Font.Color = TableGray;

			//p =section.AddParagraph();
			//var f = p.AddFormattedText("The Weekly Agenda",TextFormat.Bold);
			//f.Font.Name = "Arial Narrow";
			//f.Font.Size = 16;
			//p.Format.LeftIndent = Unit.FromInch(.5);
			//p.Format.SpaceAfter =Unit.FromInch(.25);
			//var p = section.AddParagraph();
			//var t = section.AddTable();
			//t.AddColumn(Unit.FromInch(3.75));
			//t.AddColumn(Unit.FromInch(3.75));

			//var r = t.AddRow();
			//p = r.Cells[0].AddParagraph("Day: _____________________________ ");
			//p.Format.Alignment = ParagraphAlignment.Right;
			//p = r.Cells[1].AddParagraph(" Time: ________________");
			//p.Format.Alignment = ParagraphAlignment.Left;

			p = section.AddParagraph();
			p.Format.SpaceAfter = Unit.FromInch(.25 / 2);
			p.Format.SpaceBefore = Unit.FromInch(0);
			p.Format.Alignment = ParagraphAlignment.Center;
			p.Format.Font.Size = 14;
			p.AddFormattedText("Agenda:", TextFormat.Bold | TextFormat.Underline);

			var t = section.AddTable();
			t.Format.SpaceBefore = Unit.FromInch(.05);
			t.Format.LeftIndent = Unit.FromInch(1);
			t.AddColumn(Unit.FromInch(3.75));
			t.AddColumn(Unit.FromInch(2.25));

			var recurr = recur._Recurrence.Item;

			var r = t.AddRow();
			r.Cells[0].AddParagraph("Segue");
			r.Cells[1].AddParagraph((int)recurr.SegueMinutes + " Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("Scorecard");
			r.Cells[1].AddParagraph((int)recurr.ScorecardMinutes + " Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("Rock Review");
			r.Cells[1].AddParagraph((int)recurr.RockReviewMinutes + " Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("Customer/Employee Headlines");
			r.Cells[1].AddParagraph((int)recurr.HeadlinesMinutes + " Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("To-Do List");
			r.Cells[1].AddParagraph((int)recurr.TodoListMinutes + " Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;

			var todos = recur.Todos.Where(x => x.Complete == false || x.CompleteTime > lastMeeting).OrderBy(x => x.Owner.Name).ThenBy(x => x.DueDate).ToList();
			var issues = recur.IssuesList.Issues.Where(x => x.Complete == false).OrderByDescending(x => x.Priority).ThenBy(x => x.Name).ToList();


			//var fs = ResizeToFit(new Section(), Unit.FromInch(4.5), Unit.FromInch(5.23), (Cell, ffss) => {
			//	var o = new List<Paragraph>();
			//	for (var i = 0; i < todos.Count; i++) {
			//		var pp = new Paragraph();
			//		pp.Format.LeftIndent = Unit.FromInch(1.25);
			//		pp.AddText(todos[i].Name ?? "");
			//		pp.Format.TabStops.ClearAll();
			//		pp.Format.TabStops.AddTabStop(Unit.FromInch(6), TabAlignment.Right);
			//		pp.AddTab();
			//		pp.AddText(todos[i].Owner.NotNull(x => x.Initials) ?? "");
			//		o.Add(pp);
			//	}

			//	for (var i = 0; i < issues.Count; i++) {
			//		var pp = new Paragraph();
			//		pp.AddText(issues[i].Name ?? "");
			//		pp.Format.TabStops.ClearAll();
			//		pp.Format.TabStops.AddTabStop(Unit.FromInch(6), TabAlignment.Right);
			//		pp.AddTab();
			//		pp.AddText(issues[i].Owner.NotNull(x => x.Initials) ?? "");
			//		o.Add(pp);
			//	}
			//	return o;
			//}, maxFontSize: 12, minFontSize: 8);
			var fs = 10;

			for (var i = 0; i < todos.Count; i++) {
				var pp = new Table();
				pp.Format.SpaceAfter = 0;
				pp.AddColumn(Unit.FromInch(1.25));
				pp.AddColumn(Unit.FromInch(4.75 - .36));
				pp.AddColumn(Unit.FromInch(0.36));
				pp.Format.Font.Color = TableDark;
				var rr = pp.AddRow();
				pp.Format.Font.Size = fs;
				p = rr.Cells[1].AddParagraph(todos[i].Name ?? "");
				p.Format.FirstLineIndent = Unit.FromInch(-.1);
				p.Format.LeftIndent = Unit.FromInch(.1);
				rr.Cells[2].AddParagraph(todos[i].Owner.NotNull(x => x.Initials) ?? "");
				rr.Cells[2].Format.Alignment = ParagraphAlignment.Right;
				section.Add(pp);
			}

			t = section.AddTable();
			t.Format.LeftIndent = Unit.FromInch(1);
			t.AddColumn(Unit.FromInch(3.75));
			t.AddColumn(Unit.FromInch(2.25));
			t.Format.SpaceBefore = Unit.FromInch(.05);
			r = t.AddRow();
			r.Cells[0].AddParagraph("IDS");
			r.Cells[1].AddParagraph((int)recurr.IDSMinutes + " Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;

			for (var i = 0; i < issues.Count; i++) {
				var pp = new Table();

				pp.Format.SpaceAfter = 0;
				pp.AddColumn(Unit.FromInch(1.25));
				pp.AddColumn(Unit.FromInch(4.75 - .35));
				pp.AddColumn(Unit.FromInch(0.35));
				var rr = pp.AddRow();
				//pp.Format.LeftIndent = Unit.FromInch(1.25);
				pp.Format.Font.Size = fs;
				pp.Format.Font.Color = TableDark;
				p = rr.Cells[1].AddParagraph(issues[i].Name ?? "");
				p.Format.FirstLineIndent = Unit.FromInch(-.1);
				p.Format.LeftIndent = Unit.FromInch(.1);
				rr.Cells[2].AddParagraph(issues[i].Owner.NotNull(x => x.Initials) ?? "");
				rr.Cells[2].Format.Alignment = ParagraphAlignment.Right;
				section.Add(pp);
			}
			t = section.AddTable();
			t.TopPadding = Unit.FromInch(.05);
			t.Format.LeftIndent = Unit.FromInch(1);
			t.AddColumn(Unit.FromInch(3.75));
			t.AddColumn(Unit.FromInch(2.25));
			r = t.AddRow();
			r.Cells[0].AddParagraph("Conclude");
			r.Cells[1].AddParagraph((int)recurr.ConclusionMinutes + " Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			p = r.Cells[0].AddParagraph("Recap To-Do List");
			p.Format.LeftIndent = Unit.FromInch(1 + 3 / 8.0);
			p = r.Cells[0].AddParagraph("Cascading messages");
			p.Format.LeftIndent = Unit.FromInch(1 + 3 / 8.0);
			p = r.Cells[0].AddParagraph("Rating 1-10");
			p.Format.LeftIndent = Unit.FromInch(1 + 3 / 8.0);

			return doc;
		}

		public static void AddTodos(UserOrganizationModel caller, Document doc, AngularRecurrence recur, bool addPageNumber = true) {
			//var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

			//return SetupDoc(caller, caller.Organization.Settings.RockName);

			var section = AddTitledPage(doc, "To-do List", addPageNumber: addPageNumber);

			var format = caller.Organization.NotNull(x => x.Settings.NotNull(y => y.GetDateFormat())) ?? "MM-dd-yyyy";

			var table = section.AddTable();
			table.Format.Font.Size = 9;
			table.Style = "Table";
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding = 0;

			//Number
			var column = table.AddColumn(Unit.FromInch(/*0.2*/0));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Due
			column = table.AddColumn(Unit.FromInch(0.7));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Who
			column = table.AddColumn(Unit.FromInch(1 + 0.2));
			column.Format.Alignment = ParagraphAlignment.Center;


			//Rock
			column = table.AddColumn(Unit.FromInch(4.85 + .75));
			column.Format.Alignment = ParagraphAlignment.Left;

			var row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = TableGray;
			row.Height = Unit.FromInch(0.25);

			row.Cells[1].AddParagraph("Due");
			row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

			row.Cells[2].AddParagraph("Owner");
			row.Cells[2].VerticalAlignment = VerticalAlignment.Center;

			row.Cells[3].AddParagraph("To-dos");
			row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
			row.Cells[3].Format.Alignment = ParagraphAlignment.Left;

			var mn = 1;
			foreach (var m in recur.Todos.Where(x => x.Complete == false).OrderBy(x => x.Owner.Name).ThenBy(x => x.DueDate)) {

				row = table.AddRow();
				row.HeadingFormat = false;
				row.Format.Alignment = ParagraphAlignment.Center;
				row.BottomPadding = Unit.FromInch(.15);
				row.Format.Font.Bold = false;
				row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
																		 //row.Shading.Color = TableBlue;
				row.HeightRule = RowHeightRule.AtLeast;
				row.VerticalAlignment = VerticalAlignment.Top;
				row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
				//row.Cells[0].AddParagraph("" + mn + ".");
				row.Cells[1].AddParagraph(m.DueDate.NotNull(x => x.Value.ToString(format)) ?? "Not-set");
				row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
				row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
				row.Cells[3].AddParagraph(m.Name);
				row.Cells[3].Format.Alignment = ParagraphAlignment.Left;
				mn++;
			}
		}

		public static void AddIssues(UserOrganizationModel caller, Document doc, AngularRecurrence recur, bool mergeWithTodos, bool addPageNumber = true) {
			//var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

			//return SetupDoc(caller, caller.Organization.Settings.RockName);

			var section = AddTitledPage(doc, "Issues List", addSection: !mergeWithTodos, addPageNumber: addPageNumber);

			var table = section.AddTable();
			table.Format.Font.Size = 9;
			table.Style = "Table";
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding = 0;

			//Number
			var column = table.AddColumn(Unit.FromInch(/*0.2*/0));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Priority
			var size = Unit.FromInch(0.0);
			var isPriority = recur.IssuesList.Prioritization == PrioritizationType.Priority;
			if (isPriority)
				size = Unit.FromInch(0.7);
			column = table.AddColumn(size);
			column.Format.Alignment = ParagraphAlignment.Center;

			//Who
			column = table.AddColumn(Unit.FromInch(1));
			column.Format.Alignment = ParagraphAlignment.Center;


			//Issue
			column = table.AddColumn(Unit.FromInch(4.85 + .75 + (isPriority ? 0 : .7) + 0.2));
			column.Format.Alignment = ParagraphAlignment.Left;

			var row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = TableGray;
			row.Height = Unit.FromInch(0.25);

			row.Cells[1].AddParagraph(isPriority ? "Priority" : "");
			row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

			row.Cells[2].AddParagraph("Owner");
			row.Cells[2].VerticalAlignment = VerticalAlignment.Center;

			row.Cells[3].AddParagraph("Issue");
			row.Cells[3].VerticalAlignment = VerticalAlignment.Center;

			var mn = 1;
			foreach (var m in recur.IssuesList.Issues.Where(x => x.Complete == false).OrderByDescending(x => x.Priority).ThenBy(x => x.Name)) {

				row = table.AddRow();
				row.HeadingFormat = false;
				row.Format.Alignment = ParagraphAlignment.Center;

				row.Format.Font.Bold = false;
				row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
																		 //row.Shading.Color = TableBlue;
				row.HeightRule = RowHeightRule.AtLeast;
				row.VerticalAlignment = VerticalAlignment.Center;
				row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
				//row.Cells[0].AddParagraph("" + mn + ".");

				var p = "";
				if (isPriority) {
					if (m.Priority >= 1 && m.Priority <= 3) {
						for (var i = 0; i < m.Priority; i++)
							p += "*";//"★";
					} else if (m.Priority > 3) {
						p = "* x" + m.Priority;
					}
					//row.Cells[1].Format.Font.Name = "Arial";
					row.Cells[1].AddParagraph(p);
				}

				//if (m.Priority >= 1)
				//{
				//    var location = System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\Resources\\Star.png";
				//    row.Cells[1].AddImage(location);
				//    row.Cells[1].AddParagraph(" x"+m.Priority);
				//}

				row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
				row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
				row.Cells[3].AddParagraph(m.Name);
				row.Cells[3].Format.Alignment = ParagraphAlignment.Left;
				mn++;
			}
		}

		public static void AddRocks(UserOrganizationModel caller, Document doc, AngularRecurrence recur, AngularVTO vto, bool addPageNumber = true) {
			//var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

			//return SetupDoc(caller, caller.Organization.Settings.RockName);

			var section = AddTitledPage(doc, "Quarterly " + caller.Organization.Settings.RockName, Orientation.Landscape, addPageNumber: addPageNumber);
			Table table;
			double mult;
			Row row;
			int mn;
			Column column;

			var format = caller.NotNull(x => x.Organization.NotNull(y => y.Settings.NotNull(z => z.GetDateFormat()))) ?? "MM-dd-yyyy";

			var addVTO = true;
			if (addVTO && vto != null) {
				table = section.AddTable();
				column = table.AddColumn(Unit.FromInch(5.0));
				column.Format.Alignment = ParagraphAlignment.Right;

				table.AddColumn(Unit.FromInch(5.0));
				column.Format.Alignment = ParagraphAlignment.Right;

				row = table.AddRow();
				var p1 = new Paragraph();
				p1.AddFormattedText("Future Date:", TextFormat.Bold);
				row.Cells[0].Add(p1);
				var p2 = new Paragraph();
				p2.AddText(vto.QuarterlyRocks.FutureDate.NotNull(x => x.Value.ToString(format)) ?? "");
				row.Cells[1].Add(p2);


				row = table.AddRow();
				var p3 = new Paragraph();
				p3.AddFormattedText("Revenue:", TextFormat.Bold);
				row.Cells[0].Add(p3);
				var p4 = new Paragraph();
				p4.AddText(vto.QuarterlyRocks.Revenue ?? "");
				row.Cells[1].Add(p4);

				row = table.AddRow();
				var p5 = new Paragraph();
				p5.AddFormattedText("Profit:", TextFormat.Bold);
				row.Cells[0].Add(p5);
				var p6 = new Paragraph();
				p6.AddText(vto.QuarterlyRocks.Profit ?? "");
				row.Cells[1].Add(p6);

				row = table.AddRow();
				var p7 = new Paragraph();
				p7.AddFormattedText("Measurables:", TextFormat.Bold);
				row.Cells[0].Add(p7);
				var p8 = new Paragraph();
				p8.AddText(vto.QuarterlyRocks.Measurables ?? "");
				row.Cells[1].Add(p8);

				row = table.AddRow();
				row.Height = Unit.FromPoint(25);
			}





			if (recur.Rocks.Any(x => x.CompanyRock ?? false)) {
				table = section.AddTable();
				table.Format.Font.Size = 9;

				table.Style = "Table";
				table.Rows.LeftIndent = Unit.FromInch(1.25);

				table.LeftPadding = 0;
				table.RightPadding = 0;

				table.Format.Alignment = ParagraphAlignment.Center;

				mult = 1.0;
				//Number
				column = table.AddColumn(Unit.FromInch(/*0.2*/0.001 * mult));
				column.Format.Alignment = ParagraphAlignment.Center;
				//Due
				column = table.AddColumn(Unit.FromInch(/*0.7*/0.001 * mult));
				column.Format.Alignment = ParagraphAlignment.Center;
				//Who
				column = table.AddColumn(Unit.FromInch(1 + .2 * mult));
				column.Format.Alignment = ParagraphAlignment.Center;
				//Completion
				column = table.AddColumn(Unit.FromInch(0.75 * mult));
				column.Format.Alignment = ParagraphAlignment.Center;
				//Rock
				column = table.AddColumn(Unit.FromInch((4.85 + .7) * mult));
				column.Format.Alignment = ParagraphAlignment.Left;

				row = table.AddRow();
				row.HeadingFormat = true;
				row.Format.Alignment = ParagraphAlignment.Center;
				row.Format.Font.Bold = true;
				row.Shading.Color = TableGray;
				row.Height = Unit.FromInch(0.25);

				row.Cells[1].AddParagraph(""/*"Due"*/);
				row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

				row.Cells[2].AddParagraph("Owner");
				row.Cells[2].VerticalAlignment = VerticalAlignment.Center;

				row.Cells[3].AddParagraph("Status");
				row.Cells[3].VerticalAlignment = VerticalAlignment.Center;

				row.Cells[4].AddParagraph("Company Rock");
				row.Cells[4].VerticalAlignment = VerticalAlignment.Center;

				mn = 1;
				foreach (var m in recur.Rocks.Where(x => x.CompanyRock == true).OrderBy(x => x.Owner.Name).ThenBy(x => x.DueDate)) {

					row = table.AddRow();
					row.BottomPadding = Unit.FromInch(.05);
					row.HeadingFormat = false;
					row.Format.Alignment = ParagraphAlignment.Center;

					row.Format.Font.Bold = false;
					//row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
					//row.Shading.Color = TableBlue;
					row.HeightRule = RowHeightRule.AtLeast;
					row.VerticalAlignment = VerticalAlignment.Center;
					row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
					//row.Cells[0].AddParagraph("" + mn + ".");
					//row.Cells[1].AddParagraph(m.DueDate.NotNull(x => x.Value.ToString(format)) ?? "Not-set");
					row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
					row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
					row.Cells[3].AddParagraph("" + m.Completion.NotNull(x => x.Value.GetDisplayName()));
					row.Cells[3].Format.Font.Bold = m.Completion == RockState.AtRisk;


					//Update below also
					switch (m.Completion) {
						case RockState.OnTrack:
							row.Cells[3].Format.Font.Color = Colors.DarkBlue;
							break;
						case RockState.AtRisk:
							row.Cells[3].Format.Font.Color = Colors.DarkRed;
							break;
						case RockState.Complete:
							row.Cells[3].Format.Font.Color = Colors.DarkGreen;
							break;
						default:
							break;
					}


					row.Cells[3].Format.Alignment = ParagraphAlignment.Center;
					row.Cells[4].AddParagraph("" + m.NotNull(x => x.Name));
					row.Cells[4].Format.Alignment = ParagraphAlignment.Left;
					mn++;
				}
				row = table.AddRow();
				row.HeightRule = RowHeightRule.AtLeast;
				row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0));
			}


			table = section.AddTable();
			table.Format.Font.Size = 9;
			table.Style = "Table";
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding = 0;


			mult = 10.0 / 7.5;
			//Number
			column = table.AddColumn(Unit.FromInch(0.001 * mult));//*0.2* 0.001 * mult));
			column.Format.Alignment = ParagraphAlignment.Center;
			//Due
			column = table.AddColumn(Unit.FromInch(/*0.7*/0.001 * mult));
			column.Format.Alignment = ParagraphAlignment.Center;
			//Who
			column = table.AddColumn(Unit.FromInch(1 + 0.2 * mult));
			column.Format.Alignment = ParagraphAlignment.Center;
			//Completion
			column = table.AddColumn(Unit.FromInch(0.75 * mult));
			column.Format.Alignment = ParagraphAlignment.Center;
			//Rock
			column = table.AddColumn(Unit.FromInch((4.85 + .7) * mult));
			column.Format.Alignment = ParagraphAlignment.Left;

			row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = TableGray;
			row.Height = Unit.FromInch(0.25);

			row.Cells[1].AddParagraph(/*"Due"*/"");
			row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

			row.Cells[2].AddParagraph("Owner");
			row.Cells[2].VerticalAlignment = VerticalAlignment.Center;

			row.Cells[3].AddParagraph("Status");
			row.Cells[3].VerticalAlignment = VerticalAlignment.Center;

			row.Cells[4].AddParagraph("Rock");
			row.Cells[4].Format.Alignment = ParagraphAlignment.Left;
			row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
			//table.Format.Font.Size = Unit.FromInch(.1); // --- 1/16"
			mn = 1;
			foreach (var m in recur.Rocks.OrderBy(x => x.Owner.Name).ThenBy(x => x.DueDate)) {

				row = table.AddRow();
				row.HeadingFormat = false;
				//row.Format.Alignment = ParagraphAlignment.Center;

				//row.Format.Font.Bold = false;
				//row
				//row.Shading.Color = TableBlue;
				row.HeightRule = RowHeightRule.AtLeast;
				row.BottomPadding = Unit.FromInch(.05);
				// row.VerticalAlignment = VerticalAlignment.Center;
				row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
				// row.Cells[0].AddParagraph("" + mn + ".");
				//row.Cells[1].AddParagraph(m.DueDate.NotNull(x => x.Value.ToString(format)) ?? "Not-set");
				// row.Cells[1].Format.Font.Size = Unit.FromInch(.1);
				row.Cells[2].AddParagraph("" + m.Owner.NotNull(x => x.Name));
				row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
				//row.Cells[2].Format.Font.Size = Unit.FromInch(.1);
				row.Cells[3].AddParagraph("" + m.Completion.NotNull(x => x.Value.GetDisplayName()));
				row.Cells[3].Format.Font.Bold = m.Completion == RockState.AtRisk;



				//Update above also
				switch (m.Completion) {
					case RockState.OnTrack:
						row.Cells[3].Format.Font.Color = Colors.DarkBlue;
						break;
					case RockState.AtRisk:
						row.Cells[3].Format.Font.Color = Colors.DarkRed;
						break;
					case RockState.Complete:
						row.Cells[3].Format.Font.Color = Colors.DarkGreen;
						break;
					default:
						break;
				}


				row.Cells[3].Format.Alignment = ParagraphAlignment.Center;
				//row.Cells[3].Format.Font.Size = Unit.FromInch(.1);
				row.Cells[4].AddParagraph("" + m.Name);//.Substring(0, (int)(m.Name.Length)));
													   //row.Cells[4].Format.Font.Size = 10;// Unit.FromInch(.1);
													   //row.Cells[4].Format.Alignment = ParagraphAlignment.Left;
													   //row.Cells[4].Format.KeepTogether = false;
				mn++;
			}
		}

		private static Table GenerateScorecard(AngularRecurrence recur, bool includeDisabled = false) {

			var table = new Table();
			table.Style = "Table";
			table.Borders.Color = TableBlack;
			table.Borders.Width = 1;
			/*table.Borders.Left.Width = 0.25;
            table.Borders.Right.Width = 0.25;
            table.Borders.Top.Width = 7.0/8.0;*/
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding = 0;


			//Number
			var column = table.AddColumn(Unit.FromInch(0/*0.25*/));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Who

			column = table.AddColumn(Unit.FromInch(0.75));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Measurable
			column = table.AddColumn(Unit.FromInch(2.0 + .25));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Goal
			column = table.AddColumn(Unit.FromInch(0.75));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Measured
			for (var i = 0; i < 13; i++) {
				column = table.AddColumn(Unit.FromInch(6.25 / 13.0));
				column.Format.Alignment = ParagraphAlignment.Center;
			}

			//rows

			var row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = TableGray;
			row.Height = Unit.FromInch(0.25);

			row.Cells[1].AddParagraph("Who");
			row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;

			row.Cells[2].AddParagraph("Measurable");
			row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;

			row.Cells[3].AddParagraph("Goal");
			row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;

			var numWeeks = 13;

			var weeks = recur.Scorecard.Weeks.OrderByDescending(x => x.ForWeekNumber).Take(numWeeks).OrderBy(x => x.ForWeekNumber);
			var ii = 0;
			foreach (var w in weeks) {
				row.Cells[4 + ii].AddParagraph(w.DisplayDate.ToString("MM/dd/yy") + " to " + w.DisplayDate.AddDays(6).ToString("MM/dd/yy"));
				row.Cells[4 + ii].Format.Font.Size = Unit.FromInch(0.07);
				row.Cells[4 + ii].Format.Font.Size = Unit.FromInch(0.07);
				ii++;
			}
			//var r = new Random();

			var measurables = recur.Scorecard.Measurables.OrderBy(x => x.Ordering).Where(x => includeDisabled || !(x.Disabled ?? false) && !x.IsDivider);
			var mn = 1;

			//for (var k = 0; k < 2; k++){
			foreach (var m in measurables) {

				row = table.AddRow();
				row.HeadingFormat = false;
				row.Format.Alignment = ParagraphAlignment.Center;

				row.Format.Font.Bold = false;
				row.Format.Font.Size = Unit.FromInch(0.128 * 2.0 / 3.0); // --- 1/16"
																		 //row.Shading.Color = TableBlue;
				row.HeightRule = RowHeightRule.AtLeast;
				row.VerticalAlignment = VerticalAlignment.Center;
				row.Height = Unit.FromInch((6 * 8 + 5.0) / (8 * 16.0) / 2);
				//row.Cells[0].AddParagraph("" + mn + ".");
				//row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
				row.Cells[1].AddParagraph(m.Owner.Name + "");
				row.Cells[2].AddParagraph(m.Name + "");
				row.Cells[2].Format.LeftIndent = Unit.FromInch(.1);
				row.Cells[2].Format.Alignment = ParagraphAlignment.Left;

				var modifier = m.Modifiers ?? (RadialReview.Models.Enums.UnitType.None);

				row.Cells[3].AddParagraph((m.Direction ?? LessGreater.LessThan).ToSymbol() + " " + modifier.Format(m.Target ?? 0));
				ii = 0;
				foreach (var w in weeks) {
					var found = recur.Scorecard.Scores.FirstOrDefault(x => x.ForWeek == w.ForWeekNumber && x.Measurable.Id == m.Id);
					if (found != null && found.Measured.HasValue) {
						var val = found.Measured ?? 0;
						var cell = row.Cells[4 + ii];
						cell.AddParagraph("" + modifier.Format(val.KiloFormat()));
						var dir = found.Direction ?? m.Direction;
						var target = found.Target;
						if (dir == null) {
							dir = m.Direction;
							target = m.Target;
						}
						if (dir != null) {
							if (dir.Value.MeetGoal(target ?? 0, found.AltTarget ?? m.AltTarget, val)) {
								cell.Shading.Color = Color.FromArgb(255, 223, 240, 216); //Colors.LightGreen;// Color.FromCmyk(0.0708, 0.0, 0.1, .0588);
							} else {
								cell.Shading.Color = Color.FromArgb(255, 255, 236, 242);//Colors.LightSalmon;// Color.FromCmyk(0, 0.0826, 0.0826, .0510);
							}
						}

					}
					ii++;
				}
				mn += 1;
			}
			return table;
		}

		public static bool AddScorecard(Document doc, AngularRecurrence recur, bool addPageNumber = true) {
			if (recur.Scorecard.Measurables.Any()) {
				var section = AddTitledPage(doc, "Scorecard", Orientation.Landscape, addPageNumber: addPageNumber);
				var TableGray = new Color(100, 100, 100, 100);
				var TableBlack = new Color(0, 0, 0);
				var table = GenerateScorecard(recur);
				section.Add(table);
				return true;
			}
			return false;
		}

		private static List<Paragraph> AddVtoSectionHeader(IVtoSectionHeader section, Unit fontSize, string dateformat) {
			var o = new List<Paragraph>();

			var futureDate = new Paragraph();
			o.Add(futureDate);
			futureDate.Format.SpaceBefore = Unit.FromPoint(.9 * fontSize.Point);
			futureDate.Format.Font.Name = "Arial Narrow";
			futureDate.Format.Font.Size = fontSize;
			futureDate.AddFormattedText("Future Date: ", TextFormat.Bold);
			if (section.FutureDate.HasValue)
				futureDate.AddFormattedText(section.FutureDate.Value.ToString(dateformat), TextFormat.NotBold);


			var revenue = new Paragraph();
			revenue.Format.SpaceBefore = Unit.FromPoint(.3 * fontSize.Point);
			o.Add(revenue);
			revenue.AddFormattedText("Revenue: ", TextFormat.Bold);
			revenue.Format.Font.Name = "Arial Narrow";
			revenue.Format.Font.Size = fontSize;
			if (section.Revenue != null) {
				//revenue.AddFormattedText(string.Format(Thread.CurrentThread.CurrentCulture, "{0:c0}", section.Revenue.Value), TextFormat.NotBold);
				revenue.AddFormattedText(section.Revenue, TextFormat.NotBold);
			}

			var profit = new Paragraph();
			o.Add(profit);
			profit.Format.SpaceBefore = Unit.FromPoint(.3 * fontSize.Point);
			profit.AddFormattedText("Profit: ", TextFormat.Bold);
			profit.Format.Font.Name = "Arial Narrow";
			profit.Format.Font.Size = fontSize;
			if (section.Profit != null) {
				//profit.AddFormattedText(string.Format(Thread.CurrentThread.CurrentCulture, "{0:c0}", section.Profit.Value), TextFormat.NotBold);
				profit.AddFormattedText(section.Profit, TextFormat.NotBold);
			}

			var measurables = new Paragraph();
			measurables.Format.SpaceBefore = Unit.FromPoint(.3 * fontSize.Point);
			measurables.Format.SpaceAfter = Unit.FromPoint(.6 * fontSize.Point);
			measurables.Format.Font.Name = "Arial Narrow";
			measurables.Format.Font.Size = fontSize;
			o.Add(measurables);
			measurables.AddFormattedText("Measurables: ", TextFormat.Bold);
			if (section.Measurables != null)
				measurables.AddFormattedText(section.Measurables, TextFormat.NotBold);

			return o;
		}

		static string MigraDocFilenameFromByteArray(byte[] image) {
			return "base64:" + Convert.ToBase64String(image);
		}

		public class YSize {
			public YSize(XSize ptSize) {
				Width = Unit.FromPoint(ptSize.Width);
				Height = Unit.FromPoint(ptSize.Height);
			}
			public YSize(Unit width, Unit height) {
				Width = width;
				Height = height;
			}

			public readonly static YSize Empty = new YSize(0, 0);
			public Unit Height { get; set; }
			public Unit Width { get; set; }
		}

		private static YSize GetSize(DocumentObject o, Unit width) {
			var ctx = XGraphics.CreateMeasureContext(new XSize(width, Unit.FromInch(1000)), XGraphicsUnit.Inch, XPageDirection.Downwards);
			var size = GetSize(ctx, o, GetFontFamily(o), GetFontSize(o), width/* Unit.FromInch(3.47)*/);
			return size;// new YSize(Unit.FromInch(size.Width /** 0.166044*/), /*Unit.FromInch(*/size.Height /** 0.166044)*/);
		}
		private static YSize GetSize(List<DocumentObject> os, Unit width) {
			var ctx = XGraphics.CreateMeasureContext(new XSize(width, Unit.FromInch(1000)), XGraphicsUnit.Inch, XPageDirection.Downwards);
			var size = new YSize(0, 0);
			foreach (var o in os) {
				var s = GetSize(ctx, o, GetFontFamily(o), GetFontSize(o), width/* Unit.FromInch(3.47)*/);
				size.Width = Math.Max(size.Width, s.Width);
				size.Height += s.Height;
			}
			return size;// new YSize(Unit.FromInch(size.Width * 0.166044), Unit.FromInch(size.Height * 0.166044));
		}

		private static YSize GetSize(XGraphics ctx, DocumentObject o, String fontName, Unit fontSize, Unit maxWidth) {
			var s = new YSize(0, 0);

			if (o is FormattedText) {
				var txt = (FormattedText)o;
				foreach (DocumentObject e in txt.Elements) {
					var fn = string.IsNullOrWhiteSpace(txt.FontName) ? fontName : txt.FontName;
					var size = GetSize(ctx, e, fn, fontSize, maxWidth);
					s.Width = Math.Max(s.Width, size.Width);
					s.Height += size.Height;
				}
			} else if (o is Text) {
				var txt = (Text)o;
				var wrapper = new PdfWordWrapper(ctx, maxWidth);
				wrapper.Add(txt.Content, new XFont(fontName, fontSize), XBrushes.Black);
				wrapper.Process();
				s.Width = wrapper.Size.Width;
				s.Height += wrapper.Size.Height;
			} else if (o is Paragraph) {
				var para = (Paragraph)o;
				foreach (DocumentObject e in para.Elements) {
					var family = para.Format.Font.Name;
					if (string.IsNullOrWhiteSpace(family))
						family = fontName;
					var size = GetSize(ctx, e, family, fontSize, maxWidth);
					s.Width = Math.Max(s.Width, size.Width);
					s.Height += size.Height;
				}
				s.Width += (para.Format.LeftIndent + para.Format.RightIndent);//(para.Format.LeftIndent.Inch + para.Format.RightIndent.Inch) * 6.0225;
				s.Height += (para.Format.SpaceBefore + para.Format.SpaceAfter);// (para.Format.SpaceBefore.Inch + para.Format.SpaceAfter.Inch) * 6.0225;
			} else if (o is Table) {
				var table = (Table)o;
				var family = table.Format.Font.Name;
				var h = 0.0;
				var w = 0.0;
				var maxH = new Unit(0.0);
				for (var i = 0; i < table.Rows.Count; i++) {
					for (var j = 0; j < table.Rows[i].Cells.Count; j++) {
						var curH = new Unit(0.0);
						for (var k = 0; k < table.Rows[i].Cells[j].Elements.Count; k++) {
							var size = GetSize(ctx, table.Rows[i].Cells[j].Elements[k], family, fontSize, table.Columns[j].Width);
							curH += size.Height;
						}
						maxH = Math.Max(maxH, curH);
					}
					h += Math.Max(table.Rows[i].Height/*.Inch * 6.0225*/, maxH);
					maxH = 0;
				}
				for (var i = 0; i < table.Columns.Count; i++) {
					w += table.Columns[i].Width/*.Inch * 6.0225*/;
				}
				s.Width += w;
				s.Height += h;
			} else if (o is Row) {

				var row = (Row)o;
				var family = row.Format.Font.Name;
				var h = 0.0;
				var w = 0.0;
				var maxH = new Unit(0.0);
				s.Height += Math.Max(row.Height, maxH);
				//Width is not calculated.

				s.Width += w;
				s.Height += h;
			} else if (o is Cell) {
				var cell = (Cell)o;
				var family = cell.Format.Font.Name;
				var h = 0.0;
				var w = 0.0;
				var maxH = new Unit(0.0);
				var rowMin = new Unit(0.0);
				if (cell.Row != null && cell.Row.HeightRule == RowHeightRule.AtLeast) {
					rowMin = cell.Row.Height;
				}
				for (var i = 0; i < cell.Elements.Count; i++) {
					var size = GetSize(cell.Elements[i], maxWidth);
					s.Height += Math.Max(rowMin, size.Height);
				}
				//Width is not calculated.               
			} else if (o is Character) {

			} else {
				throw new Exception("donno this type:" + o.NotNull(x => x.GetType()));
			}
			return s;
		}

		public static List<Unit> GetRowHeights(Table table) {
			var doc = new Document();
			var sec = doc.AddSection();
			var clone = table.Clone();
			sec.Add(clone);
			var renderer = new DocumentRenderer(doc);
			renderer.PrepareDocument();
			var o = new List<Unit>();
			for (var i = 0; i < clone.Rows.Count; i++) {
				var row = clone.Rows[i];
				o.Add(row.Height);
			}
			return o;
		}

		public static List<Unit> GetRowHeights(List<Row> rows, Unit maxWidth) {
			if (!rows.Any())
				return new List<Unit>();

			var table = new Table();
			var first = rows.First();

			foreach (var c in first.Cells) {
				table.AddColumn();
			}

			foreach (var row in rows) {
				var tr = table.AddRow();
				for (var i = 0; i < row.Cells.Count; i++) {
					for (var ei = 0; ei < row.Cells[i].Elements.Count; ei++) {
						tr.Cells[i].Elements.Add((DocumentObject)row.Cells[i].Elements[ei].Clone());
					}
				}
			}

			return GetRowHeights(table);
		}

		private static Unit GetFontSize(DocumentObject p) {
			Unit? size = null;
			if (p is Paragraph) {
				size = ((Paragraph)p).Format.Font.Size;
			} else if (p is Table) {
				var table = (Table)p;
				size = table.Format.Font.Size;
			}
			if (size == null)
				size = 12;
			return size.Value;
		}

		private static string GetFontFamily(DocumentObject p) {
			string family = null;
			if (p is Paragraph) {
				family = ((Paragraph)p).Format.Font.Name;
			} else if (p is Table) {
				var table = (Table)p;
				family = table.Format.Font.Name;
			}
			if (string.IsNullOrWhiteSpace(family))
				family = "Arial Narrow";
			return family;
		}

		public static List<ItemHeight> GetHeights<T>(Unit width, IEnumerable<T> paragraphs, Func<T, DocumentObject> selector = null, Unit? extraHeight = null) where T : DocumentObject {
			var ctx = XGraphics.CreateMeasureContext(new XSize(width.Inch, Unit.FromInch(1000)), XGraphicsUnit.Inch, XPageDirection.Downwards);
			return paragraphs.Select(xx => {
				DocumentObject x = (selector == null) ? xx : selector(xx);
				var f = GetFontFamily(x);
				var s = GetFontSize(x);
				var size = GetSize(ctx, x, f, s, width);
				Unit totalHeight = size.Height + (extraHeight ?? new Unit(0.0));
				return new ItemHeight() { Item = x, Height = totalHeight };
			}).ToList();
		}

		public class ItemHeight {
			public DocumentObject Item { get; set; }
			public Unit Height { get; set; }
		}

		public class FurtherAdjustments {
			public ItemHeight Unmodified { get; set; }
			public Unit MaximumWidth { get; set; }
			public Unit MaximumHeight { get; set; }
		}

		public class Page : IEnumerable<ItemHeight> {
			public List<ItemHeight> Items { get; set; }

			public IEnumerator<ItemHeight> GetEnumerator() {
				return Items.GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator() {
				return Items.GetEnumerator();
			}
		}

		public static List<Page> SplitHeights<T>(Unit width, Unit[] heights, IEnumerable<T> paragraphs, Func<T, DocumentObject> selector = null, Unit? extraHeight = null, Func<FurtherAdjustments, ItemHeight> stillTooBig = null) where T : DocumentObject {
			Unit cumulative = 0;
			var splits = new List<Page>();
			var curHeight = heights[0];
			var page = 0;
			var heightObj = GetHeights(width, paragraphs, selector, extraHeight);

			var curSplit = new List<ItemHeight>();
			for (var i = 0; i < heightObj.Count(); i++) {
				var ho = heightObj[i];
				cumulative += ho.Height;
				if (cumulative > curHeight && curSplit.Any()) {
					//next
					page += 1;
					curHeight = heights[Math.Min(page, heights.Count() - 1)];
					cumulative = ho.Height;
					splits.Add(new Page() { Items = curSplit });
					curSplit = new List<ItemHeight>();
				}
				if (cumulative > curHeight && !curSplit.Any() && stillTooBig != null) {
					try {
						var adj = stillTooBig(new FurtherAdjustments() {
							MaximumHeight = curHeight,
							MaximumWidth = width,
							Unmodified = ho,
						});
						if (adj != null)
							ho = adj;

					} catch (Exception e) {
						//fall back
					}
				}

				curSplit.Add(ho);

			}
			if (curSplit.Any()) {
				splits.Add(new Page() { Items = curSplit });
			}


			return splits;
		}

		public static void AppendAll(DocumentObject o, List<DocumentObject> toAdd) {
			foreach (var p in toAdd) {
				if (o is Paragraph)
					((Paragraph)o).Elements.Add(p);
				if (o is Cell)
					((Cell)o).Elements.Add(p);
				if (o is Section)
					((Section)o).Elements.Add(p);
			}
		}

		public static Unit ResizeToFit(DocumentObject cell, Unit width, Unit height, Func<DocumentObject, Unit, IEnumerable<DocumentObject>> paragraphs, Unit? minFontSize = null, Unit? maxFontSize = null) {
			var ctx = XGraphics.CreateMeasureContext(new XSize(width.Inch, height.Inch), XGraphicsUnit.Inch, XPageDirection.Downwards);
			var fontSize = maxFontSize ?? Unit.FromPoint(12);
			var minSize = minFontSize ?? Unit.FromPoint(8);
			List<DocumentObject> paragraphsToAdd;

			if (!(cell is Cell || cell is Paragraph || cell is Section))
				throw new Exception("cant handle:" + cell.NotNull(x => x.GetType()));

			while (true) {
				var curHeight = new Unit(0.0);
				var curWidth = new Unit(0.0);
				paragraphsToAdd = paragraphs(cell, fontSize).ToList();

				foreach (var p in paragraphsToAdd) {
					string family = null;
					if (p is Paragraph) {
						((Paragraph)p).Format.Font.Size = fontSize;
						family = ((Paragraph)p).Format.Font.Name;
					} else if (p is Table) {
						var table = (Table)p;
						table.Format.Font.Size = fontSize;
						family = table.Format.Font.Name;
					}
					if (string.IsNullOrWhiteSpace(family)) {
						if (cell is Paragraph)
							family = ((Paragraph)cell).Format.Font.Name;
						if (cell is Cell)
							family = ((Cell)cell).Format.Font.Name;
					}
					if (string.IsNullOrWhiteSpace(family))
						family = "Arial Narrow";

					var size = GetSize(ctx, p, family, fontSize, width);
					curHeight += size.Height;
					curWidth += size.Width;
				}

				if (curHeight < height || fontSize <= minSize) {
					break;
				}
				fontSize -= Unit.FromPoint(1);
			}
			AppendAll(cell, paragraphsToAdd);

			return fontSize;
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

		private static Section AddVtoPage(Document doc, string docName, string pageName) {

			//doc.AddTab

			Section section;

			section = doc.AddSection();
			section.PageSetup.Orientation = Orientation.Landscape;
			section.PageSetup.PageFormat = PageFormat.Letter;

			//var table =  section.AddTable();
			//var c = table.AddColumn();
			//var r = table.AddRow();
			//var p = r.Cells[0].AddParagraph();

			//p.

			var paragraph = new Paragraph();
			//paragraph.AddTab();
			//paragraph.AddPageField();
			//Add paragraph to footer for odd pages.
			section.Footers.Primary.AddParagraph("© 2003 - " + DateTime.UtcNow.AddMonths(3).Year + " EOS. All Rights Reserved.");

			section.Footers.Primary.Format.Font.Size = 10;
			section.Footers.Primary.Format.Font.Name = "Arial Narrow";
			//section.Footers.Primary.Format.SpaceBefore = Unit.FromInch(0.25);

			section.PageSetup.LeftMargin = Unit.FromInch(.3);
			section.PageSetup.RightMargin = Unit.FromInch(.3);
			section.PageSetup.TopMargin = Unit.FromInch(.2);
			section.PageSetup.BottomMargin = Unit.FromInch(.5);

			/////////////////////////////

			var title = section.AddTable();
			title.AddColumn(Unit.FromInch(0.05));
			title.AddColumn(Unit.FromInch(2.22));
			title.AddColumn(Unit.FromInch(10.07 - 2.22));
			// title.Borders.Color = TableBlack;
			var titleRow = title.AddRow();
			var imageFilename = HttpContext.Current.Server.MapPath("~/Content/img/EOS_Model.png");

			var img = titleRow.Cells[1].AddImage(imageFilename);
			//img.Height = Unit.FromInch(2.13);
			img.Width = Unit.FromInch(1.95);

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

			//titleRow.Cells[2].TopPadding = Unit.FromInch(.155);



			var frame = trow.Cells[0].AddTextFrame();
			frame.Height = Unit.FromInch(0.38);
			frame.Width = Unit.FromInch(5.63);
			//frame.Left = ShapePosition.Center;

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
			//rr.Shading.Color = TableGray;
			rr.HeightRule = RowHeightRule.Exactly;
			rr.VerticalAlignment = VerticalAlignment.Center;
			rr.Height = Unit.FromInch(0.38);
			// rr.Format.Font.Size = Unit.FromInch(.2);



			frame = trow.Cells[0].AddTextFrame();
			frame.Height = Unit.FromInch(0.38);
			frame.Width = Unit.FromInch(5.63);

			frame.MarginTop = Unit.FromInch(.05);
			//frame.LineFormat.Color = TableGray;

			var p = frame.AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Center;
			p.Format.LeftIndent = Unit.FromInch(2);
			p.Format.SpaceBefore = Unit.FromInch(.11);
			var ft = p.AddFormattedText(pageName, TextFormat.Bold | TextFormat.Underline);
			ft.Font.Size = 20;
			ft.Font.Name = "Arial Narrow";



			return section;
		}

		private static void AddVtoVision(Document doc, AngularVTO vto, string dateformat) {

			Cell coreValuesPanel, coreFocusPanel, tenYearPanel, marketingStrategyPanel, threeYearPanel;
			Table issueTable, rockTable, goalTable;
			Unit baseHeight = Unit.FromInch(5.1);//5.15


			var TableGray = new Color(100, 100, 100, 100);
			var TableBlack = new Color(0, 0, 0);

			AddPage_VtoVision(doc, vto, baseHeight, out coreValuesPanel, out coreFocusPanel, out tenYearPanel, out marketingStrategyPanel, out threeYearPanel);

			var values = vto.Values.ToList();
			ResizeToFit(coreValuesPanel, Unit.FromInch(5.33), Unit.FromInch(1.2), (cell, fs) => {
				var o = new List<Paragraph>();
				return OrderedList(values.Select(x => x.CompanyValue), ListType.NumberList1);
			}, maxFontSize: Unit.FromPoint(10));


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

			//////
			{
				var fs = 10;
				var p1 = new Paragraph();
				p1.Format.Font.Size = fs;
				var txt = p1.AddFormattedText("Target Market/\"The List\": ", TextFormat.Bold);
				p1.Format.Font.Name = "Arial Narrow";
				p1.AddText(vto.Strategy.TargetMarket ?? "");
				marketingParagraphs.Add(p1);

				var p2 = new Paragraph();
				p2.Format.Font.Size = fs;
				var uniques = vto.Strategy.Uniques.ToList();
				p2.Format.SpaceBefore = fs * 1.5;
				var uniquesTitle = "Uniques: ";
				if (uniques.Count == 3)
					uniquesTitle = "Three " + uniquesTitle;
				p2.AddFormattedText(uniquesTitle, TextFormat.Bold);
				p2.Format.Font.Name = "Arial Narrow";
				marketingParagraphs.Add(p2);
				marketingParagraphs.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44)));

				var p3 = new Paragraph();
				p3.Format.Font.Size = fs;
				p3.Format.SpaceBefore = fs * 1.5;
				p3.AddFormattedText("Proven Process: ", TextFormat.Bold);
				p3.Format.Font.Name = "Arial Narrow";
				p3.AddText(vto.Strategy.ProvenProcess ?? "");
				marketingParagraphs.Add(p3);

				var p4 = new Paragraph();
				p4.Format.Font.Size = fs;
				p4.Format.SpaceBefore = fs * 1.5;
				p4.AddFormattedText("Guarantee: ", TextFormat.Bold);
				p4.Format.Font.Name = "Arial Narrow";
				p4.AddText(vto.Strategy.Guarantee ?? "");
				marketingParagraphs.Add(p4);
			}
			//////
			var marketingPages = SplitHeights(Unit.FromInch(5.33), new[] { Unit.FromInch(2.7), Unit.FromInch(5.7) }, marketingParagraphs);

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

		private static void AddVtoTraction(Document doc, AngularVTO vto, string dateformat) {
			Cell oneYear, quarterlyRocks, issuesList;
			Table issueTable, rockTable, goalTable;
			Unit baseHeight = Unit.FromInch(5.1);//5.15
			AddPage_VtoTraction(doc, vto, baseHeight, out oneYear, out quarterlyRocks, out issuesList, out issueTable, out rockTable, out goalTable);

			Unit fs = 10;
			var goalObjects = new List<DocumentObject>();
			var goalsSplits = new List<Page>();
			var goalRows = new List<Row>();
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

				var goalParagraphs = goals.Select(x => {
					var c = new Cell();
					var p = c.AddParagraph(x);
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					return p;
				});


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
				}

				var headerSize = GetSize(goalObjects, Unit.FromInch(3.47));
				Unit pg1Height = baseHeight - headerSize.Height;
				goalsSplits = SplitHeights(Unit.FromInch(3), new[] { pg1Height, (baseHeight) }, goalRows);


				goalObjects.Add(goalTable);
			}

			var rockObjects = new List<DocumentObject>();
			var rockSplits = new List<Page>();
			var rockRows = new List<Row>();
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


				var rockParagraphs = rocks.Select(x => {
					var c = new Cell();
					var p = c.AddParagraph(x.Rock.Name);
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					return p;
				});



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
				rockSplits = SplitHeights(Unit.FromInch(3), new[] { pg1Height, (baseHeight) }, rockRows);
				rockObjects.Add(rockTable);

			}
			var issuesObjects = new List<DocumentObject>();
			var issueSplits = new List<Page>();
			var issueRows = new List<Row>();
			{
				var issues = vto.Issues.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();

				//issuesList.Elements.AddParagraph(" ").SpaceBefore = Unit.FromInch(0.095);
				//ResizeToFit(issuesList, Unit.FromInch(3.47), Unit.FromInch(5.15), (cell, fs) => {

				if (issues.Any()) {
					var issueParagraphs = issues.Select(x => {
						var c = new Cell();
						var p = c.AddParagraph(x);
						p.Format.SpaceBefore = Unit.FromPoint(2);
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						return p;
					});


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
						p.Format.SpaceBefore = Unit.FromPoint(2);
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						issueRows.Add(r);
					}

					//var rowHeights = GetRowHeights(issueRows, Unit.FromInch(3));
					var extraHeight = 0.51;

					issueSplits = SplitHeights(Unit.FromInch(3), new[] { (baseHeight), (baseHeight) }, issueRows, x => x.Cells[1], extraHeight);
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
			var section = AddVtoPage(doc, vto.Name ?? "", "TRACTION");

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

		public static void AddVTO(Document doc, AngularVTO vto, string dateformat) {
			if (vto.IncludeVision)
				AddVtoVision(doc, vto, dateformat);
			AddVtoTraction(doc, vto, dateformat);
		}

		public class AccNodeJs {
			public List<AccNodeJs> children { get; set; }
			public List<AccNodeJs> _children { get; set; }
			public string Name { get; set; }
			public string Position { get; set; }
			public List<string> Roles { get; set; }

			public bool isLeaf { get; set; }
			public string side { get; set; }


			public bool hasHiddenChildren { get; set; }

			public double x { get; set; }
			public double y { get; set; }
			public double width { get; set; }
			public double height { get; set; }
		}

		private class PageProp {
			public XUnit pageWidth { get; set; }
			public XUnit pageHeight { get; set; }
			public XUnit margin { get; set; }
			public XPen pen = new XPen(XColors.Black, 1);
			public XBrush brush = new XSolidBrush(XColors.Transparent);

			public double scale = 1;

			public XUnit allowedWidth {
				get {
					return pageWidth - 2 * margin;
				}
			}
			public XUnit allowedHeight {
				get {
					return pageHeight - 2 * margin;
				}
			}
		}

		#region AC Helpers 
		private static void ACDrawRole(XGraphics gfx, AccNodeJs me, PageProp pageProps, double[] origin = null) {
			origin = origin ?? new[] { 0.0, 0.0 };

			var x = (int)me.x - origin[0];
			var y = (int)me.y - origin[1];


			var top = 50 * pageProps.scale;
			var pad = 1.0 / 24.0 * me.width;

			var tf = new XTextFormatter(gfx);

			tf.Alignment = XParagraphAlignment.Center;
			XFont bold = new XFont("Times New Roman", 14 * pageProps.scale, XFontStyle.Bold);
			XFont norm = new XFont("Times New Roman", 14 * pageProps.scale, XFontStyle.Regular);
			tf.DrawString(me.Position ?? "", bold, XBrushes.Black, new XRect(x, y + 12 * pageProps.scale / 3.0, me.width, top / 2.0));
			tf.DrawString(me.Name ?? "", norm, XBrushes.Black, new XRect(x, y + top / 2.0 + 12 * pageProps.scale / 3.0, me.width, top / 2.0));

			if (me.height > top) {
				gfx.DrawLine(XPens.Black, x + pad, y + top, x + (me.width - 2 * pad), y + top);
			}

			tf = new XTextFormatter(gfx);
			tf.Alignment = XParagraphAlignment.Left;
			norm = new XFont("Times New Roman", 12 * pageProps.scale, XFontStyle.Regular);

			var h = 50 * pageProps.scale;

			if (me.Roles != null && me.Roles.Any()) {
				var rheight = (me.height - top) / (me.Roles.Count + 1);
				//h += rheight / 2.0;
				foreach (var r in me.Roles) {
					var text = r;
					if (text != null) {
						text = text.TrimStart(' ', '•', '*');
						//text = "• " + r;
					}
					var dotWidth = pad;

					tf.DrawString("•", norm, XBrushes.Black, new XRect(x + pad, y + h, dotWidth, rheight));
					//var myHeight = tf.meas
					var wrapper = new PdfWordWrapper(gfx, Unit.FromPoint(me.width - (pad * 2+ dotWidth)));
					wrapper.Add(text ?? "", norm, XBrushes.Black);
					var size = wrapper.Size;
					wrapper.Draw(gfx, x + pad + dotWidth, y + h, PdfWordWrapper.Alignment.Left);
					//tf.DrawString(text ?? "", norm, XBrushes.Black, new XRect(x + pad, y + h, me.width - pad * 2, rheight));
					h += size.Height;
				}
			}
			me.height = /*Math.Max(me.height,*/ h + 2 * pad/*)*/;
			gfx.DrawRectangle(pageProps.pen, pageProps.brush, x, y, (int)me.width, (int)me.height);
		}

		private static void DrawLine(XGraphics gfx, PageProp pageProps, List<Tuple<double, double>> points) {
			for (var i = 1; i < points.Count; i++) {
				gfx.DrawLine(pageProps.pen, points[i - 1].Item1, points[i - 1].Item2, points[i].Item1, points[i].Item2);
			}
		}

		private static void ACDrawRoleLine(XGraphics gfx, AccNodeJs parent, AccNodeJs me, PageProp pageProps, double[] origin = null) {
			origin = origin ?? new[] { 0.0, 0.0 };
			//gfx.DrawRectangle(pageProps.pen, pageProps.brush, (int)me.x - origin[0], (int)me.y - origin[1], (int)me.width, (int)me.height);

			//var parentX1 = parent.x + parent.width / 2.0 - origin[0];
			//var parentY1 = parent.y + parent.height - origin[1] - 1;
			//var parentY2 = parent.y + parent.height + (me.y - (parent.y + parent.height)) * 2 / 3 - origin[1];
			//var parentX2 = me.x + me.width / 2.0 - origin[0];

			//var parentY3 = me.y - origin[1];

			//gfx.DrawLine(pageProps.pen, parentX1, parentY1, parentX1, parentY2);
			//gfx.DrawLine(pageProps.pen, parentX1, parentY2, parentX2, parentY2);
			//gfx.DrawLine(pageProps.pen, parentX2, parentY2, parentX2, parentY3);

			var vSeparation = Math.Abs(me.y - (parent.y + parent.height)) * 2 / 3;
			var hSeparation = 25 * pageProps.scale;

			var adjS = 1 * pageProps.scale;


			var sx = parent.x - origin[0] + parent.width / 2;
			var sy = parent.y - origin[1] + parent.height - adjS;
			var tx = me.x - origin[0] + me.width / 2;
			var ty = me.y - origin[1] - adjS;
			var my = sy + vSeparation /*- origin[1]*/;
			if (me.isLeaf) {
				var tw = me.width;
				double lx;
				if (me.side == "left") {
					tx = tx - tw / 2 - adjS;
					lx = tx - hSeparation / 2 /*- origin[0]*/+ adjS;
				} else {
					tx = tx + tw / 2 - adjS;
					lx = tx + hSeparation / 2 /*- origin[0]*/+ adjS;
				}
				//return;

				//lx = tx + tw/2  - hSeparation / 2 - origin[0];

				var tyy = ty + Math.Min(10, me.height / 2);
				var points = new List<Tuple<double, double>>();
				if (!parent.isLeaf) {
					points.Add(Tuple.Create(sx, sy));
					points.Add(Tuple.Create(sx, my));
					points.Add(Tuple.Create(lx, my));
				} else {
					double ax;
					if (me.side == "left") {
						ax = sx /*- origin[0]*/ - parent.width / 2;// - d.source.width / 2;
					} else {
						ax = sx /*- origin[0]*/ + parent.width / 2 - adjS;// - d.source.width / 2;
					}


					var ay = (sy - parent.height) + Math.Min(10, parent.height / 2) /*- origin[1]*/ - adjS;//d.source.height / 2;
																									  //points.Add(Tuple.Create(ax, ay));
					points.Add(Tuple.Create(lx, ay));
				}
				points.Add(Tuple.Create(lx, tyy));
				points.Add(Tuple.Create(tx, tyy));

				DrawLine(gfx, pageProps, points);
			} else {
				var points = new List<Tuple<double, double>>() {
						Tuple.Create(sx, sy),
						Tuple.Create(sx, my),
						Tuple.Create(tx, my),
						Tuple.Create(tx, ty)
				};
				DrawLine(gfx, pageProps, points);
			}

		}

		private static void ACDrawRoles(XGraphics gfx, AccNodeJs root, PageProp pageProps, double[] origin = null) {
			ACDrawRole(gfx, root, pageProps, origin);
			if (root.children != null) {
				foreach (var c in root.children) {
					ACDrawRoleLine(gfx, root, c, pageProps, origin);
					ACDrawRoles(gfx, c, pageProps, origin);
				}
				if (root.hasHiddenChildren) {
					ACDrawEllipse(gfx, root, pageProps, origin);
				}


			}
		}

		private static void ACDrawEllipse(XGraphics gfx, AccNodeJs root, PageProp pageProps, double[] origin = null) {
			origin = origin ?? new[] { 0.0, 0.0 };
			var x = root.x + root.width / 2.0 - origin[0];
			var y = root.y + root.height - origin[1];

			for (var ii = 0; ii < 3; ii += 1) {
				var i = (3 + 6 * ii) * pageProps.scale;
				var d = (4.0) * pageProps.scale;
				gfx.DrawEllipse(XBrushes.Black, x - (d / 2.0), y + i - (d / 2.0), d, d);
			}
		}


		private static double[] ACRanges(AccNodeJs root) {
			var range = new[] { root.x, root.y, root.x + root.width, root.y + root.height };
			if (root.children != null) {
				foreach (var n in root.children) {
					var n_range = ACRanges(n);
					range[0] = Math.Min(range[0], n_range[0]);
					range[1] = Math.Min(range[1], n_range[1]);
					range[2] = Math.Max(range[2], n_range[2]);
					range[3] = Math.Max(range[3], n_range[3]);
				}
			}
			return range;
		}
		private static void ACNormalize(AccNodeJs root, double[] range, PageProp pageProp, double? forceScale = null) {
			root.x += -range[0];
			root.y += -range[1];
			var shouldScale = forceScale == null;
			var scale = forceScale ?? 1.0;

			var width = (double)(range[2] - range[0]);
			var height = (double)(range[3] - range[1]);
			if (shouldScale) {
				if (width > pageProp.allowedWidth)
					scale = pageProp.allowedWidth / width;
				if (height > pageProp.allowedHeight)
					scale = Math.Min(scale, pageProp.allowedHeight / height);
			}

			//For centering
			var numPagesWide = width * scale / pageProp.allowedWidth;
			var numPagesTall = height * scale / pageProp.allowedHeight;

			//Rescale
			root.x *= scale;
			root.y *= scale;
			root.width *= scale;
			root.height *= scale;

			pageProp.scale = scale;

			var extraWidth = (Math.Ceiling(numPagesWide) - (numPagesWide)) * pageProp.allowedWidth;
			var extraHeight = (Math.Ceiling(numPagesTall) - (numPagesTall)) * pageProp.allowedHeight;
			root.x += extraWidth / 2.0;
			root.y += extraHeight / 2.0;

			root.x += pageProp.margin.Point;
			root.y += pageProp.margin.Point;

			if (root.children != null) {
				foreach (var n in root.children) {
					ACNormalize(n, range, pageProp, scale);
				}
			}
		}
		private static Tuple<int, int> ACGetPage(double x, double y, PageProp pageProp) {
			return Tuple.Create((int)(x / pageProp.allowedWidth), (int)(y / pageProp.allowedHeight));
		}
		private static double[] ACGetOrigin(Tuple<int, int> page, PageProp pageProp) {
			return new[] {
				page.Item1 * pageProp.allowedWidth,
				page.Item2 * pageProp.allowedHeight
			};

		}
		private static void ACGeneratePages(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, AccNodeJs root, PageProp pageProp) {
			var a = pageLookup[ACGetPage(root.x, root.y, pageProp)];
			var b = pageLookup[ACGetPage(root.x + root.width, root.y, pageProp)];
			var c = pageLookup[ACGetPage(root.x + root.width, root.y + root.height, pageProp)];
			var d = pageLookup[ACGetPage(root.x, root.y + root.height, pageProp)];
			if (root.children != null) {
				foreach (var p in root.children) {
					ACGeneratePages(pageLookup, p, pageProp);
				}
			}
		}
		private static void ACDrawOnAllPages(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, DefaultDictionary<PdfPage, XGraphics> gfxLookup, AccNodeJs parent, AccNodeJs me, PageProp pageProp) {
			var pages = new List<Tuple<int, int>>();

			pages.Add(ACGetPage(me.x, me.y, pageProp));
			pages.Add(ACGetPage(me.x + me.width, me.y, pageProp));
			pages.Add(ACGetPage(me.x + me.width, me.y + me.height, pageProp));
			pages.Add(ACGetPage(me.x, me.y + me.height, pageProp));
			if (parent != null) {
				pages.Add(ACGetPage(parent.x, parent.y, pageProp));
				pages.Add(ACGetPage(parent.x + parent.width, parent.y, pageProp));
				pages.Add(ACGetPage(parent.x + parent.width, parent.y + parent.height, pageProp));
				pages.Add(ACGetPage(parent.x, parent.y + parent.height, pageProp));
			}
			pages.Distinct();

			foreach (var p in pages) {
				var origin = ACGetOrigin(p, pageProp);
				var page = pageLookup[p];
				var gfx = gfxLookup[page];
				ACDrawRole(gfx, me, pageProp, origin);
				if (parent != null)
					ACDrawRoleLine(gfx, parent, me, pageProp, origin);

				if (me.hasHiddenChildren) {
					ACDrawEllipse(gfx, me, pageProp, origin);
				}
			}
		}


		private static void ACDrawOnAllPages_Dive(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, DefaultDictionary<PdfPage, XGraphics> gfxLookup, AccNodeJs me, PageProp pageProp, AccNodeJs parent = null) {
			ACDrawOnAllPages(pageLookup, gfxLookup, parent, me, pageProp);
			if (me.children != null) {
				foreach (var c in me.children) {
					ACDrawOnAllPages_Dive(pageLookup, gfxLookup, c, pageProp, me);
				}
			}
		}

		#endregion

		//public static void AddAccountabilityChart(UserOrganizationModel caller, PdfDocument doc, AccNodeJs root) {
		//    //var tree = AccountabilityAccessor.GetTree(caller, chartId, expandAll: true);
		//    //Doesnt do anything
		//    var chart = GenerateAccountabilityChart(root, 11, 8, false);
		//    foreach (var p in chart.Pages) {
		//        doc.AddPage((PdfPage)p.Clone());
		//    }

		//}

		private static void ACGenerate_Resized(PdfPage page, AccNodeJs root, PageProp pageProp) {
			var ranges = ACRanges(root);
			ACNormalize(root, ranges, pageProp, null);
			XGraphics gfx = XGraphics.FromPdfPage(page);
			ACDrawRoles(gfx, root, pageProp);
		}

		private static void ACGenerate_Full(PdfDocument doc, AccNodeJs root, PageProp pageProp, double scale) {
			var ranges = ACRanges(root);
			ACNormalize(root, ranges, pageProp, scale);

			var pageLookup = new DefaultDictionary<Tuple<int, int>, PdfPage>(x => new PdfPage(doc) {
				Width = pageProp.pageWidth,
				Height = pageProp.pageHeight
			});
			var gfxLookup = new DefaultDictionary<PdfPage, XGraphics>(x => XGraphics.FromPdfPage(x));

			ACGeneratePages(pageLookup, root, pageProp);
			ACDrawOnAllPages_Dive(pageLookup, gfxLookup, root, pageProp);

			pageLookup.Keys.OrderBy(x => x.Item1)
				.ThenBy(x => x.Item2)
				.ToList()
				.ForEach(x => {
					doc.AddPage(pageLookup[x]);
				});
		}

		// public static AccNodeJs CreateAccountabilityChartWithPhantomJs(UserOrganizationModel caller,long organizationId) {

		// var phantomJS = new PhantomJS();
		//phantomJS.OutputReceived += (sender, e) => {
		//    Console.WriteLine("PhantomJS output: {0}", e.Data);
		//};
		//phantomJS.RunScript("for (var i=0; i<10; i++) console.log('hello from js '+i); phantom.exit();", null);

		//phantomJS.

		//            phantomJS.RunScript(@"
		//var webPage = require('webpage');
		//var page = webPage.create();

		//page.viewportSize = { width: 1920, height: 1080 };
		//page.open("""+ProductStrings.BaseUrl2+@"accountability/chart?expandAll=true"", function start(status) {
		//    page.render('google_home.jpeg', { format: 'jpeg', quality: '100'});
		//phantom.exit();
		//});",new string[0]);

		//            return null;
		//  }

		public static PdfDocument GenerateAccountabilityChart(AccNodeJs root, double width, double height, bool restrictSize = false) {

			// Create new PDF document
			PdfDocument document = new PdfDocument();
			// Create new page
			var _unusedPage = new PdfPage();
			_unusedPage.Width = XUnit.FromInch(width);
			_unusedPage.Height = XUnit.FromInch(height);

			//_unusedPage.Orientation = PdfSharp.PageOrientation.Landscape;

			var margin = XUnit.FromInch(.25);

			var pageProp = new PageProp() {
				pageWidth = _unusedPage.Width,
				pageHeight = _unusedPage.Height,
				margin = margin
			};

			if (restrictSize) {
				document.AddPage(_unusedPage);
				ACGenerate_Resized(_unusedPage, root, pageProp);
			} else {
				ACGenerate_Full(document, root, pageProp, .5);
			}
			return document;
		}
	}
}