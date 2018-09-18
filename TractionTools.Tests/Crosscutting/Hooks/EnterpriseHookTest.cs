using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.TestUtils;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using RadialReview.Models;
using RadialReview.Hooks;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Models.ViewModels;
using NHibernate;
using TractionTools.Tests.Utilities;
using static RadialReview.Accessors.PaymentAccessor;
using RadialReview.Utilities.DataTypes;
using System.Threading;
using System.Threading.Tasks;

namespace TractionTools.Tests.Hooks {
	[TestClass]
	public class EnterpriseHookTest : BaseTest {
		[TestMethod]
		[TestCategory("Hook")]
		public async Task AddUserWorks() {
			var ctx = await Ctx.Build();
			var now = DateTime.UtcNow;

			HooksRegistry.RegisterHook(new EnterpriseHook(45));

			var p = ctx.Org.Organization.PaymentPlan;
			Assert.IsInstanceOfType(p, typeof(PaymentPlan_Monthly));
			var plan = (PaymentPlan_Monthly)p;

			Assert.AreEqual(11, ctx.Org.AllUsers.Count);

			Assert.IsTrue(HooksRegistry.IsRegistered<EnterpriseHook>());


			DbQuery(s => {
				var org = s.Get<OrganizationModel>(ctx.Org.Id);
				Assert.AreEqual(149, plan.BaselinePrice);
				Assert.AreEqual(10, plan.L10PricePerPerson);
				Assert.AreEqual(10, plan.FirstN_Users_Free);
			});

			await ctx.Org.RegisterAllUsers();

			MockHttpContext();
			for (var i = 0; i < 34; i++) {
				Console.WriteLine(i);
				var created = await OrgUtil.AddUserToOrg(ctx.Org, "" + i);
				AddIsTest();
				await ctx.RegisterUser(created);
			}


			//Assert.AreEqual(45, calc.NumberL10Users);
			DbQuery(s => {
				var org = s.Get<OrganizationModel>(ctx.Org.Id);
				Assert.AreEqual(149, plan.BaselinePrice);
				Assert.AreEqual(10, plan.L10PricePerPerson);
				Assert.AreEqual(10, plan.FirstN_Users_Free);
			});
			var created2 = await OrgUtil.AddUserToOrg(ctx.Org, "mid");
			AddIsTest();
			await ctx.RegisterUser(created2);

			//Should have been added
			DbQuery(s => {
				var org = s.Get<OrganizationModel>(ctx.Org.Id);
				var calc = new UserCalculator(s, ctx.Org.Id, org.PaymentPlan, new DateRange(DateTime.MaxValue, DateTime.MaxValue));
				Assert.AreEqual(46, calc.NumberL10Users);
				Assert.AreEqual(499, calc.Plan.BaselinePrice);
				Assert.AreEqual(2, calc.Plan.L10PricePerPerson);
				Assert.AreEqual(45, calc.Plan.FirstN_Users_Free);
			});

			//Add one more, nothing should happen			
			var created3 = await OrgUtil.AddUserToOrg(ctx.Org, "last");
			AddIsTest();
			await ctx.RegisterUser(created3);
			DbQuery(s => {
				var org = s.Get<OrganizationModel>(ctx.Org.Id);
				var calc = new UserCalculator(s, ctx.Org.Id, org.PaymentPlan, new DateRange(DateTime.MaxValue, DateTime.MaxValue));
				Assert.AreEqual(47, calc.NumberL10Users);
				Assert.AreEqual(499, calc.Plan.BaselinePrice);
				Assert.AreEqual(2, calc.Plan.L10PricePerPerson);
				Assert.AreEqual(45, calc.Plan.FirstN_Users_Free);
			});

			//Remove one
			await new UserAccessor().RemoveUser(ctx.Manager, created2.Id, DateTime.UtcNow);
			//Thread.Sleep(5000);
			DbQuery(s => {
				var org = s.Get<OrganizationModel>(ctx.Org.Id);
				var calc = new UserCalculator(s, ctx.Org.Id, org.PaymentPlan, new DateRange(DateTime.MaxValue, DateTime.MaxValue));
				Assert.AreEqual(46, calc.NumberL10Users);
				Assert.AreEqual(499, calc.Plan.BaselinePrice);
				Assert.AreEqual(2, calc.Plan.L10PricePerPerson);
				Assert.AreEqual(45, calc.Plan.FirstN_Users_Free);
			});

			//Remove another
			await new UserAccessor().RemoveUser(ctx.Manager, created3.Id, DateTime.UtcNow);
			//Thread.Sleep(5000);
			DbQuery(s => {
				var org = s.Get<OrganizationModel>(ctx.Org.Id);
				var calc = new UserCalculator(s, ctx.Org.Id, org.PaymentPlan, new DateRange(DateTime.MaxValue, DateTime.MaxValue));
				Assert.AreEqual(45, calc.NumberL10Users);
				Assert.AreEqual(149, calc.Plan.BaselinePrice);
				Assert.AreEqual(10, calc.Plan.L10PricePerPerson);
				Assert.AreEqual(10, calc.Plan.FirstN_Users_Free);
			});

			//new UserAccessor().RemoveUser(ctx.Manager, ctx.Middle.Id, DateTime.UtcNow);


		}
	}
}
