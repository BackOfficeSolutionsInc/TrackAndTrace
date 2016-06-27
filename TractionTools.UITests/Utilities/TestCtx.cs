using OpenQA.Selenium;
using OpenQA.Selenium.Html5;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.UITests.Selenium;
using TractionTools.UITests.Utilities.Extensions;

namespace TractionTools.UITests.Utilities {
    public class TestCtx : IWebDriver, IDisposable, ISearchContext, IJavaScriptExecutor, IFindsById, IFindsByClassName, IFindsByLinkText, IFindsByName, IFindsByTagName, IFindsByXPath, IFindsByPartialLinkText, IFindsByCssSelector, ITakesScreenshot, IHasInputDevices, IHasCapabilities, IHasWebStorage, IHasLocationContext, IHasApplicationCache, IAllowsFileDetection, IHasSessionId {

        public void DeferException(Exception e)
        {
            BaseSelenium.PreserveStackTrace(e);
            _Deferred.Add(e);
        }

        public List<Exception> _Deferred { get; set; }
        public String Url { get; set; }
        public WithBrowsers CurrentBrowser { get; set; }
        //public List<Exception> Defer { get; set; }
        public List<ImageId> ExistingIds { get; private set; }
        public List<ImageId> ImagesNeedingGeneration { get; private set; }
        public List<ImageId> ImagesDoNotMatch { get; private set; }

        public string CurrentIdentifier { get;set; }

        public RemoteWebDriver CurrentDriver { private get; set; }
        public string TestName { get; set; }

        public TestCtx(string url,string testName, List<ImageId> imageIds, List<ImageId> needGeneration, List<ImageId> noMatch)
        {
           // Defer = new List<Exception>();
            ExistingIds = imageIds;
            Url = url;
            ImagesNeedingGeneration = needGeneration;
            ImagesDoNotMatch = noMatch;
            TestName = testName;
        }

        #region Backing Driver
        public void Close()
        {
            CurrentDriver.Close();
        }

        public string CurrentWindowHandle
        {
            get { return CurrentDriver.CurrentWindowHandle; }
        }

        public IOptions Manage()
        {
            return CurrentDriver.Manage();
        }

        public INavigation Navigate()
        {
            return CurrentDriver.Navigate();
        }

        public string PageSource
        {
            get { return CurrentDriver.PageSource; }
        }

        public void Quit()
        {
            CurrentDriver.Quit();
        }

        public ITargetLocator SwitchTo()
        {
            return CurrentDriver.SwitchTo();
        }

        public string Title
        {
            get
            {
                return CurrentDriver.Title;
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<string> WindowHandles
        {
            get
            {
                return CurrentDriver.WindowHandles;
            }
        }
        [DebuggerHidden]
        public IWebElement FindElement(By by)
        {
            return CurrentDriver.FindElement(by);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElements(By by)
        {
            return CurrentDriver.FindElements(by);
        }

        public void Dispose()
        {
            CurrentDriver.Dispose();
        }

        public object ExecuteAsyncScript(string script, params object[] args)
        {
            return CurrentDriver.ExecuteAsyncScript(script, args);
        }

        public object ExecuteScript(string script, params object[] args)
        {
            return CurrentDriver.ExecuteScript(script, args);
        }

        public IWebElement FindElementById(string id)
        {
            return CurrentDriver.FindElementById(id);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsById(string id)
        {
            return CurrentDriver.FindElementsById(id);
        }

        public IWebElement FindElementByClassName(string className)
        {
            return CurrentDriver.FindElementByClassName(className);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByClassName(string className)
        {
            return CurrentDriver.FindElementsByClassName(className);
        }

        public IWebElement FindElementByLinkText(string linkText)
        {
            return CurrentDriver.FindElementByLinkText(linkText);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByLinkText(string linkText)
        {
            return CurrentDriver.FindElementsByLinkText(linkText);
        }

        public IWebElement FindElementByName(string name)
        {
            return CurrentDriver.FindElementByName(name);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByName(string name)
        {
            return CurrentDriver.FindElementsByName(name);
        }

        public IWebElement FindElementByTagName(string tagName)
        {
            return CurrentDriver.FindElementByTagName(tagName);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByTagName(string tagName)
        {
            return CurrentDriver.FindElementsByTagName(tagName);
        }

        public IWebElement FindElementByXPath(string xpath)
        {
            return CurrentDriver.FindElementByXPath(xpath);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByXPath(string xpath)
        {
            return CurrentDriver.FindElementsByXPath(xpath);
        }

        public IWebElement FindElementByPartialLinkText(string partialLinkText)
        {
            return CurrentDriver.FindElementByPartialLinkText(partialLinkText);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByPartialLinkText(string partialLinkText)
        {
            return CurrentDriver.FindElementsByPartialLinkText(partialLinkText);
        }

        public IWebElement FindElementByCssSelector(string cssSelector)
        {
            return CurrentDriver.FindElementByCssSelector(cssSelector);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByCssSelector(string cssSelector)
        {
            return CurrentDriver.FindElementsByCssSelector(cssSelector);
        }

        public Bitmap GetBitmapScreenshot()
        { 
            using(var ms = new MemoryStream(GetScreenshot().AsByteArray)){
                return new Bitmap(Image.FromStream(ms));
            }
        }

        public Screenshot GetScreenshot()
        {
            return CurrentDriver.GetScreenshot();
        }

        public IKeyboard Keyboard
        {
            get { return CurrentDriver.Keyboard; }
        }

        public IMouse Mouse
        {
            get { return CurrentDriver.Mouse; }
        }

        public ICapabilities Capabilities
        {
            get { return CurrentDriver.Capabilities; }
        }

        public bool HasWebStorage
        {
            get { return CurrentDriver.HasWebStorage; }
        }

        public IWebStorage WebStorage
        {
            get { return CurrentDriver.WebStorage; }
        }

        public bool HasLocationContext
        {
            get { return CurrentDriver.HasLocationContext; }
        }

        public ILocationContext LocationContext
        {
            get { return CurrentDriver.LocationContext; }
        }

        public IApplicationCache ApplicationCache
        {
            get { return CurrentDriver.ApplicationCache; }
        }

        public bool HasApplicationCache
        {
            get { return CurrentDriver.HasApplicationCache; }
        }

        public IFileDetector FileDetector
        {
            get
            {
                return CurrentDriver.FileDetector;
            }
            set
            {
                CurrentDriver.FileDetector=value;
            }
        }

        public SessionId SessionId
        {
            get { return CurrentDriver.SessionId; }
        }
        #endregion


    }
}
