using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
//using Pechkin;
//using Pechkin.Synchronized;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using NHibernate;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Pdf;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using System.Xml.XPath;
using RadialReview.Models.Angular.Meeting;
using System.Reflection;
using RadialReview.Properties;
using RadialReview.Models.Angular.VTO;
using System.Threading;
using RadialReview.Controllers;
using MigraDoc.DocumentObjectModel.Shapes.Charts;
using RadialReview.Models.Charts;
using RadialReview.Engines;
using Table = MigraDoc.DocumentObjectModel.Tables.Table;
using PdfSharp.Pdf.IO;
using OxyPlot;
using VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment;
using OxyPlot.Wpf;

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
		//public void AddFont(Uri baseUri, string familyName)
		//{
		//    if (String.IsNullOrEmpty(familyName))
		//        throw new ArgumentNullException("familyName");
		//    if (familyName.Contains(","))
		//        throw new NotImplementedException("Only one family name is supported.");
		//    // family name starts right of '#'
		//    int idxHash = familyName.IndexOf('#');
		//    if (idxHash < 0)
		//        throw new ArgumentException("Family name must contain a '#'. Example './#MyFontFamilyName'", "familyName");
		//    string key = familyName.Substring(idxHash + 1);
		//    if (String.IsNullOrEmpty(key))
		//        throw new ArgumentException("familyName has invalid format.");
		//    if (this.fontFamilies.ContainsKey(key))
		//        throw new ArgumentException("An entry with the specified family name already exists.");
		//    System.Windows.Media.FontFamily fontFamily = new System.Windows.Media.FontFamily(baseUri, familyName);
		//    &nbsp;
		//    this.fontFamilies.Add(key, fontFamily);
		//}


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
		public static Document GenerateReviewPrintout(UserOrganizationModel caller, ReviewController.ReviewDetailsViewModel review, int margin) {
			var name = review.Review.ForUser.GetName();
			//var doc = CreateDoc(caller, review.ReviewContainer.ReviewName + " - " + name);

			PdfDocument document = new PdfDocument();
			PdfPage page = document.AddPage();
			page.Size = PdfSharp.PageSize.Letter;
			XGraphics gfx = XGraphics.FromPdfPage(page);
			//// HACK²
			gfx.MUH = PdfFontEncoding.Unicode;
			gfx.MFEH = PdfFontEmbedding.Default;

			// Create document from HalloMigraDoc sample
			// Document doc = HelloMigraDoc.Documents.CreateDocument();
			//var section = doc.AddSection();
			//var chartPage = PdfAccessor.AddTitledPage(doc, name);
			var scatter = new ChartsEngine().ReviewScatter2(caller, review.Review.ForUserId, review.Review.ForReviewContainer.Id, review.Review.ClientReview.ScatterChart.Groups, true, false);
			var plot = OxyplotAccessor.ScatterPlot(scatter, margin);
			//var pdfExporter = new PdfExporter { Width = 8.5 * 72, Height = 8.5 * 72, Background = OxyColors.White };

			var stream = new MemoryStream();
			//pdfExporter.Export(plot, stream);


			//PdfDocument imgDoc = new PdfDocument(stream);
			PngExporter.Export(plot, stream, 400, 400, OxyColor.FromRgb(255, 255, 255));


			Document doc = CreateDoc(caller, "Printout");
			var arr = LoadImage(stream);
			var img = MigraDocFilenameFromByteArray(arr);

			var section = doc.AddSection();
			section.AddImage(img);

			return doc;
			//var chartImg = XPdfForm.FromStream(new MemoryStream(stream.ToArray()));
			//XRect LetterRect = new XRect(0, 0, LetterWidth, LetterHeight);

			//var chartRect = new XRect(0, 0, LetterWidth / 2 * 0.9, LetterHeight / 2 * 0.9);
			//chartRect.X = LetterWidth * 0.05 / 2;
			//chartRect.Y = LetterHeight * 0.05 / 2;

			//XGraphicsContainer container = gfx.BeginContainer(chartRect, LetterRect, XGraphicsUnit.Point);
			//    gfx.DrawImage(chartImg, new XRect(0, 0, chartImg.PointWidth, chartImg.PointHeight));
			//gfx.EndContainer(container);
			///////////////////////////////////////////
			#region EOSCLIENT
			//var Model = review;
			//if (Model.CompanyValuesTable((long)review.ReviewContainer.Id).Rows.Any())
			//{
			//    var valueAnswers = Model.AnswersAbout.Where(x => x is CompanyValueAnswer).Cast<CompanyValueAnswer>().Where(x => x.IncludeReason);
			//    if (Model.ReviewContainer.AnonymousByDefault)
			//    {
			//        //@Html.Partial("Table", Model.CompanyValuesScore)
			//        throw new Exception("todo");
			//    }
			//    else
			//    {
			//        //@Html.Partial("Table", Model.CompanyValuesTable(Model.Review.ForReviewsId))
			//        throw new Exception("todo");
			//    }

			//            if (valueAnswers.Any())
			//            {
			//                <table class="valueTable" style="width:100%;margin-left: 10px;">
			//                    <thead>
			//                        <tr>
			//                            <th colspan="2">Value:</th>
			//                        </tr>
			//                    </thead>
			//                    <tbody style="vertical-align: top;">
			//                        @foreach (var r in valueAnswers.GroupBy(x => x.AboutUserId + "_" + x.Askable.Id + "_" + x.ByUserId).Select(x=>x.First()))
			//                        {
			//                            <tr style="border-top:1px solid #ddd;">
			//                                <td class="block" rowspan="2"><div class="fill @r.Exhibits"></div></td>
			//                                <td class="bold alignLeft valueTableQuestion">@r.Askable.GetQuestion()</td>

			//                            </tr>
			//                            <tr>
			//                                <td class="valueTableReason" style="white-space: normal;">
			//                                    <span class="italic">"@r.Reason"</span>
			//                                    @if (!Model.ReviewContainer.AnonymousByDefault)
			//                                    {
			//                                        @:- @r.ByUser.GetName()
			//                                    }
			//                                </td>
			//                            </tr>
			//                        }
			//                    </tbody>
			//                </table>

			//            }
			//            <div class="evaluation-heading">Company Values</div>
			//        </div>
			//    </div>
			//}
			//@if (Model.RockTable((long)ViewBag.ReviewId).Rows.Any())
			//{
			//    <div class="print-col-xs-6 zoom8 fixHeight">
			//        <div class="subsection subsection-rocks noBreak">
			//            @*Html.Partial("Table", Model.RockTable((long)ViewBag.ReviewId))*@
			//            @{
			//    var rrs = Model.AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.Rock).Cast<RockAnswer>();
			//    var first = true;
			//            }

			//            @**@
			//            @foreach (var r in rrs.GroupBy(x=>x.Askable.Id).Select(x=>x.First()))
			//            {
			//                <div class="row smallBreak2">
			//                    <div class="col-xs-3 print-col-xs-3 alignRight noPadLeft">
			//                        @if (first)
			//                        {
			//                            <div style="height: 25px;"></div>
			//                        }
			//                        <div style="height: 8px;"></div>
			//                        <div class="bold alignRight">
			//                            @r.Askable.GetQuestion()
			//                        </div>
			//                    </div>
			//                    <div class="col-xs-9">
			//                        <div class="row zoom8">
			//                            <div class="col-xs-7">
			//                                @if (first)
			//                                {
			//                                    <div class="hidden-xs1 alignCenter bold" style="border-bottom: 2px solid #494949;color: #494949;">Supervisor</div>
			//                                    <div class="hidden-xs1 fullWidth" style="height: 10px;"></div>
			//                                }
			//                                <div class="row">
			//                                    <div class="col-xs-5 alignCenter">
			//                                        @{
			//                                            var state = Tristate.Indeterminate;
			//                                            if (r.ManagerOverride == RockState.AtRisk)
			//                                            {
			//                                                state = Tristate.False;
			//                                            }
			//                                            else if (r.ManagerOverride == RockState.Complete)
			//                                            {
			//                                                state = Tristate.True;
			//                                            }
			//                                        }
			//                                        @*<div style="padding-top:5px;"></div>*@



			//                                        @Html.DisplayFor(x => state, "CompleteIncomplete")
			//                                    </div>
			//                                    <div class="col-xs-7 noPadRight">
			//                                        <div style="" class="fullWidth verticalOnly reason rockReason">
			//                                            @if (!String.IsNullOrWhiteSpace(r.OverrideReason))
			//                                            {
			//                                                @:"@r.OverrideReason"
			//                                            }
			//                                            else
			//                                            {
			//                                                <i class="gray">No comment provided.</i>
			//                                            }
			//                                        </div>
			//                                    </div>
			//                                </div>
			//                            </div>

			//                            <div class="col-xs-5">

			//                                @if (first)
			//                                {
			//                                    <div class="hidden-sm1 hidden-xs1 alignCenter bold" style="border-bottom: 2px solid #494949;color: #494949;">@Model.Review.ForUser.GetName()</div>
			//                                    <div class="hidden-sm1 hidden-xs1 fullWidth" style="height: 10px;"></div>
			//                                }
			//                                @{
			//                first = false;
			//                                }


			//                                @*<div class="row">
			//                                    <div class="col-xs-5 alignCenter">
			//                                        @Html.DisplayFor(x => r.Completion)
			//                                    </div>
			//                                                                        <div class="col-xs-12" style="padding-left: 7px;">*@
			//                                <div class="rockReasonHeight">
			//                                    @if (!String.IsNullOrWhiteSpace(r.Reason))
			//                                    {
			//                                        <i>"@r.Reason"</i>
			//                                    }
			//                                    else
			//                                    {
			//                                        <span class="gray italic">No comment provided.</span>
			//                                    }
			//                                </div>
			//                                <div class="gray markedAs">Marked as: <span class="completion toText @r.Completion"></span></div>

			//                                @*</div>
			//                                    </div>*@
			//                            </div>
			//                        </div>
			//                    </div>
			//                </div>
			//                @*<hr style="margin:10px;" class="visible-xs1" />*@
			//            }

			//            @**@


			//            <div class="evaluation-heading">@Html.Organization().Settings.RockName</div>
			//        </div>
			//    </div>
			//}

			#endregion
			//////////////////////////////

			//// Create a renderer and prepare (=layout) the document
			//MigraDoc.Rendering.DocumentRenderer docRenderer = new DocumentRenderer(doc);
			//docRenderer.PrepareDocument();

			//// For clarity we use point as unit of measure in this sample.
			//// A4 is the standard letter size in Germany (21cm x 29.7cm).

			//XRect A4Rect = new XRect(0, 0, A4Width, A4Height);

			//int pageCount = docRenderer.FormattedDocument.PageCount;
			//for (int idx = 0; idx < pageCount; idx++) {
			//    XRect rect = GetRect(idx);

			//    // Use BeginContainer / EndContainer for simplicity only. You can naturaly use you own transformations.
			//    XGraphicsContainer container = gfx.BeginContainer(rect, A4Rect, XGraphicsUnit.Point);

			//    // Draw page border for better visual representation
			//    gfx.DrawRectangle(XPens.LightGray, A4Rect);

			//    // Render the page. Note that page numbers start with 1.
			//    docRenderer.RenderPage(gfx, idx + 1);

			//    // Note: The outline and the hyperlinks (table of content) does not work in the produced PDF document.

			//    // Pop the previous graphical state
			//    gfx.EndContainer(container);
			//}



			//chartPage.ad


			//return document;
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
				//section.Footers.Primary.Format.SpaceBefore = Unit.FromInch(0.75);
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

		public static Document AddL10(Document doc, AngularRecurrence recur, DateTime? lastMeeting) {
			//CreateDoc(caller,"THE LEVEL 10 MEETING");
			var section = AddTitledPage(doc, "THE LEVEL 10 MEETING™", addPageNumber: false);
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

			var r = t.AddRow();
			r.Cells[0].AddParagraph("Segue");
			r.Cells[1].AddParagraph("5 Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("Scorecard");
			r.Cells[1].AddParagraph("5 Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("Rock Review");
			r.Cells[1].AddParagraph("5 Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("Customer/Employee Headlines");
			r.Cells[1].AddParagraph("5 Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			r = t.AddRow();
			r.Cells[0].AddParagraph("To-Do List");
			r.Cells[1].AddParagraph("5 Minutes");
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
				pp.AddColumn(Unit.FromInch(4.75 - .35));
				pp.AddColumn(Unit.FromInch(0.35));
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
			r.Cells[1].AddParagraph("60 Minutes");
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
			r.Cells[1].AddParagraph("5 Minutes");
			r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
			p = r.Cells[0].AddParagraph("Recap To-Do List");
			p.Format.LeftIndent = Unit.FromInch(1 + 3 / 8.0);
			p = r.Cells[0].AddParagraph("Cascading messages");
			p.Format.LeftIndent = Unit.FromInch(1 + 3 / 8.0);
			p = r.Cells[0].AddParagraph("Rating 1-10");
			p.Format.LeftIndent = Unit.FromInch(1 + 3 / 8.0);

			return doc;
		}

		public static void AddTodos(UserOrganizationModel caller, Document doc, AngularRecurrence recur) {
			//var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

			//return SetupDoc(caller, caller.Organization.Settings.RockName);

			var section = AddTitledPage(doc, "To-do List");

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

		public static void AddIssues(UserOrganizationModel caller, Document doc, AngularRecurrence recur, bool mergeWithTodos) {
			//var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

			//return SetupDoc(caller, caller.Organization.Settings.RockName);

			var section = AddTitledPage(doc, "Issues List", addSection: !mergeWithTodos);

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

		public static void AddRocks(UserOrganizationModel caller, Document doc, AngularRecurrence recur) {
			//var recur = L10Accessor.GetAngularRecurrence(caller, recurrenceId);

			//return SetupDoc(caller, caller.Organization.Settings.RockName);

			var section = AddTitledPage(doc, "Quarterly " + caller.Organization.Settings.RockName, Orientation.Landscape);
			Table table;
			double mult;
			Row row;
			int mn;
			Column column;

			var format = caller.NotNull(x => x.Organization.NotNull(y => y.Settings.NotNull(z => z.GetDateFormat()))) ?? "MM-dd-yyyy";

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
				column = table.AddColumn(Unit.FromInch((4.85+.7) * mult));
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
			column = table.AddColumn(Unit.FromInch((4.85+.7) * mult));
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

		public static void AddScorecard(Document doc, AngularRecurrence recur) {
			// Create a new PDF document
			//var recur = L10Accessor.GetAngularRecurrence(caller,recurrenceId);


			// var document = SetupDoc(caller, "Scorecard", Orientation.Landscape);

			var section = AddTitledPage(doc, "Scorecard", Orientation.Landscape);


			var TableGray = new Color(100, 100, 100, 100);
			var TableBlack = new Color(0, 0, 0);

			// var section = document.AddSection();



			var table = section.AddTable();
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

			var measurables = recur.Scorecard.Measurables.OrderBy(x => x.Ordering).Where(x => !(x.Disabled ?? false) && !x.IsDivider);
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
								cell.Shading.Color = Colors.LightGreen;// Color.FromCmyk(0.0708, 0.0, 0.1, .0588);
							} else {
								cell.Shading.Color = Colors.LightSalmon;// Color.FromCmyk(0, 0.0826, 0.0826, .0510);
							}
						}

					}
					ii++;
				}
				mn += 1;
			}
			//}


		}

		private static List<Paragraph> AddVtoSectionHeader(IVtoSectionHeader section, Unit fontSize, string dateformat) {
			var o = new List<Paragraph>();

			var futureDate = new Paragraph();
			o.Add(futureDate);
			futureDate.Format.SpaceBefore = Unit.FromPoint(.9 * fontSize.Point);
			futureDate.Format.Font.Name = "Arial Narrow";
			futureDate.AddFormattedText("Future Date: ", TextFormat.Bold);
			if (section.FutureDate.HasValue)
				futureDate.AddFormattedText(section.FutureDate.Value.ToString(dateformat), TextFormat.NotBold);


			var revenue = new Paragraph();
			revenue.Format.SpaceBefore = Unit.FromPoint(.3 * fontSize.Point);
			o.Add(revenue);
			revenue.AddFormattedText("Revenue: ", TextFormat.Bold);
			revenue.Format.Font.Name = "Arial Narrow";
			if (section.Revenue != null) {
				//revenue.AddFormattedText(string.Format(Thread.CurrentThread.CurrentCulture, "{0:c0}", section.Revenue.Value), TextFormat.NotBold);
				revenue.AddFormattedText(section.Revenue, TextFormat.NotBold);
			}

			var profit = new Paragraph();
			o.Add(profit);
			profit.Format.SpaceBefore = Unit.FromPoint(.3 * fontSize.Point);
			profit.AddFormattedText("Profit: ", TextFormat.Bold);
			profit.Format.Font.Name = "Arial Narrow";
			if (section.Profit != null) {
				//profit.AddFormattedText(string.Format(Thread.CurrentThread.CurrentCulture, "{0:c0}", section.Profit.Value), TextFormat.NotBold);
				profit.AddFormattedText(section.Profit, TextFormat.NotBold);
			}

			var measurables = new Paragraph();
			measurables.Format.SpaceBefore = Unit.FromPoint(.3 * fontSize.Point);
			measurables.Format.SpaceAfter = Unit.FromPoint(.6 * fontSize.Point);
			measurables.Format.Font.Name = "Arial Narrow";
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
			public YSize(Unit width, Unit height) {
				Width = width;
				Height = height;
			}

			public Unit Height { get; set; }
			public Unit Width { get; set; }
		}

		private static YSize GetSize(DocumentObject o, Unit width) {
			var ctx = XGraphics.CreateMeasureContext(new XSize(width.Inch, Unit.FromInch(1000)), XGraphicsUnit.Inch, XPageDirection.Downwards);
			var size = GetSize(ctx, o, GetFontFamily(o), GetFontSize(o), Unit.FromInch(3.47));
			return new YSize(Unit.FromInch(size.Width * 0.166044),Unit.FromInch(size.Height * 0.166044));
		}
		private static YSize GetSize(List<DocumentObject> os, Unit width) {
			var ctx = XGraphics.CreateMeasureContext(new XSize(width.Inch, Unit.FromInch(1000)), XGraphicsUnit.Inch, XPageDirection.Downwards);
			var size = new XSize(0, 0);
			foreach (var o in os) {
				var s = GetSize(ctx, o, GetFontFamily(o), GetFontSize(o), Unit.FromInch(3.47));
				size.Width = Math.Max(size.Width, s.Width);
				size.Height += s.Height;
			}
			return new YSize(Unit.FromInch(size.Width * 0.166044), Unit.FromInch(size.Height * 0.166044));
		}

		private static XSize GetSize(XGraphics ctx, DocumentObject o, String fontName, Unit fontSize, Unit maxWidth) {
			var s = new XSize(0, 0);

			if (o is FormattedText) {
				var txt = (FormattedText)o;
				foreach (DocumentObject e in txt.Elements) {
					var size = GetSize(ctx, e, txt.FontName, fontSize, maxWidth);
					s.Width = Math.Max(s.Width, size.Width);
					s.Height += size.Height;
				}
			} else if (o is Text) {
				var txt = (Text)o;
				XSize size;
				try {
					size = ctx.MeasureString(txt.Content, new XFont(fontName, fontSize.Inch * 6.0225));
				} catch (Exception) {
					size = new XSize(1, 1);
				}
				s.Width = Math.Max(s.Width, size.Width);
				s.Height += size.Height * Math.Ceiling(size.Width * 0.166044 / maxWidth.Inch);
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
				s.Width += (para.Format.LeftIndent.Inch + para.Format.RightIndent.Inch) * 6.0225;
				s.Height += (para.Format.SpaceBefore.Inch + para.Format.SpaceAfter.Inch) * 6.0225;
			} else if (o is Table) {
				var table = (Table)o;
				var family = table.Format.Font.Name;
				var h = 0.0;
				var w = 0.0;
				var maxH = 0.0;
				for (var i = 0; i < table.Rows.Count; i++) {
					for (var j = 0; j < table.Rows[i].Cells.Count; j++) {
						var curH = 0.0;
						for (var k = 0; k < table.Rows[i].Cells[j].Elements.Count; k++) {
							var size = GetSize(ctx, table.Rows[i].Cells[j].Elements[k], family, fontSize, table.Columns[j].Width);
							curH += size.Height;
						}
						maxH = Math.Max(maxH, curH);
					}
					h += Math.Max(table.Rows[i].Height.Inch * 6.0225, maxH);
					maxH = 0;
				}
				for (var i = 0; i < table.Columns.Count; i++) {
					w += table.Columns[i].Width.Inch * 6.0225;
				}
				s.Width += w;
				s.Height += h;
			} else if (o is Row) {
				var row = (Row)o;
				var family = row.Format.Font.Name;
				var h = 0.0;
				var w = 0.0;
				var maxH = 0.0;
				s.Height += Math.Max(row.Height.Inch * 6.0225, maxH);				
				
				//for (var i = 0; i < row.Cells.Count; i++) {
				//	w += row.Cells[i].Column.Width.Inch * 6.0225;
				//}

				//Width is not calculated.

				s.Width += w;
				s.Height += h;
			} else if (o is Character) {

			} else {
				throw new Exception("donno this type:" + o.NotNull(x => x.GetType()));
			}
			return s;
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

		public static List<Tuple<DocumentObject, Unit>> GetHeights(Unit width, IEnumerable<DocumentObject> paragraphs) {
			var ctx = XGraphics.CreateMeasureContext(new XSize(width.Inch, Unit.FromInch(1000)), XGraphicsUnit.Inch, XPageDirection.Downwards);
			return paragraphs.Select(x => {
				var f = GetFontFamily(x);
				var s = GetFontSize(x);
				var size = GetSize(ctx, x, f, s, width);
				var height = Unit.FromInch(size.Height* 0.166044002);

				return Tuple.Create(x, height);
			}).ToList();
		}

		public static List<List<Tuple<DocumentObject, Unit>>> SplitHeights(Unit width, Unit[] heights, IEnumerable<DocumentObject> paragraphs) {
			Unit cumulative = 0;
			var splits = new List<List<Tuple<DocumentObject, Unit>>>();
			var curHeight = heights[0];
			var page = 0;
			var heightObj = GetHeights(width, paragraphs);
			var curSplit = new List<Tuple<DocumentObject, Unit>>();
			for (var i = 0; i < heightObj.Count(); i++) {
				var ho = heightObj[i];
				cumulative += ho.Item2;
				if (cumulative > curHeight && curSplit.Any()) {
					//next
					page += 1;
					curHeight = heights[Math.Min(page, heights.Count() - 1)];
					cumulative = 0;
					splits.Add(curSplit);
					curSplit = new List<Tuple<DocumentObject, Unit>>();
				}
				curSplit.Add(ho);

			}
			if (curSplit.Any()) {
				splits.Add(curSplit);
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
				var curHeight = 0.0;
				var curWidth = 0.0;
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

				if (curHeight < 6.0225 * height.Inch || fontSize <= minSize) {
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
			section.Footers.Primary.AddParagraph("© 2003 - " + DateTime.UtcNow.Year + " EOS. All Rights Reserved.");

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

			var TableGray = new Color(100, 100, 100, 100);
			var TableBlack = new Color(0, 0, 0);
			/////////////////////////////

			var section = AddVtoPage(doc, vto.Name ?? "", "VISION");

			/////////////////////////////

			var vision = section.AddTable();
			vision.Style = "Table";
			vision.Borders.Color = TableBlack;
			vision.Borders.Width = 1;
			//table.Borders.Left.Width = 0.25;
			//table.Borders.Right.Width = 0.25;
			//table.Borders.Top.Width = 7.0/8.0;
			vision.Rows.LeftIndent = 0;
			vision.LeftPadding = 0;
			vision.RightPadding = 0;

			vision.AddColumn(Unit.FromInch(1.66 + 5.33));
			//vision.AddColumn(Unit.FromInch());
			vision.AddColumn(Unit.FromInch(3.4));

			var vrow = vision.AddRow();

			var vtoLeft = vrow.Cells[0].Elements.AddTable();
			var column = vtoLeft.AddColumn(Unit.FromInch(1.66));
			column = vtoLeft.AddColumn(Unit.FromInch(5.33));

			var row = vtoLeft.AddRow();
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
			var coreValue = row.Cells[1];//.AddParagraph("CV-asdf");


			var values = vto.Values.ToList();
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




			ResizeToFit(coreValue, Unit.FromInch(5.33), Unit.FromInch(1.2), (cell, fs) => {
				var o = new List<Paragraph>();
				return OrderedList(values.Select(x => x.CompanyValue), ListType.NumberList1);
			}, maxFontSize: Unit.FromPoint(10));

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
			var coreFocus = row.Cells[1];
			cfTitle.Format.Alignment = ParagraphAlignment.Center;
			row.VerticalAlignment = VerticalAlignment.Center;

			ResizeToFit(coreFocus, Unit.FromInch(5.33), Unit.FromInch(1.2), (cell, fs) => {
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
			var tenYear = row.Cells[1];
			tyTitle.Format.Alignment = ParagraphAlignment.Center;
			row.VerticalAlignment = VerticalAlignment.Center;

			ResizeToFit(tenYear, Unit.FromInch(5.33), Unit.FromInch(.6), (cell, fs) => {
				var o = new List<Paragraph>();
				var p1 = new Paragraph();
				p1.Format.Font.Name = "Arial Narrow";
				p1.AddText(vto.TenYearTarget ?? "");
				o.Add(p1);
				return o;
			}, maxFontSize: 10);


			row = vtoLeft.AddRow();
			var msTitle = row.Cells[0];
			msTitle.Shading.Color = TableGray;
			msTitle.AddParagraph(vto.Strategy.MarketingStrategyTitle ?? "MARKETING STRATEGY");
			msTitle.Format.Font.Name = "Arial Narrow";
			var marketingStrategy = row.Cells[1];
			msTitle.Format.Font.Bold = true;
			msTitle.Format.Font.Size = 14;
			row.Height = Unit.FromInch(2.7);
			msTitle.Format.Alignment = ParagraphAlignment.Center;
			row.VerticalAlignment = VerticalAlignment.Center;
			row.Borders.Right.Color = TableBlack;

			var uniques = vto.Strategy.Uniques.ToList();

			ResizeToFit(marketingStrategy, Unit.FromInch(5.33), Unit.FromInch(2.7), (cell, fs) => {
				var o = new List<Paragraph>();
				var p1 = new Paragraph();
				var txt = p1.AddFormattedText("Target Market/\"The List\": ", TextFormat.Bold);
				p1.Format.Font.Name = "Arial Narrow";
				p1.AddText(vto.Strategy.TargetMarket ?? "");
				o.Add(p1);

				var p2 = new Paragraph();
				p2.Format.SpaceBefore = fs * 1.5;
				var uniquesTitle = "Uniques: ";
				if (uniques.Count == 3)
					uniquesTitle = "Three " + uniquesTitle;
				p2.AddFormattedText(uniquesTitle, TextFormat.Bold);
				p2.Format.Font.Name = "Arial Narrow";
				o.Add(p2);

				o.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44)));

				var p3 = new Paragraph();
				p3.Format.SpaceBefore = fs * 1.5;
				p3.AddFormattedText("Proven Process: ", TextFormat.Bold);
				p3.Format.Font.Name = "Arial Narrow";
				p3.AddText(vto.Strategy.ProvenProcess ?? "");
				o.Add(p3);

				var p4 = new Paragraph();
				p4.Format.SpaceBefore = fs * 1.5;
				p4.AddFormattedText("Guarantee: ", TextFormat.Bold);
				p4.Format.Font.Name = "Arial Narrow";
				p4.AddText(vto.Strategy.Guarantee ?? "");
				o.Add(p4);

				return o;
			}, maxFontSize: 10);



			//RIGHT SIDE

			var vtoRight = vrow.Cells[1].Elements.AddTable();
			column = vtoRight.AddColumn(Unit.FromInch(3.4));

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
			var threeYear = row.Cells[0];
			var looksList = vto.ThreeYearPicture.LooksLike.Where(x => !string.IsNullOrWhiteSpace(x.Data)).Select(x => x.Data).ToList();

			ResizeToFit(threeYear, Unit.FromInch(3.4), Unit.FromInch(5.15), (cell, fs) => {
				var o = new List<Paragraph>();
				o.AddRange(AddVtoSectionHeader(vto.ThreeYearPicture, fs, dateformat));
				var p = new Paragraph();
				p.AddFormattedText("What does it look like?", TextFormat.Bold | TextFormat.Underline);
				p.Format.Font.Name = "Arial Narrow";
				o.Add(p);
				o.AddRange(OrderedList(looksList, ListType.BulletList1));
				return o;
			}, maxFontSize: 10);
		}
		
		private static void AddVtoTraction(Document doc, AngularVTO vto, string dateformat) {
			Cell oneYear, quarterlyRocks, issuesList;
			Table issueTable, rockTable, goalTable;
			Unit baseHeight = Unit.FromInch(5.1);//5.15
			AddPage_VtoTraction(doc, vto, baseHeight, out oneYear, out quarterlyRocks, out issuesList, out issueTable, out rockTable, out goalTable);

			Unit fs = 10;
			var goalObjects = new List<DocumentObject>();
			var goalsSplits = new List<List<Tuple<DocumentObject, Unit>>>();
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
			var rockSplits = new List<List<Tuple<DocumentObject, Unit>>>();
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
			var issueSplits = new List<List<Tuple<DocumentObject, Unit>>>();
			var issueRows = new List<Row>();
			{
				var issues = vto.Issues.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();

				//issuesList.Elements.AddParagraph(" ").SpaceBefore = Unit.FromInch(0.095);
				//ResizeToFit(issuesList, Unit.FromInch(3.47), Unit.FromInch(5.15), (cell, fs) => {


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
				issueSplits = SplitHeights(Unit.FromInch(3), new[] { (baseHeight),(baseHeight) }, issueRows);
				issuesObjects.Add(issueTable);

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

				if (p+1 < maxPage) {
					AddPage_VtoTraction(doc, vto, baseHeight, out oneYear, out quarterlyRocks, out issuesList, out issueTable, out rockTable, out goalTable);
					AppendAll(oneYear, new DocumentObject[] { goalTable }.ToList());
					AppendAll(quarterlyRocks, new DocumentObject[] { rockTable }.ToList());
					AppendAll(issuesList, new DocumentObject[] {issueTable }.ToList());

				}
			}
		}

		private static void AddPage_VtoTraction(Document doc, AngularVTO vto,Unit height, out Cell oneYear, out Cell quarterlyRocks, out Cell issuesList, out Table issueTable, out Table rockTable, out Table goalTable) {
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


	}
}