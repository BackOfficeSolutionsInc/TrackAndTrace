using System;
using System.Linq;
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
using TractionTools.Tests.Utilities;
using OpenQA.Selenium.Support.UI;

namespace TractionTools.UITests.Selenium {
    [TestClass]
    public class MeetingArchiveTests : BaseSelenium {

        private const string MEETING_NAME = "SuperMeeting";
        
        [TestMethod]
		[TestCategory("Visual")]
		public async Task L10_Archive_TestScorecard()
        {
            var testId = Guid.NewGuid();
            //Ensure correct week
            var recur = L10Utility.CreateRecurrence(MEETING_NAME);
            var auc = await GetAdminCredentials(testId);
            var au = auc.User;
            var m101 = new MeasurableModel {
                AccountableUserId = au.Id,
                AdminUserId = au.Id,
                OrganizationId = au.Organization.Id,
                Goal = 101,
                GoalDirection = RadialReview.Models.Enums.LessGreater.LessThan,
                Title = "TestMeasurable101",
                UnitType = RadialReview.Models.Enums.UnitType.Dollar,
            };
            MockHttpContext();
            DbCommit(s => {
                L10Accessor.AddMeasurable(s, PermissionsUtility.Create(s, au), RealTimeUtility.Create(), recur.Id,
                    RadialReview.Controllers.L10Controller.AddMeasurableVm.CreateNewMeasurable(recur.Id, m101));
            });

            foreach (var dow in new []{DayOfWeek.Sunday}){//Enum.GetValues(typeof(DayOfWeek))) {
                var pType = (DayOfWeek)dow;

                //TestView(auc, "/Manage/Advanced", d => {
                //    var select = new SelectElement(d.Find("#WeekStart", 10));
                //    select.SelectByText(dow.ToString());
                //    d.Find("form", 10).Submit();

                //    Thread.Sleep(1000);
                //    d.Navigate().GoToUrl("/L10/Details/" + recur.Id);
                ////});
                //DbCommit(s => {

                //});
                OrganizationAccessor.Edit(au, recur.Org.Id, weekStart: pType);

                TestView(auc, "/L10/Details/" + recur.Id, d => {

                    //d.Navigate().GoToUrl("l10/details/" + recur.Id);
                    Thread.Sleep(1000);
                    d.WaitForAngular();
                    d.WaitForAlert();

                    var element = d.WaitForText(By.Id("meeting-name"), MEETING_NAME);
                    //Assert.AreEqual(MEETING_NAME, element.Text);

                    element = d.FindElement(By.CssSelector(".measurable-column input"));
                    //element

                    Assert.IsNotNull(element);

                    var row = element.Closest(By.TagName("tr"));
                    Assert.AreEqual(auc.User.GetFirstName() + " " + auc.User.GetLastName(), row.Find(".who .picture-container").Title());
                    Assert.IsTrue(row.Find(".target.direction .direction").HasClass("direction_LessThan"));
                    Assert.AreEqual("$101.00", row.Find(".target.value input").Val());
                    Assert.IsTrue(row.Find(".target.value span").HasClass("modifiers-Dollar"));
                    var dateRows = d.Finds("#ScorecardTable thead tr");
                    var ths = dateRows[0].Finds("th");
                    for (var i = 5; i < ths.Count - 2; i++) {
                        var top = ths[i].Text;
                        var date1 = DateTime.Parse(top);
						if (date1 > DateTime.UtcNow.AddDays(180)) {
							date1 = date1.AddYears(-1);
						}

                        Assert.AreEqual(dow, date1.DayOfWeek, top);
                        var bottom = dateRows[1].Finds("th")[i].Text;
                        var date2 = DateTime.Parse(bottom);
						if (date2 > DateTime.UtcNow.AddDays(180)) {
							date2 = date2.AddYears(-1);
						}
						var dayBefore = (DayOfWeek)(((int)dow + 6) % 7);
                        Assert.AreEqual(dayBefore, date2.DayOfWeek, bottom);
                    }
                    Assert.AreEqual(dow, au.Organization.Settings.WeekStart);
                    d.WaitForAlert();
                });
            }
            //  }

            TestView(auc, "/L10/meeting/" + recur.Id, d => {
                d.WaitForAlert();
                //Start the meeting
                d.Find("#form0", 10).Submit();
                //Click Scorecard
                d.FindElement(By.PartialLinkText("Scorecard")).Click();

                d.Find(".scorecard", 5);

                var element = d.FindElementByText("TestMeasurable101");
                var row = element.Closest(By.TagName("tr"));

                row.Find(".who").WaitForText(d, auc.User.GetFirstName()[0] + " " + auc.User.GetLastName()[0], 1);

                //Assert.AreEqual(auc.User.GetFirstName()[0] + " " + auc.User.GetLastName()[0], row.Find(".who",d,5).Text);
                Assert.AreEqual("LessThan", row.Find(".target.direction span").Data("value"));
                Assert.AreEqual("<", row.Find(".target.direction").Text);
                Assert.AreEqual("$101", row.Find(".target.value").Text);
                Assert.AreEqual("Dollar", row.Find(".target.value span").Data("value"));

                BaseSelenium.ConcludeMeeting(d);

            });

            //await TestWebpage("/l10/details/" + recur.Id, d => {
            //    var element = d.FindElement(By.Id("meeting-name"));
            //    Assert.AreEqual(MEETING_NAME, element.Text);
            //});





        }

    }
}
