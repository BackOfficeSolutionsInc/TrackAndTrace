using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using RadialReview.Accessors.PDF;
using RadialReview.Areas.People.Angular;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Areas.People.Accessors.PDF {
	public class PeopleAnalyzerPdf {

		private static ForModel GetIt = new ForModel() { ModelId = -1, ModelType = "get", _PrettyString = "Get it" };
		private static ForModel WantIt = new ForModel() { ModelId = -1, ModelType = "want", _PrettyString = "Want it" };
		private static ForModel Cap = new ForModel() { ModelId = -1, ModelType = "cap", _PrettyString = "Capacity to do it" };
		private static Unit FONT_SIZE = 12;

		private class CellLocation {
			public Unit X;
			public Unit Y;
			public Unit Width;
			public string Text;
			public Unit LineX;
			public Unit LineY;
			public Unit LineWidth;
		}

		/// <summary>
		/// A hack to draw low level text on the PDF
		/// </summary>
		/// <param name="docRenderer"></param>
		/// <param name="renderer"></param>
		/// <param name="taggedCells"></param>
		/// <param name="font"></param>
		/// <param name="angleDeg"></param>
		private static void AddDiagonalText(DocumentRenderer docRenderer, PdfDocumentRenderer renderer,PdfSettings settings, List<Cell> taggedCells, XFont font, double angleDeg) {
			using (XGraphics gfx = XGraphics.FromPdfPage(renderer.PdfDocument.Pages[0])) {
				CellLocation cellLocation;
				XGraphicsState state;
				foreach (var cell in taggedCells) {

					cellLocation = (CellLocation)cell.Tag;

					var pen = new XPen(settings.BorderXColor, .5);
					var p1 = new XPoint(cellLocation.LineX, cellLocation.LineY);
					var p2 = new XPoint(
							cellLocation.LineX + Math.Cos(Math.PI / 180 * angleDeg) * cellLocation.LineWidth,
							cellLocation.LineY - Math.Sin(Math.PI / 180 * angleDeg) * cellLocation.LineWidth
						);

					var p3 = new XPoint(cellLocation.LineX + cellLocation.Width, cellLocation.LineY);
					var p4 = new XPoint(
							cellLocation.LineX + cellLocation.Width + Math.Cos(Math.PI / 180 * angleDeg) * cellLocation.LineWidth,
							cellLocation.LineY - Math.Sin(Math.PI / 180 * angleDeg) * cellLocation.LineWidth
						);

					gfx.DrawLine(pen, p1, p2);
					gfx.DrawLine(pen, p3, p4);
					gfx.DrawLine(pen, p2, p4);

					XRect position = new XRect(cellLocation.X, cellLocation.Y, cellLocation.Width, 0);
					state = gfx.Save();
					gfx.RotateAtTransform(-angleDeg, new XPoint(cellLocation.X, cellLocation.Y));
					gfx.DrawString(cellLocation.Text ?? "", font, XBrushes.Black, position);

					gfx.Restore(state);
				}
			}
		}

		/// <summary>
		/// Grab all the cells that require diagonal text in them
		/// </summary>
		/// <param name="docRenderer"></param>
		/// <returns></returns>
		private static List<Cell> GetTaggedCells(DocumentRenderer docRenderer) {
			List<Cell> taggedCells = new List<Cell>();
			DocumentObject[] docObjects = docRenderer.GetDocumentObjectsFromPage(1);
			if (docObjects != null && docObjects.Length > 0) {
				for (int i = 0; i < docObjects.Length; i++) {
					if (docObjects[i].GetType() == typeof(Table)) {
						Table tbl = (Table)docObjects[i];
						for (int j = 0; j < tbl.Rows.Count; j++) {
							for (int k = 0; k < tbl.Columns.Count; k++) {
								Cell c = tbl[j, k];
								if (c.Tag != null) {
									taggedCells.Add(c);
								}
							}
						}
					}
				}
			}
			return taggedCells;
		}

		
		/// <summary>
		/// Grab the data to display in a particular cell, given a (row,question) pair
		/// MaxDate specified the range of QC data to use, IE QC IssueDates up to and including maxDate
		/// </summary>
		/// <param name="pa"></param>
		/// <param name="row"></param>
		/// <param name="question"></param>
		/// <param name="maxDate"></param>
		/// <returns></returns>
		private static AngularPeopleAnalyzerResponse LookupCell(AngularPeopleAnalyzer pa, AngularPeopleAnalyzerRow row, IForModel question, DateTime? maxDate = null) {
			maxDate = maxDate ?? DateTime.MaxValue;

			var avail = pa.Responses.Where(x => {
				return x.Source.ModelId == question.ModelId &&
						x.Source.ModelType == question.ModelType &&
						x.About.ModelId == row.About.ModelId &&
						x.About.ModelType == row.About.ModelType &&
						x.IssueDate <= maxDate.Value;
			});

			if (avail.Any()) {
				var ordered = avail.GroupBy(x => {
					return x.IssueDate.Value;
				}).OrderByDescending(x => {
					return x.Key;//.IssueDate.getTime();
				}).First();
				var selected = ordered.OrderByDescending(x => {
					return x.Override;
				}).First();

				return selected;
			}
			return null;


		}

		/// <summary>
		/// Adds a People analyzer to the PDF. Please note, don't render the "Doc", instead render the return result
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="doc"></param>
		/// <param name="pa"></param>
		/// <param name="beforeDate"></param>
		/// <returns></returns>
		public static PdfDocumentRenderer AppendPeopleAnalyzer(UserOrganizationModel caller, Document doc, AngularPeopleAnalyzer pa, PdfSettings settings, DateTime? beforeDate = null) {
			settings = settings ?? new PdfSettings();
			beforeDate = beforeDate ?? DateTime.MaxValue;

			var resultColumns = 3 + pa.Values.Distinct(x => x.Source.PrettyString).Count();

			//Setup doc
			var section = doc.AddSection();
			section.PageSetup.LeftMargin = Unit.FromInch(.5);
			section.PageSetup.RightMargin = Unit.FromInch(1);
			section.PageSetup.TopMargin = Unit.FromInch(.5);
			section.PageSetup.BottomMargin = Unit.FromInch(.5);

			var pageWidth = doc.DefaultPageSetup.PageWidth;
			if (resultColumns > 8) {
				/*landscape*/
				section.PageSetup.Orientation = Orientation.Landscape;
				pageWidth = doc.DefaultPageSetup.PageHeight;

			}
			//Calc widths/heights
			var topMargin = section.PageSetup.TopMargin;
			var leftMargin = section.PageSetup.LeftMargin;
			var rightMargin = section.PageSetup.RightMargin;
			var usableWidth = pageWidth - leftMargin - rightMargin;
			var headerHeight = Unit.FromInch(1.5);
			var titleWidth = Unit.FromInch(2);
			var cellWidth = (usableWidth - titleWidth) / (resultColumns + 1);
			var rowHeight = Unit.FromInch(.34);

			//settigns the cell to simulate padding
			var maxCellWidth = Unit.FromInch(.57);
			var addlPadding = 0.0;
			if (cellWidth > maxCellWidth) {
				addlPadding = (1 + resultColumns) * (cellWidth - maxCellWidth) / 2.0;
				cellWidth = maxCellWidth;
			}

			//Specify diagonal info
			var angle = 45.0; //degrees
			var fs = FONT_SIZE;

			//Add a spacer to top of document
			var spacer = section.AddParagraph();
			spacer.Format.Font.Size = .1;
			spacer.Format.SpaceBefore = headerHeight;
			
			//setup table
			var table = section.AddTable();
			table.Rows.LeftIndent = addlPadding;
			table.Borders.Color = settings.BorderColor;

			//... define Columns
			table.AddColumn(titleWidth);
			for (var i = 0; i < resultColumns; i++) {
				//One column for each result
				table.AddColumn(cellWidth);
			}

			//select Values columns
			var questions = pa.Values.Distinct(x => x.Source.PrettyString).Select(x => (IForModel)x.Source).ToList();
			//add the GWC columns
			questions.Add(GetIt);
			questions.Add(WantIt);
			questions.Add(Cap);

			
			var isHeading = true;
			foreach (var row in pa.Rows) {
				var r = table.AddRow();
				r.Height = rowHeight;
				r.HeightRule = RowHeightRule.AtLeast;
				var headPara = r.Cells[0].AddParagraph(row.About.PrettyString);
				headPara.Format.Alignment = ParagraphAlignment.Center;
				r.Cells[0].VerticalAlignment = VerticalAlignment.Center;

				for (var i = 0; i < questions.Count; i++) {
					//Contents
					var value = "";
					var lu = LookupCell(pa, row, questions[i], beforeDate);
					if (lu != null)
						value = (lu.AnswerFormatted ?? "").Replace("-", "–");
					var p = r.Cells[1 + i].AddParagraph(value);
					p.Format.Alignment = ParagraphAlignment.Center;
					r.Cells[1 + i].VerticalAlignment = VerticalAlignment.Center;

					formatAnswer(lu, p);

					if (isHeading) {
						//Only on Heading
						var lines = 1;
						var textHeight = fs * lines;

						var horizOffset = Math.Cos(Math.PI / 180 * (90 - angle)) * textHeight;

						r.Cells[i + 1].Tag = new CellLocation() {
							Text = questions[i].ToPrettyString() ?? "",
							Width = cellWidth,
							X = leftMargin + addlPadding + titleWidth + cellWidth * (i + .5) + horizOffset * .5,
							Y = headerHeight + topMargin,
							LineX = leftMargin + addlPadding + titleWidth + cellWidth * i,
							LineY = headerHeight + topMargin,
							LineWidth = headerHeight / Math.Sin(Math.PI / 180 * angle)

						};
					}
				}
				isHeading = false;
			}

			DocumentRenderer docRenderer = new DocumentRenderer(doc);
			docRenderer.PrepareDocument();
			var taggedCells = GetTaggedCells(docRenderer);

			PdfDocumentRenderer renderer = new PdfDocumentRenderer();

			renderer.Document = doc;
			renderer.RenderDocument();

			XFont font = new XFont("Arial", fs, XFontStyle.Regular);
			AddDiagonalText(docRenderer, renderer,settings, taggedCells, font, angle);

			var container = pa.NotNull(y => y.SurveyContainers.Where(x => x.IssueDate <= beforeDate).OrderBy(x => x.IssueDate).LastOrDefault());
			if (container != null) {
				font = new XFont("Arial", fs+2, XFontStyle.Bold);
				AddTitleText(caller, docRenderer, renderer, beforeDate.Value, font, new XPoint(leftMargin + FONT_SIZE, topMargin + FONT_SIZE));
			}

			return renderer;
		}

		private static void AddTitleText(UserOrganizationModel caller, DocumentRenderer docRenderer, PdfDocumentRenderer renderer, DateTime date, XFont font, XPoint position) {
			using (XGraphics gfx = XGraphics.FromPdfPage(renderer.PdfDocument.Pages[0])) {
				gfx.DrawString("People Analyzer"/*container.Name ?? ""*/, font, XBrushes.Black, position);

				var datePos = new XPoint(position.X,position.Y+font.Size);
				var dateFont = new XFont(font.FontFamily.Name, font.Size - 2, XFontStyle.Regular, font.PdfOptions);

				var useDate = "as of "+date.ToLongDateString();
				if (date > DateTime.UtcNow)
					useDate = "as of "+caller.GetTimeSettings().ConvertFromServerTime(DateTime.UtcNow).Date.ToLongDateString();

				gfx.DrawString(useDate, dateFont, XBrushes.Gray,datePos);

			}

		}

		private static void formatAnswer(AngularPeopleAnalyzerResponse response, Paragraph p) {

			var red = Color.FromRgb(217, 83, 79);
			var yellow = Color.FromRgb(240, 173, 78);
			var green = Color.FromRgb(92, 184, 92);

			var dictColor = new DefaultDictionary<string, Color?>(x => null);
			dictColor.Add("often", green);
			dictColor.Add("not-often", red);
			dictColor.Add("sometimes", yellow);
			dictColor.Add("yes", green);
			dictColor.Add("no", red);

			var dictSize = new DefaultDictionary<string, float?>(x => null);
			dictSize.Add("often", FONT_SIZE + 1);
			dictSize.Add("not-often", FONT_SIZE + 1);
			dictSize.Add("sometimes", FONT_SIZE + 1);

			var answer = response.NotNull(x => x.Answer);
			if (answer != null) {
				var color = dictColor[answer];
				if (color != null) {
					p.Format.Font.Color = color.Value;
				}
				var size = dictSize[answer];
				if (size != null) {
					p.Format.Font.Size += size.Value;
				}
			}
		}
	}	
}