using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RadialReview.Utilities.Pdf {

	public class DocumentMerger {
		protected List<object> docs { get; set; }
		public DocumentMerger() {
			docs = new List<object>();
		}

		public void AddDocs(IEnumerable<MultiPageDocument> docList) {
			foreach (var doc in docList) {
				docs.Add(doc);
			}
		}
		public void AddDocs(IEnumerable<PdfDocument> docList) {
			foreach (var doc in docList) {
				docs.Add(doc);
			}
		}
		public void AddDocs(IEnumerable<Document> docList) {
			foreach (var doc in docList) {
				docs.Add(doc);
			}
		}
		public void AddDoc(PdfDocument doc) {
			docs.Add(doc);
		}
		public void AddDoc(MultiPageDocument doc) {
			docs.Add(doc);
		}
		public void AddDoc(Document doc) {
			docs.Add(doc);
		}
		public void AddDoc(PdfDocumentRenderer doc) {
			docs.Add(doc);
		}
		
		public PdfDocument Flatten(string title, bool includeNumber, bool includeDate = true, string dateFormat = null, string name = null) {
			DateTime now = DateTime.Now;

			PdfDocument document = new PdfDocument();
			document.Info.Title = title;
			document.Info.Author = "Traction Tools";
			document.Info.Keywords = "Traction Tools";
			document.Info.CreationDate = now;

			int? pages = 0;
			XFont font = new XFont("Verdana", 10, XFontStyle.Regular);

			if (!docs.Any()) {
				//Cannot save empty document
				var doc = new Document();
				var section = new Section();
				var paragraph = new Paragraph();
				var text = paragraph.AddFormattedText("Page intentionally left blank.", new Font("Verdana", 10));
				text.Color = Colors.Gray;
				paragraph.Format.Alignment = ParagraphAlignment.Center;
				section.Add(paragraph);
				doc.Add(section);
				AddDoc(doc);
			}

			foreach (var docPage in docs) {
				var doc = docPage;
				if (docPage is MultiPageDocument) {
					var scaleDoc = ((MultiPageDocument)docPage);
					doc = scaleDoc.Flatten().Document;
				}
				RenderPage(document, doc, name,ref pages, includeNumber, includeDate, dateFormat, font, now);
			}
			return document;

		}

		protected void RenderPage(PdfDocument parentDoc, object doc, string name,ref int? pageNumber, bool includeNumber, bool includeDate, string dateFormat, XFont font, DateTime now) {
			if (doc is PdfDocument) {
				RenderPdfDocument(parentDoc, (PdfDocument)doc, name,ref pageNumber, includeNumber, includeDate, dateFormat, font, now);
			}
			if (doc is Document) {
				RenderMigradoc(parentDoc, name,ref pageNumber, includeNumber, includeDate, dateFormat, font, now, (Document)doc);
			}
			if (doc is PdfDocumentRenderer) {
				RenderPdfRenderer(parentDoc, name,ref pageNumber, includeNumber, includeDate, dateFormat, font, now, (PdfDocumentRenderer)doc);
			}
		}

		private void RenderPdfRenderer(PdfDocument parentDoc, string name,ref int? pageNumber, bool includeNumber, bool includeDate, string dateFormat, XFont font, DateTime now, PdfDocumentRenderer mdoc) {
			PdfDocument newPdfDoc;
			using (var stream = new MemoryStream()) {
				mdoc.Save(stream, false);
				newPdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
			}
			foreach (var p in newPdfDoc.Pages) {
				var page = parentDoc.AddPage(p);
				page.Width = p.Width;
				page.Height = p.Height;
				page.Orientation = p.Orientation;
				XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
				pageNumber += 1;
				DrawNumber(gfx, font, pageNumber != null ? (int?)(pageNumber) : null, now, includeNumber, includeDate, dateFormat, name);
			}
		}

		private void RenderMigradoc(PdfDocument parentDoc, string name,ref int? pageNumber, bool includeNumber, bool includeDate, string dateFormat, XFont font, DateTime now, Document mdoc) {
			DocumentRenderer docRenderer = new DocumentRenderer(mdoc);
			docRenderer.PrepareDocument();
			int pageCount = docRenderer.FormattedDocument.PageCount;
			for (int idx = 0; idx < pageCount; idx++) {
				PdfPage page = parentDoc.AddPage();
				var pageInfo = docRenderer.FormattedDocument.GetPageInfo(idx + 1);
				page.Width = pageInfo.Width;
				page.Height = pageInfo.Height;
				page.Orientation = pageInfo.Orientation;
				XGraphics gfx = XGraphics.FromPdfPage(page);
				gfx.MUH = PdfFontEncoding.Unicode;
				docRenderer.RenderPage(gfx, idx + 1);
				pageNumber += 1;
				DrawNumber(gfx, font, pageNumber != null ? (int?)(pageNumber) : null, now, includeNumber, includeDate, dateFormat, name);
			}
		}

		private void RenderPdfDocument(PdfDocument parentDoc, PdfDocument pdfDoc, string name,ref int? pageNumber, bool includeNumber, bool includeDate, string dateFormat, XFont font, DateTime now) {

			PdfDocument newPdfDoc;
			using (var stream = new MemoryStream()) {
				pdfDoc.Save(stream, false);
				newPdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
			}
			try {
				foreach (var p in newPdfDoc.Pages) {
					var page = parentDoc.AddPage(p);
					page.Width = p.Width;
					page.Height = p.Height;
					page.Orientation = p.Orientation;
					XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
					pageNumber += 1;
					DrawNumber(gfx, font, pageNumber != null ? (int?)(pageNumber) : null, now, includeNumber, includeDate, dateFormat, name);
				}
			} catch (Exception e) {
				throw;
			}
		}

		protected void DrawNumber(XGraphics gfx, XFont font, int? number, DateTime? date, bool includeNumber, bool includeDate, string dateFormat, string name = null) {
			var wmargin = 35;
			var hmargin = 22;

			var size = new XSize(Math.Max(0, gfx.PageSize.Width - wmargin * 2), Math.Max(0, gfx.PageSize.Height - hmargin * 2));

			if (includeNumber && number != null) {
				gfx.DrawString(number.ToString(), font, XBrushes.Black, new XRect(new XPoint(wmargin, hmargin), size), XStringFormats.BottomRight);
			}
			if (includeDate && date != null) {
				var text = "" + date.Value.ToString(dateFormat ?? "MM-dd-yyyy") + "   " + name ?? "";
				var gray = new XSolidBrush(XColor.FromArgb(100, 100, 100, 100));
				var dateFont = new XFont("Arial Narrow", 9, XFontStyle.Regular);

				gfx.DrawString(text, dateFont, gray, new XRect(new XPoint(wmargin/*22*/, hmargin), size), XStringFormats.BottomLeft);
			}
		}
	}
}