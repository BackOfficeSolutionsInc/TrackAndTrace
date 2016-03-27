using OpenQA.Selenium;
using OpenQA.Selenium.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.UITests.Utilities.Extensions {
    public class SingleElementSearchContext : ISearchContext, 
        IFindsById, IFindsByClassName, IFindsByName, IFindsByTagName,
        IFindsByCssSelector,IFindsByXPath {
        protected IWebElement Element;
        protected IWebElement Document;
        public SingleElementSearchContext(IWebElement e){
            Element = e;
            if (e!=null)
                Document= e.FindElement(By.XPath("/*"));
        }

        public IWebElement FindElement(By by){
            if (Element == null)
                return null;
            if (Element.FindElement(by).Equals(Element))
                return Element;
            return null;
        }
        public IWebElement FindElementById(string id){
            if (Element == null)
                return null;
            return Element.GetAttribute("id") == id?Element:null;
        }
        public IWebElement FindElementByClassName(string className)
        {
            if (Element == null)
                return null;
            var clss = Element.GetAttribute("class");
            if (clss !=null && clss.Split(' ').Any(x=>x==className))
                return Element;
            return null;
        }
        public IWebElement FindElementByName(string name){
            if (Element == null)
                return null;
            return Element.GetAttribute("name") == name ? Element : null;
        }
        public IWebElement FindElementByTagName(string tagName){
            if (Element == null || tagName==null)
                return null;
            return Element.TagName.Trim().ToLower() == tagName.Trim().ToLower() ? Element : null;
        }
        public IWebElement FindElementByCssSelector(string cssSelector){
            if (Element == null && Document!=null)
                return null;
            if (Document.FindElements(By.CssSelector(cssSelector)).Any(x => x.Equals(Element)))
                return Element;
            return null;
        }        
        public IWebElement FindElementByXPath(string xpath){
            if (Element == null && Document != null)
                return null;
            if (Document.FindElements(By.XPath(xpath)).Any(x => x.Equals(Element)))
                return Element;
            return null;
        }

        #region Unimplemented
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsById(string id){
            throw new NotImplementedException();
        }
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElements(By by){
            throw new NotImplementedException();
        }
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByClassName(string className){
            throw new NotImplementedException();
        }
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByName(string name){
            throw new NotImplementedException();
        }
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByTagName(string tagName){
            throw new NotImplementedException();
        }
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByCssSelector(string cssSelector){
            throw new NotImplementedException();
        }
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElementsByXPath(string xpath){
            throw new NotImplementedException();
        }
        #endregion
    }
}
