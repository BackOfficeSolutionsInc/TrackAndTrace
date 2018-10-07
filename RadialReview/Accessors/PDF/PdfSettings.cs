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
using static RadialReview.Models.OrganizationModel;
using PdfSharp.Drawing;

namespace RadialReview.Accessors.PDF {
	public class PdfSettings {

		public Color BorderColor { get; set; }
		public Color FillColor { get; set; }

		public XColor BorderXColor { get { return XColor.FromArgb(BorderColor.Argb); } }
		public XColor FillXColor { get { return XColor.FromArgb(FillColor.Argb); } }

		public PdfSettings() {
			BorderColor = new Color(0, 0, 0);
			FillColor = new Color(100, 100, 100, 100);
		}

		public PdfSettings(OrganizationSettings settings) {
			BorderColor = settings.PrimaryColor.ToMigradocColor();
			FillColor = settings.PrimaryColor.WithAlpha(100).ToMigradocColor();
		}
	}
}