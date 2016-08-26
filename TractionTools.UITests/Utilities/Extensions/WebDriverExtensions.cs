using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TractionTools.UITests.Utilities.Extensions;
using OpenQA.Selenium.Chrome;
using TractionTools.UITests.Utilities;
using TractionTools.UITests.Selenium;
using System.IO;
using System.Drawing;
using OpenQA.Selenium.Interactions.Internal;
using OpenQA.Selenium.Interactions;
using Microsoft.Test.VisualVerification;
using System.Diagnostics;

namespace TractionTools.UITests {

    public static class WebDriverExtensions {

        #region Screenshot

        public static bool TestScreenshot(this TestCtx ctx, string identifier = null)
        {
            ctx.CurrentIdentifier = identifier ?? ctx.CurrentIdentifier;
            var screenshotDriver = ctx as ITakesScreenshot;
            Thread.Sleep(200);
            var screenshot = screenshotDriver.GetScreenshot();
            Thread.Sleep(200);
            using (var ms = new MemoryStream(screenshot.AsByteArray)) {
                var curScreen = new Bitmap(Image.FromStream(ms));
                return new ImageCompareUtil(ctx).Compare(curScreen);
            }
        }

        public static void EnsureDifferent(this TestCtx driver, Action<IWebDriver> action, double timeoutSeconds = 10,int waitMs=0)
        {
            var original = driver.GetBitmapScreenshot();

            action(driver);
            if (waitMs > 0) {
                Thread.Sleep(waitMs);
            }
            Bitmap newest = null;
            try {
                Timeout<Exception, bool>(timeoutSeconds, () => {
                    newest = driver.GetBitmapScreenshot();

                    if (ImageCompareUtil.Comparer(original, newest))//They are the same
                        throw new Exception("Image is not different.");
                    return true;
                });
            } catch (Exception) {

                var folder = BaseSelenium.GetScreenshotFolder();
                var file = Path.Combine(folder, driver.CurrentIdentifier+"-before.png");
                original.Save(file);
                if (newest != null) {
                    file = Path.Combine(folder, driver.CurrentIdentifier + "-after.png");
                    newest.Save(file);
                    file = Path.Combine(folder, driver.CurrentIdentifier + "-diff.png");
                    ImageCompareUtil.Comparer(original, newest, file, true);
                }

                throw;
            }
        }

        public static string TakeScreenshot(this IWebDriver driver, string file)
        {
            var screenshotDriver = driver as ITakesScreenshot;
            var screenshot = screenshotDriver.GetScreenshot();
            screenshot.SaveAsFile(file, ImageFormat.Png);
            return file;
        }

        #endregion

        #region Form
        public static void Check(this IWebElement element)
        {
            if (!element.Selected)
                element.Click();
        }
        public static void Uncheck(this IWebElement element)
        {
            if (element.Selected)
                element.Click();
        }
        #endregion

        #region Debugging
        public static void Highlight(this IWebDriver d, IWebElement e)
        {
            var jsDriver = (IJavaScriptExecutor)d;
            string highlightJavascript = @"arguments[0].style.cssText = ""border-width: 2px !important; border-style: solid !important;background-color:rgba(255,0,0,.5) !important; border-color: red !important"";  ";
            jsDriver.ExecuteScript(highlightJavascript, new object[] { e });
            Thread.Sleep(700);
            var clear = @"arguments[0].style.cssText = ""border-width: 0px; border-style: solid; background-color:transparent; border-color: red"";";
            jsDriver.ExecuteScript(clear, e);
        }
        #endregion

        #region Mouse
        public static void ClickAt(this IWebDriver d, IWebElement e,int xoff=0,int yoff=0)
        {
            new Actions(d).MoveToElement(e).MoveByOffset(xoff,yoff).Click().Perform();
        }
        public static void Click(this IWebElement e, double timeoutSeconds)
        {

            Timeout<Exception, bool>(timeoutSeconds, () => {
                e.Click();
                return true;
            });

            //int i = -1;
            //var start = DateTime.UtcNow;
            //var dur = TimeSpan.FromSeconds(timeoutSeconds);
            //while (DateTime.UtcNow - start < dur) {
            //    try {
            //        e.Click();
            //        break;
            //    } catch (ElementNotVisibleException ex) {
            //        Thread.Sleep(200);
            //        continue;
            //    }
            //}
        }

       
        #endregion

