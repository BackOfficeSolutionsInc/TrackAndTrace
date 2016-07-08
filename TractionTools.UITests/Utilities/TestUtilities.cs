using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using TractionTools.UITests.Selenium;

namespace TractionTools.UITests.Utilities {
    [TestClass]
    public class TestUtilities : BaseSelenium {


        [TestMethod]
        public async Task Utils_TestCreateAdmin()
        {
            var testId1 = Guid.NewGuid();
            var auc1 = await GetAdminCredentials(testId1);

            var testId2 = Guid.NewGuid();
            var auc2 = await GetAdminCredentials(testId2);

            Assert.AreNotEqual(auc1.Username, auc2.Username);
        }

    }
}
