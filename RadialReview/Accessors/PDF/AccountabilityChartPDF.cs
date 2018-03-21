using MigraDoc.DocumentObjectModel;
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

#pragma warning disable CS0162 // Unreachable code detected
namespace RadialReview.Accessors.PDF {
	public class AccountabilityChartPDF {

		public const bool DEBUG = false;
		public const string FONT = "Arial";

		public class ACNode : D3.Layout.node<ACNode> {
			public string Name { get; set; }
			public string Position { get; set; }
			public List<string> Roles { get; set; }
			public bool isLeaf { get { return children == null || children.Count == 0; } }

			//public string side = "left";
			public bool hasHiddenChildren = false;
		}

		private static ACNode dive(AngularAccountabilityNode aanode, TreeSettings settings) {
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
				Name = aanode.Name ?? aanode.User.NotNull(x => x.Name),
				Position = pos,
				Roles = roles,
				Id = aanode.Id,
				children = children.Select(x => dive(x, settings)).ToList(),
				width = settings.baseWidth - settings.hSeparation,
				height = settings.baseHeight,
				hasHiddenChildren = aanode.HasChildren() && aanode.collapsed,

			};

			return node;
		}

		private static ACNode diveSelectedNode(AngularAccountabilityNode aanode, TreeSettings settings, List<long> selectedNodes) {
			var pos = "";
			var roles = new List<String>();
			var children = aanode.children ?? new List<AngularAccountabilityNode>();

			if (aanode.Group != null) {
				if (aanode.Group.Position != null)
					pos = aanode.Group.Position.Name ?? "";
				if (aanode.Group.RoleGroups != null)
					roles = aanode.Group.RoleGroups.SelectMany(x => x.Roles.Select(y => y.Name)).ToList();
			}

			children = children.Where(t => selectedNodes.Contains(t.Id));

			var node = new ACNode() {
				Name = aanode.Name ?? aanode.User.NotNull(x => x.Name),
				Position = pos,
				Roles = roles,
				Id = aanode.Id,
				children = children.Select(x => diveSelectedNode(x, settings, selectedNodes)).ToList(),
				width = settings.baseWidth - settings.hSeparation,
				height = settings.baseHeight,
				hasHiddenChildren = aanode.HasChildren() && aanode.collapsed,

			};

			return node;
		}

		private static double HeightFunc(ACNode node, PageProp pageProps) {
			var testDoc = new PdfDocument();
			var testPage = testDoc.AddPage();
			var tester = XGraphics.FromPdfPage(testPage);
			ACDrawRole(tester, node, pageProps, null);
			return node.height;
		}

		private static Action<AngularAccountabilityNode> _collapser = new Action<AngularAccountabilityNode>(r => {
			r.collapsed = false;
			if (r.children != null) {
				foreach (var child in r.children) {
					child.collapsed = true;
				}
			}
		});

		public static List<PdfDocument> GenerateAccountabilityChartSingleLevels(AngularAccountabilityNode root, double width, double height, bool restrictSize = false, TreeSettings settings = null) {
			var docs = new List<PdfDocument>();
			_collapser(root);
			var doc = GenerateAccountabilityChart(root, width, height, restrictSize, settings);
			docs.Add(doc);

			root.collapsed = false;

			if (root.children != null) {
				foreach (var child in root.children) {
					child.collapsed = false;
					if (child.children != null && child.children.Any()) {
						_collapser(child);
						var childDoc = GenerateAccountabilityChart(child, width, height, restrictSize, settings, true);
						docs.Add(childDoc);
					}
				}
			}
			return docs;
		}

		public static List<PdfDocument> GenerateAccountabilityChartSingleLevels(List<AngularAccountabilityNode> roots, double width, double height, bool restrictSize = false, TreeSettings settings = null) {
			var settingsEmpty = settings == null;
			var docs = new List<PdfDocument>();
			roots = roots.Where(x => x != null).Distinct(x => x.Id).ToList();
			var seen = new List<long>();
			foreach (var r in roots) {
				_collapser(r);
				if (r.HasChildren() || !seen.Contains(r.Id)) {

					var useCompact = r.GetDirectChildren().Count() >= 8;


					var useSettings = settings ?? new TreeSettings() {
						compact = useCompact
					};

					docs.Add(GenerateAccountabilityChart(r, width, height, restrictSize, useSettings, r._hasParent ?? false));

					seen.Add(r.Id);
					seen.AddRange(r.children.Select(x => x.Id));
				}
			}
			return docs;
		}

