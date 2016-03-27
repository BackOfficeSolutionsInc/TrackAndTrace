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

namespace TractionTools.UITests {
    
    public static class WebDriverExtensions {

        #region Screenshot
        public static string TakeScreenshot(this IWebDriver driver,string file)
        {
            var screenshotDriver = driver as ITakesScreenshot;
            var screenshot = screenshotDriver.GetScreenshot();
            screenshot.SaveAsFile(file, ImageFormat.Png);
            return file;
        }
        #endregion

        #region Form
        public static void Check(this IWebElement element){
            if (!element.Selected)
                element.Click();
        }
        public static void Uncheck(this IWebElement element){
            if (element.Selected)
                element.Click();
        }
        #endregion

        #region Jquery
        public static IWebElement Parent(this IWebElement e)
        {
            return e.FindElement(By.XPath(".."));
        }
        public static IWebElement Closest(this IWebElement e, By by) {
            var thisE = e.Parent();
            while (true) {
                if (thisE == null)
                    throw new NoSuchElementException();
                if (Matches(thisE, by))
                    return thisE;
                thisE = thisE.Parent();
            }
        }
        public static bool Matches(this IWebElement e, By by)
        {
            var found = by.FindElement(new SingleElementSearchContext(e));
            return found != null;
        }
        #endregion
     
        #region FindElement
        public static IWebElement FindElementByText(this IWebDriver driver, string text, TimeSpan? timeout=null)
        {
            return FindElement(driver,By.XPath("//*[contains(text(), '" + text + "')]"),timeout??TimeSpan.FromSeconds(10));

        }
        public static IWebElement FindElement(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            return FindElement(driver, by, TimeSpan.FromSeconds(timeoutInSeconds));
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
        public static void WaitForAlert(this IWebDriver driver)
        {
            int i = 0;
            while (i++ < 5) {
                try {
                    var alert = driver.SwitchTo().Alert();
                    alert.Accept();
                    break;
                } catch (NoAlertPresentException e) {
                    Thread.Sleep(1000);
                    continue;
                }
            }
        }
        public static void WaitForAngular(this IWebDriver driver, TimeSpan? timeout=null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(10);
            DateTime start = DateTime.UtcNow;
            while (true) {
                try {
                    var es = driver.FindElements(By.ClassName("ng-cloak"));
                    if (!es.Any())
                        return;
                    if ((DateTime.UtcNow - start) > timeout)
                        throw new TimeoutException("Angular did not complete within the timeout.");
                } catch (FormatException e) {

                }
                Thread.Sleep(50);
            }
        }
        #endregion
    }
}
