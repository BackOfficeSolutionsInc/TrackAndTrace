using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web;

namespace RadialReview.Tests
{
    [TestClass]
    public class ColorGradientTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            for(int i=-100;i<100;i++)
            {
                var str=HtmlExtensions.Color(null, i, 0, 1, 90).ToHtmlString();
                Console.WriteLine("<div style='background-color:" + str + "'>"+str+"</div>");
            }
        }
    }
}
