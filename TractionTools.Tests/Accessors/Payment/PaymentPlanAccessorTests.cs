using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.TestUtils;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.Periods;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using System.Threading.Tasks;
using RadialReview;
using System.Collections.Generic;
using TractionTools.Tests.Utilities;
using RadialReview.Models.Accountability;
using RadialReview.Utilities;

namespace TractionTools.Tests.Accessors {
	[TestClass]
	public class PaymentPlanAccessorTests : BaseTest {



#pragma warning disable CS0618 // Type or member is obsolete
		[TestMethod]
		public void PaymentPlanTypesTests() {
			var types = Enum.GetNames(typeof(PaymentPlanType));
			Assert.AreEqual(5, types.Length, "Make sure to update PaymentAccessor switch-cases and add test cases below");

			Assert.AreEqual(PaymentPlanType.SelfImplementer_Monthly_March2016, PaymentAccessor.GetPlanType("self-Implementer"));
			Assert.AreEqual(PaymentPlanType.SelfImplementer_Monthly_March2016, PaymentAccessor.GetPlanType("selfimplementer"));
			Assert.AreEqual(PaymentPlanType.SelfImplementer_Monthly_March2016, PaymentAccessor.GetPlanType("SELFimplementer"));

			Assert.AreEqual(PaymentPlanType.Enterprise_Monthly_March2016, PaymentAccessor.GetPlanType("ENTERPRISE"));
			Assert.AreEqual(PaymentPlanType.Professional_Monthly_March2016, PaymentAccessor.GetPlanType("professional"));

			var plan = PaymentAccessor.GeneratePlan(PaymentPlanType.SelfImplementer_Monthly_March2016);

			Assert.AreEqual(0, plan.Id);
			Assert.AreEqual(0, plan.OrgId);
			Assert.AreEqual(199, plan.BaselinePrice);
			Assert.AreEqual(10, plan.FirstN_Users_Free);
			Assert.AreEqual(12, plan.L10PricePerPerson);
			Assert.AreEqual(3, plan.ReviewPricePerPerson);

			plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Enterprise_Monthly_March2016);

			Assert.AreEqual(0, plan.Id);
			Assert.AreEqual(0, plan.OrgId);
			Assert.AreEqual(500, plan.BaselinePrice);
			Assert.AreEqual(45, plan.FirstN_Users_Free);
			Assert.AreEqual(2, plan.L10PricePerPerson);
			Assert.AreEqual(2, plan.ReviewPricePerPerson);


			plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Professional_Monthly_March2016);

			Assert.AreEqual(0, plan.Id);
			Assert.AreEqual(0, plan.OrgId);
			Assert.AreEqual(149, plan.BaselinePrice);
			Assert.AreEqual(10, plan.FirstN_Users_Free);
			Assert.AreEqual(10, plan.L10PricePerPerson);
			Assert.AreEqual(2, plan.ReviewPricePerPerson);
		}
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
		private async Task TestPlan(UserModel userModel, PaymentPlanType plan, int baseCharge,
			int chargeAnd19Users_L10, int chargeAnd19Users_L10_Review, int chargeAnd17Users_L10, int chargeAnd17Users_L10_Review,
			int chargeAnd107Users_L10_Review) {
			MockHttpContext();
			//UserOrganizationModel user;
			//AccountabilityNode userNode;
			var now = DateTime.UtcNow;


			var data = new OrgCreationData() {
				Name = "PaymentPlanTest " + plan + " Org",
				EnableL10 = true,
				EnableReview = true,
			};

			var res = await new OrganizationAccessor().CreateOrganization(userModel, plan, now, data);
			DbCommit(s => {
				var o = s.Get<PaymentPlan_Monthly>(res.organization.PaymentPlan.Id);
				o.NoChargeForUnregisteredUsers = false;
				s.Update(o);
			});


			var token = await PaymentAccessor.GenerateFakeCard(plan + " " + DateTime.UtcNow.ToJavascriptMilliseconds());

			await PaymentAccessor.SetCard(res.NewUser, res.organization.Id, token.id, token.@class, token.card_type,
				token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year,
				"", "", "", "", "", "", "", "", "", true);

			Assert.IsNotNull(res.organization.PaymentPlan);
			Assert.IsNotNull(res.organization.PaymentPlan.Task);
			Assert.IsNotNull(res.organization.PaymentPlan.Description);


			var result = await TaskAccessor.ExecuteTask_Test(res.organization.PaymentPlan.Task, now);
			Assert.AreEqual(0, result.Response.amount_settled);

			var nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(28));
			Assert.AreEqual(0, result.Response.amount_settled);

			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(31));
			Assert.AreEqual(baseCharge, result.Response.amount_settled);

			var ids = new List<long>();
			DbCommit(s => {
				for (var i = 0; i < 9; i++) {
					var u = new UserOrganizationModel() { Organization = res.organization, CreateTime = now.AddDays(32 + i) };
					s.Save(u);
					ids.Add(u.Id);
				}
			});

			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(62));
			Assert.AreEqual(baseCharge, result.Response.amount_settled);

			DbCommit(s => {
				for (var i = 0; i < 9; i++) {
					s.Save(new UserOrganizationModel() {
						Organization = res.organization,
						CreateTime = now.AddDays(62 + i)
					});
				}
			});

			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(74));
			Assert.AreEqual(chargeAnd19Users_L10, result.Response.amount_settled);


			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(93));
			Assert.AreEqual(chargeAnd19Users_L10_Review, result.Response.amount_settled);


			DbCommit(s => {
				for (var i = 0; i < 2; i++) {
					var u = s.Get<UserOrganizationModel>(ids[i]);
					u.DeleteTime = now.AddDays(63);
					s.Update(u);
				}
			});

			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(73));
			Assert.AreEqual(chargeAnd19Users_L10, result.Response.amount_settled);

			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(94));
			Assert.AreEqual(chargeAnd17Users_L10_Review, result.Response.amount_settled);

			DbCommit(s => {
				for (var i = 0; i < 90; i++) {
					s.Save(new UserOrganizationModel() { Organization = res.organization, CreateTime = now.AddDays(62) });
				}
			});

			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(94));
			Assert.AreEqual(chargeAnd107Users_L10_Review, result.Response.amount_settled);

		}
