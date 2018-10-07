using MigraDoc.DocumentObjectModel;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using static RadialReview.Utilities.Pdf.MultipageLayoutOptimizer;

namespace RadialReview.Utilities.Pdf {
	public enum DocumentsPerPage {
		One,
		Two,
		Four
	}
	public class MultiPageDocument {
		public static Unit DEFAULT_MARGIN = Unit.FromInch(.2);
		public delegate Cell CellGenerator(PdfPage inPage, PdfPage outPage);
		/// <summary>
		/// Generate the cells that appear on the selected page
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		private delegate List<CellGenerator> PageGenerator(int page);
		
		#region subclasses
		public class Settings {
			public double MaxScale { get; set; }
			public double MinScale { get; set; }
			public Unit Margin { get; internal set; }
			public XSize OutputSize { get; set; }

			public Settings() {
				MaxScale = double.MaxValue;
				MinScale = 0;
				Margin = DEFAULT_MARGIN;
				OutputSize = new XSize(Unit.FromInch(8.5), Unit.FromInch(11));
			}
			public Settings(XSize size) : this() {
				OutputSize = size;
			}
		}

		public class Cell {
			public Cell(XRect resize, XSize cropSize, double scale) {
				Resize = resize;
				CropSize = cropSize;
				Scale = scale;
			}
			public XRect Resize { get; set; }
			public XSize CropSize { get; set; }
			public double Scale { get; set; }
		}
		#endregion

		public PageOrientation Orientation { get; set; }
		//public XSize OutputSize { get; set; }
		public Settings DocumentSettings { get; set; }
		protected PdfDocument IntermediateDocument { get; set; }
		private PageGenerator PageGen { get; set; }



		/// <summary>
		/// Reorders in place
		/// </summary>
		/// <param name="orderings"></param>
		/// <param name="doc"></param>
		private void ReorderPagesInPlace(int[] orderings, PdfDocument doc) {
			//Reordering of document pages is dumb in pdfsharp...

			var indexes = orderings.Select((x, i) => new { x, i }).OrderBy(x => x.x).Select(x => x.i).ToList();
			var newIdx = 0;
			//naive ordering...
			for (var i = 0; i < indexes.Count; i++) {
				var index = indexes[i];
				doc.Pages.MovePage(index, newIdx); //one indexed...
				//adjust the indexes...
				for (var j = i+1; j < indexes.Count; j++) {
					if (indexes[j] <= index) {
						indexes[j] += 1;
					}
				}
				newIdx += 1;
			}
		}

		public MultiPageDocument(MultipageDocumentLayout layout) : this(layout.Settings) {		
			PageGen = (i) => {
				if (i < layout.PageLayouts.Count) {
					return layout.PageLayouts[i].GetGenerators();
				} else {
					return layout.PageLayouts[layout.PageLayouts.Count - 1].GetGenerators();
				}
			};
			var dm = new DocumentMerger();			
			dm.AddDocs(layout.UnorderedDocuments.Select(x=>x.Document));
			var document = dm.Flatten("", false, false, null, null);
			ReorderPagesInPlace(layout.PageOrders.ToArray(), document);
			IntermediateDocument = document;
		}

		public MultiPageDocument( IEnumerable<PdfDocument> docs, DocumentsPerPage perPage, Settings settings = null) : this(docs, GetDefaultOrientation(perPage), null, settings) {
			PageGen = (i)=>GetDefaultCellGenerators(perPage, settings);
		}

		public MultiPageDocument( IEnumerable<Document> docs, DocumentsPerPage perPage, Settings settings = null) : this(docs, GetDefaultOrientation(perPage), null, settings) {
			PageGen = (i) => GetDefaultCellGenerators(perPage, settings);
		}

		public MultiPageDocument( IEnumerable<Document> docs, PageOrientation orientation, List<CellGenerator> pageCells, Settings settings = null) : this( orientation, pageCells, settings) {
			var dm = new DocumentMerger();
			dm.AddDocs(docs);
			IntermediateDocument = dm.Flatten("", false, false, null, null);
		}

		public MultiPageDocument( IEnumerable<PdfDocument> docs, PageOrientation orientation, List<CellGenerator> pageCells, Settings settings = null) : this( orientation, pageCells, settings) {
			var dm = new DocumentMerger();
			dm.AddDocs(docs);
			IntermediateDocument = dm.Flatten("", false, false, null, null);
		}

		private MultiPageDocument( PageOrientation orientation, List<CellGenerator> pageCells, Settings settings) : this(settings) {
			PageGen = (i) => pageCells;
			Orientation = orientation;
		}

		private MultiPageDocument(Settings settings) {
			DocumentSettings = settings ?? new Settings();
		}

		public static CellGenerator GetScaledCellGenerator(XRect pageCell, Settings settings) {
			return (inPage, outPage) => {
				return ScaleTo(inPage, pageCell, new XSize(outPage.Width, outPage.Height), settings);
			};
		}

