using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.UITests.Selenium;
using System.Threading.Tasks;
using TractionTools.UITests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using TractionTools.Tests.Utilities;
using RadialReview.Models;

namespace TractionTools.UITests.MeetingArchive {
	[TestClass]
	public class MeetingTests : BaseSelenium {

		#region TestWrappers
		private class RunLayoutTests : IDisposable {

			private TestCtx d { get; set; }

			private string PAGENAME;
			private string TODO_TEXT;
			private string TODO_DETAILS;
			private string ISSUE_TEXT;
			private string ISSUE_DETAILS;
			private string TODO_DATE;

			public RunLayoutTests(TestCtx context) {
				d = context;
				d.WaitUntil(x => x.Find(".elapsed-time").Displayed);
				d.EnsureDifferent(x => x.Find(".settings-button").Click());
				d.EnsureDifferent(x => x.Find(".level-10").Click());
				d.EnsureDifferent(x => x.Find(".print-button").Click());
				d.EnsureDifferent(x => x.FindElementByText("Quarterly Printout", 1).Click());

				d.WaitUntil(x => x.Finds("#modalBody .checkbox").Count == 7);
				d.EnsureDifferent(x => x.Find("#modalCancel").Click());

				d.Find(".button-bar", 15);

				d.Find(".notesButton", 10).Click();

				var notes = d.Find(".notes", 5);

				Assert.AreEqual("about:blank", notes.Find("iframe").Attr("src"));
				Assert.IsTrue(d.Find(".notes-instruction", 15).Displayed);

				d.TestScreenshot("notes");

				notes.Find(".tab.add").Click();

				var name = d.Find("#modal #Name", 6);
				PAGENAME = "A new page!";
				name.SendKeys(PAGENAME);
				name.SendKeys(Keys.Return);

				var activeTab = d.Find(".tab.active", 10);

				Assert.AreEqual(PAGENAME, activeTab.Text);

				Assert.AreNotEqual("about:blank", notes.Find("iframe").Attr("src"));

				d.SwitchTo().Frame(notes.Find("iframe")).Find(".enabledtoolbar", 5);
				d.SwitchTo().DefaultContent();

				d.TestScreenshot("NewNote");

				Throws<Exception>(() => d.Find(".notesButton").Click());

				Assert.IsTrue(notes.Find(".overlay").Displayed);

				//d.Find(".overlay").Click();
				d.ExecuteScript("$('.overlay').click()");

				d.Find(".button-bar .issuesModal").Click(4);

				ISSUE_TEXT = "A new issue!!";
				ISSUE_DETAILS = "issue details!!";
				d.Find("#modal #Message", 8).SendKeys(ISSUE_TEXT);
				d.Find("#modal textarea", 4).SendKeys(ISSUE_DETAILS);

				d.TestScreenshot("IssueModal");

				d.Find("#modalOk").Click();

				d.WaitUntil(x => x.Find(".button-bar .todoModal").Displayed);


				d.Find(".button-bar .todoModal").Click(2);

				TODO_TEXT = "A new todo!!";
				TODO_DETAILS = "todo details!!";

				d.WaitUntil(35, x => x.Find("#modal #Message").Displayed);

				d.Find("#modal #Message", 6).SendKeys(TODO_TEXT);
				d.Find("#modal textarea", 6).SendKeys(TODO_DETAILS);

				TODO_DATE = d.Find(".client-date").Val();

				d.TestScreenshot("TodoModal");
				d.Find("#modalOk").Click();

				//  Assert.IsFalse(d.Find(".start-video ").Displayed);
				d.EnsureDifferent(x => x.Find(".videoconference-container .clicker").Click(), waitMs: 500);

				d.TestScreenshot("VideoBar");
				//Assert.IsTrue(d.Find(".start-video ").Displayed);
				d.EnsureDifferent(x => x.Find(".videoconference-container .clicker").Click(), waitMs: 500);
				// Assert.IsFalse(d.Find(".start-video ").Displayed);




			}

