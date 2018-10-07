using MigraDoc.DocumentObjectModel;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using static RadialReview.Utilities.Pdf.MultiPageDocument;
using static RadialReview.Utilities.Pdf.MultipageDocumentPageLayout;

namespace RadialReview.Utilities.Pdf {

    [DebuggerDisplay("{DebugInfo} \t- {TrialResult.Score}")]
    public class MultipageDocumentLayout {
        public MultipageDocumentLayout(IEnumerable<PdfDocumentAndStats> unorderedDocuments, LayoutTrialResult result, Settings settings, string debugInfo) {
            UnorderedDocuments = unorderedDocuments;
            PageLayouts = result.PageLayouts;
            Pages = result.SubPages;
            Size = settings.OutputSize;
            Settings = settings;
            TrialResult = result;
            var allPagesUnOrdered = unorderedDocuments.SelectMany(x => x.GetPages()).ToList();
            PageOrders = Pages.Select(x => allPagesUnOrdered.IndexOf(x));
            DebugInfo = string.Format("{0,-15}", debugInfo);
        }

        public IEnumerable<PdfDocumentAndStats> UnorderedDocuments { get; set; }
        public List<MultipageDocumentPageLayout> PageLayouts { get; set; }
        public List<PdfPageAndStats> Pages { get; set; }
        public IEnumerable<int> PageOrders { get; private set; }

        public XSize Size { get; set; }
        public Settings Settings { get; set; }
        public LayoutTrialResult TrialResult { get; set; }

        public string DebugInfo { get; set; }
        public bool IsFallback { get; set; }

        public PdfDocument DrawDebug() {
            var doc = new PdfDocument();

            var col = new SolidBrush(System.Drawing.Color.FromArgb(128, (byte)0, (byte)255, (byte)0));
            var i = 0;
            foreach (var layout in PageLayouts) {
                var p = doc.AddPage();
                p.Width = Size.Width;
                p.Height = Size.Height;
                var gfx = XGraphics.FromPdfPage(p);
                foreach (var rect in layout.CellLayouts.Select(x => x.Rectangle).ToList()) {
                    rect.Inflate(-1, -1);
                    gfx.DrawRectangle(col, rect);
                    gfx.DrawString("" + i, new XFont("arial", 12), XBrushes.Black, rect.Center);
                    i++;
                }
            }

            return doc;
        }

        public static MultipageDocumentLayout GetFallback(IEnumerable<PdfDocumentAndStats> docs, DocumentsPerPage dpp, Settings settings) {
            var layouts = new List<MultipageDocumentPageLayout>() { new MultipageDocumentPageLayout() {
                    CellLayouts = MultiPageDocument.GetDefaultCellGenerators(dpp,settings).Select(x=>
                        new CellLayout() {
                            Generator = x,
                        }).ToList()
                    }
                };
            var trialResult = new LayoutTrialResult(1, 1, docs.SelectMany(x => x.GetPages()).ToList(), layouts, new List<double> { 1.0 });
            return new MultipageDocumentLayout(docs, trialResult, settings, "fallback") {
                IsFallback = true
            };
        }

        public MultiPageDocument ToMultiPageDocument() {
            return new MultiPageDocument(this);
        }
    }

    public class MultipageDocumentPageLayout {
        public class CellLayout {
            public CellGenerator Generator { get; set; }
            public XRect Rectangle { get; set; }
        }

        public List<CellLayout> CellLayouts { get; set; }

        public List<XRect> GetRectangles() {
            return CellLayouts.Select(x => x.Rectangle).ToList();
        }
        public List<CellGenerator> GetGenerators() {
            return CellLayouts.Select(x => x.Generator).ToList();
        }

        public static MultipageDocumentPageLayout Merge(IEnumerable<MultipageDocumentPageLayout> layouts) {
            return new MultipageDocumentPageLayout() {
                CellLayouts = layouts.SelectMany(x => x.CellLayouts).ToList(),
            };
        }

