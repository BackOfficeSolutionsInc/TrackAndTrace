using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Internals;
using MigraDoc.Rendering;
using TractionTools.Tests.Permissions;
using System.IO;
using RadialReview.Utilities.Constants;

namespace MigraDoc.DocumentObjectModel {
	[TestClass]
	public class MigradocExtension : BasePermissionsTest {



		[TestMethod]
		public void MigradocExtension1() {
			var doc = new Document();
			var s = doc.AddSection();
			var i =s.AddImage(PdfImageConst.GreenCheck);
			i.Width = Unit.FromInch(.1);
			i.Height = Unit.FromInch(.1);
			i = s.AddImage(PdfImageConst.RedX	);
			i.Width = Unit.FromInch(.2);
			i.Height = Unit.FromInch(.3);

			Save(doc, "MigradocExtension.pdf");
		}

		private void Save(Document doc, string name) {
			PdfDocumentRenderer renderer = new PdfDocumentRenderer(true);
			renderer.Document = doc;
			renderer.RenderDocument();
			renderer.PdfDocument.Save(Path.Combine(GetCurrentPdfFolder(), name));
			renderer.PdfDocument.Save(Path.Combine(GetPdfFolder(), name));
		}
	}
}
