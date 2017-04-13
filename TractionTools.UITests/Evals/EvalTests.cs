using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.UITests.Selenium;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using System.Threading;

namespace TractionTools.UITests.Evals {
	[TestClass]
	public class EvalTests : BaseSelenium {

		[TestMethod]
		[TestCategory("Visual")]
		public async Task CanCreateEval() {
			var testId = Guid.NewGuid();
			//var AUC = await GetAdminCredentials(testId);
			var AUC = await GetAdminCredentials(testId);
			var org = OrgUtil.CreateFullOrganization("CanCreateEval");
			//Visible for admin and managers
			TestView(org.GetCredentials(org.Manager), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(2));

				d.Find("#header-tab-reviews").Click();
				Assert.IsTrue(d.Find("#issue-eval-btn").Displayed);
			});
			TestView(org.GetCredentials(org.Middle), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(2));
				d.Find("#header-tab-reviews").Click();
				Assert.IsTrue(d.Find("#issue-eval-btn").Displayed);
			});

			//Not visible for employee
			TestView(org.GetCredentials(org.Employee), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(2));
				d.Find("#header-tab-reviews").Click();
				d.NotFind("#issue-eval-btn");
			});
		}

		[TestMethod]
		[TestCategory("Visual")]
		public async Task CreateNoSupervisor() {
			var testId = Guid.NewGuid();
			//var AUC = await GetAdminCredentials(testId);
			var AUC = await GetAdminCredentials(testId);
			var orgName = "CreateNoSupervisor";
			var org = OrgUtil.CreateFullOrganization(orgName);
			string reviewName = null;
			//Visible for admin and managers
			TestView(org.GetCredentials(org.Manager), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));

				d.Find("#header-tab-reviews").Click();
				d.Find("#issue-eval-btn").Click();

				new SelectElement(d.Find("#SelectedTeam")).SelectByText("All of " + orgName);
				d.Find("#pageIssueTeam");

				d.Find("[name='customize'][value='self']").Closest(".btn").Click();
				d.Find("#pageIssueOrganization");

				Thread.Sleep(6000);

				reviewName = "Review " + testId;
				d.Find("#ReviewName").SendKeys(reviewName);
				d.Find("#submitButton").Click();

				d.WaitUntil(2, x => x.Finds(".reviews-table tbody tr").Count == 1);
				var rows = d.Finds(".reviews-table tbody tr");
				Assert.AreEqual(1, rows.Count);
				var row = rows[0];
				row.Find(".review-extra-options").Click();
				d.TestScreenshot("CreateNoSupervisor_Dropdown");

				d.Find(".advanced-link");

				var take = row.Find("td > a");
				Assert.AreEqual(reviewName, take.Text);

				take.Click();

				d.WaitUntil(10, x => x.Finds("#nameList")[0].Finds("li a").Count == 4);
				//Self
				var links = d.Finds("#nameList")[0].Finds("li a");

				Thread.Sleep(2000);
				//Make sure we're reviewing only these users
				org.AssertAllUsers(u => links.Any(x => x.Text == u.GetName()), org.Manager, org.E1, org.Employee, org.Middle);
			});


			TestView(org.GetCredentials(org.Employee), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));
				d.Find("#header-tab-reviews").Click();

				var rows = d.Finds(".reviews-table tbody tr");
				Assert.AreEqual(1, rows.Count);
				var row = rows[0];

				row.Find(".review-extra-options").Click();
				d.TestScreenshot("CreateNoSupervisor_DropdownEmployee");
				d.NotFind(".advanced-link", 1);

				var take = row.Find("td > a");
				Assert.AreEqual(reviewName, take.Text);
				take.Click();
				d.WaitUntil(10, x => x.Finds("#nameList")[0].Finds("li a").Count > 0);
				Thread.Sleep(2000);
				var links = d.Finds("#nameList")[0].Finds("li a");
				//Make sure we're reviewing only these users
				org.AssertAllUsers(u => links.Any(x => x.Text == u.GetName()), org.Employee);
			});

			TestView(org.GetCredentials(org.Middle), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));
				d.Find("#header-tab-reviews").Click();

				var take = d.Finds(".reviews-table tbody tr")[0].Find("td > a");
				Assert.AreEqual(reviewName, take.Text);
				take.Click();
				d.WaitUntil(10, x => x.Finds("#nameList")[0].Finds("li a").Count > 0);
				Thread.Sleep(2000);
				var links = d.Finds("#nameList")[0].Finds("li a");
				//Make sure we're reviewing only these users
				org.AssertAllUsers(u => links.Any(x => x.Text == u.GetName()), org.Middle, org.E1, org.E2, org.E3);
			});

		}

		[TestMethod]
		[TestCategory("Visual")]
		public async Task CreateSupervisor() {
			var testId = Guid.NewGuid();
			//var AUC = await GetAdminCredentials(testId);
			var AUC = await GetAdminCredentials(testId);
			var orgName = "CreateSupervisor";
			var org = OrgUtil.CreateFullOrganization(orgName);
			string reviewName = null;
			//Visible for admin and managers
			TestView(org.GetCredentials(org.Manager), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));

				d.Find("#header-tab-reviews").Click();
				d.Find("#issue-eval-btn").Click();

				new SelectElement(d.Find("#SelectedTeam")).SelectByText("All of " + orgName);
				d.Find("#pageIssueTeam");

				d.Find("[name='customize'][value='manager']").Closest(".btn").Click();
				d.Find("#pageIssueOrganization");

				Thread.Sleep(2000);

				reviewName = "Review " + testId;
				d.Find("#ReviewName").SendKeys(reviewName);
				d.Find("#submitButton").Click();

				d.Find("#header-tab-reviews").Click();


				d.WaitUntil(4, x => x.Finds(".reviews-table tbody tr.prereview").Count == 1);
				var rows = d.Finds(".reviews-table tbody tr.prereview");
				Assert.AreEqual(1, rows.Count);
				var row = rows[0];
				row.Find(".review-extra-options").Click();
				d.TestScreenshot("CreateSupervisor_Dropdown1");

				d.Find(".advanced-link");

				var take = row.Find("td > a");
				Assert.AreEqual(reviewName, take.Text);

				take.Click();

				d.Find("[href='#advanced']").Click();

				d.WaitUntil(5, x=> x.Finds(".customize .selectable").Count == 4);


				d.Find("#first_" + org.Employee.Id).Click();
				Thread.Sleep(1000);
				d.Find("[name='customize_"+org.Employee.Id+"_"+org.E6.Id+"_" + org.E6Node.Id+"']").Click();

				Thread.Sleep(250);

				d.Find("[name='review'][value='issuePrereview']").Click();
				d.Find("#header-tab-reviews").Click();

				d.Finds(".reviews-table tbody tr.prereview")[0].Find(".review-extra-options").Click();
				d.Find(".issue-immediately").Click();
				d.Find("#modalOk").Click();

				d.TestScreenshot("CreateSupervisor_CreatingReview");

				d.Find("#header-tab-reviews").Click();

				d.WaitUntil(4, x => x.Finds(".reviews-table tbody tr").Count == 1);

				//Confirm row looks correct
				rows = d.Finds(".reviews-table tbody tr");
				Assert.AreEqual(1, rows.Count);
				row = rows[0];
				row.Find(".review-extra-options").Click();
				d.TestScreenshot("CreateSupervisor_Dropdown2");

				d.Find(".advanced-link");
				take = row.Find("td > a");
				Assert.AreEqual(reviewName, take.Text);

				take.Click();

				d.WaitUntil(10, x => x.Finds("#nameList")[0].Finds("li a").Count == 4);
				
				var links = d.Finds("#nameList")[0].Finds("li a");
				Thread.Sleep(2000);
				//Make sure we're reviewing only these users
				org.AssertAllUsers(u => links.Any(x => x.Text == u.GetName()), org.Manager, org.E1, org.Employee, org.Middle);

				
			});

			TestView(org.GetCredentials(org.Employee), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));
				d.Find("#header-tab-reviews").Click();

				var rows = d.Finds(".reviews-table tbody tr");
				Assert.AreEqual(1, rows.Count);
				var row = rows[0];

				row.Find(".review-extra-options").Click();
				d.TestScreenshot("CreateSupervisor_DropdownEmployee");
				d.NotFind(".advanced-link", 1);

				var take = row.Find("td > a");
				Assert.AreEqual(reviewName, take.Text);
				take.Click();
				d.WaitUntil(10, x => x.Finds("#nameList")[0].Finds("li a").Count > 0);
				Thread.Sleep(2000);
				var links = d.Finds("#nameList")[0].Finds("li a");
				//Make sure we're reviewing only these users
				org.AssertAllUsers(u => links.Any(x => x.Text == u.GetName()), org.Employee, org.E6);
			});

			TestView(org.GetCredentials(org.Middle), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));
				d.Find("#header-tab-reviews").Click();

				var take = d.Finds(".reviews-table tbody tr")[0].Find("td > a");
				Assert.AreEqual(reviewName, take.Text);
				take.Click();
				d.WaitUntil(10, x => x.Finds("#nameList")[0].Finds("li a").Count > 0);
				Thread.Sleep(2000);
				var links = d.Finds("#nameList")[0].Finds("li a");
				//Make sure we're reviewing only these users
				org.AssertAllUsers(u => links.Any(x => x.Text == u.GetName()), org.Middle, org.E1, org.E2, org.E3);
			});



		}
	}
}
