using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.UITests.Selenium;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;

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
				d.DefaultTimeout(TimeSpan.FromSeconds(15));

				d.Find("#header-tab-reviews").Click();
				Assert.IsTrue(d.Find("#issue-eval-btn").Displayed);
			});
			TestView(await org.GetCredentials(org.Middle), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));
				d.Find("#header-tab-reviews").Click();
				Assert.IsTrue(d.Find("#issue-eval-btn").Displayed);
			});

			//Not visible for employee
			TestView(await org.GetCredentials(org.Employee), "/", d => {
				d.DefaultTimeout(TimeSpan.FromSeconds(15));
				d.Find("#header-tab-reviews").Click();
				Assert.IsFalse(d.Find("#issue-eval-btn").Displayed);
			});



		}
	}
}
