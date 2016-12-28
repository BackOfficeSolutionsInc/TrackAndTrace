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
			//Recur = L10Utility.CreateRecurrence(MeetingName);
		}

		[TestMethod]
		public async Task FAQ_CreateMeeting() {

			var testId = Guid.NewGuid();
			var AUC = await GetAdminCredentials(testId);
			TestView(AUC, "/", d => {

				d.DefaultTimeout(TimeSpan.FromSeconds(15));


				var item = d.Find(".faq-tile #faq_createL10");
				item.Click();

				d.Find(".anno-content");
				d.Find("#header-tab-l10").Click();

				Assert.IsTrue(d.Find(".anno-content").Text.Contains("Once created, your Level 10 meetings will show up here."));

				d.Find(".anno-button").Click();
				d.WaitForNotVisible(".anno-content");

				Assert.IsTrue(d.Find(".anno-content").Text.Contains("Click this button to create a new Level 10 meeting"));
				d.Find("#l10-create-meeting").Click();

				var text = d.Find(".anno-content").Text;
				Assert.IsTrue(text.Contains("This is the Level 10 wizard!"));
				Assert.IsTrue(text.Contains("Use this screen to build your L10 meeting."));
				Assert.IsTrue(text.Contains("Note:You only need to create one L10 per team."));
				d.Find(".anno-btn").Click();

			});

		}
	}
}