			private void TestTodoWasAdded() {
				//GO TO THE TODO PAGE
				d.FindElementByLinkText("To-do List").Click();

				var todoPage = d.Find(".todo", 5);

				var rows = todoPage.Finds(".todo-row");
				Assert.AreEqual(1, rows.Count);

				d.WaitUntil(x => x.Find(".new-indicator").Displayed);

				//Assert.IsTrue(rows.First().Find(".new-indicator").Displayed);
				Throws<NoSuchElementException>(() => rows.First().Find(".overdue-indicator"));

				Assert.AreEqual(TODO_TEXT, rows.First().Find(".message").Text);

				Assert.IsTrue(String.IsNullOrWhiteSpace(d.Find("#todoDetails").Text));


				d.TestScreenshot("TodoPage");

				d.EnsureDifferent(x => x.Find(".todo-row").Click());
				Assert.IsFalse(String.IsNullOrWhiteSpace(d.Find(".todoDetails").Text));

				Assert.AreEqual(TODO_TEXT, d.Find(".message-holder").Text);
				var messageHolder = d.Find(".message-holder");
				messageHolder.Find("span").Click();
				var EDITED_TODO = "edited todo";
				messageHolder.Find("textarea").SendKeys(Keys.Control + "a");
				messageHolder.Find("textarea").SendKeys(EDITED_TODO);
				d.Find(".level-10").Click();
				Assert.AreEqual(EDITED_TODO, d.Find(".message-holder span", 2).Text);
				Assert.AreEqual(EDITED_TODO, todoPage.Find(".todo-row .message").Text);
				var completion_BeforeCheck = d.Find(".todo-completion-percentage").Text;

				d.Find(".todo-row .todo-checkbox").Check();
				//Assert.IsFalse(d.Find(".todo-row .todo-checkbox").Enabled);
				//Assert.IsFalse(d.Find(".todoDetails .doneButton input").Enabled);            

				d.WaitUntil(3, x => x.Find(".todo-row .todo-checkbox").Enabled);
				Assert.IsTrue(d.Find(".todoDetails .doneButton input").Enabled);

				//Todo completion number should not change
				Assert.AreEqual(completion_BeforeCheck, d.Find(".todo-completion-percentage").Text);

				d.TestScreenshot("TodoPageSelectedChecked");

				var etherPad = d.SwitchTo().Frame(d.SwitchTo().Frame(d.SwitchTo().Frame(d.Find(".todoDetails iframe")).Find("iframe")).Find("iframe"));
				Assert.AreEqual(TODO_DETAILS, etherPad.Find("body").Text);

				d.SwitchTo().DefaultContent();
			}

			private void TestIssueWasAdded() {
				///GO TO THE ISSUES PAGE
				d.FindElementByLinkText("IDS").Click();
				var idsPage = d.Find(".ids.prioritization-Rank", 5);
				var rows = idsPage.Finds(".issue-row");
				Assert.AreEqual(1, rows.Count);
				Throws<NoSuchElementException>(() => rows.First().Find(".overdue-indicator"));

				Assert.AreEqual(ISSUE_TEXT, rows.First().Find(".message").Text);

				Assert.IsTrue(String.IsNullOrWhiteSpace(d.Find("#issueDetails").Text));


				d.TestScreenshot("IssuePage");

				d.EnsureDifferent(x => x.Find(".issue-row").Click());
				Assert.IsFalse(String.IsNullOrWhiteSpace(d.Find(".issueDetails").Text));

				Assert.AreEqual(ISSUE_TEXT, d.Find(".message-holder").Text);
				var messageHolder = d.Find(".message-holder");
				messageHolder.Find("span").Click();
				var EDITED_ISSUE = "edited issue";
				messageHolder.Find("textarea", d, 2).SendKeys(Keys.Control + "a");
				messageHolder.Find("textarea", d, 2).SendKeys(EDITED_ISSUE);
				d.Find(".level-10").Click();
				Assert.AreEqual(EDITED_ISSUE, d.Find(".message-holder span", 2).Text);
				Assert.AreEqual(EDITED_ISSUE, idsPage.Find(".issue-row .message").Text);

				var etherPad = d.SwitchTo().Frame(d.SwitchTo().Frame(d.SwitchTo().Frame(d.Find(".issueDetails iframe", 15)).Find("iframe", 15)).Find("iframe", 15));
				Assert.AreEqual(ISSUE_DETAILS, etherPad.Find("body").Text);
				d.SwitchTo().DefaultContent();

				d.Find(".issue-row .issue-checkbox").Check();
				d.WaitUntil(3, x => x.Finds(".issue-row").Where(y => y.Displayed).Count() == 0);

				d.TestScreenshot("IDSPageSelectedChecked");


			}

