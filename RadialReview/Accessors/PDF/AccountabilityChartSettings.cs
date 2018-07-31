using PdfSharp.Drawing;
using RadialReview.Models;

namespace RadialReview.Accessors.PDF {
    public partial class AccountabilityChartPDF {
		//public class AccountabilityChartSettings {
		//	public Color FillColor { get; set; }
		//	public Color TextColor { get; set; }
		//	public Color BorderColor { get; set; }
		//}


		public class AccountabilityChartSettings {
			public XUnit pageWidth { get; set; }
			public XUnit pageHeight { get; set; }
			public XUnit margin { get; set; }
			public XPen linePen = new XPen(XColors.Gray, .5) {
				LineJoin = XLineJoin.Miter,
				MiterLimit = 10,
				LineCap = XLineCap.Square
			};
			public XPen boxPen = new XPen(XColors.Black, 1);
			public XBrush brush = new XSolidBrush(XColors.Transparent);

			public double scale = 1;

			public XUnit allowedWidth {get {return pageWidth - 2 * margin;}}
			public XUnit allowedHeight {get {return pageHeight - 2 * margin;}}

			public AccountabilityChartSettings() {}
			

			public AccountabilityChartSettings(OrganizationModel.OrganizationSettings settings) {
				boxPen = new XPen(settings.PrimaryColor.ToXColor(), 1);
				linePen = new XPen(settings.PrimaryColor.ToXColor(), .5) {
					LineJoin = XLineJoin.Miter,
					MiterLimit = 10,
					LineCap = XLineCap.Square
				};

			}
		}
	}
}