        #region Jquery

        public static OUT Timeout<E, OUT>(double timeout, Func<OUT> action) where E : Exception
        {
            //int i = -1;
            var start = DateTime.UtcNow;
            var dur = TimeSpan.FromSeconds(timeout);
            E ex = default(E);
            while (DateTime.UtcNow - start < dur) {
                try {
                    return action();
                } catch (E e) {
                    Thread.Sleep(200);
                    ex = e;
                    continue;
                }
            }
            if (ex==null)
                throw new TimeoutException("Did not complete within the timeout.");
            throw ex;
        }
        [DebuggerHidden]
        public static T WaitUntil<T>(this IWebDriver d, Func<IWebDriver, T> func)
        {
            return WaitUntil<T>(d, 10, func);
        }
        [DebuggerHidden]
        public static T WaitUntil<T>(this IWebDriver d,double timeoutSeconds, Func<IWebDriver, T> func){
            return new WebDriverWait(d, TimeSpan.FromSeconds(timeoutSeconds)).Until(func);
        }


        [Obsolete("Add the WebDriver after the selector.", true)]
        public static void Find(this IWebElement e, string selector, int timeoutSeconds)
        {
            throw new Exception("Not Implemented");
        }
        

        public static IWebElement Find(this IWebElement e, string selector, IWebDriver d, int timeoutSeconds)
        {
            if (timeoutSeconds > 0) {
                return new WebDriverWait(d, TimeSpan.FromSeconds(timeoutSeconds))
                    .Until(x => e.Find(selector));
            }
            return Find(e, selector);
        }

        [DebuggerHidden]
        public static IWebElement Find(this IWebElement e, string selector)
        {
            return e.FindElement(By.CssSelector(selector));
        }

        [DebuggerHidden]
        public static IWebElement Find(this IWebDriver d, string selector, int? timeoutSeconds = null)
        {
            return d.FindElement(By.CssSelector(selector), timeoutSeconds);
        }
        public static List<IWebElement> Finds(this ISearchContext e, string selector)
        {
            return e.FindElements(By.CssSelector(selector)).ToList();
        }

        public static bool HasClass(this IWebElement e, string clazz)
        {
            return e.Matches(By.ClassName(clazz));
        }
        public static string Data(this IWebElement e, string name)
        {
            return e.GetAttribute("data-" + name);
        }
        public static string Attr(this IWebElement e, string name)
        {
            return e.GetAttribute(name);
        }


        public static string Title(this IWebElement e)
        {
            return e.GetAttribute("title");
        }

        public static string Val(this IWebElement e)
        {
            return e.GetAttribute("value");
        }

        public static IWebElement Parent(this IWebElement e)
        {
            return e.FindElement(By.XPath(".."));
        }
        public static IWebElement Closest(this IWebElement e, By by)
        {
            var thisE = e.Parent();
            while (true) {
                if (thisE == null)
                    throw new NoSuchElementException();
                if (Matches(thisE, by))
                    return thisE;
                try {
                    thisE = thisE.Parent();
                } catch (InvalidSelectorException) {
                    thisE = null;
                }
            }
        }
        public static bool Matches(this IWebElement e, By by)
        {
            var found = by.FindElement(new SingleElementSearchContext(e));
            return found != null;
        }
        #endregion

        #region FindElement

        public static IWebElement FindElementByText(this IWebDriver driver, string text, double timeoutSeconds)
        {
            return FindElementByText(driver, text, TimeSpan.FromSeconds(timeoutSeconds));
        }

