using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.UITests.Selenium;
using TractionTools.UITests.Utilities;

namespace TractionTools.UITests.MeetingArchive {
        
    [TestClass]
    public class VideoConferenceTest : BaseSelenium {
        [TestMethod]
        public async Task TestVideo(){

            var testId = Guid.NewGuid();
            var auc = await GetAdminCredentials(testId);

            var recur = Generator.CreateRecurrence("VideoRecur");

            TestView(auc, "/l10/meeting/" + recur.Id, d => {
                d.FindElement(By.Id("form0"), 10).Submit();
                d.FindElement(By.PartialLinkText("Segue"), 10).Click();
                d.FindElement(By.CssSelector(".videoconference-container .clicker"), 10).Click();

                d.FindElement(By.CssSelector(".start-video"), 10).Click();

                d.FindElement(By.CssSelector(".video-container"), 30);

            });

        }

    }
}