		public static PdfDocument GenerateAccountabilityChart(AngularAccountabilityNode root, double width, double height, bool restrictSize = false, TreeSettings settings = null, bool anyAboveRoot = false, List<long> selectedNode = null) {

			settings = settings ?? new TreeSettings();
			var rootACNode = new ACNode();

			if (selectedNode != null)
				rootACNode = diveSelectedNode(root, settings, selectedNode);
			else
				rootACNode = dive(root, settings);


			var margin = XUnit.FromInch(.5);
			var pageProp = new PageProp() {
				pageWidth = XUnit.FromInch(width),
				pageHeight = XUnit.FromInch(height),
				margin = margin
			};
			var n = JS.Tree.Update(rootACNode, x => HeightFunc(x, pageProp), settings);
			return GenerateAccountabilityChart(rootACNode, pageProp, settings, restrictSize, anyAboveRoot);
		}



		private static PdfDocument GenerateAccountabilityChart(ACNode root, PageProp pageProps, TreeSettings settings, bool restrictSize = false, bool anyAboveRoot = false) {

			// Create new PDF document
			PdfDocument document = new PdfDocument();
			// Create new page

			var _unusedPage = new PdfPage();
			_unusedPage.Width = pageProps.pageWidth;
			_unusedPage.Height = pageProps.pageHeight;

			if (restrictSize) {
				document.AddPage(_unusedPage);
				ACGenerate_Resized(_unusedPage, root, pageProps, settings, anyAboveRoot);
			} else {
				ACGenerate_Full(document, root, pageProps, settings, .5, anyAboveRoot);
			}
			return document;
		}

		private class PageProp {
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


			if (me.side == "left") {
				x += me.width / 2;
			} else if (me.side == "right") {
				x -= me.width / 2;
			}



			//var top = 50 * pageProps.scale;
			var pad = 1.0 / 24.0 * me.width;

			var linePad = 12 * pageProps.scale / 6.0;

			var tf = new XTextFormatter(gfx);

			tf.Alignment = XParagraphAlignment.Center;
			XFont bold = new XFont(FONT, Math.Max(1, 14 * pageProps.scale), XFontStyle.Bold);
			XFont norm = new XFont(FONT, Math.Max(1, 14 * pageProps.scale), XFontStyle.Regular);

			//tf.DrawString(me.Position ?? "", bold, XBrushes.Black, new XRect(x, y + 12 * pageProps.scale / 6.0, Math.Max(0, me.width), top / 2.0));
			//tf.DrawString(me.Name ?? "", norm, XBrushes.Black, new XRect(x, y + top / 2.0 + 12 * pageProps.scale / 6.0, Math.Max(0, me.width), top / 2.0));


			//Position
			var positionWrapper = new PdfWordWrapper(gfx, Unit.FromPoint(Math.Max(0, me.width)));
			positionWrapper.Add(me.Position ?? "", bold, XBrushes.Black);

			var shift = Math.Max((50 * pageProps.scale / 2.0 - pad - linePad * 3/**/) - positionWrapper.Size.Height, 0) / 2.0;

			positionWrapper.Draw(gfx, x, y + linePad + shift, PdfWordWrapper.Alignment.Center);
			var nameWrapper = new PdfWordWrapper(gfx, Unit.FromPoint(Math.Max(0, me.width)));

			var ch = Math.Max(
				(50 * pageProps.scale / 2.0) + linePad,
				(positionWrapper.Size.Height * 1.1) + linePad
			);

			//Title
			var titleWrapper = new PdfWordWrapper(gfx, Unit.FromPoint(Math.Max(0, me.width)));
			titleWrapper.Add(me.Name ?? "", norm, XBrushes.Black);
			titleWrapper.Draw(gfx, x, y + ch, PdfWordWrapper.Alignment.Center);

			ch += Math.Max(
				(50 * pageProps.scale / 2.0),
				(titleWrapper.Size.Height * 1.1) + linePad
			);

			//var size = positionWrapper.Size;
			//ch += 12 * pageProps.scale / 6.0;

			if (me.height > ch) {
				gfx.DrawLine(XPens.Black, x + pad, y + ch, x + (me.width - 2 * pad), y + ch);
			}

