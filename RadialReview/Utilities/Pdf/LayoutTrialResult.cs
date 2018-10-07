using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Pdf {

	[DebuggerDisplay("{Score}, {MinScale}, {SubPages.Count}")]
	public class LayoutTrialResult {

		private LayoutTrialResult() { }

		public static LayoutTrialResult Blank() {
			return new LayoutTrialResult() {
				PageLayouts = new List<MultipageDocumentPageLayout>(),
				SubPages = new List<PdfPageAndStats>(),
				MinScale = double.MaxValue,

				RectTypeOnPageMax = 1,
			};
		}

		public LayoutTrialResult(double fillArea, double totalArea, List<PdfPageAndStats> subPages, List<MultipageDocumentPageLayout> pageLayouts, List<double> scales) {
			FillArea = fillArea;
			TotalArea = totalArea;
			SubPages = subPages;
			PageLayouts = pageLayouts;
			MinScale = scales.Any() ? scales.Min() : double.MaxValue;
			ScalesSum = scales.Sum();
			ScalesCount = scales.Count();

			var rectTypes = PageLayouts.Select(x => {
				var rectTypesOnPage = x.GetRectangles().Select(y => y.Width + "_" + y.Height).Distinct().Count();
				return rectTypesOnPage; //2 is just as ok as 1, penalize for more
			});

			if (rectTypes.Any()) {
				RectTypeOnPageMax = rectTypes.Max();
				RectTypeOnPageSum = rectTypes.Sum();
				RectTypeOnPageCount = rectTypes.Count();
			}
		}

		public LayoutTrialResult Clone() {
			return new LayoutTrialResult() {
				SubPages = SubPages.Select(x => x).ToList(),
				FillArea = FillArea,
				PageLayouts = PageLayouts.Select(x => x).ToList(),
				TotalArea = TotalArea,
				MinScale = MinScale,
				RectTypeOnPageMax = RectTypeOnPageMax,
				ScalesCount = ScalesCount,
				ScalesSum = ScalesSum,
				RectTypeOnPageSum = RectTypeOnPageSum,
				RectTypeOnPageCount = RectTypeOnPageCount,
				ShouldBoost=ShouldBoost,
				

			};
		}

		public LayoutTrialResult UnionAfter(LayoutTrialResult after) {
			var clone = Clone();
			clone.FillArea = (clone.FillArea + after.FillArea);
			clone.TotalArea += after.TotalArea;
			clone.SubPages.AddRange(after.SubPages);
			clone.PageLayouts.AddRange(after.PageLayouts);
			clone.MinScale = Math.Min(after.MinScale, MinScale);
			clone.RectTypeOnPageMax = Math.Max(after.RectTypeOnPageMax, RectTypeOnPageMax);
			clone.ScalesSum = ScalesSum + after.ScalesSum;
			clone.ScalesCount = ScalesCount + after.ScalesCount;
			clone.RectTypeOnPageSum = RectTypeOnPageSum + after.RectTypeOnPageSum;
			clone.RectTypeOnPageCount = RectTypeOnPageCount + after.RectTypeOnPageCount;
			clone.ShouldBoost = ShouldBoost || after.ShouldBoost;
			return clone;
		}

		public bool ShouldBoost { get; set; }


		public double? FillPercentage { get { return TotalArea == 0 ? (double?)null : FillArea / TotalArea; } }
		public double FillArea { get; set; }
		public double TotalArea { get; set; }
		public List<PdfPageAndStats> SubPages { get; set; }
		public List<MultipageDocumentPageLayout> PageLayouts { get; set; }
		//Scales	
		public double MinScale { get; set; }
		public double ScalesSum { get; private set; }
		public double ScalesCount { get; private set; }
		public double ScaleAverage { get { return ScalesCount == 0 ? 0 : ScalesSum / ScalesCount; } }
		//Rects
		public int RectTypeOnPageMax { get; private set; }
		public int RectTypeOnPageSum { get; private set; }
		public int RectTypeOnPageCount { get; private set; }
		public double RectTypeOnPageAverage { get { return RectTypeOnPageCount == 0 ? 0.0 : RectTypeOnPageSum / (double)RectTypeOnPageCount; } }

		public class Weights {
			public double pagesWeight = -0.16; //set-point
			public double rectPerPageMaxWeight = -0.21902;
			public double fillPercentWeight = 1.0; //1.35692;
			public double minScaleWeight = 0.45;//0.481884;
			public double avgScaleWeight = 1;
			public double rectPerPageAvgWeight = -1;
		}

		public static Weights Weighting = new Weights();
		
		
		/// <summary>
		///	WEIGHTING FORMULA..
		/// 
		///		Solve for w_1, w_2:
		///				[scale_1 ^ w_1 / pages_1 ^ w_2] == [scale_2 ^ w_1 / pages_2 ^ w_2]
		///				
		///		Or minimize
		///			(-1 + [scale_1 ^ w_1 / pages_1 ^ w_2]
		///				* [scale_2 ^ w_1 / pages_2 ^ w_2]
		///				* [scale_3 ^ w_1 / pages_3 ^ w_2]
		///				... 
		///				* [scale_n ^ w_1 / pages_n ^ w_2]
		///
		///			)^2 
		///				 					
		///
		///		Ex: If you want to split the page when scaling is smaller than 0.7
		///
		///		Condition 1: 
		///			scale_1 = 0.7
		///			pages_1 = 1
		///		Condition 2:
		///			scale_2 = 1.0
		///			pages_2 = 2
		///
		///		Solve: [(.7 ^ w_1) / (1^w_2)] = [(1 ^ w_1) / (2^w_2)]
		///
		///			w2 => 0.514573 w1
		///				w1 = 1.94336
		///				w2 = 1 
		/// </summary>
		public double Score {
			get {
				var numPages = PageLayouts.Count;


				/*
					3 rectagles = 2 pages
					.75 scale = 2 pages
					.6 fill on 2 pages = 1.00 fill on one page
				 */

				//var pagesWeight = -0.16; //set-point
				//var rectPerPageMaxWeight = -0.21902;
				//var fillPercentWeight = 1.0; //1.35692;
				//var minScaleWeight = 0.45;//0.481884;
				//var avgScaleWeight = 1;
				//var rectPerPageAvgWeight = -1;

				var pages = Math.Pow(PageLayouts.Count, Weighting.pagesWeight);
				var rectTypePerPageMax = Math.Pow(RectTypeOnPageMax, Weighting.rectPerPageMaxWeight);
				var fillPercent = Math.Pow(FillPercentage ?? 0, Weighting.fillPercentWeight);
				var minScale = Math.Pow(MinScale, Weighting.minScaleWeight);
				var avgScale = Math.Pow(ScaleAverage, Weighting.avgScaleWeight);
				var rectTypePerPageAvg = Math.Pow(RectTypeOnPageAverage, Weighting.rectPerPageAvgWeight);
				var epsilon = ShouldBoost ? 1.00000001 : 1;


				return pages * rectTypePerPageMax * fillPercent * minScale * avgScale * rectTypePerPageAvg*epsilon;
				//return (FillPercentage ?? 0) * scale / Math.Max(1, numPages * maxRectTypeOnPage);
			}
		}




	}
}