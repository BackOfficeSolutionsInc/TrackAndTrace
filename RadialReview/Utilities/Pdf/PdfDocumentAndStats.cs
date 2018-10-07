using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Pdf {

	public class DocStats {
		public DocStats(XRect? boundary, double scale) {
			Boundry = boundary??XRect.Empty;
			HasBoundry = Boundry != XRect.Empty;
			Scale = scale;

		}
		public XRect Boundry { get; set; }
		public double Scale { get; set; }
		public bool HasBoundry { get; set; }
	}

	public class PdfDocumentAndStats {
		public PdfDocument Document { get; set; }
		public DocStats Stats { get; set; }

		public PdfDocumentAndStats(PdfDocument doc,DocStats stats) {
			Stats = stats ?? new DocStats(null,1);
			Document = doc;
			if (Stats.HasBoundry) {
				foreach (var p in Document.Pages) {
					p.CropBox = new PdfRectangle(Stats.Boundry);
				}
			}
		}

		public static implicit operator PdfDocument(PdfDocumentAndStats p) {
			return p.Document;
		}

		public IEnumerable<PdfPageAndStats> GetPages() {
			foreach (var p in Document.Pages) {
				yield return new PdfPageAndStats() {
					Boundry = Stats.Boundry,
					HasBoundry = Stats.HasBoundry,
					Page = p,
					Scale = Stats.Scale
				};
			}
		}
	}

	public class PdfPageAndStats {
		public XRect Boundry { get; set; }
		public PdfPage Page { get; set; }
		public bool HasBoundry { get; set; }
		public double Scale { get; set; }
		public override bool Equals(object obj) {
			if (obj is PdfPageAndStats) {
				return Page.Equals((obj as PdfPageAndStats).Page);
			}
			return false;
		}
		public override int GetHashCode() {
			return Page.GetHashCode();
		}
	}
}