			tf = new XTextFormatter(gfx);
			tf.Alignment = XParagraphAlignment.Left;
			norm = new XFont(FONT, Math.Max(1, 12 * pageProps.scale), XFontStyle.Regular);

			var h = ch + pad;//50 * pageProps.scale;
			var top = ch;

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

					tf.DrawString("•", norm, XBrushes.Black, new XRect(x + pad, y + h + (12 * pageProps.scale) / 4.5, dotWidth, Math.Max(0, rheight)));
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
			gfx.DrawRectangle(pageProps.boxPen, pageProps.brush, x, y, (int)Math.Max(0, me.width), (int)Math.Max(0, me.height));


			if (DEBUG) {
				gfx.DrawRectangle(new XPen(XPens.Blue) { DashStyle = XDashStyle.Dot }, x + PAGE_MARGIN, y + PAGE_MARGIN, me.width - PAGE_MARGIN * 2, me.height - PAGE_MARGIN * 2);

				gfx.DrawRectangle(XPens.Blue, x, y, me.width, me.height);
			}
		}

		private static void DrawLine(XGraphics gfx, PageProp pageProps, List<Tuple<double, double>> points) {

			gfx.DrawLines(pageProps.linePen, points.Select(x => new PointF((float)x.Item1, (float)x.Item2)).ToArray());
			//for (var i = 1; i < points.Count; i++) {
			//	var x1 = points[i - 1].Item1;
			//	var y1 = points[i - 1].Item2;
			//	var x2 = points[i].Item1;
			//	var y2 = points[i].Item2;
			//	var w2 = pageProps.pen.Width / 2.0;


			//	if (x1 < x2)
			//		x2 += w2;
			//	else if (x1 > x2)
			//		x2 -= w2;

			//	gfx.DrawLine(pageProps.pen, x1, y1, x2, y2);


			//	//if (i > 1 && i < points.Count - 1) {
			//	//	var w2 = pageProps.pen.Width; 
			//	//	var color = pageProps.pen.Color;
			//	//	var brush = new XSolidBrush(color);
			//	//	gfx.DrawEllipse(new XPen(color,0),brush, points[i].Item1 - w4, points[i].Item2 - w4, w2, w2);
			//	//}
			//}
		}


