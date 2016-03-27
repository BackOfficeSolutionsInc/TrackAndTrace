//using OpenQA.Selenium;
//using OpenQA.Selenium.Support.UI;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace TractionTools.Tests {
//    public static class WebDriverExtensions {

//        public static IWebElement FindElement(this IWebDriver driver, string text, TimeSpan? timeout=null)
//        {
//            return FindElement(driver,By.XPath("//*[contains(text(), '" + text + "')]"),timeout??TimeSpan.FromSeconds(-1));

//        }

//        public static IWebElement FindElement(this IWebDriver driver, By by, int timeoutInSeconds)
//        {
//            return FindElement(driver, by, TimeSpan.FromSeconds(timeoutInSeconds));
//        }
//        public static IWebElement FindElement(this IWebDriver driver, By by, TimeSpan timeout)
//        {
//            if (timeout.TotalMilliseconds > 0) {
//                return new WebDriverWait(driver, timeout)
//                    .Until(ExpectedConditions.ElementExists(by));
//            }
//            return driver.FindElement(by);    
//        }

//        public static void WaitForAngular(this IWebDriver driver, TimeSpan? timeout=null)
//        {
//            timeout = timeout ?? TimeSpan.FromSeconds(10);
//            DateTime start = DateTime.UtcNow;
//            while (true) {
//                var es = driver.FindElements(By.ClassName("ng-cloak"));
//                if (!es.Any())
//                    return;
//                if ((DateTime.UtcNow - start) > timeout)
//                    throw new TimeoutException("Angular did not complete within the timeout.");
//            }
//        }
//    }
//}
