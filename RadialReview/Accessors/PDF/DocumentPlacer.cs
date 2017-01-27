using MigraDoc.DocumentObjectModel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors.PDF {
	//	public class DocumentPlacer {

	//		public static Unit DEFAULT_PAGE_MARGINS = Unit.FromInch(0.3);
	//		public static Unit DEFAULT_COLUMN_MARGINS = Unit.FromInch(0.15);

	//		public Unit DefaultMargin { get; set; }

	//		public DocumentArea CurrentPlaceableArea { get; set; }
	//		public IEnumerable<DocumentArea> PlaceableAreas { get; set; }
	//		public PdfDocument Document { get; set; }

	//		public DocumentPlacer() {
	//			DefaultMargin = DEFAULT_PAGE_MARGINS;
	//		}

	//		public static IEnumerable<DocumentArea> GeneratePlaceableAreaFromPage(PdfDocument doc,int columns = 1) {
	//			if (columns < 1)
	//				columns = 1;

	//			while (true) {
	//				var page = doc.AddPage();
	//				XGraphics gfx = XGraphics.FromPdfPage(page);
	//				var xMargin = DEFAULT_PAGE_MARGINS;
	//				var yMargin = DEFAULT_PAGE_MARGINS;
	//				var colMargin = DEFAULT_COLUMN_MARGINS;
	//				var curX = xMargin;
	//				var curY = yMargin;
	//				var columnWidth = (page.Width - (2 * xMargin) - ((columns - 1) * colMargin)) / columns;
	//				var columnHeight = page.Height - (2 * yMargin);

	//				for (var i = 0; i < columns; i++) {
	//					var placement = new XRect(curX, curY, columnWidth, columnHeight);
	//					curX += columnWidth + colMargin;
	//					yield return new DocumentArea(page, gfx, placement, 0);					
	//				}
	//			});
	//		}
	//	}

	//	public DocumentPlacer(IEnumerable<DocumentArea> placeableAreas) : this() {
	//		if (placeableAreas == null)
	//			throw new ArgumentNullException("placeableAreas");

	//		PlaceableAreas = placeableAreas;
	//	}



	//}

	//public abstract class DocumentElement {
	//	public abstract Unit GetWidth();
	//	public abstract Unit GetHeight();
	//}

	//public class DocumentArea {
	//	public XRect OuterPlacement { get; set; }
	//	public XRect InnerPlacement { get; set; }
	//	public PdfPage Page { get; set; }
	//	public XGraphics Gfx { get; set; }
	//	public bool IsLastType { get; set; }

	//	private XRect FromMargin(XRect placement, Unit margin) {
	//		var marg = margin;
	//		return new XRect(placement.Left + marg, placement.Top + marg, Math.Max(1, placement.Width - 2 * marg), Math.Max(1, placement.Height - 2 * marg));
	//	}

	//	public DocumentArea(PdfPage page, XGraphics gfx) : this(page, gfx, new XRect(0, 0, page.Width, page.Height)) {
	//	}
	//	public DocumentArea(PdfPage page, XGraphics gfx, XRect placement) : this(page, gfx, new XRect(0, 0, page.Width, page.Height), DocumentPlacer.DEFAULT_PAGE_MARGINS) {
	//	}

	//	public DocumentArea(PdfPage page, XGraphics gfx, XRect placement, Unit margins) {
	//		Page = page;
	//		Gfx = gfx;
	//		OuterPlacement = placement;
	//		InnerPlacement = FromMargin(placement, margins);
	//	}




	//}

}