			public void Dispose() {
				TestTodoWasAdded();
				TestIssueWasAdded();
			}
		}
		#endregion

		[TestMethod]
		[TestCategory("Visual")]
		public async Task L10_Meeting_Segue() {
			var testId = Guid.NewGuid();
			var auc = await GetAdminCredentials(testId);
			var recur = await L10Utility.CreateRecurrence("Meeting");

			TestView(auc, "/l10/meeting/" + recur.Id, d => {

				Assert.IsFalse(d.Find(".elapsed-time").Displayed);
				d.Find("#form0", 10).Submit();
				d.FindElement(By.PartialLinkText("Segue"), 10).Click();
				using (new RunLayoutTests(d)) {
					//Nothing to test on this page I guess...
				}
				BaseSelenium.ConcludeMeeting(d);
			});

		}
		private class MM {
			public string name;
			public decimal value;
			public UserOrganizationModel owner;
			public LessGreater dir;
			public MeasurableModel measurable;
		}


		[TestMethod]
		[TestCategory("Visual")]
		public async Task L10_Meeting_Scorecard() {
			var testId = Guid.NewGuid();
			var auc = await GetAdminCredentials(testId);
			var au = auc.User;
			var recur = await L10Utility.CreateRecurrence("Scorecard");
			var measurables = new[] {
				new MM{name="TestM1",value=10,owner=au,dir=LessGreater.LessThan} ,
				new MM{name="TestM2",value=12,owner=au,dir=LessGreater.GreaterThan} ,
				new MM{name="TestM3",value=14,owner=au,dir=LessGreater.LessThan} ,
			};

			MockHttpContext();
			DbCommit(async s => {
				foreach (var m in measurables) {
					var m101 = new MeasurableModel {
						AccountableUserId = m.owner.Id,
						AdminUserId = m.owner.Id,
						OrganizationId = au.Organization.Id,
						Goal = m.value,
						GoalDirection = m.dir,
						Title = m.name,
						UnitType = RadialReview.Models.Enums.UnitType.Dollar,
					};

					var builder = MeasurableBuilder.Build(m.name, m.owner.Id, m.owner.Id, UnitType.Dollar, m.value, m.dir);
					var m1 = await ScorecardAccessor.CreateMeasurable(s, PermissionsUtility.Create(s, au) , builder);
					await L10Accessor.AttachMeasurable(s, PermissionsUtility.Create(s, au), recur.Id, m1.Id);

					//await L10Accessor.AddMeasurable(s, PermissionsUtility.Create(s, au), RealTimeUtility.Create(), recur.Id,
					//	 RadialReview.Controllers.L10Controller.AddMeasurableVm.CreateMeasurableViewModel(recur.Id, m101));
					m.measurable = m101;
				}
			});


			TestView(auc, "/l10/meeting/" + recur.Id, d => {

				d.Find("#form0", 10).Submit();
				d.FindElement(By.PartialLinkText("Scorecard"), 10).Click();
				using (new RunLayoutTests(d)) {
					//Nothing to test on this page I guess...
				}
				BaseSelenium.ConcludeMeeting(d);
			});
		}
	}
}