#pragma warning restore CS0618 // Type or member is obsolete

		[TestMethod]
		public async Task ChargeOrgs() {
			var types = Enum.GetNames(typeof(PaymentPlanType));
			Assert.AreEqual(5, types.Length, "Make sure to update PaymentAccessor switch-cases and add test cases below");
			MockApplication();

			UserModel userModel = null;
			DbCommit(s => {
				userModel = new UserModel();
				s.Save(userModel);
			});

			var slf_reviewPrice = 3 * 100;
			var pro_reviewPrice = 2 * 100;
			var ent_reviewPrice = 2 * 100;

			var slf_l10Price = 12 * 100;
			var pro_l10Price = 10 * 100;
			var ent_l10Price = 2 * 100;

			var slf_baseprice = 199 * 100;
			var pro_baseprice = 149 * 100;
			var ent_baseprice = 500 * 100;

			var slf_freeusers = 10;
			var pro_freeusers = 10;
			var ent_freeusers = 45;

			////
			var l10Price = pro_l10Price;
			var reviewPrice = pro_reviewPrice;
			var baseprice = pro_baseprice;
			var numFreeUsers = pro_freeusers;
			await TestPlan(userModel, PaymentPlanType.Professional_Monthly_March2016,
				baseprice,
				baseprice + Math.Max(0, 19 - numFreeUsers) * l10Price,
				baseprice + Math.Max(0, 19 - numFreeUsers) * l10Price + 19 * reviewPrice,
				baseprice + Math.Max(0, 17 - numFreeUsers) * l10Price,
				baseprice + Math.Max(0, 17 - numFreeUsers) * l10Price + 17 * reviewPrice,
				baseprice + Math.Max(0, 107 - numFreeUsers) * l10Price + 107 * reviewPrice
			);

			///
			l10Price = ent_l10Price;
			reviewPrice = ent_reviewPrice;
			baseprice = ent_baseprice;
			numFreeUsers = ent_freeusers;
			await TestPlan(userModel, PaymentPlanType.Enterprise_Monthly_March2016,
				baseprice,
				baseprice + Math.Max(0, 19 - numFreeUsers) * l10Price,
				baseprice + Math.Max(0, 19 - numFreeUsers) * l10Price + 19 * reviewPrice,
				baseprice + Math.Max(0, 17 - numFreeUsers) * l10Price,
				baseprice + Math.Max(0, 17 - numFreeUsers) * l10Price + 17 * reviewPrice,
				baseprice + Math.Max(0, 107 - numFreeUsers) * l10Price + 107 * reviewPrice
			);



			///
			l10Price = slf_l10Price;
			reviewPrice = slf_reviewPrice;
			baseprice = slf_baseprice;
			numFreeUsers = slf_freeusers;
			await TestPlan(userModel, PaymentPlanType.SelfImplementer_Monthly_March2016,
				baseprice,
				baseprice + Math.Max(0, 19 - numFreeUsers) * l10Price,
				baseprice + Math.Max(0, 19 - numFreeUsers) * l10Price + 19 * reviewPrice,
				baseprice + Math.Max(0, 17 - numFreeUsers) * l10Price,
				baseprice + Math.Max(0, 17 - numFreeUsers) * l10Price + 17 * reviewPrice,
				baseprice + Math.Max(0, 107 - numFreeUsers) * l10Price + 107 * reviewPrice
			);

			Assert.Inconclusive("Also test Eval Only");

		}
	}
}
