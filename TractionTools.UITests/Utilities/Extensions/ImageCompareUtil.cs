using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.IO;
using TractionTools.UITests.Selenium;
using Microsoft.Test.VisualVerification;
using System.Drawing.Imaging;

namespace TractionTools.UITests.Utilities.Extensions {

    public class ImageId {
        public string TestMethod { get; set; }
        public string Url { get; set; }
        public WithBrowsers Browser { get; set; }
        public string Identifier { get; set; }

        public string GetName()
        {
            var builder = "" + Browser + "~";
            if (TestMethod == null && Url == null)
                throw new Exception("Could not generate Id");
            if (TestMethod != null) {
                builder += TestMethod;
            }
            if (Url != null) {
                var split = Url.Split('/');
                var end = split.LastOrDefault();
                long num;
                if (end != null && long.TryParse(end, out num)) {
                    split[split.Length - 1] = "{id}";
                }

                builder += "~" + string.Join("_", split);

            }

            if (!String.IsNullOrWhiteSpace(Identifier)) {
                builder += "~" + Identifier;
            }

            builder += ".png";
            return builder.Trim();
        }
    }

    public class ImageCompareUtil {

        public static object _SCFileLock = new Object();

        protected ImageId Id = null;
        protected TestCtx Info = null;

        public ImageCompareUtil(TestCtx info)
        {
            Id = new ImageId() {
                Url = info.Url,
                TestMethod = info.TestName,
                Browser = info.CurrentBrowser,
                Identifier = info.CurrentIdentifier
            };
            Info = info;
            var myName = Id.GetName();
            if (info.ExistingIds.Any(x => x.GetName() == myName))
                Info.DeferException(new Exception("ImageId (" + myName + ") is not unique"));

        }

        private static Histogram histogram;

        public static Histogram GetHistogram()
        {
            if (histogram == null) {
                histogram = Histogram.FromSnapshot(Snapshot.FromFile(Path.Combine(BaseSelenium.GetTestSolutionPath(), "_Histograms", "tolerance.png")));

                for (var i = 0; i < 256; i++)
                    histogram[i] = .0005;
                histogram[0] = .98;
                histogram[1] = .001;
                histogram[2] = .001;

            }
            return histogram;
        }


        public static bool Comparer(Bitmap baseImg, Bitmap newImg, string diffFile = null, bool forceSaveDiff = false)
        {
            var b = Snapshot.FromBitmap(baseImg);
            var n = Snapshot.FromBitmap(newImg);

            Snapshot difference = n.CompareTo(b);

            SnapshotVerifier v = new SnapshotHistogramVerifier(GetHistogram());// SnapshotColorVerifier(Color.Black, new ColorDifference());
            // 5. Evaluate the difference image

            if (v.Verify(difference) == VerificationResult.Fail) {
                if (diffFile != null) {
                    b.ToFile(diffFile.Replace(".png", "") + ".base.png", ImageFormat.Png);
                    n.ToFile(diffFile.Replace(".png", "") + ".curr.png", ImageFormat.Png);
                    difference.ToFile(diffFile.Replace(".png", "") + ".diff.png", ImageFormat.Png);
                }
                return false;
            }
            if (forceSaveDiff && diffFile != null) {
                b.ToFile(diffFile.Replace(".png", "") + ".base.png", ImageFormat.Png);
                n.ToFile(diffFile.Replace(".png", "") + ".curr.png", ImageFormat.Png);
                difference.ToFile(diffFile.Replace(".png", "") + ".diff.png", ImageFormat.Png);
            }
            return true;
        }

        public bool Compare(Bitmap screenshot)
        {
            if (Regenerate(Id, screenshot))
                return false;
            var basePath = GetBaseImagePath(Id);
            if (!File.Exists(basePath)) {
                Info.ImagesNeedingGeneration.Add(Id);
                Info.DeferException(new AssertInconclusiveException("No comparison image: " + Id.GetName()));
                return false;
            } else {
                var baseImg = new Bitmap(Image.FromFile(basePath));
                var same = Comparer(screenshot, baseImg, Path.Combine(BaseSelenium.GetScreenshotFolder("Errors"), Id.GetName()));
                if (!same) {
                    Info.ImagesDoNotMatch.Add(Id);
                    //diff.Save(Path.Combine(BaseSelenium.GetScreenshotFolder("Errors"),Id.GetName()));
                    Info.DeferException(new AssertInconclusiveException("Images do not match: " + Id.GetName()));
                }
                return same;
            }
        }

        public static string GetBaseImagePath(ImageId id)
        {
            var imageFolder = Path.Combine(BaseSelenium.GetTempFile(), "BaseScreens");
            var file = Path.Combine(imageFolder, id.GetName());
            if (!Directory.Exists(imageFolder))
                Directory.CreateDirectory(imageFolder);
            return file;
        }

        private static string GetResetFile()
        {
            var folder = Path.Combine(BaseSelenium.GetTestSolutionPath(), "_Reset");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "ScreenCaptures.txt");
            if (!File.Exists(file))
                File.AppendAllText(file, "# Add one ImageId per line. \n# The test framework will edit this file to remove the line once the ScreenCapture has been generated\n# Regenerate images at any time.\n# Use the hash to ignore a line.");
            return file;

        }
        private bool Regenerate(ImageId id, Image replaceWith)
        {
            lock (_SCFileLock) {
                var path = GetResetFile();
                var lines = File.ReadAllLines(path);
                var name = id.GetName();
                if (lines.Where(x => !x.TrimStart().StartsWith("#")).Any(x => x.Trim() == name)) {
                    replaceWith.Save(GetBaseImagePath(id));
                    File.WriteAllLines(path, lines.Where(x => x.Trim() != name));
                    return true;
                }
                return false;
            }
        }

    }
}
