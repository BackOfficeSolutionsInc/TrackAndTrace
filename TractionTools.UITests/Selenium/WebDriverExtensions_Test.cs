using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using TractionTools.Tests.TestUtils;

namespace TractionTools.UITests.Selenium {
    [TestClass]
    public class WebDriverExtensionTests :BaseTest {
        private static ChromeDriver driver;//= new ChromeDriver();
        [ClassInitialize]
        public static void Startup(TestContext c)
        {
            driver = new ChromeDriver();
            //HackerNews is noted for not changing their website... extrapolation is dangerous
            driver.Navigate().GoToUrl("https://news.ycombinator.com/");
        }
        [ClassCleanup]
        public static void Teardown(){
            driver.Close();
        }

        [TestMethod]
        public void Matches()
        {


            Assert.IsTrue(driver.FindElement(By.Id("hnmain")).Matches(By.Id("hnmain")));
            Assert.IsFalse(driver.FindElement(By.Id("hnmain")).Matches(By.Id("hnmai")));
            Assert.IsTrue(driver.FindElement(By.Id("hnmain")).Matches(By.TagName("table")));
            Assert.IsFalse(driver.FindElement(By.Id("hnmain")).Matches(By.ClassName("not-a-class")));
            Assert.IsTrue(driver.FindElement(By.Id("hnmain")).Matches(By.CssSelector("center > table")));
            Assert.IsTrue(driver.FindElement(By.Id("hnmain")).Matches(By.CssSelector("center > table#hnmain")));
            Assert.IsTrue(driver.FindElement(By.ClassName("itemlist")).Matches(By.CssSelector("table#hnmain > tbody > tr > td > table")));

        }

        [TestMethod]
        public void Parent()
        {
            Assert.IsTrue(driver.FindElement(By.ClassName("brandname")).Parent().Matches(By.ClassName("pagetop")));
            Assert.IsTrue(driver.FindElement(By.Id("hnmain")).Parent().Matches(By.TagName("center")));
        }

        [TestMethod]
        public void Closest()
        {
            Assert.IsTrue(driver.FindElement(By.ClassName("itemlist")).Closest(By.TagName("table")).Matches(By.Id("hnmain")));
            Throws<NoSuchElementException>(() => driver.FindElement(By.ClassName("itemlist")).Closest(By.TagName("span")));

            Assert.IsTrue(driver.FindElement(By.ClassName("athing")).Closest(By.TagName("table")).Matches(By.ClassName("itemlist")));
            Assert.IsFalse(driver.FindElement(By.ClassName("athing")).Closest(By.TagName("table")).Matches(By.CssSelector("center > table")));


            
        }
    }
}