        public string Sha() {
            var sha = SHA256.Create();
            StringBuilder b = new StringBuilder();
            foreach (var r in CellLayouts.Select(x => x.Rectangle)) {
                b.AppendLine(r.X + "_" + r.Y + "_" + r.Width + "_" + r.Height);
            }

            using (var stream = b.ToString().ToStream()) {
                var arr = sha.ComputeHash(stream);
                var shaStr = new StringBuilder();
                foreach (var a in arr) {
                    shaStr.Append(String.Format("{0:X2}", a));
                }
                return shaStr.ToString();
            }
        }

        public string GetOrderingString() {
            var j = CellLayouts.Select(x => x.Rectangle).OrderByDescending(x => x.Width * x.Height).ThenByDescending(x => x.Width).ThenByDescending(x => x.Height).ThenBy(x => x.X).ThenBy(x => x.Y)
                .Select(r => string.Format("{0,-20} {1,-20} {2,-20} {3,-20}", r.X, r.Y, r.Width, r.Height)).ToList();

            var joined = string.Join("/", j);
            if (joined.Length > 4000) {
                int a = 0;
            }
            return string.Format("{0,4000}", joined);


        }

    }

    public class MultipageLayoutOptimizer {


        /// <summary>
        /// Build list of pages from document.
        /// </summary>
        private static IEnumerable<PdfPage> ExtractPages(PdfDocumentAndStats doc) {
            foreach (var p in doc.Document.Pages) {
                yield return p;
            }
        }

        //private static PdfDocument FlattenPages(List<PdfPageAndStats> pages) {
        //	var doc = new PdfDocument();
        //	foreach (var p in pages) {
        //		try {
        //			doc.AddPage(p.Page.Clone() as PdfPage);
        //		} catch (Exception e) {
        //			throw e;
        //		}
        //	}
        //	return doc;
        //}


        public class OptimizerSettings {
            public bool UseWeights = true;
            public DocumentsPerPage FallbackDocumentsPerPage = DocumentsPerPage.Four;
        }
        public static OptimizerSettings OptimizationSettings = new OptimizerSettings();

        public static MultipageDocumentLayout GetBestLayout(IEnumerable<PdfDocumentAndStats> docs, Settings settings,TimeSpan timeout, LayoutFitness score = null, bool reorderable = false) {

            try {
                if (!OptimizationSettings.UseWeights)
                    throw new LayoutTimeoutException("Using fall-back.");

                var best = GetPotentialLayouts(docs, settings, score, reorderable, timeout).ToList();

                #region debugger code
                var shouldRunDebug = false;
                if (shouldRunDebug) {
                    try {
                        var now = DateTime.UtcNow.ToJsMs();
                        var dir = "C:\\Users\\Clay\\Desktop\\temp\\PDFS\\" + now + "\\";
                        Directory.CreateDirectory(dir);

                        var i = 0;
                        var csv = new Csv();
                        foreach (var layout in best) {
                            csv.Add("" + i, "FillPercent", "" + layout.TrialResult.FillPercentage);
                            csv.Add("" + i, "MinScale", "" + layout.TrialResult.MinScale);
                            csv.Add("" + i, "numPages", "" + layout.TrialResult.PageLayouts.Count);
                            csv.Add("" + i, "MaxRectTypeOnPage", "" + layout.TrialResult.RectTypeOnPageMax);
                            csv.Add("" + i, "AvgScale", "" + layout.TrialResult.ScaleAverage);
                            csv.Add("" + i, "AvgRect", "" + layout.TrialResult.RectTypeOnPageAverage);
                            csv.Add("" + i, "Boost", "" + layout.TrialResult.ShouldBoost);
                            csv.Add("" + i, "Score", "" + layout.TrialResult.Score);
                            csv.Add("" + i, "Debug", "" + layout.DebugInfo);
                            i++;
                        }
                        using (StreamWriter file = new StreamWriter(dir + "00_data_" + now + ".csv", true)) {
                            file.Write(csv.ToCsv());
                        }

                        i = 0;
                        foreach (var layout in best.Take(50)) {
                            var scaledDoc = new MultiPageDocument(layout);
                            scaledDoc.Flatten().Document.Save(dir + i + "_(" + layout.TrialResult.Score + ") - " + layout.DebugInfo.Trim() + ".pdf");
                            //layout.DrawDebug().Save("C:\\Users\\Clay\\Desktop\\temp\\PDFS\\" + now + "-" + i + "_(" + layout.Result.Score + ").pdf");
                            i += 1;
                        }

                    } catch (Exception e) {
                        int a = 0;
                    }
                }
                #endregion

                return best.FirstOrDefault();
            } catch (LayoutTimeoutException e) {
                return MultipageDocumentLayout.GetFallback(docs, OptimizationSettings.FallbackDocumentsPerPage, settings);
            }
        }

