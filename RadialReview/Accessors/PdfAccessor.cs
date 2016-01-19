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

namespace RadialReview.Accessors
{
	public class LayoutHelper
	{
		private readonly PdfDocument _document;
		private readonly XUnit _topPosition;
		private readonly XUnit _bottomMargin;
		private XUnit _currentPosition;
		public LayoutHelper(PdfDocument document, XUnit topPosition, XUnit bottomMargin)
		{
			_document = document;
			_topPosition = topPosition;
			_bottomMargin = bottomMargin;
			// Set a value outside the page - a new page will be created on the first request.
			_currentPosition = bottomMargin + 10000;
		}
		public XUnit GetLinePosition(XUnit requestedHeight)
		{
			return GetLinePosition(requestedHeight, -1f);
		}
		public XUnit GetLinePosition(XUnit requestedHeight, XUnit requiredHeight)
		{
			XUnit required = requiredHeight == -1f ? requestedHeight : requiredHeight;
			if (_currentPosition + required > _bottomMargin)
				CreatePage();
			XUnit result = _currentPosition;
			_currentPosition += requestedHeight;
			return result;
		}
		public XGraphics Gfx { get; private set; }
		public PdfPage Page { get; private set; }

		void CreatePage()
		{
			Page = _document.AddPage();
			Page.Size = PageSize.A4;
			Gfx = XGraphics.FromPdfPage(Page);
			_currentPosition = _topPosition;
		}
	}


	public class PdfAccessor
	{




