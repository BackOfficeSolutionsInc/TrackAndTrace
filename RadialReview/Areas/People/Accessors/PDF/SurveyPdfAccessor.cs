using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Drawing;
using RadialReview.Accessors;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.Constants;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Accessors.PDF {
	public class SurveyPdfAccessor {


		public static Color TableGray = new Color(100, 100, 100, 100);
		public static Color TableDark = new Color(50, 50, 50);
		public static Color TableBlack = new Color(0, 0, 0);


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


		public static void AppendSurveyAbout(Document doc, ISurveyAbout survey) {
			var iSections = survey.GetSections().OrderBy(x => x.GetOrdering());
			var section = doc.AddSection();
			foreach (var iSection in iSections) {
				AddSurveySection(section, iSection);
			}
		}

		protected static void AddSectionTitle(Cell section, string pageTitle, Orientation orientation = Orientation.Portrait) {
			var frame = section.Elements.AddTextFrame();
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

		}

		protected static void AddSurveySection(Section section, ISectionAbout iSection) {
			//var section = doc.AddSection();
			var usableWidth = section.Document.DefaultPageSetup.PageWidth - section.Document.DefaultPageSetup.LeftMargin - section.Document.DefaultPageSetup.RightMargin;
			var superTable = section.AddTable();
			superTable.AddColumn(usableWidth);
			var superRow = superTable.AddRow();
			var cell = superRow.Cells[0];
			//cell.Borders.Color = Color.FromArgb(150, 0, 0, 150);


			AddSectionTitle(cell, iSection.GetName());
			PdfSectionFactory.CreateSection(cell, iSection, usableWidth);

			var p = cell.AddParagraph();
			p.Format.SpaceAfter = Unit.FromInch(.15);
		}
	}

	public class PdfSectionFactory {

		public static XFont _FontLargeBold = new XFont("Verdana", 20, XFontStyle.Bold);
		public static XFont _Font = new XFont("Verdana", 10, XFontStyle.Regular);
		public static XFont _FontBold = new XFont("Verdana", 10, XFontStyle.Bold);
		//public static XFont _Font8 = new XFont("Verdana", 8, XFontStyle.Regular);

		public static XFont _Font7 = new XFont("Verdana", 7, XFontStyle.Regular);
		public static XFont _Font7Bold = new XFont("Verdana", 7, XFontStyle.Bold);
		//public static XFont _Font7Bold = new XFont("Verdana", 7, XFontStyle.Bold);
		public static Color _Black = Color.FromArgb(255, 51, 51, 51);
		public static XBrush _BlackText = new XSolidBrush(XColor.FromArgb((int)_Black.A, (int)_Black.R, (int)_Black.G, (int)_Black.B));
		public static Color _Gray = Color.FromArgb((128 + 255) / 2, 51, 51, 51);
		public static XBrush _GrayText = new XSolidBrush(XColor.FromArgb((int)_Gray.A, (int)_Gray.R, (int)_Gray.G, (int)_Gray.B));
		public static Unit _DefaultMargin = Unit.FromInch(0.3);

		public static Unit BottomPad = Unit.FromInch(.25);


		private static XPoint[] Cross(double x, double y, double size = 1.0) {
			var xpointArray = new[] {
				new XPoint(-0.223606797749978 * size + x, 0.0 * size + y),
				new XPoint(-0.447213595499958 * size + x, -0.223606797749978 * size + y),
				new XPoint(-0.223606797749978 * size + x, -0.447213595499958 * size + y),
				new XPoint(0.0 * size + x, -0.223606797749978 * size + y),
				new XPoint(0.223606797749978 * size + x, -0.447213595499958 * size + y),
				new XPoint(0.447213595499958 * size + x, -0.223606797749978 * size + y),
				new XPoint(0.223606797749978 * size + x, 0.0 * size + y),
				new XPoint(0.447213595499958 * size + x, 0.223606797749978 * size + y),
				new XPoint(0.223606797749978 * size + x, 0.447213595499958 * size + y),
				new XPoint(0.0 * size + x, 0.223606797749978 * size + y),
				new XPoint(-0.223606797749978 * size + x, 0.447213595499958 * size + y),
				new XPoint(-0.447213595499958 * size + x, 0.223606797749978 * size + y),
			};
			return xpointArray;
		}

		private static XPoint[] Check(double x, double y, double size = 1.0) {
			return new[] {
				new XPoint(-0.134259259259258*size+x,0.402777777777779*size+y),
				new XPoint(0.449074074074075*size+x,-0.180555555555555*size+y),
				new XPoint(0.282407407407408*size+x,-0.347222222222223*size+y),
				new XPoint(-0.134259259259258*size+x,0.0694444444444446*size+y),
				new XPoint(-0.300925925925925*size+x,-0.097222222222223*size+y),
				new XPoint(-0.467592592592592*size+x,0.0694444444444446*size+y),

			};
		}

		//var complete = managerRockState.IsComplete();
		//		if (complete != null) {
		//			if (complete.Value) {
		//				gfx.DrawPolygon(XBrushes.White, Check(completionBox.Center.X, completionBox.Center.Y, rockCompletionBoxW* 0.6), XFillMode.Winding);
		//			} else {
		//				gfx.DrawPolygon(XBrushes.White, Cross(completionBox.Center.X, completionBox.Center.Y, rockCompletionBoxW* 0.6), XFillMode.Winding);
		//			}
		//		} else {					
		//			gfx.DrawString("Not Set", _Font8Bold, XBrushes.White, completionBox, XStringFormats.Center);
		//		}




		public static Font HeadingFont = new Font("Verdana", 12);
		static PdfSectionFactory() {
			HeadingFont.Underline = Underline.Single;
			HeadingFont.Bold = true;
		}


		public static void CreateSection(Cell cell, ISectionAbout isection, Unit usableWidth) {
			switch (isection.GetSectionType()) {
				case "Rocks":
					AddRocksSection(cell, isection, usableWidth);
					break;
				case "Roles":
					AddRolesSection(cell, isection, usableWidth);
					break;
				case "Values":
					AddValuesSection(cell, isection, usableWidth);
					break;
				default:
					throw new ArgumentOutOfRangeException("Unknown section type");
			}
		}

		private static void AddValuesSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {
			var questions = iSection.GetItemContainers();

			var table = new Table();
			cell.Elements.Add(table);

			var bys = questions.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());

			//ADD COLUMNS:
			//Value Column
			var valueColumn = table.AddColumn(usableWidth / 2.0);

			//By Column
			var byWidth = usableWidth / 2.0 / bys.Count();
			foreach (var by in bys) {
				table.AddColumn(byWidth);
			}

			//ADD HEADER
			var headerRow = table.AddRow();
			headerRow.Height = BottomPad * 1.5;
			headerRow.HeightRule = RowHeightRule.AtLeast;
			for (var i = 0; i < bys.Count(); i++) {
				var c = headerRow.Cells[i + 1];
				var paragraph = c.AddParagraph();
				paragraph.AddFormattedText(bys.ElementAt(i).ToPrettyString() ?? "err", TextFormat.Underline);
				c.VerticalAlignment = VerticalAlignment.Center;
			}


			//ADD VALUE ROWS
			var valueQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Value).ToList();
			foreach (var valueItemContainer in valueQuestions) {
				//Construct Question
				var row = table.AddRow();
				var valuePara = row.Cells[0].AddParagraph();
				valuePara.AddFormattedText(valueItemContainer.GetName() ?? "", TextFormat.Bold);
				valuePara = row.Cells[0].AddParagraph();
				valuePara.AddFormattedText(valueItemContainer.GetHelp() ?? "");
				valuePara.Format.SpaceAfter = BottomPad;

				//Add Responses
				var responseLookup = valueItemContainer.GetResponses().ToDefaultDictionary(x => x.GetByAbout().GetBy().ToKey(), x => x, x => null);
				for (var i = 0; i < bys.Count(); i++) {
					var c = row.Cells[i + 1];
					var by = bys.ElementAt(i);
					var paragraph = c.AddParagraph();
					var itemContainer = valueItemContainer;
					var options = itemContainer.GetFormat().GetSetting<Dictionary<string, object>>("options");
					var rawResponse = responseLookup[by.ToKey()].NotNull(x => x.GetAnswer());
					if (rawResponse != null) {
						var response = (string)options.GetOrDefault(rawResponse, rawResponse);
						paragraph.AddFormattedText(response ?? "");
					} else {
						paragraph.Format.Font.Color = _Gray;
						paragraph.Format.Font.Italic = true;
						paragraph.AddFormattedText("No response");
					}
				}
			}


			//ADD GENERAL COMMENTS
			var generalComments = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.GeneralComment);
			if (generalComments.SelectMany(x => x.GetResponses().Where(y => !string.IsNullOrWhiteSpace(y.GetAnswer()))).Any()) {
				var genCommentsHeading = cell.AddParagraph();
				genCommentsHeading.Format.SpaceBefore = BottomPad;
				genCommentsHeading.AddFormattedText("General Comments", HeadingFont);
				genCommentsHeading.Format.SpaceAfter = BottomPad;

				foreach (var generalCommentQuestion in generalComments) {
					//Do we have a response?
					foreach (var comment in generalCommentQuestion.GetResponses()) {
						var answer = comment.GetAnswer();
						var by = comment.GetByAbout().GetBy().ToPrettyString();
						//Do we have an answer
						if (!string.IsNullOrEmpty(answer)) {
							var paragraph = cell.AddParagraph();
							paragraph.AddFormattedText(by ?? "", TextFormat.Bold);
							paragraph = cell.AddParagraph();
							paragraph.AddText(answer ?? "");
							paragraph.Format.SpaceAfter = BottomPad;
						}
					}
				}
			}
		}

		private static void AddRolesSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {

			var questions = iSection.GetItemContainers();
			var table = new Table();

			cell.Elements.Add(table);

			var bys = questions.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());

			//ADD COLUMNS:
			var paddingLeft = (usableWidth / 4.0) / 2.0;
			var answerWidth = usableWidth * 3.0 / 4.0;
			var responseColumn = table.AddColumn(usableWidth);

			var rolesQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Role).ToList();

			var rolesCellRow = table.AddRow();
			rolesCellRow.Format.Alignment = ParagraphAlignment.Center;
			var rolesCell = rolesCellRow.Cells[0];

			var gwcCellRow = table.AddRow();
			gwcCellRow.Format.Alignment = ParagraphAlignment.Center;
			var gwcCell = gwcCellRow.Cells[0];

			rolesCell.Format.Alignment = ParagraphAlignment.Center;
			gwcCell.Format.Alignment = ParagraphAlignment.Center;

			var roleTable = new Table();

			var tableWidth = Unit.FromInch(.2) + Unit.FromInch(2);
			var left = (usableWidth - tableWidth) / 2.0;
			roleTable.AddColumn(left);
			//roleTable.AddColumn(Unit.FromInch(.2));
			roleTable.AddColumn(Unit.FromInch(2));


			//ROLES 
			var ii = 1;
			foreach (var roleItemContainer in rolesQuestions) {
				var row = roleTable.AddRow();
				var p = row.Cells[1].AddParagraph((roleItemContainer.GetName() ?? ""));
				p.Format.Font.Bold = true;
				p.Format.Alignment = ParagraphAlignment.Center;
				ii++;
			}
			var spacer = roleTable.AddRow();
			spacer.Height = BottomPad;
			spacer.HeightRule = RowHeightRule.AtLeast;

			rolesCell.Elements.Add(roleTable);

			var gwcTable = new Table();
			var titleWidth = Unit.FromInch(1.5);
			gwcTable.AddColumn((usableWidth - answerWidth) / 2.0);
			gwcTable.AddColumn(titleWidth);
			answerWidth -= Unit.FromInch(1.5);
			var colWidth = answerWidth / Math.Max(1, bys.Count());
			for (var i = 0; i < bys.Count(); i++) {
				gwcTable.AddColumn(colWidth);
			}

			//ADD HEADER
			var headerRow = gwcTable.AddRow();
			headerRow.Height = BottomPad * 1.5;
			headerRow.HeightRule = RowHeightRule.AtLeast;
			for (var i = 0; i < bys.Count(); i++) {
				var c = headerRow.Cells[i + 2];
				var paragraph = c.AddParagraph();
				var p = paragraph.AddFormattedText(bys.ElementAt(i).ToPrettyString() ?? "err", TextFormat.Underline);
				c.VerticalAlignment = VerticalAlignment.Center;
				paragraph.Format.Alignment = ParagraphAlignment.Center;
			}

			var rolesAnswers = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.GWC).ToList();
			var responseLookup = rolesAnswers.ToDictionary(
										x => x.GetFormat().GetSetting<string>("gwc"),
										y => y.GetResponses().ToDictionary(
											x => x.GetByAbout().GetBy().ToKey(),
											x => x
										)
									);

			var gwc = new Dictionary<string, string> { { "get", "Get it" }, { "want", "Want it" }, { "cap", "Capacity to do it" } };
			var order = new[] { "get", "want", "cap" };

			//GWC RESPONSES
			for (var i = 0; i < order.Count(); i++) {
				var questionKey = order[i];
				var gwcStr = gwc[questionKey];

				var row = gwcTable.AddRow();
				var p = row.Cells[1].AddParagraph(gwcStr);
				p.Format.Alignment = ParagraphAlignment.Right;

				for (var j = 0; j < bys.Count(); j++) {
					var by = bys.ElementAt(j);
					var userResponse = responseLookup[questionKey][by.ToKey()];
					var para = row.Cells[j + 2].AddParagraph(userResponse.GetAnswer() ?? "na");
					para.Format.Alignment = ParagraphAlignment.Center;
				}
			}
			gwcCell.Elements.Add(gwcTable);

			//ADD GENERAL COMMENTS
			var generalComments = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.GeneralComment);
			if (generalComments.SelectMany(x => x.GetResponses().Where(y => !string.IsNullOrWhiteSpace(y.GetAnswer()))).Any()) {
				var genCommentsHeading = cell.AddParagraph();
				genCommentsHeading.Format.SpaceBefore = BottomPad;
				genCommentsHeading.AddFormattedText("General Comments", HeadingFont);
				genCommentsHeading.Format.SpaceAfter = BottomPad;

				foreach (var generalCommentQuestion in generalComments) {
					//Do we have a response?
					foreach (var comment in generalCommentQuestion.GetResponses()) {
						var answer = comment.GetAnswer();
						var by = comment.GetByAbout().GetBy().ToPrettyString();
						//Do we have an answer
						if (!string.IsNullOrEmpty(answer)) {
							var paragraph = cell.AddParagraph();
							paragraph.AddFormattedText(by ?? "", TextFormat.Bold);
							paragraph = cell.AddParagraph();
							paragraph.AddText(answer ?? "");
							paragraph.Format.SpaceAfter = BottomPad;
						}
					}
				}
			}
		}

		private static void AddRocksSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {

			var questions = iSection.GetItemContainers();

			var table = new Table();
			cell.Elements.Add(table);

			var bys = questions.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());

			//ADD COLUMNS:
			//Rock Column
			var rockColumn = table.AddColumn(usableWidth / 2.0);

			//By Column
			var byWidth = usableWidth / 2.0 / bys.Count();
			foreach (var by in bys) {
				table.AddColumn(byWidth);
			}

			//ADD HEADER
			var headerRow = table.AddRow();
			headerRow.Height = BottomPad * 1.5;
			headerRow.HeightRule = RowHeightRule.AtLeast;
			for (var i = 0; i < bys.Count(); i++) {
				var c = headerRow.Cells[i + 1];
				var paragraph = c.AddParagraph();
				paragraph.AddFormattedText(bys.ElementAt(i).ToPrettyString() ?? "err", TextFormat.Underline);
				c.VerticalAlignment = VerticalAlignment.Center;
			}

			//ADD ROWS
			var rockQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Rock).ToList();
			foreach (var valueItemContainer in rockQuestions) {
				//Construct Question
				var row = table.AddRow();
				var valuePara = row.Cells[0].AddParagraph();
				valuePara.AddFormattedText(valueItemContainer.GetName() ?? "", TextFormat.Bold);
				valuePara = row.Cells[0].AddParagraph();
				valuePara.AddFormattedText(valueItemContainer.GetHelp() ?? "");
				valuePara.Format.SpaceAfter = BottomPad;

				//Add Responses
				var responseLookup = valueItemContainer.GetResponses().ToDefaultDictionary(x => x.GetByAbout().GetBy().ToKey(), x => x, x => null);
				for (var i = 0; i < bys.Count(); i++) {
					var c = row.Cells[i + 1];
					var by = bys.ElementAt(i);
					var paragraph = c.AddParagraph();
					var itemContainer = valueItemContainer;
					var options = itemContainer.GetFormat().GetSetting<Dictionary<string, object>>("options");
					var rawResponse = responseLookup[by.ToKey()].NotNull(x => x.GetAnswer());
					c.VerticalAlignment = VerticalAlignment.Top;
					//c.Borders.Color = Color.FromArgb(150, 150, 0, 0);
					var iconSize = Unit.FromInch(.5);
					if (rawResponse != null) {
						var response = (string)options.GetOrDefault(rawResponse, rawResponse);
						if (rawResponse == "done") {
							var img = paragraph.AddImage(PdfImageConst.GreenCheck);
							img.Top = Unit.FromInch(-.25);
							img.Width = iconSize;
							img.Height = iconSize;
						} else if (rawResponse == "not-done") {
							var img = paragraph.AddImage(PdfImageConst.RedX);
							img.Top = Unit.FromInch(-.4);
							img.Width = iconSize;
							img.Height = iconSize;
						} else {
							//var img = c.AddImage(PdfImageConst.NotSet);
							//img.Width = iconSize;
							//img.Height = iconSize;
							var noResponse=c.AddParagraph();
							noResponse.Format.Font.Color = _Gray;
							noResponse.Format.Font.Italic = true;
							noResponse.AddFormattedText("unknown response");
						}
						//paragraph.AddFormattedText(response ?? "");
					} else {
						//paragraph.Format.Font.Color = _Gray;
						//paragraph.Format.Font.Italic = true;
						//paragraph.AddFormattedText("No response");
						var img = paragraph.AddImage(PdfImageConst.NotSet);
						img.Width = iconSize;
						img.Height = iconSize;
					}
				}
			}

			//ADD GENERAL COMMENTS
			var generalComments = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.GeneralComment);
			if (generalComments.SelectMany(x => x.GetResponses().Where(y => !string.IsNullOrWhiteSpace(y.GetAnswer()))).Any()) {
				var genCommentsHeading = cell.AddParagraph();
				genCommentsHeading.Format.SpaceBefore = BottomPad;
				genCommentsHeading.AddFormattedText("General Comments", HeadingFont);
				genCommentsHeading.Format.SpaceAfter = BottomPad;

				foreach (var generalCommentQuestion in generalComments) {
					//Do we have a response?
					foreach (var comment in generalCommentQuestion.GetResponses()) {
						var answer = comment.GetAnswer();
						var by = comment.GetByAbout().GetBy().ToPrettyString();
						//Do we have an answer
						if (!string.IsNullOrEmpty(answer)) {
							var paragraph = cell.AddParagraph();
							paragraph.AddFormattedText(by ?? "", TextFormat.Bold);
							paragraph = cell.AddParagraph();
							paragraph.AddText(answer ?? "");
							paragraph.Format.SpaceAfter = BottomPad;
						}
					}
				}
			}
		}
	}
}
