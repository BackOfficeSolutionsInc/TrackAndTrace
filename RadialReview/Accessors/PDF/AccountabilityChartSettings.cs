using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.DocumentObjectModel;
using RadialReview.Models.Angular.VTO;
using Table = MigraDoc.DocumentObjectModel.Tables.Table;
using VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment;
using RadialReview.Utilities;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using RadialReview.Utilities.DataTypes;
using MigraDoc.DocumentObjectModel;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using static RadialReview.Accessors.PDF.D3.Layout;
using static RadialReview.Accessors.PDF.JS.Tree;
using static RadialReview.Utilities.Pdf.DocumentMerger;
using RadialReview.Models.Components;
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

			public AccountabilityChartSettings(ColorComponent border) {
				boxPen = new XPen(border.ToXColor(), 1);
			}

			public AccountabilityChartSettings(OrganizationModel.OrganizationSettings settings) : this(settings.PrimaryColor){}
		}
	}
}