        private class PageScore {
            public PdfPageAndStats Page { get; set; }
            public double Score { get; set; }
            public int Index { get; set; }
        }

        private delegate double SplitOnPropery(PdfPageAndStats page, int index);
        public delegate double LayoutFitness(LayoutTrialResult layout);
        /// <summary>
        /// Split the pages into groups. try to pair groups together and then remerge the document
        /// </summary>
        /// <param name="results"></param>
        /// <param name="testableLayouts"></param>
        /// <param name="splitOnProperty"></param>
        /// <param name="fitnessScore"></param>
        /// <param name="debugInfo"></param>
        /// <returns></returns>
        private static void TrySplitting(List<MultipageDocumentLayout> results, List<MultipageDocumentPageLayout> testableLayouts, SplitOnPropery splitOnProperty, LayoutFitness fitnessScore, string debugInfo, TimeoutCheck breakout) {

            var tryOnThese = results.OrderByDescending(x => fitnessScore(x.TrialResult))
                                    .Where((x, i) => i % 3 == 0)
                                    .Take(3);

            foreach (var tryOn in tryOnThese) {
                if (tryOn == null)
                    continue;

                //var results = new List<MultipageDocumentLayout>();
                try {
                    var orderedByProperty = tryOn.Pages.Select((x, i) => new { page = x, score = splitOnProperty(x, i), idx = i }).OrderByDescending(x => x.score).ToList();
                    //var scales = pages.OrderByDescending(x => x.Score).ToList();
                    if (orderedByProperty.Count > 3) {

                        for (var groups = 2; groups <= 3; groups += 1) {
                            //Split into two groups, then three groups
                            var breaks = JenksFisher.CreateJenksFisherBreaksIndexArray(orderedByProperty.Select(x => x.score).ToList(), groups);
                            breaks.Add(orderedByProperty.Count);
                            var merged = LayoutTrialResult.Blank();
                            merged.ShouldBoost = true;
                            if (breaks.Count == groups + 1) {
                                for (var s = 0; s < breaks.Count - 1; s++) {
                                    var breakIndexStart = breaks[s];
                                    var breakIndexEnd = breaks[s + 1];
                                    var gropPages = orderedByProperty.Skip(breakIndexStart).Take(breakIndexEnd - breakIndexStart).Select(x => x.page).ToList();
                                    var bestLayoutForGroup = PerformLayoutTrials(tryOn.Settings, gropPages.ToList(), testableLayouts, breakout).OrderByDescending(x => fitnessScore(x)).FirstOrDefault();
                                    //add the page group to the result.
                                    merged = merged.UnionAfter(bestLayoutForGroup);
                                }
                            } else {
                                //invalid split, skip entire thing...
                                //throw new Exception("bad split");
                                continue;
                            }
                            //Add the result as a candiate...
                            results.Add(new MultipageDocumentLayout(tryOn.UnorderedDocuments, merged, tryOn.Settings, debugInfo));
                        }
                        //Make sure the result list is always sorted by the fitness function

                    }
                } catch (Exception e) {
                    //ops
                    int a = 0;
                }
                results = results.OrderByDescending(x => fitnessScore(x.TrialResult)).ToList();
            }
        }