		public static List<CellGenerator> GetDefaultCellGenerators(DocumentsPerPage dpp, Settings settings) {
			var boxes = new List<CellGenerator>();
			switch (dpp) {
				case DocumentsPerPage.One: {
						boxes.Add((inPage, outPage) => GetScaledBox(inPage, outPage, settings, 0, 0, 1, 1));
						break;
					}
				case DocumentsPerPage.Two: {
						boxes.Add((inPage, outPage) => GetScaledBox(inPage, outPage, settings, 0, 0, 2, 1));
						boxes.Add((inPage, outPage) => GetScaledBox(inPage, outPage, settings, 1, 0, 2, 1));
						break;
					}
				case DocumentsPerPage.Four: {
						boxes.Add((inPage, outPage) => GetScaledBox(inPage, outPage, settings, 0, 0, 2, 2));
						boxes.Add((inPage, outPage) => GetScaledBox(inPage, outPage, settings, 1, 0, 2, 2));
						boxes.Add((inPage, outPage) => GetScaledBox(inPage, outPage, settings, 0, 1, 2, 2));
						boxes.Add((inPage, outPage) => GetScaledBox(inPage, outPage, settings, 1, 1, 2, 2));
						break;
					}
				default:
					throw new ArgumentOutOfRangeException("Unknown:" + dpp);
			}
			return boxes;
		}

		/// <summary>
		/// Use CropBox on pdfPage to resize to box.
		/// </summary>
		/// <param name="inputPage"></param>
		/// <param name="outputPage"></param>
		/// <param name="settings"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="horizontalCells"></param>
		/// <param name="verticalCells"></param>
		/// <returns></returns>
		public static Cell GetScaledBox(PdfPage inputPage, PdfPage outputPage, Settings settings, int x, int y, int horizontalCells, int verticalCells) {
			var pageSize = new XSize(outputPage.Width, outputPage.Height);

			var cellWidth = outputPage.Width / (double)horizontalCells;
			var cellHeight = outputPage.Height / (double)verticalCells;
			var x1 = cellWidth * x;
			var y1 = cellHeight * y;
			var cellBox = new XRect(x1, y1, cellWidth, cellHeight);

			return ScaleTo(inputPage, cellBox, pageSize, settings);
		}
		
		public FlattenResult Flatten() {
			return Flatten(Orientation, PageGen, true);
		}

		public class FlattenResult {
			public PdfDocument Document { get; set; }
			public FlattenStats FlattenStats { get; set; }
		}

		public class FlattenStats {
			public double TotalArea { get; set; }
			public double FilledArea { get; set; }
			public double? FillPercentage {
				get {
					return FilledArea != 0 ? TotalArea / FilledArea : (double?)null;
				}
			}
			public List<double> Scales { get; set; }
			public double MinScale { get; set; }
		}

		public FlattenStats CalculateFlattenStats() {
			return Flatten(Orientation, PageGen, false).FlattenStats;
		}

		public static FlattenResult Flatten(Settings settings, List<PdfPage> pages, List<CellGenerator> pageCells, PageOrientation orientation, Action<XGraphics, XRect, int> draw) {
			return Flatten(settings, pages, (i) => pageCells, orientation, draw);
		}


		private FlattenResult Flatten(PageOrientation orientation, PageGenerator pageCellsOnPage, bool constructDocument) {
			var doc = IntermediateDocument;
			using (var stream = new MemoryStream()) {
				doc.Save(stream, false);
				var pages = new List<PdfPage>();
				var form = XPdfForm.FromStream(stream);
				for (var i = 0; i < form.PageCount; i++) {
					form.PageNumber = i + 1;
					pages.Add(form.Page);
				}

				Action<XGraphics, XRect, int> draw = null;
				if (constructDocument) {
					draw = (gfx, box, pageNum) => {
						form.PageNumber = pageNum + 1;//Pdf is one-indexed..
						gfx.DrawImage(form, box);
					};
				}
				return Flatten(DocumentSettings, pages, pageCellsOnPage, orientation, draw);
			}
		}

