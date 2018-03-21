using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.UITests.Selenium;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RadialReview.Accessors;

namespace TractionTools.UITests.L10Wizard {
	[TestClass]
	public class L10Wizard : BaseSelenium {

		public static Credentials AUC { get; set; }
		public static String MeetingName { get; set; }
		public static L10 Recur { get; set; }

		[ClassInitialize]
		public static void Setup(TestContext ctx) {
			var testId = Guid.NewGuid();
			AUC = GetAdminCredentials(testId).GetAwaiter().GetResult();

			MeetingName = "WizardMeeting";
			var org = OrgUtil.CreateOrganization("WizardTests").GetAwaiter().GetResult();

			Recur = L10Utility.CreateRecurrence(name:MeetingName,org:org).GetAwaiter().GetResult();

			Recur.AddAttendee(Recur.Creator).GetAwaiter().GetResult();

		}

		[TestMethod]
		[TestCategory("Visual")]
		public void Visual_L10_Wizard_Basics() {
			TestView(AUC, "/l10/wizard/" + Recur.Id, d => {
				d.Find("#l10-wizard-menu", 10);
				var basics = d.FindElement(By.PartialLinkText("Basics"), 10);
				Assert.IsTrue(basics.HasClass("selected"));
				d.FindElement(By.CssSelector(".nextButton"), 10);
				d.WaitForNotVisible(".backButton");
				Assert.IsFalse(d.Find(".backButton", 10).Displayed);
				Assert.IsTrue(d.Find(".nextButton", 10).Displayed);

				d.WaitUntil(x => MeetingName == d.Find("#l10-wizard-name input").Val());
				//Assert.AreEqual(MeetingName, d.Find("#l10-wizard-name input").Val());

				var select = d.Find("#l10-wizard-teamtype select");

				d.WaitUntil(x => "string:LeadershipTeam" == select.Val());

				//Assert.AreEqual(, select.Val());
				d.TestScreenshot("Basics");

				select.Click();
				new SelectElement(select).SelectByText("Other");
				Assert.AreEqual("string:Other", select.Val());
				d.TestScreenshot("Basics-Select");
			});
		}
		[TestMethod]
		[TestCategory("Visual")]
		public void Visual_L10_Wizard_Attendees() {
			TestView(AUC, "/l10/wizard/" + Recur.Id, d => {
				d.Find("#l10-wizard-menu", 10);
				var pageTitle = d.FindElement(By.PartialLinkText("Attendees"), 10);
				pageTitle.Click();
				Assert.IsTrue(d.WaitUntil(x => pageTitle.HasClass("selected")));

				Assert.IsTrue(d.Find(".backButton", 10).Displayed);
				Assert.IsTrue(d.Find(".nextButton", 10).Displayed);

				var page = d.Find("#l10-wizard-attendees");

				// d.WaitForVisible("#l10-wizard-attendees .empty-search");


				d.TestScreenshot("Blank");

				Assert.AreEqual("ATTENDEES LIST", d.Find(".title-bar").Text);
				Assert.IsTrue(d.Find(".upload-attendees").Displayed);

				Assert.IsFalse(d.Find(".livesearch-container").Displayed);
				Assert.IsFalse(d.Find(".create-user").Displayed);
				Assert.IsFalse(d.Find(".upload-users").Displayed);

				page.Find(".create-row").Click();

				//Assert.IsTrue(d.WaitUntil(x => x.Find(".livesearch-container").Displayed));
				//Assert.IsTrue(d.WaitUntil(x => x.Find(".create-user").Displayed));
				//Assert.IsTrue(d.WaitUntil(x => x.Find(".upload-users").Displayed));

				Assert.IsTrue(d.WaitUntil(10, x => x.Find(".seoc .select2-selection--single", 10).Displayed));

				d.TestScreenshot("Basics-Select");
			});
		}
		[TestMethod]
		[TestCategory("Visual")]
		public async Task Visual_L10_Wizard_Scorecard() {
			TestView(AUC, "/l10/wizard/" + Recur.Id, d => {
				d.Find("#l10-wizard-menu", 10);
				var pageTitle = d.FindElement(By.PartialLinkText("Scorecard"), 10);
				pageTitle.Click();
				Assert.IsTrue(d.WaitUntil(x => pageTitle.HasClass("selected")));

				Assert.IsTrue(d.Find(".backButton", 10).Displayed);
				Assert.IsTrue(d.Find(".nextButton", 10).Displayed);

				var page = d.Find("#l10-wizard-scorecard");

				d.WaitForVisible("#l10-wizard-scorecard .empty-search");

				d.TestScreenshot("Blank");

				Assert.AreEqual("SCORECARD MEASURABLES", page.Find(".title-bar").Text);

				Assert.IsTrue(page.Find(".upload-scorecard").Displayed);

				page.Find(".create-row").Click();

				d.Find("[placeholder='Measurable Name']", 20);
				d.Find("#modalOk").Click();

				//d.Wait(1000);
				//d.Find("#modalOk").Submit();

				var rows = d.WaitUntil(15, x => {
					var f = x.Finds("#ScorecardTable tbody tr");
					if (f.Count == 0)
						return null;
					return f;
				});
				d.WaitForNotVisible("#l10-wizard-scorecard .empty-search");

				Assert.AreEqual(1, rows.Count);

				var row = rows[0];
				d.TestScreenshot("Scorecard-BeforeAdd");

				var measurableName = "Measurable-Name";
				row.Find(".measurable-column input").SendKeys(measurableName);
				row.Find(".value.goal-column input").SendKeys("1234");

				d.Find("body").Click();

				d.Wait(1000);


				d.Find("#modalOk").Submit();

				d.TestScreenshot("Scorecard-AfterAdd");

				//d.Wait(1000);
				//d.Find("#modalOk").Submit();

				//rows = d.WaitUntil(15, x => {
				//	var f = x.Finds("[placeholder='Measurable Name']");
				//	if (f.Count == 0)
				//		return null;
				//	return f;
				//});

				//d.Find("[placeholder='Measurable Name']",20);
				//d.Find("#modalOk").Click();

				rows = d.WaitUntil(15, x => {
					var f = x.Finds("#ScorecardTable tbody tr");
					if (f.Count == 0)
						return null;
					return f;
				});
				row = rows[0];


				row.Find(".picture").Click();
				d.WaitForVisible(".editable-wrap");
				d.TestScreenshot("Rocks-Picture");

				row.FindVisible(".delete-row").Click();
				d.WaitForNotVisible(/*"#l10-wizard-scorecard */".editable-wrap");

				d.WaitForVisible("#l10-wizard-scorecard .empty-search");

			});
		}
		[TestMethod]
		[TestCategory("Visual")]
		public async Task Visual_L10_Wizard_Rocks() {

			TestView(AUC, "/l10/wizard/" + Recur.Id, d => {
				d.Find("#l10-wizard-menu", 10);
				var pageTitle = d.FindElement(By.PartialLinkText("Rocks"), 10);
				pageTitle.Click();
				Assert.IsTrue(d.WaitUntil(x => pageTitle.HasClass("selected")));

				Assert.IsTrue(d.Find(".backButton", 10).Displayed);
				Assert.IsTrue(d.Find(".nextButton", 10).Displayed);

				var page = d.Find("#l10-wizard-rocks");

				d.WaitForVisible("#l10-wizard-rocks .empty-search");

				d.TestScreenshot("Blank");

				Assert.AreEqual("QUARTERLY ROCKS", page.Find(".title-bar").Text);

				Assert.IsTrue(page.Find(".upload-rocks").Displayed);

				page.Find(".create-row").Click();


				d.Find("[placeholder='Rock Name']", 20);
				d.Find("#modalForm").Submit();

				var rows = d.WaitUntil(20, x => {
					var f = x.Finds(".rock-pane tbody tr[md-row]");
					if (f.Count == 0)
						return null;
					return f;
				});
				d.WaitForNotVisible("#l10-wizard-rocks .empty-search");

				Assert.AreEqual(1, rows.Count); //extra one for ".vs-repeat-before-content"

				var row = rows[0];
				d.TestScreenshot("Rocks-BeforeAdd");

				var measurableName = "Rock-Name";
				row.Find(".message-column input").SendKeys(measurableName);
				var box = row.Find(".checkbox-column md-checkbox");

				Assert.AreEqual("false", box.Attr("aria-checked"));

				box.Click();

				Assert.AreEqual("true", box.Attr("aria-checked"));

				d.TestScreenshot("Rocks-AfterAdd");

				row.Find(".picture").Click();
				d.WaitForVisible(/*"#l10-wizard-rocks */".editable-wrap");
				d.TestScreenshot("Rocks-Picture");

				row.Find(".delete-row-archive").Click();
				d.WaitForVisible(/*"#l10-wizard-rocks */".grayRow");
				d.Wait(2000);
				d.Navigate().Refresh();
				d.WaitForVisible("#l10-wizard-rocks .empty-search");

			});
		}
		[TestMethod]
		[TestCategory("Visual")]
		public async Task Visual_L10_Wizard_Todo() {
			await L10Accessor.AddAttendee(AUC.User, Recur.Id, AUC.User.Id);

			TestView(AUC, "/l10/wizard/" + Recur.Id, d => {
				d.Find("#l10-wizard-menu", 10);
				var pageTitle = d.FindElement(By.PartialLinkText("To-dos"), 10);
				pageTitle.Click();
				Assert.IsTrue(d.WaitUntil(x => pageTitle.HasClass("selected")));

				Assert.IsTrue(d.Find(".backButton", 10).Displayed);
				Assert.IsTrue(d.Find(".nextButton", 10).Displayed);

				var page = d.Find("#l10-wizard-todos");

				d.WaitForVisible("#l10-wizard-todos .empty-search");

				//Assert.IsTrue(d.WaitUntil(x => page.Find(".empty-search").Displayed));

				d.TestScreenshot("Blank");

				Assert.AreEqual("TO-DO LIST", page.Find(".title-bar").Text);

				Assert.IsTrue(page.Find(".upload-todos").Displayed);

				page.Find(".create-row").Click();

				var rows = d.WaitUntil(x => {
					var f = x.Finds(".todo-pane tbody tr[md-row]");
					if (f.Count == 0)
						return null;
					return f;
				});

				d.WaitForNotVisible("#l10-wizard-todos .empty-search");

				Assert.AreEqual(1, rows.Count); //extra one for repeat container

				var row = rows[0];
				d.TestScreenshot("BeforeAdd");

				var measurableName = "Todo-Name";
				row.Find(".message-column input").SendKeys(measurableName);

				d.TestScreenshot("AfterAdd");

				row.Find(".picture").Click();
				Assert.IsTrue(d.WaitUntil(x => x.Find(".editable-wrap").Displayed));
				d.TestScreenshot("Picture");

				row.Find(".delete-row").Click();

				d.WaitForNotVisible(/*"#l10-wizard-todos */".editable-wrap");

				d.WaitForVisible("#l10-wizard-todos .empty-search");

			});
		}
		[TestMethod]
		[TestCategory("Visual")]
		public void Visual_L10_Wizard_Issues() {
			TestView(AUC, "/l10/wizard/" + Recur.Id, d => {
				d.Find("#l10-wizard-menu", 10);
				var pageTitle = d.FindElement(By.PartialLinkText("Issues"), 10);
				pageTitle.Click();
				Assert.IsTrue(d.WaitUntil(x => pageTitle.HasClass("selected")));

				Assert.IsTrue(d.Find(".backButton", 10).Displayed);
				d.WaitForNotVisible(".nextButton");

				var page = d.Find("#l10-wizard-issues");

				d.WaitForVisible("#l10-wizard-issues .empty-search");

				d.TestScreenshot("Blank");

				Assert.AreEqual("ISSUES LIST", page.Find(".title-bar").Text);

				Assert.IsTrue(d.Find(".upload-issues").Displayed);

				page.Find(".create-row").Click();

				var rows = d.WaitUntil(x => {
					var f = x.Finds(".issues-pane tbody tr[md-row]");
					if (f.Count == 0)
						return null;
					return f;
				});
				d.WaitForNotVisible("#l10-wizard-issues .empty-search");

				Assert.AreEqual(1, rows.Count);//extra one for ".vs-repeat-before-content"

				var row = rows[0];
				d.TestScreenshot("BeforeAdd");

				var measurableName = "Issue-Name";
				row.Find(".message-column input").SendKeys(measurableName);

				d.TestScreenshot("AfterAdd");

				row.Find(".picture").Click();
				d.WaitForVisible(/*"#l10-wizard-issues */".editable-wrap");
				d.TestScreenshot("Picture");

				row.Find(".delete-row").Click();

				d.WaitForNotVisible(/*"#l10-wizard-issues */".editable-wrap");

				d.WaitForVisible("#l10-wizard-issues .empty-search");

			});
		}
	}
}
