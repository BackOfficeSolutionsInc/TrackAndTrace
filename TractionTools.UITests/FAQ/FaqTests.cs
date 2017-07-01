using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.UITests.Selenium;
using System.Threading.Tasks;

namespace TractionTools.UITests.FAQ {
	[TestClass]
	public class FaqTests : BaseSelenium {


		
		[ClassInitialize]
		public static void Setup(TestContext ctx) {

			//MeetingName = "WizardMeeting";
			//Recur = await L10Utility.CreateRecurrence(MeetingName);
		}

		[TestMethod]
		[TestCategory("Visual")]
		public async Task FAQ_CreateMeeting() {

			var testId = Guid.NewGuid();
			var AUC = await GetAdminCredentials(testId);
			TestView(AUC, "/", d => {

				d.DefaultTimeout(TimeSpan.FromSeconds(15));


				var item = d.Find(".faq-tile #faq_createL10");
				item.Click();

				d.Find(".anno-content");
				d.Find("#header-tab-l10").Click();

				Assert.IsTrue(d.Find(".anno-content").Wait(400).Text.Contains("Once created, your Level 10 meetings will show up here."));

				d.Find(".anno-btn").Wait(400).Click();
				d.WaitForNotVisible(".anno-content");



				d.Find(".anno-content").WaitForText(d,"Click this button to create a new Level 10 meeting",1000.0);
				d.Find("#l10-create-meeting").Click();
				d.Wait(1000);
				d.Find("#l10-create-new-meeting").Click();

				var text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("This is the Level 10 wizard!"));
				Assert.IsTrue(text.Contains("Use this screen to build your L10 meeting."));
				Assert.IsTrue(text.Contains("Note:You only need to create one meeting per team."));
				d.Find(".anno-btn").Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("Let's give it a name!"));
				Assert.IsTrue(text.Contains("Set your meetings name here."));
				Assert.IsTrue(text.Contains("Ex: Leadership Team, Sales Team, Ops Team..."));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("What kind of team is this?"));
				Assert.IsTrue(text.Contains("Select the team type from this drop-down."));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("Where's the save button?"));
				Assert.IsTrue(text.Contains("There isn't one. Any changes you make to your meeting are automatically saved."));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("Let's add some attendees."));
				Assert.IsTrue(text.Contains("Click Attendees."));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("Who's in your meeting?"));
				Assert.IsTrue(text.Contains("Your attendees will show up here."));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("Click this button to add users."));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();


				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("Three ways to add users"));
				Assert.IsTrue(text.Contains("You can use the search function to add existing users."));
				Assert.IsTrue(text.Contains("You can create a new user and add to the meeting."));
				Assert.IsTrue(text.Contains("You can upload a list of users."));
				Assert.IsTrue(text.Contains(""));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("Try it out for yourself!"));
				Assert.IsTrue(text.Contains("Use the menu to edit your attendees, scorecard measurables, rocks, to-dos and issues."));
				Assert.IsTrue(text.Contains(""));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();

				d.Wait(500);
				text = d.Find(".anno-content").Wait(400).Text;
				Assert.IsTrue(text.Contains("When you're done, you can start the meeting from the L10 tab."));
				d.Find(".anno-btn:not(.anno-btn-low-importance)").Wait(400).Click();
				
			});

		}
	}
}
