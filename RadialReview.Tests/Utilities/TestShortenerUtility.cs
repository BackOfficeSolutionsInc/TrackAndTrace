using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities;
using System.Collections.Generic;

namespace RadialReview.Tests.Utilities
{
    [TestClass]
    public class TestShortenerUtility
    {
        [TestMethod]
        public void ShortenerUtilityTest()
        {
            var found=new List<String>();
            for(int i=0;i<10000;i+=1)
            {
                var shortened=ShortenerUtility.Shorten(i);
                var expaneded=ShortenerUtility.Expand(shortened);
                Assert.IsTrue(!found.Contains(shortened));
                found.Add(shortened);
                Assert.IsTrue(expaneded == i);
            }
        }
    }
}