		private static void ACDrawRoleLine(XGraphics gfx, ACNode parent, ACNode me, PageProp pageProps, TreeSettings settings, double[] origin = null) {
			origin = origin ?? new[] { 0.0, 0.0 };

			var separation = Math.Abs(me.y - (parent.y + parent.height));

			var vSeparation = Math.Max(separation * 2.0 / 3.0, separation - 6.6667);
			var hSeparation = settings.hSeparation * pageProps.scale;

			var adjS = pageProps.linePen.Width * .5 /** pageProps.scale*/;


			var sx = parent.x - origin[0] + parent.width / 2;
			var sy = parent.y - origin[1] + parent.height - adjS;
			var tx = me.x - origin[0] + me.width / 2;
			var ty = me.y - origin[1] - adjS;
			var my = sy + vSeparation /*- origin[1]*/;

			var tempFont = new XFont("Times New Roman", Math.Max(1, 12 * pageProps.scale), XFontStyle.Regular);
			var sideL = "";
			if (me.isLeaf && parent.side != "left" && parent.side != "right") {
				var tw = me.width;
				double lx;
				if (me.side == "left") {
					//tx = tx - tw / 2 - adjS;
					//tx = tx + tw - adjS;
					lx = tx - hSeparation / 2 /*- origin[0]*/+ adjS;
					sideL = "L";
					//gfx.DrawString("L", tempFont, XBrushes.Red, sx, sy);
				} else if (me.side == "right") {
					//tx = tx + tw / 2 - adjS;
					//tx = tx - tw - adjS;
					lx = tx + hSeparation / 2 /*- origin[0]*/+ adjS;
					sideL = "R";
					//gfx.DrawString("R", tempFont, XBrushes.Red, sx, sy);
				} else {
					//lx = tx + tw/2  - hSeparation / 2 - origin[0];//maybe?
					sideL = "S";
					lx = tx /*+ hSeparation / 2 - origin[0]+ adjS*/;
				}

				var tyy = ty; //+ Math.Min(10, me.height / 2);
				var points = new List<Tuple<double, double>>();
				if (!parent.isLeaf) {
					points.Add(Tuple.Create(sx, sy));
					points.Add(Tuple.Create(sx, my));
					points.Add(Tuple.Create(lx, my));
				} else {
					//double ax;
					//if (me.side == "left") {
					//	//ax = sx /*- origin[0]*/ - parent.width / 2;// - d.source.width / 2;
					//	ax = sx /*- origin[0]*/ + parent.width / 2;// - d.source.width / 2;
					//} else if (me.side == "right") {
					//	//ax = sx /*- origin[0]*/ + parent.width / 2 - adjS;// - d.source.width / 2;
					//	ax = sx /*- origin[0]*/ - parent.width / 2 - adjS;// - d.source.width / 2;
					//}


					var ay = (sy - parent.height) + Math.Min(10, parent.height / 2) /*- origin[1]*/ - adjS;//d.source.height / 2;
																										   //points.Add(Tuple.Create(ax, ay));
					points.Add(Tuple.Create(lx, ay));
				}


				points.Add(Tuple.Create(lx, tyy));
				points.Add(Tuple.Create(tx, tyy));

				DrawLine(gfx, pageProps, points);
				if (DEBUG) {
					gfx.DrawString(sideL + "_" + tx + "_" + tyy, tempFont, XBrushes.Red, tx, tyy);
				}
			} else {
				var tw = me.width;
				var th = me.height;
				var points = new List<Tuple<double, double>>();

				if (me.side == "left" || me.side == "right") {

					var maxH = 30 * pageProps.scale;

					if (parent != null && (parent.side == "left" || parent.side == "right")) {
						var txParent = parent.x - origin[0] + parent.width / 2;
						var twParent = parent.width;
						var leftRightSep = twParent + hSeparation /*- adjS*/;
						if (parent.side == "right")
							leftRightSep = -1 * leftRightSep /*- adjS*/;

						var tyParent = parent.y - origin[1] - adjS;
						var thParent = parent.height;
						var eyParent = tyParent + Math.Min(thParent / 2, maxH);
						sx = txParent + leftRightSep - adjS;
						sy = eyParent;
					}

					if (me.side == "left") {
						var bx = tx + tw + hSeparation - adjS;
						var ex = tx + tw - adjS;
						var ey = ty + Math.Min(th / 2, maxH);

						points.Add(Tuple.Create(sx, sy));
						points.Add(Tuple.Create(sx, my));
						points.Add(Tuple.Create(bx, my));
						points.Add(Tuple.Create(bx, ey));
						points.Add(Tuple.Create(ex, ey));

					} else if (me.side == "right") {
						var bx = tx - (tw + hSeparation) - adjS;
						var ex = tx - (tw + adjS);
						var ey = ty + Math.Min(th / 2, maxH);

						points.Add(Tuple.Create(sx, sy));
						points.Add(Tuple.Create(sx, my));
						points.Add(Tuple.Create(bx, my));
						points.Add(Tuple.Create(bx, ey));
						points.Add(Tuple.Create(ex, ey));

						//tx = tx - (tw + hSeparation / 2) + adjS;
						//my = my + th / 2;
						//ty = my;
					}
				} else {
					//no action
					points.Add(Tuple.Create(sx, sy));
					points.Add(Tuple.Create(sx, my));
					points.Add(Tuple.Create(tx, my));
					points.Add(Tuple.Create(tx, ty));
				}



				if (DEBUG) {
					gfx.DrawString(me.side + "_" + tx + "_" + ty, tempFont, XBrushes.DarkRed, tx, ty);
				}
				DrawLine(gfx, pageProps, points);
			}

		}

		private static void ACDrawRoles(XGraphics gfx, ACNode root, PageProp pageProps, TreeSettings settings, double[] origin = null, bool anyAboveRoot = false) {
			if (root.children != null) {
				foreach (var c in root.children) {
					ACDrawRoleLine(gfx, root, c, pageProps, settings, origin);
					ACDrawRoles(gfx, c, pageProps, settings, origin);
				}
			}
			ACDrawRole(gfx, root, pageProps, origin);
			if (root.hasHiddenChildren) {
				ACDrawEllipse(gfx, root, pageProps, origin);
			}
			if (anyAboveRoot) {
				ACDrawEllipse(gfx, root, pageProps, origin, true);
			}
		}

