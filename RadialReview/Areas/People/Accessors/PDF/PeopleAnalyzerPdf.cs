using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using RadialReview.Areas.People.Angular;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Models.Interfaces;
using RadialReview.Reflection;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Accessors.PDF {
	public class PeopleAnalyzerPdf {


		private class CellLocation {
			public Unit X;
			public Unit Y;
			public Unit Width;
			public string Text;
			public Unit LineX;
			public Unit LineY;
			public Unit LineWidth;
		}

		private static void AddVerticalText(DocumentRenderer docRenderer, PdfDocumentRenderer renderer, List<Cell> taggedCells, XFont font, double angleDeg) {
			using (XGraphics gfx = XGraphics.FromPdfPage(renderer.PdfDocument.Pages[0])) {
				CellLocation cellLocation;
				XGraphicsState state;
				foreach (var cell in taggedCells) {

					cellLocation = (CellLocation)cell.Tag;

					var pen = new XPen(XColors.LightGray, .5);
					var p1 = new XPoint(cellLocation.LineX, cellLocation.LineY);
					var p2 = new XPoint(
							cellLocation.LineX + Math.Cos(Math.PI / 180 * angleDeg) * cellLocation.LineWidth,
							cellLocation.LineY - Math.Sin(Math.PI / 180 * angleDeg) * cellLocation.LineWidth
						);

					var p3 = new XPoint(cellLocation.LineX + cellLocation.Width, cellLocation.LineY);
					var p4 = new XPoint(
							cellLocation.LineX + cellLocation.Width + Math.Cos(Math.PI / 180 * angleDeg) * cellLocation.LineWidth,
							cellLocation.LineY - Math.Sin(Math.PI / 180 * angleDeg) * cellLocation.LineWidth
						);

					gfx.DrawLine(pen, p1, p2);
					gfx.DrawLine(pen, p3, p4);
					gfx.DrawLine(pen, p2, p4);

					XRect position = new XRect(cellLocation.X, cellLocation.Y, cellLocation.Width, 0);// cellLocation.Width);
					state = gfx.Save();
					gfx.RotateAtTransform(-angleDeg, new XPoint(cellLocation.X, cellLocation.Y));
					gfx.DrawString(cellLocation.Text ?? "", font, XBrushes.Black, position);

					gfx.Restore(state);
				}
			}
		}

		private static List<Cell> GetTaggedCells(DocumentRenderer docRenderer) {
			List<Cell> taggedCells = new List<Cell>();
			DocumentObject[] docObjects = docRenderer.GetDocumentObjectsFromPage(1);
			if (docObjects != null && docObjects.Length > 0) {
				for (int i = 0; i < docObjects.Length; i++) {
					if (docObjects[i].GetType() == typeof(Table)) {
						Table tbl = (Table)docObjects[i];
						for (int j = 0; j < tbl.Rows.Count; j++) {
							for (int k = 0; k < tbl.Columns.Count; k++) {
								Cell c = tbl[j, k];
								if (c.Tag != null) {
									taggedCells.Add(c);
								}
							}
						}
					}
				}
			}
			return taggedCells;
		}

		private static ForModel GetIt = new ForModel() { ModelId = -1, ModelType = "get", _PrettyString = "Get it" };
		private static ForModel WantIt = new ForModel() { ModelId = -1, ModelType = "want", _PrettyString = "Want it" };
		private static ForModel Cap = new ForModel() { ModelId = -1, ModelType = "cap", _PrettyString = "Capacity to do it" };
		private static Unit FONT_SIZE = 12;

		private static AngularPeopleAnalyzerResponse Lookup(AngularPeopleAnalyzer pa, AngularPeopleAnalyzerRow row, IForModel question, DateTime? maxDate = null) {
			maxDate = maxDate ?? DateTime.MaxValue;

			var avail = pa.Responses.Where(x => {
				return x.Source.ModelId == question.ModelId &&
						x.Source.ModelType == question.ModelType &&
						x.About.ModelId == row.About.ModelId &&
						x.About.ModelType == row.About.ModelType &&
						x.IssueDate <= maxDate.Value;
			});

			if (avail.Any()) {
				var ordered = avail.GroupBy(x => {
					return x.IssueDate.Value;
				}).OrderByDescending(x => {
					return x.Key;//.IssueDate.getTime();
				}).First();
				var selected = ordered.OrderByDescending(x => {
					return x.Override;
				}).First();

				return selected;
			}
			return null;


		}


		public static PdfDocumentRenderer AppendPeopleAnalyzer(Document doc, string title, AngularPeopleAnalyzer pa, DateTime? beforeDate = null) {
			beforeDate = beforeDate ?? DateTime.MaxValue;

			var resultColumns = 3 + pa.Values.Distinct(x => x.Source.PrettyString).Count();

			//Setup doc
			var section = doc.AddSection();
			section.PageSetup.LeftMargin = Unit.FromInch(.5);
			section.PageSetup.RightMargin = Unit.FromInch(1);
			section.PageSetup.TopMargin = Unit.FromInch(.5);
			section.PageSetup.BottomMargin = Unit.FromInch(.5);

			var pageWidth = doc.DefaultPageSetup.PageWidth;
			if (resultColumns > 8) {
				/*landscape*/
				section.PageSetup.Orientation = Orientation.Landscape;
				pageWidth = doc.DefaultPageSetup.PageHeight;

			}
			//Calc widths/heights
			var topMargin = section.PageSetup.TopMargin;
			var leftMargin = section.PageSetup.LeftMargin;
			var rightMargin = section.PageSetup.RightMargin;
			var usableWidth = pageWidth - leftMargin - rightMargin;
			var headerHeight = Unit.FromInch(1.5);
			var titleWidth = Unit.FromInch(2);
			var cellWidth = (usableWidth - titleWidth) / (resultColumns + 1);
			var rowHeight = Unit.FromInch(.34);

			var maxCellWidth = Unit.FromInch(.57);
			var addlPadding = 0.0;
			if (cellWidth > maxCellWidth) {
				addlPadding = (1 + resultColumns) * (cellWidth - maxCellWidth) / 2.0;
				cellWidth = maxCellWidth;
			}

			//Specify diagonal info
			var angle = 45.0; //degrees
			var fs = FONT_SIZE;

			var spacer = section.AddParagraph();
			spacer.Format.Font.Size = .1;
			spacer.Format.SpaceBefore = headerHeight;

			//var containerTable = section.AddTable();
			//containerTable.AddColumn(addlPadding);
			//containerTable.AddColumn(usableWidth);
			//var containerCell =  containerTable.AddRow().Cells[1];
			//containerCell.Format.LeftIndent = addlPadding;

			//var table = containerCell.Elements.AddTable();
			var table = section.AddTable();
			table.Rows.LeftIndent = addlPadding;
			table.Borders.Color = Colors.LightGray;

			//Columns
			table.AddColumn(titleWidth);
			for (var i = 0; i < resultColumns; i++) {
				//One column for each result
				table.AddColumn(cellWidth);
			}
			//Empty placeholder
			//table.AddColumn(cellWidth);

			var questions = pa.Values.Distinct(x => x.Source.PrettyString).Select(x => (IForModel)x.Source).ToList();
			questions.Add(GetIt);
			questions.Add(WantIt);
			questions.Add(Cap);


			//var r = table.AddRow();
			//r.Height = .1;
			//r.Borders.Top.Visible = false;
			//r.Borders.Left.Visible = false;
			//r.Borders.Right.Visible = false;
			var first = true;
			foreach (var row in pa.Rows) {
				var r = table.AddRow();
				r.Height = rowHeight;
				r.HeightRule = RowHeightRule.AtLeast;
				var headPara = r.Cells[0].AddParagraph(row.About.PrettyString);
				headPara.Format.Alignment = ParagraphAlignment.Center;
				r.Cells[0].VerticalAlignment = VerticalAlignment.Center;

				for (var i = 0; i < questions.Count; i++) {
					//Contents
					var value = "";
					var lu = Lookup(pa, row, questions[i], beforeDate);
					if (lu != null)
						value = (lu.AnswerFormatted ?? "").Replace("-", "–");
					var p = r.Cells[1 + i].AddParagraph(value);
					p.Format.Alignment = ParagraphAlignment.Center;
					r.Cells[1 + i].VerticalAlignment = VerticalAlignment.Center;

					formatAnswer(lu, p);


					if (first) {
						//Heading
						var lines = 1;
						var textHeight = fs * lines;

						var horizOffset = Math.Cos(Math.PI / 180 * (90 - angle)) * textHeight;

						r.Cells[i + 1].Tag = new CellLocation() {
							Text = questions[i].ToPrettyString() ?? "",
							Width = cellWidth,
							X = leftMargin + addlPadding + titleWidth + cellWidth * (i + .5) + horizOffset * .5,
							Y = headerHeight + topMargin,
							LineX = leftMargin + addlPadding + titleWidth + cellWidth * i,
							LineY = headerHeight + topMargin,
							LineWidth = headerHeight / Math.Sin(Math.PI / 180 * angle)

						};



					}
				}
				first = false;
			}

			DocumentRenderer docRenderer = new DocumentRenderer(doc);
			docRenderer.PrepareDocument();
			var taggedCells = GetTaggedCells(docRenderer);

			PdfDocumentRenderer renderer = new PdfDocumentRenderer();

			renderer.Document = doc;
			renderer.RenderDocument();

			XFont font = new XFont("Arial", fs, XFontStyle.Regular);
			AddVerticalText(docRenderer, renderer, taggedCells, font, angle);

			var container = pa.NotNull(y => y.SurveyContainers.Where(x => x.IssueDate <= beforeDate).OrderBy(x => x.IssueDate).LastOrDefault());
			if (container != null) {
				font = new XFont("Arial", fs+2, XFontStyle.Bold);
				AddTitleText(docRenderer, renderer, beforeDate.Value, font, new XPoint(leftMargin + FONT_SIZE, topMargin + FONT_SIZE));
			}

			return renderer;
		}

		private static void AddTitleText(DocumentRenderer docRenderer, PdfDocumentRenderer renderer, DateTime date, XFont font, XPoint position) {
			using (XGraphics gfx = XGraphics.FromPdfPage(renderer.PdfDocument.Pages[0])) {
				gfx.DrawString("People Analyzer"/*container.Name ?? ""*/, font, XBrushes.Black, position);

				var datePos = new XPoint(position.X,position.Y+font.Size);
				var dateFont = new XFont(font.FontFamily.Name, font.Size - 2, XFontStyle.Regular, font.PdfOptions);

				var useDate = "as of "+date.ToLongDateString();
				if (date > DateTime.UtcNow)
					useDate = "as of Today";

				gfx.DrawString(useDate, dateFont, XBrushes.Gray,datePos);

			}

		}

		private static void formatAnswer(AngularPeopleAnalyzerResponse response, Paragraph p) {

			var red = Color.FromRgb(217, 83, 79);
			var yellow = Color.FromRgb(240, 173, 78);
			var green = Color.FromRgb(92, 184, 92);

			var dictColor = new DefaultDictionary<string, Color?>(x => null);
			dictColor.Add("often", green);
			dictColor.Add("not-often", red);
			dictColor.Add("sometimes", yellow);
			dictColor.Add("yes", green);
			dictColor.Add("no", red);

			var dictSize = new DefaultDictionary<string, float?>(x => null);
			dictSize.Add("often", FONT_SIZE + 1);
			dictSize.Add("not-often", FONT_SIZE + 1);
			dictSize.Add("sometimes", FONT_SIZE + 1);
			//dictSize.Add("yes", green);
			//dictSize.Add("no", red);

			//p.Format.Font.Bold = true;

			var answer = response.NotNull(x => x.Answer);
			if (answer != null) {
				var color = dictColor[answer];
				if (color != null) {
					p.Format.Font.Color = color.Value;
				}

				var size = dictSize[answer];
				if (size != null) {
					p.Format.Font.Size += size.Value;
				}
			}

		}
	}

	#region diagonal table

	public class DiagonalTable {

		/*
		public Table transform(dynamic item, Unit maxTextWidth) {
			////var parent = $(item).parent();
			//item.splitLines({ width: maxTextWidth, tag: "<span class='diagonal-item-row' style='" + rotateStyle + "'>" });
			//item.append($("<div class='diagonal-width-marker'></div>"));
			//item.append($("<div class='diagonal-bar-top diagonal-bar' style='" + rotateStyle + "'></div>"));
			//item.append($("<div class='diagonal-bar-cap'></div>"));
			//item.prepend($("<div class='diagonal-fill' style='" + skewStyle + "'></div>"));
			//$(item).wrapInner("<div class='diagonal-item-container'></div>");

			var table = new Table();
			table.AddColumn();
			table.AddRow();

			return table;
		}
		
		public DiagonalTable(Cell[][] cells, double angleRad, Unit maxHeight, Unit extraPad) {
			var angle = angleRad;
			var rotateStyle = "-ms-transform: rotate(" + angle + "rad);-webkit-transform: rotate(" + angle + "rad);transform: rotate(" + angle + "rad);";
			var skewStyle = "-ms-transform: skewX(" + angle + "rad);-webkit-transform: skewX(" + angle + "rad);transform: skewX(" + angle + "rad);transform-origin: 0% 100%;-webkit-transform-origin: 0% 100%;-ms-transform-origin: 0% 100%;";
			//$(tableSelector).addClass("diagonal-table");
			//var head = $(tableSelector).find("thead");
			//if (head.length == 0) {
			//	console.error("Missing thead");
			//}
			//head.find("td,th").css("white-space", "nowrap");

			if (cells.Length == 0) {
				//No cells, no heading.
				return;
			}
			var head = cells[0];

			//var beta = Math.PI / 2.0 - angle;
			//console.log("angle", angle);
			var alpha = -angle;
			var beta = Math.PI / 2.0 - alpha;

			////alpha = alpha - Math.floor(alpha / (Math.PI / 2.0)) * (Math.PI / 2.0);
			//console.log("alpha:", alpha);
			//console.log("beta:", beta);

			var H = maxHeight / Math.Cos(beta);


			var laters = new List<Action>();
			var maxA = 0.0;
			var maxB = 0.0;

			//$(tableSelector).find("thead th,thead td").each(function() {
			//	var header = $(this);
			foreach(var header in head) {
				var lineWidth = header.width();
				var lineHeight = header.height();
				var B = lineHeight * Math.Tan(beta);
				maxB = Math.Max(maxB, B);
				var maxWidth = H - B;
				var A = lineHeight / Math.Cos(beta);
				maxA = Math.Max(maxA, A);
				laters.Add(()=>{
					transform(header, maxWidth, A);
				});
			}
			
			foreach(var later in laters) {
				later();
			}

			var calcMaxW = 0;
			$(tableSelector).find(".diagonal-item-row").each(function() {
				calcMaxW = Math.max(calcMaxW, $(this).width());
			})

			var calcHyp = maxB + calcMaxW + extraPad;
			var calcHeight = Math.Sin(alpha) * calcHyp;
			$(tableSelector).find(".diagonal-width-marker").css("height", Math.Abs(calcHeight));
			$(tableSelector).find(".diagonal-fill").css("height", Math.abs(calcHeight) - 1);
			var containers = $(tableSelector).find(".diagonal-item-container");
			containers.each(function(i) {
				var n = $(this).find(".diagonal-item-row").length;
				$(this).find(".diagonal-width-marker").css("width", maxA * n);
				var cellW = $(this).width();
				var additionalLeft = 0
					if (cellW > maxA * n) {
					additionalLeft = (cellW - maxA * n) / 2;
				}

				if (i == containers.length - 1) {
					var lastBar = $("<div class='diagonal-bar-bottom diagonal-bar' style='" + rotateStyle + "'></div>");
					lastBar.css("left", cellW + 1);
					$(this).append(lastBar);
				}
				$(this).find(".diagonal-bar").each(function() {
					$(this).css("width", calcHyp);
				});
				$(this).find(".diagonal-bar-cap").each(function() {
					var x = calcHeight / Math.tan(beta);
					$(this).css("left", x);
				});


				$(this).find(".diagonal-item-row").each(function(i) {
					//debugger;
					$(this).css("left", maxA * (i + 1) + additionalLeft);
					//$(this).css("margin-left", -maxB);
					//$(this).css("padding-left", maxB);
					//$(this).css("width", calcHyp);
					//var D = maxB * Math.sin(alpha);
					//$(this).css("bottom", -D);


					if ($(this).text() == "") {
						$(this).html("&nbsp;");
					}
				});
			});


			if (calcHeight > maxHeight) {
				DiagonalTable(tableSelector, angleRad, calcHeight)
			}

			splitLines = function(jq, maxLineWidth) {
				if (jq < maxLineWidth) {
					return jq;
				} else {
					var parts = jq.Split(' ');

				}
			};


		}
		*/
	}



	//	public class DiagonalTable {

	//		/*
	// * 
	//	Usage: DiagonalTable("table", -Math.PI / 4, 100);
	// * 
	// */
	//	/**
	//* Splits new lines of text into separate divs
	//*
	//* ### Options:
	//* - `width` string The width of the box. By default, it tries to use the
	//*	 element's width. If you don't define a width, there's no way to split it
	//*	 by lines!
	//*	- `tag` string The tag to wrap the lines in
	//*	- `keepHtml` boolean Whether or not to try and preserve the html within
	//*	 the element. Default is true
	//*
	//*	@@param options object The options object
	//*	@@license MIT License (http://www.opensource.org/licenses/mit-license.php)
	//*/
	//			/**
	//			 * Creates a temporary clone
	//			 *
	//			 * @@param element element The element to clone
	//			 */
	//			//public  _createTemp(element) {
	//			//	return element.clone().css({ position: 'absolute' });
	//			//};

	//			/**
	//			 * Splits contents into words, keeping their original Html tag. Note that this
	//			 * tags *each* word with the tag it was found in, so when the wrapping begins
	//			 * the tags stay intact. This may have an effect on your styles (say, if you have
	//			 * margin, each word will inherit those styles).
	//			 *
	//			 * @@param node contents The contents
	//			 */
	//			public List<string> _splitHtmlWords(string contents) {
	//				var words = new List<string>();
	//				var splitContent;
	//				for (var c = 0; c < contents.Length; c++) {
	//					if (contents[c].nodeType == 3) {
	//						splitContent = _splitWords(contents[c].textContent || contents[c].toString());
	//					} else {
	//						var tag = $(contents[c]).clone();
	//						splitContent = _splitHtmlWords(tag.contents());
	//						for (var t = 0; t < splitContent.length; t++) {
	//							tag.empty();
	//							splitContent[t] = tag.html(splitContent[t]).wrap('<p></p>').parent().html();
	//						}
	//					}
	//					for (var w = 0; w < splitContent.length; w++) {
	//						words.push(splitContent[w]);
	//					}
	//				}
	//				return words;
	//			};

	//			/**
	//			 * Splits words by spaces
	//			 *
	//			 * @@param string text The text to split
	//			 */
	//			public List<String> _splitWords(string text) {
	//			return text.Split(' ').ToList();
	//			}

	//			/**
	//			 * Formats html with tags and wrappers.
	//			 *
	//			 * @@param tag
	//			 * @@param html content wrapped by the tag
	//			 */
	//			//public void _markupContent(tag, html) {
	//			//	// wrap in a temp div so .html() gives us the tags we specify
	//			//	tag = '<div>' + tag;
	//			//	// find the deepest child, add html, then find the parent
	//			//	return $(tag)
	//			//		.find('*:not(:has("*"))')
	//			//		.html(html)
	//			//		.parentsUntil()
	//			//		.slice(-1)
	//			//		.html();
	//			//}

	//		/**
	//		 * The jQuery plugin function. See the top of this file for information on the
	//		 * options
	//		 */
	//		public List<string> splitLines(dynamic @this,object options) {
	//				var settings = new {
	//					width= "auto",
	//					tag="<div>",
	//					wrap="",
	//					keepHtml=true
	//				};

	//			if (options!=null) {
	//				$.extend(settings, options);
	//			}

	//			dynamic newHtml = new object();
	//			var contents = @this.contents();
	//			var text = @this.text();
	//			@this.append(newHtml);
	//			newHtml.text("42");
	//			var maxHeight = newHtml.height() + 2;
	//			newHtml.empty();

	//			var tempLine = _createTemp(newHtml);
	//			if (settings.width !== 'auto') {
	//				tempLine.width(settings.width);
	//			}
	//			@this.append(tempLine);
	//			var words = settings.keepHtml ? _splitHtmlWords(contents) : _splitWords(text);
	//			var prev;
	//			for (var w = 0; w < words.length; w++) {
	//				var html = tempLine.html();
	//				tempLine.html(html + words[w] + ' ');
	//				if (tempLine.html() == prev) {
	//					// repeating word, it will never fit so just use it instead of failing
	//					prev = '';
	//					newHtml.append(_markupContent(settings.tag, tempLine.html()));
	//					tempLine.html('');
	//					continue;
	//				}
	//				if (tempLine.height() > maxHeight) {
	//					prev = tempLine.html();
	//					tempLine.html(html);
	//					newHtml.append(_markupContent(settings.tag, tempLine.html()));
	//					tempLine.html('');
	//					w--;
	//				}
	//			}
	//			newHtml.append(_markupContent(settings.tag, tempLine.html()));

	//			@this.html(newHtml.html());

	//		};
	//	})(jQuery);


	#endregion
}