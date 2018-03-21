using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Drawing;
using RadialReview.Accessors;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.Constants;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Accessors.PDF {
	public class SurveyPdfAccessor {


		public static Color DebugColor = Color.FromArgb(0, 0, 0, 0);
		//public static Color DebugColor = Color.FromArgb(150, 150, 0, 0);

		public static Color TableGray = new Color(100, 100, 100, 100);
		public static Color TableDark = new Color(50, 50, 50);
		//public static Color TableBlack = new Color(0, 0, 0);

		public static Color TractionOrange = new Color(239, 118, 34);
		public static Color TableWhite = Colors.White;// new Color(255, 255, 255);
		public static Color TractionBlack = new Color(62, 57, 53);// new Color(255, 255, 255);
		public static Unit BottomPad = Unit.FromInch(.25);

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


		public static void AppendSurveyAbout(Document doc, string title, DateTime localTime, ISurveyAbout survey) {
			doc.DefaultPageSetup.StartingNumber = 1;
			var iSections = survey.GetSections().OrderBy(x => x.GetOrdering());
			var section = doc.AddSection();

			var usableWidth = doc.DefaultPageSetup.PageWidth - doc.DefaultPageSetup.LeftMargin - doc.DefaultPageSetup.RightMargin;
			var topPara = section.AddParagraph();
			topPara.AddTab();
			topPara.Format.AddTabStop(usableWidth / 2, TabAlignment.Center);
			topPara.Format.Font.Size = 16;
			topPara.Format.Font.Color = TractionBlack;
			topPara.AddFormattedText(survey.GetAbout().NotNull(x => x.ToPrettyString().ToUpper()) ?? "", TextFormat.Bold);

			//topPara = section.AddParagraph();
			topPara.Format.SpaceAfter = BottomPad * .5;

			///
			Style style;
			style = doc.Styles[StyleNames.Footer];
			style.ParagraphFormat.TabStops.Clear();

			style.ParagraphFormat.AddTabStop(usableWidth / 2, TabAlignment.Center);
			style.ParagraphFormat.AddTabStop(usableWidth, TabAlignment.Right);
			var headParagraph = section.Headers.Primary.AddParagraph();
			var ftParagraph = section.Footers.Primary.AddParagraph();

			FormattedText ft = ftParagraph.AddFormattedText(survey.GetAbout().ToPrettyString() + " - " + survey.GetIssueDate().ToString("MMMM yyyy"), TextFormat.Bold);
			ft.Font.Size = 6;
			ftParagraph.AddTab();
			ftParagraph.AddTab();
			ftParagraph.Format.Font.Size = 6;
			ftParagraph.Format.KeepTogether = true;
			ft = ftParagraph.AddFormattedText(title.NotNull(x => x + " | ") ?? "", TextFormat.NotBold);

			var pf = ftParagraph.AddPageField();
			//pf.Font.Size = 8;

			ftParagraph = section.Footers.Primary.AddParagraph();
			ft = ftParagraph.AddFormattedText("Printed: " + localTime.Date.ToShortDateString(), TextFormat.NotBold);
			ft.Font.Size = 6;

			//FormattedText hd = headParagraph.AddFormattedText(title ?? "", TextFormat.Bold);
			//headParagraph.Format.ver
			//hd.Font.Size = 8;

			foreach (var iSection in iSections) {
				AddSurveySection(section, iSection);
			}
		}

		protected static void AddSectionTitle(Cell section, string pageTitle, Orientation orientation = Orientation.Portrait) {
			var frame = section.Elements.AddTextFrame();
			frame.Height = Unit.FromInch(.75);
			frame.Width = Unit.FromInch(8);
			if (orientation == Orientation.Landscape)
				frame.Width = Unit.FromInch(10.5);
			// frame.LineFormat.Color = TableGray;
			//frame.Left = ShapePosition.Center;

			frame.MarginRight = Unit.FromInch(.25);
			frame.MarginLeft = Unit.FromInch(.25);


			var title = frame.AddTable();
			title.Borders.Color = TractionOrange;//TableBlack;

			var size = Unit.FromInch(7);
			if (orientation == Orientation.Landscape)
				size = Unit.FromInch(9);
			var c = title.AddColumn(size);
			c.Format.Alignment = ParagraphAlignment.Center;
			var rr = title.AddRow();
			rr.Cells[0].AddParagraph(pageTitle ?? "");
			rr.Format.Font.Bold = true;
			rr.Format.Font.Color = TableWhite;
			//rr.Format.Font.Size = .4;
			rr.Format.Font.Name = "Arial Narrow";
			rr.Shading.Color = TractionOrange;// TableGray;
			rr.HeightRule = RowHeightRule.AtLeast;
			rr.VerticalAlignment = VerticalAlignment.Center;
			rr.Height = Unit.FromInch(0.4);
			rr.Format.Font.Size = Unit.FromInch(.2);

		}

		protected static void AddSurveySection(Section section, ISectionAbout iSection) {
			//var section = doc.AddSection();
			var usableWidth = section.Document.DefaultPageSetup.PageWidth - section.Document.DefaultPageSetup.LeftMargin - section.Document.DefaultPageSetup.RightMargin;
			var superTable = new Table();//section.AddTable();
			superTable.AddColumn(usableWidth);
			var superRow = superTable.AddRow();
			var cell = superRow.Cells[0];
			//cell.Borders.Color = Color.FromArgb(150, 0, 0, 150);


			AddSectionTitle(cell, iSection.GetName());
			var shouldDraw = PdfSectionFactory.CreateSection(cell, iSection, usableWidth);

			if (shouldDraw) {
				section.Elements.Add(superTable);
			}

			//var p = cell.AddParagraph();
			//p.Format.SpaceAfter = Unit.FromInch(.15);
		}
	}

	public class PdfSectionFactory {

		public static bool CreateSection(Cell cell, ISectionAbout isection, Unit usableWidth) {
			switch (isection.GetSectionType()) {
				case "Rocks":
					return AddRocksSection(cell, isection, usableWidth);
				case "Roles":
					return AddRolesSection(cell, isection, usableWidth);
				case "Values":
					return AddValuesSection(cell, isection, usableWidth);
				case "GeneralComments":
					return AddGeneralCommentsSection(cell, isection, usableWidth);
				case "LeadershipAssessment":
					return AddLeadershipAssessmentSection(cell, isection, usableWidth);
				case "ManagementAssessment":
					return AddManagementAssessmentSection(cell, isection, usableWidth);
				case "RockCompletion":
					return AddRockCompletionSection(cell, isection, usableWidth);
				default:
					throw new ArgumentOutOfRangeException("Unknown section type");
			}
		}

		private static Color DebugColor = SurveyPdfAccessor.DebugColor;

		public static Color _Gray = Color.FromArgb((128 + 255) / 2, 51, 51, 51);
		public static Unit _DefaultMargin = Unit.FromInch(0.3);

		public static Unit BottomPad = SurveyPdfAccessor.BottomPad;// Unit.FromInch(.25);





		public static Font HeadingFont = new Font("Verdana", 12);
		static PdfSectionFactory() {
			HeadingFont.Underline = Underline.Single;
			HeadingFont.Bold = true;
		}


		private static void DrawCheckX(Paragraph paragraph, string value, string valueWhenChecked, string valueWhenX) {
			var iconSize = Unit.FromInch(.4);
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


		private class PdfTable {
			public IEnumerable<IItemContainerAbout> ItemContainers { get; set; }

			public Action<Cell, IForModel> UserBuilder { get; set; }
			public Action<Cell, IItem> ItemBuilder { get; set; }
			public Action<Cell, IItemFormat, IResponse> ResponseBuilder { get; set; }

			public static Unit DefaultHorizontalPadding = Unit.FromInch(.1);
			public static Unit DefaultItemTitleWidth = Unit.FromInch(3);

			protected Unit HorizontalPadding { get; set; }
			protected Unit ItemTitleWidth { get; set; }
			protected Unit TotalWidth { get; set; }
			protected bool ShouldAddSpacerRow { get; set; }

			public static Action<Cell, IItem> DefaultItemBuilder(ParagraphAlignment alignment = ParagraphAlignment.Right, Unit? firstLineIndent = null) {
				return new Action<Cell, IItem>((c, item) => {
					var para = c.AddParagraph();
					c.VerticalAlignment = VerticalAlignment.Top;
					para.Format.Alignment = alignment;
					para.AddFormattedText(item.GetName() ?? "", TextFormat.Bold);
					para = c.AddParagraph();
					para.Format.Alignment = alignment;
					para.AddFormattedText(item.GetHelp() ?? "");
					para.Format.LeftIndent = Unit.FromInch(0.0375);
					c.Row.Height = BottomPad;//*2.5;
					c.Row.HeightRule = RowHeightRule.AtLeast;
					para.Format.SpaceAfter = BottomPad / 2.0;
					if (firstLineIndent != null) {
						para.Format.FirstLineIndent = firstLineIndent.Value;
					}

				});
			}

			public PdfTable(Unit totalWidth, IEnumerable<IItemContainerAbout> itemContainers, Action<Cell, IItemFormat, IResponse> responseBuilder, Action<Cell, IItem> itemBuilder = null, Action<Cell, IForModel> userBuilder = null, bool addSpacerRow = true) {
				HorizontalPadding = DefaultHorizontalPadding;
				ItemTitleWidth = DefaultItemTitleWidth;
				ItemContainers = itemContainers;
				ResponseBuilder = responseBuilder;
				TotalWidth = totalWidth;
				ShouldAddSpacerRow = addSpacerRow;

				UserBuilder = userBuilder ?? new Action<Cell, IForModel>((c, by) => {
					var paragraph = c.AddParagraph();
					paragraph.AddFormattedText(by.ToPrettyString() ?? "err", TextFormat.Underline);
					paragraph.Format.Alignment = ParagraphAlignment.Center;
					c.VerticalAlignment = VerticalAlignment.Center;
				});

				ItemBuilder = itemBuilder ?? DefaultItemBuilder();
			}

			public static Table Build(Unit totalWidth, IEnumerable<IItemContainerAbout> itemContainers, Action<Cell, IItemFormat, IResponse> responseBuilder, Action<Cell, IItem> itemBuilder = null, Action<Cell, IForModel> userBuilder = null) {
				var builder = new PdfTable(totalWidth, itemContainers, responseBuilder, itemBuilder, userBuilder);
				return builder.BuildTable();
			}

			public IEnumerable<IForModel> GetByOrdered() {
				return ItemContainers.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());
			}


			public Table BuildTable() {
				var table = new Table();
				var bys = GetByOrdered();

				//COLUMNS
				var usableWidth = TotalWidth - HorizontalPadding * 2 - ItemTitleWidth;
				//left padding
				table.AddColumn(HorizontalPadding);
				//Item Width
				table.AddColumn(ItemTitleWidth);
				var byWidth = usableWidth / Math.Max(1, bys.Count() + 1);
				//By width
				foreach (var by in bys) {
					table.AddColumn(byWidth);
				}
				//Right padding
				table.AddColumn(HorizontalPadding);

				//HEADERS
				var headerRow = table.AddRow();
				headerRow.Borders.Color = DebugColor;
				headerRow.Height = BottomPad;//* 1.5;
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

				if (ShouldAddSpacerRow) {
					AddSpacerRow(table);
				}

				return table;
			}

			public static void AddSpacerRow(Table table) {
				var r = table.AddRow();
				r.Height = BottomPad * 1;
				r.HeightRule = RowHeightRule.AtLeast;
			}
		}

		private static bool DrawComments(Cell c, string heading, IEnumerable<IItemContainerAbout> generalComments, Unit usableWidth) {

			if (generalComments.SelectMany(x => x.GetResponses().Where(y => !string.IsNullOrWhiteSpace(y.GetAnswer()))).Any()) {

				var table = new Table();
				table.AddColumn(usableWidth * .05);
				table.AddColumn(usableWidth * .9);
				var row = table.AddRow();
				c.Elements.Add(table);

				var cell = row[1];

				var genCommentsHeading = cell.AddParagraph();
				genCommentsHeading.Format.SpaceAfter = BottomPad;
				genCommentsHeading.AddFormattedText(heading ?? "Comments", HeadingFont);
				genCommentsHeading.Format.SpaceAfter = BottomPad * .5;

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

				cell.AddParagraph();
				//row.Format.SpaceAfter = BottomPad;
				//cell.Format.SpaceBefore= BottomPad*2;
				return true;
			}
			return false;
		}

		private static bool AddValuesSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {

			var valueQuestions = iSection.GetItemContainers().Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.Value).ToList();
			var responseBuilder = new Action<Cell, IItemFormat, IResponse>((c, format, resp) => {
				var paragraph = c.AddParagraph();
				var options = format.GetSetting<Dictionary<string, object>>("options");
				var rawResponse = resp.NotNull(x => x.GetAnswer());

				if (rawResponse != null) {
					var response = (string)options.GetOrDefault(rawResponse, rawResponse);
					response = QuarterlyConversationAccessor.TransformValueAnswer[response];
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
			DrawComments(cell, ValueSection.ValueCommentHeading, generalComments, usableWidth);
			return true;
			//if (generalComments.SelectMany(x => x.GetResponses().Where(y => !string.IsNullOrWhiteSpace(y.GetAnswer()))).Any()) {
			//	var genCommentsHeading = cell.AddParagraph();
			//	//genCommentsHeading.Format.SpaceBefore = BottomPad;
			//	genCommentsHeading.AddFormattedText("General Comments", HeadingFont);
			//	genCommentsHeading.Format.SpaceAfter = BottomPad*2;

			//	foreach (var generalCommentQuestion in generalComments) {
			//		//Do we have a response?
			//		foreach (var comment in generalCommentQuestion.GetResponses()) {
			//			var answer = comment.GetAnswer();
			//			var by = comment.GetByAbout().GetBy().ToPrettyString();
			//			//Do we have an answer
			//			if (!string.IsNullOrEmpty(answer)) {
			//				var paragraph = cell.AddParagraph();
			//				paragraph.AddFormattedText(by ?? "", TextFormat.Bold);
			//				paragraph = cell.AddParagraph();
			//				paragraph.AddText(answer ?? "");
			//				paragraph.Format.SpaceAfter = BottomPad;
			//			}
			//		}
			//	}
			//}
		}

		private static bool AddRockCompletionSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {
			var questions = iSection.GetItemContainers();
			var rockCompletionQuestions = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.RockCompletion).ToList();

			var numRocksLastQ = rockCompletionQuestions.FirstOrDefault(x => x.GetFormat().GetSetting<String>("QN") == RockCompletionSection.NUMBER_LAST_QUARTER).NotNull(x => x.GetResponses().FirstOrDefault(y => y != null).GetAnswer());
			var numRocksCompletedLastQ = rockCompletionQuestions.FirstOrDefault(x => x.GetFormat().GetSetting<String>("QN") == RockCompletionSection.NUMBER_COMPLETE_LAST_QUARTER).NotNull(x => x.GetResponses().FirstOrDefault(y => y != null).GetAnswer());


			//Generate the table:
			var table = new Table();

			var width1 = Unit.FromInch(3);
			var width2 = Unit.FromInch(1);
			var pad = (usableWidth - (width1 * 2)) / 2.0;

			table.AddColumn(pad);
			table.AddColumn(width1);
			table.AddColumn(width2);
			table.AddColumn(width1 - width2);
			table.AddColumn(pad);



			var row = table.AddRow();
			var p = row.Cells[1].AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Right;
			p.AddFormattedText("# of Rocks completed last Quarter");
			p = row.Cells[2].AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Center;
			p.AddFormattedText(numRocksCompletedLastQ ?? "_______________");

			row = table.AddRow();
			p = row.Cells[1].AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Right;
			p.AddFormattedText("# of Rocks last Quarter");
			p = row.Cells[2].AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Center;
			p.AddFormattedText(numRocksLastQ ?? "_______________");

			double numRocks;
			double numRocksComplete;
			string percent = null;
			if (double.TryParse(numRocksLastQ, out numRocks) && double.TryParse(numRocksCompletedLastQ, out numRocksComplete)) {
				percent = "0";
				if (numRocksComplete != 0) {
					percent = Math.Round(numRocksComplete / numRocks * 100) + "%";
				}
			}
			row = table.AddRow();

			p = row.Cells[1].AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Right;
			p.AddFormattedText("Rock completion percentage");
			p = row.Cells[2].AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Center;
			p.AddFormattedText(percent ?? "_______________");


			PdfTable.AddSpacerRow(table);
			//Append table
			cell.Elements.Add(table);
			return true;
		}

		private static bool AddRolesSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {
			var questions = iSection.GetItemContainers();

			var table = new Table();
			//table.Borders.Color = Colors.Red;
			cell.Elements.Add(table);

			var bys = questions.SelectMany(x => x.GetResponses().Select(y => y.GetByAbout().GetBy())).Distinct(x => x.ToKey());

			//ADD COLUMNS:
			var paddingLeft = PdfTable.DefaultHorizontalPadding;// (usableWidth / 4.0) / 2.0;
			var answerWidth = usableWidth - PdfTable.DefaultItemTitleWidth - PdfTable.DefaultHorizontalPadding;// * 3.0 / 4.0;
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
			//roleTable.Borders.Color = Colors.Red;

			var tableWidth = usableWidth * .8;//		Unit.FromInch(5);// + Unit.FromInch(2);
			var leftPadWidth = (usableWidth - tableWidth) / 2.0;
			roleTable.AddColumn(leftPadWidth);
			roleTable.AddColumn(tableWidth);
			roleTable.AddColumn(leftPadWidth);


			//ROLES LIST
			if (rolesQuestions.Any()) {
				var ii = 1;
				foreach (var roleItemContainer in rolesQuestions) {
					var row = roleTable.AddRow();
					var p = row.Cells[1].AddParagraph((roleItemContainer.GetName() ?? ""));
					p.Format.Font.Bold = true;
					p.Format.Alignment = ParagraphAlignment.Center;
					ii++;
				}
			} else {
				var row = roleTable.AddRow();
				var p = row.Cells[1].AddParagraph("No roles.");
				p.Format.Font.Color = _Gray;
				p.Format.Font.Italic = true;
				p.Format.Alignment = ParagraphAlignment.Center;
			}
			var spacer = roleTable.AddRow();
			spacer.Height = BottomPad;
			spacer.HeightRule = RowHeightRule.AtLeast;
			rolesCell.Elements.Add(roleTable);

			//GWC Answers

			if (rolesQuestions.Any()) {
				var gwcTable = new Table();
				gwcTable.AddColumn(0);// paddingLeft);
				gwcTable.AddColumn(PdfTable.DefaultItemTitleWidth);

				var colWidth = answerWidth / Math.Max(1, bys.Count() + 1);
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
				var responseLookup = rolesAnswers.ToDefaultDictionary(
											x => x.GetFormat().GetSetting<string>("gwc"),
											y => y.GetResponses().ToDefaultDictionary(
												x => x.GetByAbout().GetBy().ToKey(),
												x => x,
												x => null
											),
											x => new DefaultDictionary<string, IResponse>(y => null)
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
					p.Format.Font.Bold = true;
					//p.Format.Font.Size
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

				var r = gwcTable.AddRow();
				r.Height = BottomPad * 1;
				r.HeightRule = RowHeightRule.AtLeast;

				gwcCell.Elements.Add(gwcTable);
			}

			//ADD GENERAL COMMENTS
			var generalComments = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.GeneralComment);
			DrawComments(cell, RoleSection.RoleCommentHeading, generalComments, usableWidth);
			return true;
			//if (generalComments.SelectMany(x => x.GetResponses().Where(y => !string.IsNullOrWhiteSpace(y.GetAnswer()))).Any()) {
			//	var genCommentsHeading = cell.AddParagraph();
			//	//genCommentsHeading.Format.SpaceBefore = BottomPad;
			//	genCommentsHeading.AddFormattedText("General Comments", HeadingFont);
			//	genCommentsHeading.Format.SpaceAfter = BottomPad*2;

			//	foreach (var generalCommentQuestion in generalComments) {
			//		//Do we have a response?
			//		foreach (var comment in generalCommentQuestion.GetResponses()) {
			//			var answer = comment.GetAnswer();
			//			var by = comment.GetByAbout().GetBy().ToPrettyString();
			//			//Do we have an answer
			//			if (!string.IsNullOrEmpty(answer)) {
			//				var paragraph = cell.AddParagraph();
			//				paragraph.AddFormattedText(by ?? "", TextFormat.Bold);
			//				paragraph = cell.AddParagraph();
			//				paragraph.AddText(answer ?? "");
			//				paragraph.Format.SpaceAfter = BottomPad;
			//			}
			//		}
			//	}
			//}
		}

		private static bool AddRocksSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {
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

			var tableBuilder = new PdfTable(usableWidth, rockQuestions, responseBuilder, addSpacerRow: false);
			var table = tableBuilder.BuildTable();

			//Add the rock completion percentage
			table.Format.Borders.Color = DebugColor;
			var percentRow = table.AddRow();
			percentRow.TopPadding = BottomPad * .5;
			var percentLookup = CalculateCompletionPercent(rockQuestions);
			var bys = tableBuilder.GetByOrdered();
			for (var i = 0; i < bys.Count(); i++) {
				var by = bys.ElementAt(i);
				var p = percentRow.Cells[i + 2].AddParagraph(percentLookup[by.ToKey()].ToPercentage("n/a"));
				p.Format.Alignment = ParagraphAlignment.Center;
			}

			PdfTable.AddSpacerRow(table);
			//Append table
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
			DrawComments(cell, RockSection.RockCommentHeading, generalComments, usableWidth);

			return true;
		}

		private static bool AddGeneralCommentsSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {

			var questions = iSection.GetItemContainers();
			var generalComments = questions.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.GeneralComment);

			return DrawComments(cell, "General Comments", generalComments, usableWidth);

		}

		private static Action<Cell, IItemFormat, IResponse> LmaTable() {
			return new Action<Cell, IItemFormat, IResponse>((c, format, resp) => {
				var paragraph = c.AddParagraph();
				//var options = format.GetSetting<Dictionary<string, object>>("options");
				var rawResponse = resp.NotNull(x => x.GetAnswer());

				//if (rawResponse != null) {
				//	//	var response = (string)options.GetOrDefault(rawResponse, rawResponse);
				//	paragraph.AddFormattedText(rawResponse ?? "");
				//} else {
				//	paragraph.Format.Font.Color = _Gray;
				//	paragraph.Format.Font.Italic = true;
				//	paragraph.AddFormattedText("No response");
				//}

				DrawCheckX(paragraph, rawResponse, "yes", "no");
			});
		}

		private static bool AddLeadershipAssessmentSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {

			var questions = iSection.GetItemContainers();
			var leadershipQuestions = iSection.GetItemContainers().Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.LeadershipAssessment).ToList();
			var table = PdfTable.Build(usableWidth, leadershipQuestions, LmaTable(), PdfTable.DefaultItemBuilder(ParagraphAlignment.Left));
			cell.Elements.Add(table);

			//return DrawComments(cell, "General Comments", generalComments, usableWidth);
			return leadershipQuestions.Any();

		}

		private static bool AddManagementAssessmentSection(Cell cell, ISectionAbout iSection, Unit usableWidth) {

			var questions = iSection.GetItemContainers();
			var managementQuestions = iSection.GetItemContainers().Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.ManagementAssessment).ToList();
			var table = PdfTable.Build(usableWidth, managementQuestions, LmaTable(), PdfTable.DefaultItemBuilder(ParagraphAlignment.Left));
			cell.Elements.Add(table);

			return managementQuestions.Any();

		}

		private static DefaultDictionary<string, Ratio> CalculateCompletionPercent(List<IItemContainerAbout> items) {
			var dict = new DefaultDictionary<string, Ratio>(x => new Ratio());
			foreach (var i in items.SelectMany(x => x.GetResponses())) {
				dict[i.GetByAbout().GetBy().ToKey()].Add((i.GetAnswer() == "done").ToInt(), 1);
			}
			return dict;
		}





	}
}