		private static void ACDrawEllipse(XGraphics gfx, ACNode root, PageProp pageProps, double[] origin = null, bool above = false) {
			origin = origin ?? new[] { 0.0, 0.0 };
			var x = root.x + root.width / 2.0 - origin[0];
			var y = root.y - origin[1];

			var mult = 1;
			if (above == false) {
				y += root.height;
			} else {
				y -= 1;
				mult = -1;
			}

			var d = (3.0) * pageProps.scale;
			var i = (3 + 6 * 2/*ii*/) * pageProps.scale;
			var adj = pageProps.linePen.Width * .5;
			gfx.DrawLine(pageProps.linePen, x - (d / 2.0), y - (d / 2.0) + adj, x - (d / 2.0), y + mult * i - (d / 2.0) + adj);

			//for (var ii = 0; ii < 3; ii += 1) {
			//	var i = (3 + 6 * ii) * pageProps.scale;
			//	var d = (3.0) * pageProps.scale;
			//	gfx.DrawEllipse(new XPen(XColors.Black, 0.5), x - (d / 2.0), y + mult * i - (d / 2.0), d, d);
			//}
		}

		private static double[] ACRanges(ACNode root) {
			var x = root.x;
			var y = root.y;
			var me = root;
			if (me.side == "left") {
				x += me.width / 2;
			} else if (me.side == "right") {
				x -= me.width / 2;
			}

			var range = new[] { x, y, x + root.width, y + root.height };
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
		private static double fixDivision(double val) {
			var precision = 10000000;
			return Math.Round(val * precision) / precision;
		}

		private static void ACNormalize(ACNode root, double[] range, PageProp pageProp, double? forceScale = null) {
			root.x += -range[0];
			root.y += -range[1];

			//////Added
			//if (root.side == "left") {
			//	root.x += root.width / 2;
			//} else if (root.side == "right") {
			//	root.x -= root.width / 2;
			//}
			//////
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
			var numPagesWide = fixDivision(width * scale / pageProp.allowedWidth);
			var numPagesTall = fixDivision(height * scale / pageProp.allowedHeight);

			//Rescale
			//root.x += -range[0]* scale;		 //added
			//root.y += -range[1]* scale;		 //added
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
			//			return Tuple.Create((int)Math.Round(x / pageProp.allowedWidth), (int)Math.Round(y / pageProp.allowedHeight));
		}
		private static double[] ACGetOrigin(Tuple<int, int> page, PageProp pageProp) {
			return new[] {
				page.Item1 * pageProp.allowedWidth,
				page.Item2 * pageProp.allowedHeight
			};

		}

		private static double PAGE_MARGIN = 3;
		private static List<Tuple<int, int>> PagesForNode(ACNode me, PageProp pageProp) {
			var pages = new List<Tuple<int, int>>();
			pages.Add(ACGetPage(me.x + PAGE_MARGIN, me.y + PAGE_MARGIN, pageProp));
			pages.Add(ACGetPage(me.x + me.width - PAGE_MARGIN, me.y + PAGE_MARGIN, pageProp));
			pages.Add(ACGetPage(me.x + me.width - PAGE_MARGIN, me.y + me.height - PAGE_MARGIN, pageProp));
			pages.Add(ACGetPage(me.x + PAGE_MARGIN, me.y + me.height - PAGE_MARGIN, pageProp));
			return pages;
		}


		private static void ACGeneratePages(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, ACNode root, PageProp pageProp) {
			//var a = pageLookup[ACGetPage(root.x + PAGE_MARGIN, root.y + PAGE_MARGIN, pageProp)];
			//var b = pageLookup[ACGetPage(root.x + root.width - PAGE_MARGIN, root.y + PAGE_MARGIN, pageProp)];
			//var c = pageLookup[ACGetPage(root.x + root.width - PAGE_MARGIN, root.y + root.height - PAGE_MARGIN, pageProp)];
			//var d = pageLookup[ACGetPage(root.x + PAGE_MARGIN, root.y + root.height - PAGE_MARGIN, pageProp)];
			var pages = PagesForNode(root, pageProp);
			foreach (var p in pages) {
				var a = pageLookup[p];
			}
			if (root.children != null) {
				foreach (var p in root.children) {
					ACGeneratePages(pageLookup, p, pageProp);
				}
			}
		}
		private static void ACDrawOnAllPages(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, DefaultDictionary<PdfPage, XGraphics> gfxLookup, ACNode parent, ACNode me, PageProp pageProp, TreeSettings settings, bool anyAboveRoot = false) {
			var pages = new List<Tuple<int, int>>();

			pages.AddRange(PagesForNode(me, pageProp));
			//pages.Add(ACGetPage(me.x + PAGE_MARGIN, me.y+ PAGE_MARGIN, pageProp));
			//pages.Add(ACGetPage(me.x + me.width, me.y, pageProp));
			//pages.Add(ACGetPage(me.x + me.width, me.y + me.height, pageProp));
			//pages.Add(ACGetPage(me.x, me.y + me.height, pageProp));
			if (parent != null) {
				pages.AddRange(PagesForNode(parent, pageProp));
				//pages.Add(ACGetPage(parent.x, parent.y, pageProp));
				//pages.Add(ACGetPage(parent.x + parent.width, parent.y, pageProp));
				//pages.Add(ACGetPage(parent.x + parent.width, parent.y + parent.height, pageProp));
				//pages.Add(ACGetPage(parent.x, parent.y + parent.height, pageProp));
			}
			pages = pages.Distinct().ToList();

			foreach (var p in pages) {
				var origin = ACGetOrigin(p, pageProp);
				var page = pageLookup[p];
				var gfx = gfxLookup[page];
				ACDrawRole(gfx, me, pageProp, origin);
				if (parent != null)
					ACDrawRoleLine(gfx, parent, me, pageProp, settings, origin);

				if (me.hasHiddenChildren) {
					ACDrawEllipse(gfx, me, pageProp, origin);
				}

				if (anyAboveRoot) {
					ACDrawEllipse(gfx, me, pageProp, origin, true);
				}

			}
		}

		private static void ACDrawOnAllPages_Dive(DefaultDictionary<Tuple<int, int>, PdfPage> pageLookup, DefaultDictionary<PdfPage, XGraphics> gfxLookup, ACNode me, PageProp pageProp, TreeSettings settings, ACNode parent = null, bool anyAboveRoot = false) {
			ACDrawOnAllPages(pageLookup, gfxLookup, parent, me, pageProp, settings, anyAboveRoot);
			if (me.children != null) {
				foreach (var c in me.children) {
					ACDrawOnAllPages_Dive(pageLookup, gfxLookup, c, pageProp, settings, me);
				}
			}
		}

		private static void RemoveBlankPages(PdfDocument doc) {
			int _emptyNum = 4;
			int _cnt = doc.PageCount;
			for (int i = 0; i < _cnt; i++) {
				if (doc.Pages[i].Elements.Count == _emptyNum) {
					doc.Pages.RemoveAt(i);
					_cnt--;
				}
			}
		}

		#endregion

		private static void ACGenerate_Resized(PdfPage page, ACNode root, PageProp pageProp, TreeSettings settings, bool anyAboveRoot = false) {
			var ranges = ACRanges(root);
			ACNormalize(root, ranges, pageProp, null);
			XGraphics gfx = XGraphics.FromPdfPage(page);
			ACDrawRoles(gfx, root, pageProp, settings, anyAboveRoot: anyAboveRoot);
		}

		private static void ACGenerate_Full(PdfDocument doc, ACNode root, PageProp pageProp, TreeSettings settings, double scale, bool anyAboveRoot = false) {
			var ranges = ACRanges(root);
			ACNormalize(root, ranges, pageProp, scale);

			var gfxLookup = new DefaultDictionary<PdfPage, XGraphics>(x => XGraphics.FromPdfPage(x));
			var pageLookup = new DefaultDictionary<Tuple<int, int>, PdfPage>(x => {
				var page = new PdfPage(doc) {
					Width = pageProp.pageWidth,
					Height = pageProp.pageHeight
				};

				if (DEBUG) {
					XGraphics g = gfxLookup[page];
					g.DrawString("(" + x.Item1 + "," + x.Item2 + ")", new XFont(FONT, 10), XBrushes.Green, 10, 10);
				}
				return page;
			});

			ACGeneratePages(pageLookup, root, pageProp);
			ACDrawOnAllPages_Dive(pageLookup, gfxLookup, root, pageProp, settings, anyAboveRoot: anyAboveRoot);

			pageLookup.Keys.OrderBy(x => x.Item1)
				.ThenBy(x => x.Item2)
				.ToList()
				.ForEach(x => {
					doc.AddPage(pageLookup[x]);
				});
			RemoveBlankPages(doc);
		}
	}
}
#pragma warning restore CS0162 // Unreachable code detected