        private static IEnumerable<MultipageDocumentLayout> GetPotentialLayouts(IEnumerable<PdfDocumentAndStats> docs, Settings settings, LayoutFitness layoutFitness, bool reorderable,TimeSpan timeout) {
            layoutFitness = layoutFitness ?? ((x) => x.Score);

            var pages = docs.SelectMany(x => x.GetPages()).ToList();
            var useThirds = true;
            if (pages.Count > 7)
                useThirds = false;

            var breakout = new TimeoutCheck(timeout);
            var testableLayouts = GetTestableLayouts(settings, breakout, 2, useThirds);


            var layouts = PerformLayoutTrials(settings, pages.ToList(), testableLayouts, breakout)
                .OrderByDescending(x => layoutFitness(x))
                .ToList();

            var results = layouts
                .Select(o => new MultipageDocumentLayout(docs, o, settings, "Primary"))
                .ToList();

            /*if (reorderable) {
				//try ordering by area..
				TrySplitting(results, testableLayouts, (x, i) => x.Boundry.Width * x.Boundry.Height / x.Scale, layoutFitness, "ByArea");
			}*/

            if (reorderable && results.Any()) {
                //Split on largest scaling.
                var scales = results.First().ToMultiPageDocument().CalculateFlattenStats().Scales;
                TrySplitting(results, testableLayouts, (x, i) => scales[i] * x.Scale, layoutFitness, "ByScaling", breakout);
            }

            return results.OrderByDescending(x => layoutFitness(x.TrialResult)).ToList();
        }

        protected static List<LayoutTrialResult> PerformLayoutTrials(Settings settings, List<PdfPageAndStats> allSubPages, List<MultipageDocumentPageLayout> testableLayouts, TimeoutCheck breakout) {
            var orientation = PageOrientation.Landscape;
            var width = settings.OutputSize.Width;
            var height = settings.OutputSize.Height;

            var output = new List<LayoutTrialResult>();
            var pageCount = allSubPages.Count();

            if (pageCount == 0)
                return output;

            breakout.ShouldTimeout();

            foreach (var layout in testableLayouts) {
                var cellCount = layout.CellLayouts.Count;

                if (pageCount < cellCount)
                    continue;

                var subPages = allSubPages.Take(cellCount).ToList();

                //Calculate used area
                var pages = subPages.Select(x => x.Page).ToList();
                var res = Flatten(settings, pages, layout.GetGenerators(), orientation, null);
                var stats = res.FlattenStats;
                var scales = stats.Scales.Select((x, i) => x * subPages[i].Scale).ToList();

                //var minScale = subPages.Min(x => x.Scale * res.FlattenStats.MinScale);

                var fillArea = stats.FilledArea;
                var totalArea = width * height;

                var result = new LayoutTrialResult(fillArea, totalArea, subPages, new List<MultipageDocumentPageLayout> { layout }, scales);

                //Has Additional pages
                var remainingSubPages = allSubPages.Skip(cellCount).ToList();
                if (remainingSubPages.Any()) {
                    var mergeWith = PerformLayoutTrials(settings, remainingSubPages, testableLayouts, breakout);
                    output.AddRange(mergeWith.Select(x => result.UnionAfter(x)).ToList());
                } else {
                    output.Add(result);
                }

                breakout.ShouldTimeout();
            }
            return output;
        }


        /// <summary>     
        /// Very expensive. Avoid more than 4 deep
        /// </summary>
        public static List<MultipageDocumentPageLayout> GetTestableLayouts(Settings settings,TimeoutCheck breakout, int maxDepth, bool useThirds = true) {
            if (maxDepth >= 4)
                throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth to large. Make it less than 4.");

            var layouts = GetTestableLayouts(new List<CellLayout>(), new HashSet<string>(), new XRect(0, 0, settings.OutputSize.Width, settings.OutputSize.Height), settings, 0, maxDepth, useThirds, breakout);
            return layouts;
        }