		public static Document GetScorecard(UserOrganizationModel caller, long recurrenceId)
		{
			//byte[] pdfBuf = new SynchronizedPechkin(new GlobalConfig()).Convert("<html><body><h1>Hello world!</h1></body></html>");
			// Create a new PDF document
			var recur = L10Accessor.GetAngularRecurrence(caller,recurrenceId);
		

			var document = new Document();



			document.Info.Title = "Scorecard";
			document.Info.Author = caller.GetName();
			document.Info.Comment = "Created with Traction® Tools";


			//document.DefaultPageSetup.PageFormat = PageFormat.Letter;
			document.DefaultPageSetup.Orientation= Orientation.Landscape;

			document.DefaultPageSetup.LeftMargin = Unit.FromInch(.5);
			document.DefaultPageSetup.RightMargin = Unit.FromInch(.5);
			document.DefaultPageSetup.TopMargin = Unit.FromInch(.5);
			document.DefaultPageSetup.BottomMargin= Unit.FromInch(.5);
			document.DefaultPageSetup.PageWidth = Unit.FromInch(8);
			document.DefaultPageSetup.PageHeight = Unit.FromInch(11);

			//var TableBlue = new Color(100, 10, 10, 100);
			//var TableBorder = new Color(100, 10, 100, 10);
			var TableGray = new Color(100, 100, 100, 100);
			var TableBlack = new Color(0, 0, 0);


			var section = document.AddSection();

			var paragraph = new Paragraph();
			paragraph.AddTab();
			paragraph.AddPageField();
			// Add paragraph to footer for odd pages.
			section.Footers.Primary.Add(paragraph);
			section.Footers.Primary.Format.SpaceBefore = Unit.FromInch(-.25);



			var frame = section.AddTextFrame();
			frame.Height = Unit.FromInch(.75);
			frame.Width = Unit.FromInch(10);
			//frame.Left = ShapePosition.Center;
			
			frame.MarginRight = Unit.FromInch(1);
			frame.MarginLeft = Unit.FromInch(1);

			var title = frame.AddTable();
			title.Borders.Color = TableBlack;

			var c = title.AddColumn(Unit.FromInch(8));
			c.Format.Alignment = ParagraphAlignment.Center;
			var rr = title.AddRow();
			rr.Cells[0].AddParagraph("Scorecard");
			rr.Format.Font.Bold = true;
			rr.Shading.Color = TableGray;
			rr.HeightRule = RowHeightRule.AtLeast;
			rr.VerticalAlignment = VerticalAlignment.Center;
			rr.Height = Unit.FromInch(0.4);
			rr.Format.Font.Size = Unit.FromInch(.2);


			/*var paragraph = frame.AddParagraph();

			//var paragraph =new Paragraph();
			paragraph.AddText("Scorecard");
			paragraph.Format.Alignment=ParagraphAlignment.Center;
			paragraph.Format.Borders.Width = 2.5;
			paragraph.Format.Shading.Color = Colors.SkyBlue;*/
			
			/*paragraph.Format.LeftIndent  = Unit.FromInch(1);
			paragraph.Format.RightIndent = Unit.FromInch(1);
			paragraph.Format.SpaceBefore = Unit.FromInch(13.0 / 16.0 - 0.5);
			paragraph.Format.SpaceAfter = Unit.FromInch(3.0 / 16.0);*/

			//frame.Add(paragraph);


			//section.Add(paragraph);
			
			var table = section.AddTable();
			table.Style = "Table";
			table.Borders.Color = TableBlack;
			table.Borders.Width = 1;
			/*table.Borders.Left.Width = 0.25;
			table.Borders.Right.Width = 0.25;
			table.Borders.Top.Width = 7.0/8.0;*/
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding= 0;


			//Number
			var column = table.AddColumn(Unit.FromInch(0.25));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Who

			column = table.AddColumn(Unit.FromInch(0.75));
			column.Format.Alignment = ParagraphAlignment.Center;
			
			//Measurable
			column = table.AddColumn(Unit.FromInch(2.0));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Goal
			column = table.AddColumn(Unit.FromInch(0.75));
			column.Format.Alignment = ParagraphAlignment.Center;

			//Measured
			for (var i = 0; i < 13; i++){
				column = table.AddColumn(Unit.FromInch(6.25/13.0));
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
			row.Cells[1].VerticalAlignment=VerticalAlignment.Bottom;

			row.Cells[2].AddParagraph("Measurable");
			row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;

			row.Cells[3].AddParagraph("Goal");
			row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;

			var numWeeks = 13;

			var weeks = recur.Scorecard.Weeks.OrderByDescending(x => x.ForWeekNumber).Take(numWeeks).OrderBy(x => x.ForWeekNumber);
			var ii = 0;
			foreach(var w in weeks){
				row.Cells[4 + ii].AddParagraph(w.DisplayDate.ToString("MM/dd/yy") + " to " + w.DisplayDate.AddDays(6).ToString("MM/dd/yy"));
				row.Cells[4 + ii].Format.Font.Size = Unit.FromInch(0.07);
				row.Cells[4 + ii].Format.Font.Size = Unit.FromInch(0.07);
				ii++;
			}
			var r = new Random();

			var measurables = recur.Scorecard.Measurables.OrderBy(x => x.Ordering).Where(x=>!(x.Disabled??false) && !x.IsDivider);
			var mn = 1;

			for (var k = 0; k < 2; k++){
				foreach (var m in measurables){

					row = table.AddRow();
					row.HeadingFormat = false;
					row.Format.Alignment = ParagraphAlignment.Center;

					row.Format.Font.Bold = false;
					row.Format.Font.Size = Unit.FromInch(0.128*2.0/3.0); // --- 1/16"
					//row.Shading.Color = TableBlue;
					row.HeightRule = RowHeightRule.AtLeast;
					row.VerticalAlignment = VerticalAlignment.Center;
					row.Height = Unit.FromInch((6*8 + 5.0)/(8*16.0)/2);
					row.Cells[0].AddParagraph("" + mn + ".");
					row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
					row.Cells[1].AddParagraph(m.Owner.Name);
					row.Cells[2].AddParagraph(m.Name);
					row.Cells[2].Format.Alignment = ParagraphAlignment.Left;

					var modifier = m.Modifiers ?? (RadialReview.Models.Enums.UnitType.None);

					row.Cells[3].AddParagraph((m.Direction ?? LessGreater.LessThan).ToSymbol() + " " + modifier.Format(m.Target ?? 0));
					ii = 0;
					foreach (var w in weeks){
						var found = recur.Scorecard.Scores.FirstOrDefault(x => x.ForWeek == w.ForWeekNumber && x.Measurable.Id == m.Id);
						if (found != null && found.Measured.HasValue){
							var val = found.Measured ?? 0;
							row.Cells[4 + ii].AddParagraph(modifier.Format(val.KiloFormat()));
						}
						ii++;
					}
					mn += 1;
					//	row.Cells[j].AddParagraph("$"+(r.NextDouble()*Math.Pow(10, (j-5)*2)).KiloFormat());
					//}
				}
			}


			//	section
			/*
			// Put a logo in the header
			Image image = section.Headers.Primary.AddImage("../../PowerBooks.png");
			image.Height = "2.5cm";
			image.LockAspectRatio = true;
			image.RelativeVertical = RelativeVertical.Line;
			image.RelativeHorizontal = RelativeHorizontal.Margin;
			image.Top = ShapePosition.Top;
			image.Left = ShapePosition.Right;
			image.WrapFormat.Style = WrapStyle.Through;

			// Create footer
			Paragraph paragraph = section.Footers.Primary.AddParagraph();
			paragraph.AddText("PowerBooks Inc · Sample Street 42 · 56789 Cologne · Germany###");
			paragraph.Format.Font.Size = 9;
			paragraph.Format.Alignment = ParagraphAlignment.Center;

			// Create the text frame for the address
			var addressFrame = section.AddTextFrame();
			addressFrame.Height = "3.0cm";
			addressFrame.Width = "7.0cm";
			addressFrame.Left = ShapePosition.Left;
			addressFrame.RelativeHorizontal = RelativeHorizontal.Margin;
			addressFrame.Top = "5.0cm";
			addressFrame.RelativeVertical = RelativeVertical.Page;

			// Put sender in address frame
			paragraph = addressFrame.AddParagraph("PowerBooks Inc · Sample Street 42 · 56789 Cologne");
			paragraph.Format.Font.Name = "Times New Roman";
			paragraph.Format.Font.Size = 7;
			paragraph.Format.SpaceAfter = 3;

			// Add the print date field
			paragraph = section.AddParagraph();
			paragraph.Format.SpaceBefore = "8cm";
			paragraph.Style = "Reference";
			paragraph.AddFormattedText("INVOICE", TextFormat.Bold);
			paragraph.AddTab();
			paragraph.AddText("Cologne, ");
			paragraph.AddDateField("dd.MM.yyyy");

			// Create the item table
			var table = section.AddTable();
			table.Style = "Table";
			table.Borders.Color = TableBorder;
			table.Borders.Width = 0.25;
			table.Borders.Left.Width = 0.5;
			table.Borders.Right.Width = 0.5;
			table.Rows.LeftIndent = 0;

			// Before you can add a row, you must define the columns
			var column = table.AddColumn("1cm");
			column.Format.Alignment = ParagraphAlignment.Center;

			column = table.AddColumn("2.5cm");
			column.Format.Alignment = ParagraphAlignment.Right;

			column = table.AddColumn("3cm");
			column.Format.Alignment = ParagraphAlignment.Right;

			column = table.AddColumn("3.5cm");
			column.Format.Alignment = ParagraphAlignment.Right;

			column = table.AddColumn("2cm");
			column.Format.Alignment = ParagraphAlignment.Center;

			column = table.AddColumn("4cm");
			column.Format.Alignment = ParagraphAlignment.Right;

			// Create the header of the table
			Row row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = TableBlue ;
			row.Cells[0].AddParagraph("Item");
			row.Cells[0].Format.Font.Bold = false;
			row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
			row.Cells[0].VerticalAlignment = VerticalAlignment.Bottom;
			row.Cells[0].MergeDown = 1;
			row.Cells[1].AddParagraph("Title and Author");
			row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
			row.Cells[1].MergeRight = 3;
			row.Cells[5].AddParagraph("Extended Price");
			row.Cells[5].Format.Alignment = ParagraphAlignment.Left;
			row.Cells[5].VerticalAlignment = VerticalAlignment.Bottom;
			row.Cells[5].MergeDown = 1;

			row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = TableBlue;
			row.Cells[1].AddParagraph("Quantity");
			row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
			row.Cells[2].AddParagraph("Unit Price");
			row.Cells[2].Format.Alignment = ParagraphAlignment.Left;
			row.Cells[3].AddParagraph("Discount (%)");
			row.Cells[3].Format.Alignment = ParagraphAlignment.Left;
			row.Cells[4].AddParagraph("Taxable");
			row.Cells[4].Format.Alignment = ParagraphAlignment.Left;

			table.SetEdge(0, 0, 6, 2, Edge.Box, BorderStyle.Single, 0.75, Color.Empty);

			XPathNavigator item = SelectItem("/invoice/to");
			Paragraph afParagraph = addressFrame.AddParagraph();
			afParagraph.AddText(GetValue(item, "name/singleName"));
			afParagraph.AddLineBreak();
			afParagraph.AddText(GetValue(item, "address/line1"));
			afParagraph.AddLineBreak();
			afParagraph.AddText(GetValue(item, "address/postalCode") + " " + GetValue(item, "address/city"));

			// Iterate the invoice items
			double totalExtendedPrice = 0;
			//XPathNodeIterator iter = navigator.Select("/invoice/items/*");
			for(var i=0;i<40;i++){
				item = null;
				double quantity = GetValueAsDouble(item, "quantity");
				double price = GetValueAsDouble(item, "price");
				double discount = GetValueAsDouble(item, "discount");

				// Each item fills two rows
				Row row1 = table.AddRow();
				Row row2 = table.AddRow();
				row1.TopPadding = 1.5;
				row1.Cells[0].Shading.Color = TableGray;
				row1.Cells[0].VerticalAlignment = VerticalAlignment.Center;
				row1.Cells[0].MergeDown = 1;
				row1.Cells[1].Format.Alignment = ParagraphAlignment.Left;
				row1.Cells[1].MergeRight = 3;
				row1.Cells[5].Shading.Color = TableGray;
				row1.Cells[5].MergeDown = 1;

				row1.Cells[0].AddParagraph(GetValue(item, "itemNumber"));
				afParagraph = row1.Cells[1].AddParagraph();
				afParagraph.AddFormattedText(GetValue(item, "title"), TextFormat.Bold);
				afParagraph.AddFormattedText(" by ", TextFormat.Italic);
				afParagraph.AddText(GetValue(item, "author"));
				row2.Cells[1].AddParagraph(GetValue(item, "quantity"));
				row2.Cells[2].AddParagraph(price.ToString("0.00") + " €");
				row2.Cells[3].AddParagraph(discount.ToString("0.0"));
				row2.Cells[4].AddParagraph();
				row2.Cells[5].AddParagraph(price.ToString("0.00"));
				double extendedPrice = quantity * price;
				extendedPrice = extendedPrice * (100 - discount) / 100;
				row1.Cells[5].AddParagraph(extendedPrice.ToString("0.00") + " €");
				row1.Cells[5].VerticalAlignment = VerticalAlignment.Bottom;
				totalExtendedPrice += extendedPrice;

				table.SetEdge(0, table.Rows.Count - 2, 6, 2, Edge.Box, BorderStyle.Single, 0.75);
			}

			// Add an invisible row as a space line to the table
			row = table.AddRow();
			row.Borders.Visible = false;

			// Add the total price row
			row = table.AddRow();
			row.Cells[0].Borders.Visible = false;
			row.Cells[0].AddParagraph("Total Price");
			row.Cells[0].Format.Font.Bold = true;
			row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
			row.Cells[0].MergeRight = 4;
			row.Cells[5].AddParagraph(totalExtendedPrice.ToString("0.00") + " €");

			// Add the VAT row
			row = table.AddRow();
			row.Cells[0].Borders.Visible = false;
			row.Cells[0].AddParagraph("VAT (19%)");
			row.Cells[0].Format.Font.Bold = true;
			row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
			row.Cells[0].MergeRight = 4;
			row.Cells[5].AddParagraph((0.19 * totalExtendedPrice).ToString("0.00") + " €");

			// Add the additional fee row
			row = table.AddRow();
			row.Cells[0].Borders.Visible = false;
			row.Cells[0].AddParagraph("Shipping and Handling");
			row.Cells[5].AddParagraph(0.ToString("0.00") + " €");
			row.Cells[0].Format.Font.Bold = true;
			row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
			row.Cells[0].MergeRight = 4;

			// Add the total due row
			row = table.AddRow();
			row.Cells[0].AddParagraph("Total Due");
			row.Cells[0].Borders.Visible = false;
			row.Cells[0].Format.Font.Bold = true;
			row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
			row.Cells[0].MergeRight = 4;
			totalExtendedPrice += 0.19 * totalExtendedPrice;
			row.Cells[5].AddParagraph(totalExtendedPrice.ToString("0.00") + " €");

			// Set the borders of the specified cell range
			table.SetEdge(5, table.Rows.Count - 4, 1, 4, Edge.Box, BorderStyle.Single, 0.75);

			// Add the notes paragraph
			afParagraph = document.LastSection.AddParagraph();
			afParagraph.Format.SpaceBefore = "1cm";
			afParagraph.Format.Borders.Width = 0.75;
			afParagraph.Format.Borders.Distance = 3;
			afParagraph.Format.Borders.Color = TableBorder;
			afParagraph.Format.Shading.Color = TableGray;
			item = SelectItem("/invoice");
			afParagraph.AddText(GetValue(item, "notes"));
			*/
			return document;
		}

		private static double GetValueAsDouble(XPathNavigator item, string quantity)
		{
			return 12.4;
		}

		private static XPathNavigator SelectItem(string invoice)
		{
			return null;
		}

		private static string GetValue(XPathNavigator item, string p)
		{
			return "12.40";
		}


		/*public static PdfPage GenerateScorecard(ISession s, PermissionsUtility perms, long recurrenceId)
		{
			perms.ViewL10Recurrence(recurrenceId);


		}*/
	}
}