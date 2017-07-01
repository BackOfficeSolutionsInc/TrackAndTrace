using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Drawing;
using RadialReview.Accessors;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
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


		public static void AppendSurveyAbout(Document doc,ISurveyContainer container,IForModel about) {

			throw new NotImplementedException("TODO");
			//var sections = container.GetSections().OrderBy(x=>x.GetOrdering());

			//foreach (var section in sections) {
			//	AddSurveySection(doc, section);
			//}

		}

		protected static void AddSectionTitle(Section section, string pageTitle, Orientation orientation = Orientation.Portrait) {
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

		}

		protected static void AddSurveySection(Document doc, ISection iSection) {
			var section = doc.AddSection();

			AddSectionTitle(section, iSection.GetName());

			PdfSectionFactory.CreateSection(section, iSection);
		}



	}

	public class PdfSectionFactory {


		public static XFont _FontLargeBold = new XFont("Verdana", 20, XFontStyle.Bold);
		public static XFont _Font = new XFont("Verdana", 10, XFontStyle.Regular);
		public static XFont _FontBold = new XFont("Verdana", 10, XFontStyle.Bold);
		public static XFont _Font8 = new XFont("Verdana", 8, XFontStyle.Regular);
		public static XFont _Font8Bold = new XFont("Verdana", 8, XFontStyle.Bold);
		public static XFont _Font7 = new XFont("Verdana", 7, XFontStyle.Regular);
		public static XFont _Font7Bold = new XFont("Verdana", 7, XFontStyle.Bold);
		//public static XFont _Font7Bold = new XFont("Verdana", 7, XFontStyle.Bold);
		public static Color _Black = Color.FromArgb(255, 51, 51, 51);
		public static XBrush _BlackText = new XSolidBrush(XColor.FromArgb((int)_Black.A, (int)_Black.R, (int)_Black.G, (int)_Black.B));
		public static Color _Gray = Color.FromArgb((128 + 255) / 2, 51, 51, 51);
		public static XBrush _GrayText = new XSolidBrush(XColor.FromArgb((int)_Gray.A, (int)_Gray.R, (int)_Gray.G, (int)_Gray.B));
		public static Unit _DefaultMargin = Unit.FromInch(0.3);


		public static void CreateSection(Section section, ISection isection) {
			switch (isection.GetSectionType()) {
				case "Rocks":
					AddRocksSection(section, isection);
					break;
				case "Roles":
					AddRolesSection(section, isection);
					break;
				case "Values":AddValuesSection(section, isection);
					break;
				default:
					throw new ArgumentOutOfRangeException("Unknown section type");
			}			
		}

		private static void AddValuesSection(Section section, ISection iSection) {
			var usableWidth = section.Document.DefaultPageSetup.PageWidth - section.Document.DefaultPageSetup.LeftMargin - section.Document.DefaultPageSetup.RightMargin;
			

			var table = section.AddTable();

			var data = iSection.GetItemContainers();
			var bys = data.Where(x=>x.HasResponse()).Select(x => x.GetResponse().GetByAbout().GetBy()).Distinct(x => x.ToKey()).ToList();
			var abouts = data.Select(x => x.GetResponse().GetByAbout().GetAbout()).Distinct(x => x.ToKey());
			if (abouts.Count() != 1)
				throw new ArgumentOutOfRangeException("Error displaying values section. Unexpected 'About' count.");

			//Value
			var valueColumn = table.AddColumn(usableWidth/2.0);

			//Responses
			var byWidth = usableWidth / 2.0 / bys.Count();
			foreach (var by in bys) {
				table.AddColumn(byWidth);
			}

			var headerRow = table.AddRow();
			for (var i = 0; i < bys.Count(); i++) {
				var cell = headerRow.Cells[i + 1];
				var paragraph = cell.AddParagraph();
				paragraph.AddFormattedText(bys.ElementAt(i).ToPrettyString() ?? "err", TextFormat.Underline);
			}

			var values = data
				.Where(x=>x.GetFormat().GetQuestionIdentifier()==SurveyQuestionIdentifier.Values)
				.Select(x => x.GetItem())
				.Distinct(x => x.GetSource().ToKey());

			foreach (var value in values) {
				var row = table.AddRow();
				var valuePara = row.Cells[0].AddParagraph();
				valuePara.AddFormattedText(value.GetName(),TextFormat.Bold);
				valuePara = row.Cells[0].AddParagraph();
				valuePara.AddFormattedText(value.GetHelp());

				for (var i = 0; i < bys.Count(); i++) {
					var cell = row.Cells[i + 1];
					var by = bys[i];
					var paragraph = cell.AddParagraph();

					var itemContainer = data.Where(x => x.GetItem().Id == value.Id && by.ToKey() == x.GetResponse().GetByAbout().GetBy().ToKey()).SingleOrDefault();

					var options = itemContainer.GetFormat().GetSetting<Dictionary<string,string>>("options");
					var rawResponse = itemContainer.NotNull(x=>x.GetResponse().GetAnswer());
					if (rawResponse != null) {
						var response = options.GetOrDefault(rawResponse, rawResponse);
						paragraph.AddFormattedText(response ?? "");
					} else {
						paragraph.Format.Font.Color = _Gray;
						paragraph.Format.Font.Italic= true;
						paragraph.AddFormattedText("No response");
					}
				}
			}

			var generalComments = data.Where(x => x.GetFormat().GetQuestionIdentifier() == SurveyQuestionIdentifier.GeneralComments);
			
			foreach (var comments in generalComments) {
				//Do we have a response?
				if (comments.HasResponse()) {
					var response = comments.GetResponse();
					var answer = response.GetAnswer();
					//Do we have an answer
					if (!string.IsNullOrEmpty(answer)) {
						var paragraph = section.AddParagraph();
						paragraph.AddFormattedText(response.GetByAbout().GetBy().ToPrettyString()??"",TextFormat.Bold);
						paragraph = section.AddParagraph();
						paragraph.AddText(answer ?? "");
					}
				}
			}
			
		}

		private static void AddRolesSection(Section section, ISection iSection) {
//			throw new NotImplementedException();
		}

		private static void AddRocksSection(Section section, ISection iSection) {
		//	throw new NotImplementedException();
		}
	}
}