        private static void AddIfUnique(List<MultipageDocumentPageLayout> output, HashSet<string> shas, MultipageDocumentPageLayout found) {
            var sha = found.Sha();
            if (!shas.Contains(sha)) {
                output.Add(found);
                shas.Add(sha);
            } else {
                int a = 0;
            }
        }

        private static List<MultipageDocumentPageLayout> GetTestableLayouts(List<CellLayout> cellLayouts, HashSet<string> shas, XRect r, Settings settings, int depth, int maxDepth, bool useThirds, TimeoutCheck breakout) {
            var output = new List<MultipageDocumentPageLayout>();
            //Just use rect as layout..
            //	 _____
            //	|     |
            //	|     |
            //	|     |
            //	|_____|
            //
            AddIfUnique(output, shas, new MultipageDocumentPageLayout() {
                CellLayouts = cellLayouts.Union(new[] { new CellLayout() {
                        Generator =  GetScaledCellGenerator(r, settings),
                        Rectangle = r
                    }}).ToList()
            });

            if (depth >= maxDepth)
                return output;

            var margin = settings.Margin;
            var halfMargin = margin * .5;

            var left = r.X + halfMargin;
            var top = r.Y + halfMargin;

            //horizontal half
            //	 _____
            //	|     |
            //	|_____|
            //	|     |
            //	|_____|
            //
            try {
                var halfHeight = r.Height / 2;
                var r1 = new XRect(left, top, r.Width - margin, halfHeight - margin);
                var r2 = new XRect(left, top + halfHeight, r.Width - margin, halfHeight - margin);
                DiveOnSplit(r1, r2, cellLayouts, shas, settings, depth, maxDepth, output, useThirds, breakout);
            } catch (Exception) {
            }
            //vertical half
            //	 _____
            //	|  |  |
            //	|  |  |
            //	|  |  |
            //	|__|__|
            //
            try {
                var halfWidth = r.Width / 2;
                var r1 = new XRect(left, top, halfWidth - margin, r.Height - margin);
                var r2 = new XRect(left + halfWidth, top, halfWidth - margin, r.Height - margin);
                DiveOnSplit(r1, r2, cellLayouts, shas, settings, depth, maxDepth, output, useThirds, breakout);
            } catch (Exception) {
            }

            //Dont do this on the last round...  
            if (maxDepth > depth + 1 && useThirds) {
                //vertical third
                //	 ____
                //	| |  |
                //	| |  |
                //	|_|__|
                //
                try {
                    var thirdWidth = r.Width / 3;
                    var r1 = new XRect(left, top, thirdWidth - margin, r.Height - margin);
                    var r2 = new XRect(left + thirdWidth, top, thirdWidth * 2 - margin, r.Height - margin);
                    DiveOnSplit(r1, r2, cellLayouts, shas, settings, depth, maxDepth, output, useThirds, breakout);

                } catch (Exception) {
                }
                //horizontal third
                //	 ____
                //	|____|
                //	|    |
                //	|____|
                //
                try {
                    var thirdHeight = r.Height / 3;
                    var r1 = new XRect(left, top, r.Width - margin, thirdHeight - margin);
                    var r2 = new XRect(left, top + thirdHeight, r.Width - margin, thirdHeight * 2 - margin);
                    DiveOnSplit(r1, r2, cellLayouts, shas, settings, depth, maxDepth, output, useThirds, breakout);
                } catch (Exception) {
                }
            }
            return output;
        }

        private static void DiveOnSplit(XRect r1, XRect r2, List<CellLayout> cellLayouts, HashSet<string> shas, Settings settings, int depth, int maxDepth, List<MultipageDocumentPageLayout> output, bool useThirds,TimeoutCheck breakout) {
            var cells = cellLayouts.ToList();
            var first = GetTestableLayouts(cells, shas, r1, settings, depth + 1, maxDepth, useThirds,breakout);
            var second = GetTestableLayouts(cells, shas, r2, settings, depth + 1, maxDepth, useThirds, breakout);
            foreach (var f in first) {
                foreach (var s in second) {
                    AddIfUnique(output, shas, Merge(new[] { f, s }));
                }
            }
        }
    }
}