		private static FlattenResult Flatten(Settings settings, List<PdfPage> pages, PageGenerator pageCellsOnPage, PageOrientation orientation, Action<XGraphics, XRect, int> draw) {
			double totalArea = 0;
			double filledArea = 0;
			var outputPageNumber = 0;
			var pageCells = pageCellsOnPage(outputPageNumber);
			int docPerPage = pageCells.Count;
			PdfDocument outputDocument = null;
			var outPages = new List<PdfPage>();
			var inputPageNumber = 0;
			var scales = new List<double>();
			var minScale = double.MaxValue;
			for (int idx = 0; idx < pages.Count; /*inc at end of loop*/) {
				var width = settings.OutputSize.Width;
				var height = settings.OutputSize.Height;

				var outputPage = new PdfPage() {
					Width = width,
					Height = height,
					Orientation = orientation,
				};
				outPages.Add(outputPage);

				XGraphics gfx = null;
				if (draw != null) {
					outputDocument = outputDocument ?? new PdfDocument();
					outputDocument.AddPage(outputPage);
					gfx = XGraphics.FromPdfPage(outputPage);
				}

				totalArea += width * height;
				for (var i = 0; i < docPerPage; i++) {
					var currPage = idx + i;
					if (currPage < pages.Count) {
						var page = pages[currPage];
						var pageCell = pageCells[i](page, outputPage);//Which corner are we in?		
						var box = pageCell.Resize;
						draw?.Invoke(gfx, box, inputPageNumber);
						inputPageNumber += 1;
						filledArea += pageCell.CropSize.Width * pageCell.CropSize.Height;
						scales.Add(pageCell.Scale);
						minScale = Math.Min(minScale, pageCell.Scale);
					}
				}
				//Increment
				idx += docPerPage;
				outputPageNumber += 1;
				//Recalculate
				pageCells = pageCellsOnPage(outputPageNumber);
				docPerPage = pageCells.Count;
			}

			return new FlattenResult() {
				Document = outputDocument,
				FlattenStats = new FlattenStats() {
					FilledArea = filledArea,
					TotalArea = totalArea,
					Scales = scales,
					MinScale = minScale,
				}
			};
		}

		private static Cell ScaleTo(PdfPage inputPage, XRect cellBox, XSize pageSize, Settings settings) {
			var inWidth = inputPage.Width;
			var inHeight = inputPage.Height;
			var cropBox = new XRect(0, 0, inWidth, inHeight);
			if (!inputPage.CropBox.IsEmpty) {
				cropBox = inputPage.CropBox.ToXRect();
			}
			var inputSize = new XSize(inWidth, inHeight);
			return ScaleTo(cropBox, cellBox, pageSize, settings, inputSize);
		}

		/// <summary>
		/// Crop box is to be fit into the cellBox. The cellbox is a location on the page 
		///	
		///		page
		///		 ___________		crop
		///		|  _		|		 ____
		///		| |_|..cell |		|	 |\ 
		///		|			|		|    |  \
		///		|			|		|____|  _'
		///		|			|		 `-....|_| ..cell
		/// 
		/// </summary>
		/// <param name="cropBox"></param>
		/// <param name="cellBox"></param>
		/// <param name="pageSize"></param>
		/// <param name="settings"></param>
		/// <param name="inputSize"></param>
		/// <returns></returns>
		private static Cell ScaleTo(XRect cropBox, XRect cellBox, XSize pageSize, Settings settings, XSize inputSize) {

			var pageWidth = pageSize.Width;
			var pageHeight = pageSize.Height;

			var width = cropBox.Width;
			var height = cropBox.Height;

			var centerCropX = cropBox.X + width * .5;
			var centerCropY = cropBox.Y + height * .5;

			var cellWidth = cellBox.Width;
			var cellHeight = cellBox.Height;
			var x1 = cellBox.X;
			var y1 = cellBox.Y;

			var cropWidthWider = width > height;
			var cellHeightTaller = cellWidth < cellHeight;

			var cropAreEqual = width == height;

			//All calculations applied to the center of the objects to make scaling easier...
			var shiftX = (x1) + settings.Margin + (cellWidth * .5 - centerCropX); //Center + cellOffset
			var shiftY = (y1) + settings.Margin + (cellHeight * .5 - centerCropY);//center + cellOffset

			var xScale = (cellWidth - settings.Margin * 2) / width;
			var yScale = (cellHeight - settings.Margin * 2) / height;

			var scale = Math.Min(xScale, yScale);

			scale = Math.Min(scale, settings.MaxScale);

			var inWider = inputSize.Width > inputSize.Height;
			var pageWider = pageWidth > pageHeight;
			var swapWidth = inWider && !pageWider || !inWider && pageWider;

			var pageScaledWidth = inputSize.Width * scale;
			var pageScaledHeight = inputSize.Height * scale;
			return new Cell(
				new XRect(centerCropX + shiftX - pageScaledWidth * .5,
							centerCropY + shiftY - pageScaledHeight * .5,
							Math.Max(0,Math.Abs(pageScaledWidth) - settings.Margin * 2),
							Math.Max(0, Math.Abs(pageScaledHeight) - settings.Margin * 2)
				), new XSize(width, height), scale);
		}

		protected static PageOrientation GetDefaultOrientation(DocumentsPerPage perPage) {
			switch (perPage) {
				case DocumentsPerPage.One:
					return PageOrientation.Portrait;
				case DocumentsPerPage.Two:
					return PageOrientation.Landscape;
				case DocumentsPerPage.Four:
					return PageOrientation.Portrait;
				default:
					throw new ArgumentOutOfRangeException("" + perPage);
			}
		}
	}
}