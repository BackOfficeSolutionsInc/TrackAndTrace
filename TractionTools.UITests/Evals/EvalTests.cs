using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.UITests.Selenium;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;
using OpenQA.Selenium.Support.UI;

namespace TractionTools.UITests.Evals {
	[TestClass]
	public class EvalTests : BaseSelenium {

		[TestMethod]
		public async Task CanCreateEval() {
			var testId = Guid.NewGuid();
			//var AUC = await GetAdminCredentials(testId);
			var AUC = await GetAdminCredentials(testId);
			var org = OrgUtil.CreateFullOrganization("CanCreateEval");
			//Visible for admin and managers
			TestView(await org.GetCredentials(org.Manager), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(2));

				d.Find("#header-tab-reviews").Click();
				Assert.IsTrue(d.Find("#issue-eval-btn").Displayed);
			});
			TestView(await org.GetCredentials(org.Middle), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(2));
				d.Find("#header-tab-reviews").Click();
				Assert.IsTrue(d.Find("#issue-eval-btn").Displayed);
			});

			//Not visible for employee
			TestView(await org.GetCredentials(org.Employee), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(2));
				d.Find("#header-tab-reviews").Click();
				d.NotFind("#issue-eval-btn");
			});
		}

		[TestMethod]
		public async Task CreateNoSupervisor() {
			var testId = Guid.NewGuid();
			//var AUC = await GetAdminCredentials(testId);
			var AUC = await GetAdminCredentials(testId);
			var orgName = "CreateNoSupervisor";
			var org = OrgUtil.CreateFullOrganization(orgName);
			//Visible for admin and managers
			TestView(await org.GetCredentials(org.Manager), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));

				d.Find("#header-tab-reviews").Click();
				d.Find("#issue-eval-btn").Click();

				new SelectElement(d.Find("#SelectedTeam")).SelectByText("All of "+ orgName);
				d.Find("#pageIssueTeam");

				d.Find("[name='customize'][value='self']").Closest(".btn").Click();
				d.Find("#pageIssueOrganization");

				d.Find("#ReviewName").SendKeys("Review " + testId);
				d.Find("#submitButton").Click();

				var rows = d.Finds(".reviews-table tbody tr");
				Assert.AreEqual(1, rows.Count);
				var row = rows[0];


			});
		}
	}
}
