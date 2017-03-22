using MigraDoc.DocumentObjectModel;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors.PDF {
	public class AccountabilityChartPDF {

		public class ACNode : D3.Layout.node<ACNode> {
			public string Name { get; set; }
			public string Position { get; set; }
			public List<string> Roles { get; set; }
			public bool isLeaf { get { return children == null || children.Count == 0; } }

			public string side = "fix me";
			public bool hasHiddenChildren = false;
		}

		private static ACNode dive(AngularAccountabilityNode aanode) {
			var pos = "";
			var roles = new List<String>();
			var children = aanode.children ?? new List<AngularAccountabilityNode>();

			if (aanode.Group != null) {
				if (aanode.Group.Position != null)
					pos = aanode.Group.Position.Name ?? "";
				if (aanode.Group.RoleGroups != null)
					roles = aanode.Group.RoleGroups.SelectMany(x => x.Roles.Select(y => y.Name)).ToList();
			}

			var node = new ACNode() {
				Name = aanode.Name,
				Position = pos,
				Roles = roles,
				Id = aanode.Id,
				children = children.Select(x => dive(x)).ToList(),
				width = 200,
				height = 200,
			};
			
			return node;
		}

		public static PdfDocument GenerateAccountabilityChart(AngularAccountabilityNode root, double width, double height, bool restrictSize = false) {
			var rootACNode = dive(root);
			return GenerateAccountabilityChart(rootACNode, width, height, restrictSize);
		}

		private static PdfDocument GenerateAccountabilityChart(ACNode root, double width, double height, bool restrictSize = false) {

			// Create new PDF document
			PdfDocument document = new PdfDocument();
			// Create new page
			var _unusedPage = new PdfPage();
			_unusedPage.Width = XUnit.FromInch(width);
			_unusedPage.Height = XUnit.FromInch(height);

			//_unusedPage.Orientation = PdfSharp.PageOrientation.Landscape;

			var margin = XUnit.FromInch(.25);

			var pageProp = new PageProp() {
				pageWidth = _unusedPage.Width,
				pageHeight = _unusedPage.Height,
				margin = margin
			};

			if (restrictSize) {
				document.AddPage(_unusedPage);
				ACGenerate_Resized(_unusedPage, root, pageProp);
			} else {
				ACGenerate_Full(document, root, pageProp, .5);
			}
			return document;
		}

		//public class AccNodeJs {
		//	public List<AccNodeJs> children { get; set; }
		//	public List<AccNodeJs> _children { get; set; }
		//	public string Name { get; set; }
		//	public string Position { get; set; }
		//	public List<string> Roles { get; set; }

		//	public bool isLeaf { get; set; }
		//	public string side { get; set; }


		//	public bool hasHiddenChildren { get; set; }

		//	public double x { get; set; }
		//	public double y { get; set; }
		//	public double width { get; set; }
		//	public double height { get; set; }
		//}

		private class PageProp {
			public XUnit pageWidth { get; set; }
			public XUnit pageHeight { get; set; }
			public XUnit margin { get; set; }
			public XPen pen = new XPen(XColors.Black, 1);
			public XBrush brush = new XSolidBrush(XColors.Transparent);

			public double scale = 1;

			public XUnit allowedWidth {
				get {
					return pageWidth - 2 * margin;
				}
			}
			public XUnit allowedHeight {
				get {
					return pageHeight - 2 * margin;
				}
			}
		}

		#region AC Helpers 
		private static void ACDrawRole(XGraphics gfx, ACNode me, PageProp pageProps, double[] origin = null) {
			origin = origin ?? new[] { 0.0, 0.0 };

			var x = (int)me.x - origin[0];
			var y = (int)me.y - origin[1];


			var top = 50 * pageProps.scale;
			var pad = 1.0 / 24.0 * me.width;

			var tf = new XTextFormatter(gfx);

			tf.Alignment = XParagraphAlignment.Center;
			XFont bold = new XFont("Times New Roman", 14 * pageProps.scale, XFontStyle.Bold);
			XFont norm = new XFont("Times New Roman", 14 * pageProps.scale, XFontStyle.Regular);
			tf.DrawString(me.Position ?? "", bold, XBrushes.Black, new XRect(x, y + 12 * pageProps.scale / 3.0, Math.Max(0, me.width), top / 2.0));
			tf.DrawString(me.Name ?? "", norm, XBrushes.Black, new XRect(x, y + top / 2.0 + 12 * pageProps.scale / 3.0, Math.Max(0, me.width), top / 2.0));

			if (me.height > top) {
				gfx.DrawLine(XPens.Black, x + pad, y + top, x + (me.width - 2 * pad), y + top);
			}

			tf = new XTextFormatter(gfx);
			tf.Alignment = XParagraphAlignment.Left;
			norm = new XFont("Times New Roman", 12 * pageProps.scale, XFontStyle.Regular);

			var h = 50 * pageProps.scale;

			if (me.Roles != null && me.Roles.Any()) {
				var rheight = (me.height - top) / (me.Roles.Count + 1);
				//h += rheight / 2.0;
				foreach (var r in me.Roles) {
					var text = r;
					if (text != null) {
						text = text.TrimStart(' ', '•', '*');
						//text = "• " + r;
					}
					var dotWidth = Math.Max(0, pad);

					tf.DrawString("•", norm, XBrushes.Black, new XRect(x + pad, y + h, dotWidth, Math.Max(0, rheight)));
					//var myHeight = tf.meas
					var wrapper = new PdfWordWrapper(gfx, Unit.FromPoint(me.width - (pad * 2 + dotWidth)));
					wrapper.Add(text ?? "", norm, XBrushes.Black);
					var size = wrapper.Size;
					wrapper.Draw(gfx, x + pad + dotWidth, y + h, PdfWordWrapper.Alignment.Left);
					//tf.DrawString(text ?? "", norm, XBrushes.Black, new XRect(x + pad, y + h, me.width - pad * 2, rheight));
					h += size.Height;
				}
			}
			me.height = /*Math.Max(me.height,*/ h + 2 * pad/*)*/;
			gfx.DrawRectangle(pageProps.pen, pageProps.brush, x, y, (int)Math.Max(0, me.width), (int)Math.Max(0, me.height));
		}

		private static void DrawLine(XGraphics gfx, PageProp pageProps, List<Tuple<double, double>> points) {
			for (var i = 1; i < points.Count; i++) {
				gfx.DrawLine(pageProps.pen, points[i - 1].Item1, points[i - 1].Item2, points[i].Item1, points[i].Item2);
			}
		}

		private static void ACDrawRoleLine(XGraphics gfx, ACNode parent, ACNode me, PageProp pageProps, double[] origin = null) {
			origin = origin ?? new[] { 0.0, 0.0 };
			//gfx.DrawRectangle(pageProps.pen, pageProps.brush, (int)me.x - origin[0], (int)me.y - origin[1], (int)me.width, (int)me.height);

			//var parentX1 = parent.x + parent.width / 2.0 - origin[0];
			//var parentY1 = parent.y + parent.height - origin[1] - 1;
			//var parentY2 = parent.y + parent.height + (me.y - (parent.y + parent.height)) * 2 / 3 - origin[1];
			//var parentX2 = me.x + me.width / 2.0 - origin[0];

			//var parentY3 = me.y - origin[1];

			//gfx.DrawLine(pageProps.pen, parentX1, parentY1, parentX1, parentY2);
			//gfx.DrawLine(pageProps.pen, parentX1, parentY2, parentX2, parentY2);
			//gfx.DrawLine(pageProps.pen, parentX2, parentY2, parentX2, parentY3);

			var vSeparation = Math.Abs(me.y - (parent.y + parent.height)) * 2 / 3;
			var hSeparation = 25 * pageProps.scale;

			var adjS = 1 * pageProps.scale;


			var sx = parent.x - origin[0] + parent.width / 2;
			var sy = parent.y - origin[1] + parent.height - adjS;
			var tx = me.x - origin[0] + me.width / 2;
			var ty = me.y - origin[1] - adjS;
			var my = sy + vSeparation /*- origin[1]*/;
			if (me.isLeaf) {
				var tw = me.width;
				double lx;
				if (me.side == "left") {
					tx = tx - tw / 2 - adjS;
					lx = tx - hSeparation / 2 /*- origin[0]*/+ adjS;
				} else {
					tx = tx + tw / 2 - adjS;
					lx = tx + hSeparation / 2 /*- origin[0]*/+ adjS;
				}
				//return;

				//lx = tx + tw/2  - hSeparation / 2 - origin[0];

				var tyy = ty + Math.Min(10, me.height / 2);
				var points = new List<Tuple<double, double>>();
				if (!parent.isLeaf) {
					points.Add(Tuple.Create(sx, sy));
					points.Add(Tuple.Create(sx, my));
					points.Add(Tuple.Create(lx, my));
				} else {
					double ax;
					if (me.side == "left") {
						ax = sx /*- origin[0]*/ - parent.width / 2;// - d.source.width / 2;
					} else {
						ax = sx /*- origin[0]*/ + parent.width / 2 - adjS;// - d.source.width / 2;
					}


					var ay = (sy - parent.height) + Math.Min(10, parent.height / 2) /*- origin[1]*/ - adjS;//d.source.height / 2;
																										   //points.Add(Tuple.Create(ax, ay));
					points.Add(Tuple.Create(lx, ay));
				}
				points.Add(Tuple.Create(lx, tyy));
				points.Add(Tuple.Create(tx, tyy));

				DrawLine(gfx, pageProps, points);
			} else {
				var points = new List<Tuple<double, double>>() {
						Tuple.Create(sx, sy),
						Tuple.Create(sx, my),
						Tuple.Create(tx, my),
						Tuple.Create(tx, ty)
				};
				DrawLine(gfx, pageProps, points);
			}

		}

		private static void ACDrawRoles(XGraphics gfx, ACNode root, PageProp pageProps, double[] origin = null) {
			ACDrawRole(gfx, root, pageProps, origin);
			if (root.children != null) {
				foreach (var c in root.children) {
					ACDrawRoleLine(gfx, root, c, pageProps, origin);
					ACDrawRoles(gfx, c, pageProps, origin);
				}
				if (root.hasHiddenChildren) {
					ACDrawEllipse(gfx, root, pageProps, origin);
				}


			}
		}

		private static void ACDrawEllipse(XGraphics gfx, ACNode root, PageProp pageProps, double[] origin = null) {
			origin = origin ?? new[] { 0.0, 0.0 };
			var x = root.x + root.width / 2.0 - origin[0];
			var y = root.y + root.height - origin[1];

			for (var ii = 0; ii < 3; ii += 1) {
				var i = (3 + 6 * ii) * pageProps.scale;
				var d = (4.0) * pageProps.scale;
				gfx.DrawEllipse(XBrushes.Black, x - (d / 2.0), y + i - (d / 2.0), d, d);
			}
		}


		private static double[] ACRanges(ACNode root) {
			var range = new[] { root.x, root.y, root.x + root.width, root.y + root.height };
			if (root.children != null) {
				foreach (var n in root.children) {
					var n_range = ACRanges(n);
					range[0] = Math.Min(range[0], n_range[0]);
					range[1] = Math.Min(range[1], n_range[1]);
					range[2] = Math.Max(range[2], n_range[2]);
					range[3] = Math.Max(range[3], n_range[3]);
				}
			}
			return range;
		}
		private static void ACNormalize(ACNode root, double[] range, PageProp pageProp, double? forceScale = null) {
			root.x += -range[0];
			root.y += -range[1];
			var shouldScale = forceScale == null;
			var scale = forceScale ?? 1.0;

			var width = (double)(range[2] - range[0]);
			var height = (double)(range[3] - range[1]);
			if (shouldScale) {
				if (width > pageProp.allowedWidth)
					scale = pageProp.allowedWidth / width;
				if (height > pageProp.allowedHeight)
					scale = Math.Min(scale, pageProp.allowedHeight / height);
			}

			//For centering
			var numPagesWide = width * scale / pageProp.allowedWidth;
			var numPagesTall = height * scale / pageProp.allowedHeight;

			//Rescale
			root.x *= scale;
			root.y *= scale;
			root.width *= scale;
			root.height *= scale;

			pageProp.scale = scale;

			var extraWidth = (Math.Ceiling(numPagesWide) - (numPagesWide)) * pageProp.allowedWidth;
			var extraHeight = (Math.Ceiling(numPagesTall) - (numPagesTall)) * pageProp.allowedHeight;
			root.x += extraWidth / 2.0;
			root.y += extraHeight / 2.0;

			root.x += pageProp.margin.Point;
			root.y += pageProp.margin.Point;

			if (root.children != null) {
				foreach (var n in root.children) {
					ACNormalize(n, range, pageProp, scale);
				}
			}
		}
		private static Tuple<int, int> ACGetPage(double x, double y, PageProp pageProp) {
			return Tuple.Create((int)(x / pageProp.allowedWidth), (int)(y / pageProp.allowedHeight));
		}
		private static double[] ACGetOrigin(Tuple<int, int> page, PageProp pageProp) {
			return new[] {
				page.Item1 * pageProp.allowedWidth,
				page.Item2 * pageProp.allowedHeight
			};

		}
		private static void ACGeneratePages(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, ACNode root, PageProp pageProp) {
			var a = pageLookup[ACGetPage(root.x, root.y, pageProp)];
			var b = pageLookup[ACGetPage(root.x + root.width, root.y, pageProp)];
			var c = pageLookup[ACGetPage(root.x + root.width, root.y + root.height, pageProp)];
			var d = pageLookup[ACGetPage(root.x, root.y + root.height, pageProp)];
			if (root.children != null) {
				foreach (var p in root.children) {
					ACGeneratePages(pageLookup, p, pageProp);
				}
			}
		}
		private static void ACDrawOnAllPages(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, DefaultDictionary<PdfPage, XGraphics> gfxLookup, ACNode parent, ACNode me, PageProp pageProp) {
			var pages = new List<Tuple<int, int>>();

			pages.Add(ACGetPage(me.x, me.y, pageProp));
			pages.Add(ACGetPage(me.x + me.width, me.y, pageProp));
			pages.Add(ACGetPage(me.x + me.width, me.y + me.height, pageProp));
			pages.Add(ACGetPage(me.x, me.y + me.height, pageProp));
			if (parent != null) {
				pages.Add(ACGetPage(parent.x, parent.y, pageProp));
				pages.Add(ACGetPage(parent.x + parent.width, parent.y, pageProp));
				pages.Add(ACGetPage(parent.x + parent.width, parent.y + parent.height, pageProp));
				pages.Add(ACGetPage(parent.x, parent.y + parent.height, pageProp));
			}
			pages.Distinct();

			foreach (var p in pages) {
				var origin = ACGetOrigin(p, pageProp);
				var page = pageLookup[p];
				var gfx = gfxLookup[page];
				ACDrawRole(gfx, me, pageProp, origin);
				if (parent != null)
					ACDrawRoleLine(gfx, parent, me, pageProp, origin);

				if (me.hasHiddenChildren) {
					ACDrawEllipse(gfx, me, pageProp, origin);
				}
			}
		}


		private static void ACDrawOnAllPages_Dive(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, DefaultDictionary<PdfPage, XGraphics> gfxLookup, ACNode me, PageProp pageProp, ACNode parent = null) {
			ACDrawOnAllPages(pageLookup, gfxLookup, parent, me, pageProp);
			if (me.children != null) {
				foreach (var c in me.children) {
					ACDrawOnAllPages_Dive(pageLookup, gfxLookup, c, pageProp, me);
				}
			}
		}

		#endregion

		private static void ACGenerate_Resized(PdfPage page, ACNode root, PageProp pageProp) {
			var ranges = ACRanges(root);
			ACNormalize(root, ranges, pageProp, null);
			XGraphics gfx = XGraphics.FromPdfPage(page);
			ACDrawRoles(gfx, root, pageProp);
		}

		private static void ACGenerate_Full(PdfDocument doc, ACNode root, PageProp pageProp, double scale) {
			var ranges = ACRanges(root);
			ACNormalize(root, ranges, pageProp, scale);

			var pageLookup = new DefaultDictionary<Tuple<int, int>, PdfPage>(x => new PdfPage(doc) {
				Width = pageProp.pageWidth,
				Height = pageProp.pageHeight
			});
			var gfxLookup = new DefaultDictionary<PdfPage, XGraphics>(x => XGraphics.FromPdfPage(x));

			ACGeneratePages(pageLookup, root, pageProp);
			ACDrawOnAllPages_Dive(pageLookup, gfxLookup, root, pageProp);

			pageLookup.Keys.OrderBy(x => x.Item1)
				.ThenBy(x => x.Item2)
				.ToList()
				.ForEach(x => {
					doc.AddPage(pageLookup[x]);
				});
		}
	}
}