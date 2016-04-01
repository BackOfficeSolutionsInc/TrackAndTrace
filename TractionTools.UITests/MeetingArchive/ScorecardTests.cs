using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.TestUtils;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading.Tasks;
using RadialReview.Controllers;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Scorecard;
using System.Threading;
using TractionTools.UITests.Utilities;

namespace TractionTools.UITests.Selenium {
    [TestClass]
    public class MeetingArchiveTests : BaseSelenium {

        private const string MEETING_NAME = "SuperMeeting";

     

        [TestMethod]
        public async Task TestAdmin()
        {
            var testId1 = Guid.NewGuid();
            var auc1 = await GetAdminCredentials(testId1);

            var testId2 = Guid.NewGuid();
            var auc2 = await GetAdminCredentials(testId2);
        }
       

        [TestMethod]
        public async Task TestScorecard()
        {
            var testId = Guid.NewGuid();
            //Ensure correct week
            var recur = Generator.CreateRecurrence(MEETING_NAME);
            var auc = await GetAdminCredentials(testId);
            var au = auc.User;
            var m101 = new MeasurableModel{
                    AccountableUserId = au.Id,
                    AdminUserId = au.Id,
                    OrganizationId=au.Organization.Id,
                    Goal = 101,
                    GoalDirection = RadialReview.Models.Enums.LessGreater.LessThan,
                    Title = "TestMeasurable101",
                    UnitType = RadialReview.Models.Enums.UnitType.Dollar,    
            };
            MockHttpContext();
            DbCommit(s=>{
                L10Accessor.AddMeasurable(s,PermissionsUtility.Create(s,au),RealTimeUtility.Create(),recur.Id,
                    RadialReview.Controllers.L10Controller.AddMeasurableVm.CreateNewMeasurable(recur.Id, m101));
            });


            TestView(auc, "/L10/Details/" + recur.Id, d => {
                //d.Navigate().GoToUrl("l10/details/" + recur.Id);
                Thread.Sleep(1000);
                d.WaitForAngular();
                d.WaitForAlert();

                var element = d.WaitForText(By.Id("meeting-name"), MEETING_NAME);
                //Assert.AreEqual(MEETING_NAME, element.Text);

                element = d.FindElementByText("TestMeasurable101");
                //element

                Assert.IsNotNull(element);

                var row = element.Closest(By.TagName("tr"));
                Assert.AreEqual(auc.User.GetFirstName() + " " + auc.User.GetLastName(), row.FindElement(By.ClassName("who")).Text);
                Assert.IsTrue(row.FindElement(By.CssSelector(".target.direction")).FindElement(By.TagName("span")).Matches(By.ClassName("direction_LessThan")));
                Assert.AreEqual("101", row.FindElement(By.CssSelector(".target.value")).Text);
                Assert.IsTrue(row.FindElement(By.CssSelector(".target.value")).FindElement(By.TagName("span")).Matches(By.ClassName("Dollar")));

                var date1 = DateTime.Parse(d.FindElement(By.XPath("//*[@id='ScorecardTable']/thead/tr[1]/th[last()]")).Text);
                Assert.AreEqual(DayOfWeek.Sunday, date1.DayOfWeek);
                var date2 = DateTime.Parse(d.FindElement(By.XPath("//*[@id='ScorecardTable']/thead/tr[2]/th[last()]")).Text);
                Assert.AreEqual(DayOfWeek.Saturday, date2.DayOfWeek);
                Assert.AreEqual(DayOfWeek.Sunday, au.Organization.Settings.WeekStart);
            });

            TestView(auc, "/L10/meeting/" + recur.Id, d => {
                d.WaitForAlert();
                //Start the meeting
                d.FindElement(By.Id("form0"),10).Submit();
                //Click Scorecard
                d.FindElement(By.PartialLinkText("Scorecard")).Click();

                var element = d.FindElementByText("TestMeasurable101");
                var row = element.Closest(By.TagName("tr"));

                Assert.AreEqual(auc.User.GetFirstName()[0] + " " + auc.User.GetLastName()[0], row.FindElement(By.ClassName("who")).Text);
                Assert.IsTrue(row.FindElement(By.CssSelector(".target.direction")).FindElement(By.TagName("span")).Matches(By.ClassName("direction_LessThan")));
                Assert.AreEqual("<",row.FindElement(By.CssSelector(".target.direction")).Text);
                Assert.AreEqual("101", row.FindElement(By.CssSelector(".target.value")).Text);
                Assert.IsTrue(row.FindElement(By.CssSelector(".target.value")).FindElement(By.TagName("span")).Matches(By.ClassName("Dollar")));

                d.FindElement(By.PartialLinkText("Conclude"), 10).Click();
                d.FindElement(By.Id("SendEmail"), 10).Uncheck();
                d.FindElement(By.Id("form0"), 10).Submit();
                

            });

            //await TestWebpage("/l10/details/" + recur.Id, d => {
            //    var element = d.FindElement(By.Id("meeting-name"));
            //    Assert.AreEqual(MEETING_NAME, element.Text);
            //});





        }

    }
}
