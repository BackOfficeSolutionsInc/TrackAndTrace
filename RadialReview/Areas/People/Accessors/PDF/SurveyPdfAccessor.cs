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


		public static void AppendSurveyAbout(Document doc, DateTime localTime, ISurveyAbout survey) {
			var iSections = survey.GetSections().OrderBy(x => x.GetOrdering());
			var section = doc.AddSection();

			///
			Style style;
			style = doc.Styles[StyleNames.Footer];
			style.ParagraphFormat.TabStops.Clear();
			var usableWidth = doc.DefaultPageSetup.PageWidth - doc.DefaultPageSetup.LeftMargin - doc.DefaultPageSetup.RightMargin;

			style.ParagraphFormat.AddTabStop(usableWidth/2, TabAlignment.Center);
			style.ParagraphFormat.AddTabStop(usableWidth, TabAlignment.Right);

			var paragraph = section.Footers.Primary.AddParagraph();

			FormattedText ft = paragraph.AddFormattedText(survey.GetAbout().ToPrettyString() + " - " + survey.GetIssueDate().ToString("MMMM yyyy"), TextFormat.Bold);
			ft.Font.Size = 6;
			paragraph.AddTab();
			paragraph.AddTab();
			var pf = paragraph.AddPageField();
			

			paragraph = section.Footers.Primary.AddParagraph();
			ft = paragraph.AddFormattedText("Printed: "+localTime.Date.ToShortDateString(), TextFormat.NotBold);
			ft.Font.Size = 6;
		

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

		//private static Color DebugColor = Color.FromArgb(150, 150, 0, 0);
		private static Color DebugColor = Color.FromArgb(0, 0, 0, 0);

		public static Color _Gray = Color.FromArgb((128 + 255) / 2, 51, 51, 51);
		public static Unit _DefaultMargin = Unit.FromInch(0.3);

		public static Unit BottomPad = Unit.FromInch(.25);

		//public static XFont _FontLargeBold = new XFont("Verdana", 20, XFontStyle.Bold);
		//public static XFont _Font = new XFont("Verdana", 10, XFontStyle.Regular);
		//public static XFont _FontBold = new XFont("Verdana", 10, XFontStyle.Bold);
		//public static XFont _Font8 = new XFont("Verdana", 8, XFontStyle.Regular);

		//public static XFont _Font7 = new XFont("Verdana", 7, XFontStyle.Regular);
		//public static XFont _Font7Bold = new XFont("Verdana", 7, XFontStyle.Bold);
		////public static XFont _Font7Bold = new XFont("Verdana", 7, XFontStyle.Bold);
		//public static Color _Black = Color.FromArgb(255, 51, 51, 51);
		//public static XBrush _BlackText = new XSolidBrush(XColor.FromArgb((int)_Black.A, (int)_Black.R, (int)_Black.G, (int)_Black.B));
		//public static XBrush _GrayText = new XSolidBrush(XColor.FromArgb((int)_Gray.A, (int)_Gray.R, (int)_Gray.G, (int)_Gray.B));
		



		public static Font HeadingFont = new Font("Verdana", 12);
		static PdfSectionFactory() {
			HeadingFont.Underline = Underline.Single;
			HeadingFont.Bold = true;
		}


		private static void DrawCheckX(Paragraph paragraph, string value, string valueWhenChecked, string valueWhenX) {
			var iconSize = Unit.FromInch(.5);
			if (value == valueWhenChecked) {
				var img = paragraph.AddImage(PdfImageConst.GreenCheck);
				img.Width = iconSize;
				img.Height = iconSize;
			} else if (value == valueWhenX) {
				var img = paragraph.AddImage(PdfImageConst.RedX);
				img.Width = iconSize;
				img.Height = iconSize;
			} else {
				var img = paragraph.AddImage(PdfImageConst.NotSet);
				img.Width = iconSize;
				img.Height = iconSize;
			}
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

		private class PdfTable {
			public IEnumerable<IItemContainerAbout> ItemContainers { get; set; }

			public Action<Cell, IForModel> UserBuilder { get; set; }
			public Action<Cell, IItem> ItemBuilder { get; set; }
			public Action<Cell, IItemFormat, IResponse> ResponseBuilder { get; set; }

			protected Unit HorizontalPadding { get; set; }
			protected Unit ItemTitleWidth { get; set; }
			protected Unit TotalWidth { get; set; }

			public static Table Build(Unit totalWidth, IEnumerable<IItemContainerAbout> itemContainers, Action<Cell, IItemFormat, IResponse> responseBuilder, Action<Cell, IItem> itemBuilder = null, Action<Cell, IForModel> userBuilder = null) {
				var builder = new PdfTable(totalWidth, itemContainers, responseBuilder, itemBuilder, userBuilder);
				return builder.BuildTable();
			}

			private PdfTable(Unit totalWidth, IEnumerable<IItemContainerAbout> itemContainers, Action<Cell, IItemFormat, IResponse> responseBuilder, Action<Cell, IItem> itemBuilder = null, Action<Cell, IForModel> userBuilder = null) {
				HorizontalPadding = Unit.FromInch(.1);
				ItemTitleWidth = Unit.FromInch(2);
				ItemContainers = itemContainers;
				ResponseBuilder = responseBuilder;
				TotalWidth = totalWidth;

				UserBuilder = userBuilder ?? new Action<Cell, IForModel>((c, by) => {
					var paragraph = c.AddParagraph();
					paragraph.AddFormattedText(by.ToPrettyString() ?? "err", TextFormat.Underline);
					paragraph.Format.Alignment = ParagraphAlignment.Center;
					c.VerticalAlignment = VerticalAlignment.Center;
				});

				ItemBuilder = itemBuilder ?? new Action<Cell, IItem>((c, item) => {
					var valuePara = c.AddParagraph();
					valuePara.Format.Alignment = ParagraphAlignment.Right;
					valuePara.AddFormattedText(item.GetName() ?? "", TextFormat.Bold);
					valuePara = c.AddParagraph();
					valuePara.Format.Alignment = ParagraphAlignment.Right;
					valuePara.AddFormattedText(item.GetHelp() ?? "");
					valuePara.Format.SpaceAfter = BottomPad;
				});

			}

			private Table BuildTable() {
				var table = new Table();
				var bys = ItemContainers.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());

				//COLUMNS
				var usableWidth = TotalWidth - HorizontalPadding * 2 - ItemTitleWidth;
				//left padding
				table.AddColumn(HorizontalPadding);
				//Item Width
				table.AddColumn(ItemTitleWidth);
				var byWidth = usableWidth / Math.Max(1, bys.Count());
				//By width
				foreach (var by in bys) {
					table.AddColumn(byWidth);
				}
				//Right padding
				table.AddColumn(HorizontalPadding);

				//HEADERS
				var headerRow = table.AddRow();
				headerRow.Height = BottomPad * 1.5;
				headerRow.HeightRule = RowHeightRule.AtLeast;
				for (var i = 0; i < bys.Count(); i++) {
					//USER
					var cell = headerRow.Cells[i + 2];
					cell.VerticalAlignment = VerticalAlignment.Bottom;
					try {
						UserBuilder(cell, bys.ElementAt(i));
						//cell.Elements.Add(userBuilt);
					} catch (Exception) {
						cell.AddParagraph("err1");
					}
				}


				//ROWS
				foreach (var itemContainer in ItemContainers) {
					//CONSTRUCT ROW
					var row = table.AddRow();
					//ROW HEADING
					var cell = row.Cells[1];
					cell.VerticalAlignment = VerticalAlignment.Center;
					try {
						//Row title
						ItemBuilder(cell, itemContainer.GetItem());
						//cell.Elements.Add(itemBuilt);
					} catch (Exception) {
						cell.AddParagraph("err2");
					}

					//ROW CONTENTS
					var responseLookup = itemContainer.GetResponses().ToDefaultDictionary(x => x.GetByAbout().GetBy().ToKey(), x => x, x => null);
					for (var i = 0; i < bys.Count(); i++) {
						cell = row.Cells[i + 2];
						cell.Format.Alignment = ParagraphAlignment.Center;
						var by = bys.ElementAt(i);
						try {
							//Add Responses
							var response = responseLookup[by.ToKey()];
							ResponseBuilder(cell, itemContainer.GetFormat(), response);
							//cell.Elements.Add(itemBuilt);
						} catch (Exception) {
							cell.AddParagraph("err3");
						}
					}
				}

				return table;
			}
		}

		private static void AddValuesSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {

			var valueQuestions = iSection.GetItemContainers().Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Value).ToList();
			var responseBuilder = new Action<Cell, IItemFormat, IResponse>((c, format, resp) => {
				var paragraph = c.AddParagraph();
				var options = format.GetSetting<Dictionary<string, object>>("options");
				var rawResponse = resp.NotNull(x => x.GetAnswer());

				if (rawResponse != null) {
					var response = (string)options.GetOrDefault(rawResponse, rawResponse);
					paragraph.AddFormattedText(response ?? "");
				} else {
					paragraph.Format.Font.Color = _Gray;
					paragraph.Format.Font.Italic = true;
					paragraph.AddFormattedText("No response");
				}
			});

			var table = PdfTable.Build(usableWidth, valueQuestions, responseBuilder);
			cell.Elements.Add(table);

			#region Old
			//var questions = iSection.GetItemContainers();

			//var table = new Table();
			//cell.Elements.Add(table);

			//var bys = questions.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());

			//table.Borders.Color = DebugColor;

			////ADD COLUMNS:
			////Value Column
			////ADD COLUMNS:
			//var paddingLeft = (usableWidth / 4.0) / 2.0;
			//usableWidth = usableWidth * 3.0 / 4.0;
			////
			//var padColumn = table.AddColumn(paddingLeft);
			//var valueColumn = table.AddColumn(usableWidth / 2.0);

			////By Column
			//var byWidth = usableWidth / 2.0 / bys.Count();
			//foreach (var by in bys) {
			//	table.AddColumn(byWidth);
			//}

			////ADD HEADER
			//var headerRow = table.AddRow();
			//headerRow.Height = BottomPad * 1.5;
			//headerRow.HeightRule = RowHeightRule.AtLeast;
			//for (var i = 0; i < bys.Count(); i++) {
			//	var c = headerRow.Cells[i + 2];
			//	c.Format.Alignment = ParagraphAlignment.Center;
			//	var paragraph = c.AddParagraph();
			//	paragraph.AddFormattedText(bys.ElementAt(i).ToPrettyString() ?? "err", TextFormat.Underline);
			//	c.VerticalAlignment = VerticalAlignment.Center;
			//}


			////ADD VALUE ROWS
			//var valueQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Value).ToList();
			//foreach (var valueItemContainer in valueQuestions) {
			//	//Construct Question
			//	var row = table.AddRow();
			//	var cc = row.Cells[1];
			//	var valuePara = cc.AddParagraph();
			//	valuePara.Format.Alignment = ParagraphAlignment.Right;
			//	valuePara.AddFormattedText(valueItemContainer.GetName() ?? "", TextFormat.Bold);
			//	valuePara = cc.AddParagraph();
			//	valuePara.Format.Alignment = ParagraphAlignment.Right;
			//	valuePara.AddFormattedText(valueItemContainer.GetHelp() ?? "");
			//	valuePara.Format.SpaceAfter = BottomPad;

			//	//Add Responses
			//	var responseLookup = valueItemContainer.GetResponses().ToDefaultDictionary(x => x.GetByAbout().GetBy().ToKey(), x => x, x => null);
			//	for (var i = 0; i < bys.Count(); i++) {
			//		var c = row.Cells[i + 2];
			//		var by = bys.ElementAt(i);
			//		c.Format.Alignment = ParagraphAlignment.Center;
			//		var paragraph = c.AddParagraph();
			//		var itemContainer = valueItemContainer;
			//		var options = itemContainer.GetFormat().GetSetting<Dictionary<string, object>>("options");
			//		var rawResponse = responseLookup[by.ToKey()].NotNull(x => x.GetAnswer());

			//		if (rawResponse != null) {
			//			var response = (string)options.GetOrDefault(rawResponse, rawResponse);
			//			paragraph.AddFormattedText(response ?? "");
			//		} else {
			//			paragraph.Format.Font.Color = _Gray;
			//			paragraph.Format.Font.Italic = true;
			//			paragraph.AddFormattedText("No response");
			//		}
			//	}
			//}
			#endregion

			var questions = iSection.GetItemContainers();

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
			//var roleQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Role).ToList();


			//var responseBuilder = new Action<Cell, IItemFormat, IResponse>((c, format, resp) => {
			//	var paragraph = c.AddParagraph();
			//	var options = format.GetSetting<Dictionary<string, object>>("options");
			//	var rawResponse = resp.NotNull(x => x.GetAnswer());
			//	c.VerticalAlignment = VerticalAlignment.Center;
			//	c.Borders.Color = DebugColor;
			//	var para = c.AddParagraph();
			//	para.Format.Alignment = ParagraphAlignment.Center;
			//	DrawCheckX(para, resp.GetAnswer(), "yes", "no");
			//});

			//var table = PdfTable.Build(usableWidth, roleQuestions, responseBuilder);
			//cell.Elements.Add(table);

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
			var leftPadWidth = (usableWidth - tableWidth) / 2.0;
			roleTable.AddColumn(leftPadWidth);
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
				var ccc = row.Cells[1];
				var p = ccc.AddParagraph(gwcStr);
				ccc.VerticalAlignment = VerticalAlignment.Center;
				ccc.Borders.Color = DebugColor;//Color.FromArgb(150, 150, 0, 0);
				p.Format.Alignment = ParagraphAlignment.Right;

				for (var j = 0; j < bys.Count(); j++) {
					var by = bys.ElementAt(j);
					var userResponse = responseLookup[questionKey][by.ToKey()].NotNull(x => x.GetAnswer());
					var cc = row.Cells[j + 2];
					cc.VerticalAlignment = VerticalAlignment.Center;
					cc.Borders.Color = DebugColor;// Color.FromArgb(150, 150, 0, 0);
					var para = cc.AddParagraph();// userResponse.GetAnswer() ?? "na");
					para.Format.Alignment = ParagraphAlignment.Center;
					DrawCheckX(para, userResponse, "yes", "no");
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
			var rockQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Rock).ToList();
			var responseBuilder = new Action<Cell, IItemFormat, IResponse>((c, format, resp) => {
				var paragraph = c.AddParagraph();
				paragraph.Format.Alignment = ParagraphAlignment.Center;
				var options = format.GetSetting<Dictionary<string, object>>("options");
				var rawResponse = resp.NotNull(x => x.GetAnswer());
				//c.VerticalAlignment = VerticalAlignment.Top;
				DrawCheckX(paragraph, rawResponse, "done", "not-done");
			});

			var table = PdfTable.Build(usableWidth, rockQuestions, responseBuilder);
			cell.Elements.Add(table);

			#region OLD
			//var table = new Table();
			//cell.Elements.Add(table);
			//var bys = questions.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());

			////ADD COLUMNS:
			//var paddingLeft = (usableWidth / 4.0) / 2.0;
			//var answerWidth = usableWidth * 3.0 / 4.0;
			//usableWidth = usableWidth * 3.0 / 4.0;

			////ADD COLUMNS:
			////Rock Column
			//var rockColumn = table.AddColumn(usableWidth / 2.0);

			////By Column
			//var byWidth = usableWidth / 2.0 / bys.Count();
			//foreach (var by in bys) {
			//	table.AddColumn(byWidth);
			//}

			////ADD HEADER
			//var headerRow = table.AddRow();
			//headerRow.Height = BottomPad * 1.5;
			//headerRow.HeightRule = RowHeightRule.AtLeast;
			//for (var i = 0; i < bys.Count(); i++) {
			//	var c = headerRow.Cells[i + 1];
			//	var paragraph = c.AddParagraph();
			//	paragraph.AddFormattedText(bys.ElementAt(i).ToPrettyString() ?? "err", TextFormat.Underline);
			//	c.VerticalAlignment = VerticalAlignment.Center;
			//	paragraph.Format.Alignment = ParagraphAlignment.Center;
			//}

			////ADD ROWS
			//var rockQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Rock).ToList();
			//foreach (var valueItemContainer in rockQuestions) {
			//	//Construct Question
			//	var row = table.AddRow();
			//	var cc = row.Cells[0];
			//	cc.VerticalAlignment = VerticalAlignment.Center;
			//	cc.Borders.Color = DebugColor;// Color.FromArgb(150, 150, 0, 0);
			//	var valuePara = cc.AddParagraph();
			//	valuePara.Format.Alignment = ParagraphAlignment.Right;
			//	valuePara.AddFormattedText(valueItemContainer.GetName() ?? "", TextFormat.Bold);

			//	if (!string.IsNullOrWhiteSpace(valueItemContainer.GetHelp())) {
			//		valuePara = cc.AddParagraph();
			//		valuePara.AddFormattedText(valueItemContainer.GetHelp() ?? "");
			//		valuePara.Format.SpaceAfter = BottomPad;
			//		valuePara.Format.Alignment = ParagraphAlignment.Right;
			//	}
			//	//Add Responses
			//	var responseLookup = valueItemContainer.GetResponses().ToDefaultDictionary(x => x.GetByAbout().GetBy().ToKey(), x => x, x => null);
			//	for (var i = 0; i < bys.Count(); i++) {
			//		var c = row.Cells[i + 1];
			//		var by = bys.ElementAt(i);
			//		var paragraph = c.AddParagraph();
			//		paragraph.Format.Alignment = ParagraphAlignment.Center;
			//		var itemContainer = valueItemContainer;
			//		var options = itemContainer.GetFormat().GetSetting<Dictionary<string, object>>("options");
			//		var rawResponse = responseLookup[by.ToKey()].NotNull(x => x.GetAnswer());
			//		c.VerticalAlignment = VerticalAlignment.Top;
			//		DrawCheckX(paragraph, rawResponse, "done", "not-done");
			//	}
			//}
			#endregion
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