        [DebuggerHidden]
        public static IWebElement FindElementByText(this IWebDriver driver, string text, TimeSpan? timeout = null)
        {
            return FindElement(driver, By.XPath("//*[contains(text(), '" + text + "')]"), timeout ?? TimeSpan.FromSeconds(15));

        }
        [DebuggerHidden]
        public static IWebElement FindElement(this IWebDriver driver, By by, int? timeoutInSeconds)
        {
            if (timeoutInSeconds == null)
                return driver.FindElement(by);

            return FindElement(driver, by, TimeSpan.FromSeconds(timeoutInSeconds.Value));
        }
        public static IWebElement FindElement(this IWebDriver driver, By by, TimeSpan timeout)
        {
            if (timeout.TotalMilliseconds > 0) {
                return new WebDriverWait(driver, timeout)
                    .Until(ExpectedConditions.ElementExists(by));
            }
            return driver.FindElement(by);
        }
        #endregion

        #region Wait
        public static IWebElement WaitForText(this IWebElement e, IWebDriver d, string text, double timeout)
        {
            return WaitForText(e, d, text, TimeSpan.FromSeconds(timeout));
        }
        public static IWebElement WaitForText(this IWebElement e, IWebDriver d, string text, TimeSpan? timeout = null)
        {
            var _timeout = timeout ?? TimeSpan.FromSeconds(10);
            var found = new WebDriverWait(d, _timeout).Until(ExpectedConditions.TextToBePresentInElement(e, text));
            if (found)
                return e;
            throw new NoSuchElementException();
        }

        public static void WaitForNotVisible(this IWebDriver driver, string selector, TimeSpan? timeout = null)
        {
            var found = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(10)).Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector(selector)));
            if (found)
                return;
            throw new NoSuchElementException();
            //try {
            //    WaitForVisible(driver, selector, timeout);
            //} catch (NoSuchElementException e) {
            //    return;
            //} catch (WebDriverTimeoutException e) {
            //    return;
            //}
            //throw new NoSuchElementException();
        }

        public static IWebElement WaitForVisible(this IWebDriver driver, string selector, TimeSpan? timeout = null)
        {
            //var start = DateTime.UtcNow;
            //var _timeout = timeout ?? TimeSpan.FromSeconds(10);
            //var element = driver.FindElement(By.CssSelector(selector), _timeout);
            //var remain = _timeout - (DateTime.UtcNow - start);
            var found = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(10)).Until(ExpectedConditions.ElementIsVisible(By.CssSelector(selector)));
            if (found!=null)
                return found;
            throw new NoSuchElementException();

        }

        public static IWebElement WaitForText(this IWebDriver driver, By by, string text, TimeSpan? timeout = null)
        {
            var start = DateTime.UtcNow;
            var _timeout = timeout ?? TimeSpan.FromSeconds(10);
            var element = driver.FindElement(by, _timeout);
            var remain = _timeout - (DateTime.UtcNow - start);
            var found = new WebDriverWait(driver, remain).Until(ExpectedConditions.TextToBePresentInElement(element, text));
            if (found)
                return element;
            throw new NoSuchElementException();
        }
        public static void WaitForAlert(this IWebDriver driver, double seconds = 5)
        {
            //Timeout<NoAlertPresentException, bool>(seconds, () => {

            //    var alert = driver.SwitchTo().Alert();
            //    alert.Accept();
            //    return true;
            //});

            //int i = -1;
            var start = DateTime.UtcNow;
            var dur = TimeSpan.FromSeconds((double)seconds);
            while (DateTime.UtcNow - start < dur) {
                try {
                    var alert = driver.SwitchTo().Alert();
                    alert.Accept();
                    break;
                } catch (NoAlertPresentException) {
                    Thread.Sleep(200);
                    continue;
                }
            }
        }
        public static void WaitForAngular(this IWebDriver driver, TimeSpan? timeout = null)
        {
        //    Timeout<FormatException, bool>(timeout, () => {
        //        var es = driver.FindElements(By.ClassName("ng-cloak"));
        //        if (!es.Any())
        //            return true;
        //        if ((DateTime.UtcNow - start) > timeout)
        //                throw new TimeoutException("Angular did not complete within the timeout.");
        //    });

            timeout = timeout ?? TimeSpan.FromSeconds(10);
            DateTime start = DateTime.UtcNow;
            while (true) {
                try {
                    var es = driver.FindElements(By.ClassName("ng-cloak"));
                    if (!es.Any())
                        return;
                    
                } catch (FormatException) {

                }
                Thread.Sleep(50);
            }
        }
        #endregion
    }
}
