﻿using System;
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




        [TestMethod]
        public void PaymentPlanTypesTests()
        {
            var types = Enum.GetNames(typeof(PaymentPlanType));
            Assert.AreEqual(5, types.Length, "Make sure to update PaymentAccessor switch-cases and add test cases below");

            Assert.AreEqual(PaymentPlanType.SelfImplementer_Monthly_March2016, PaymentAccessor.GetPlanType("self-Implementer"));
            Assert.AreEqual(PaymentPlanType.SelfImplementer_Monthly_March2016, PaymentAccessor.GetPlanType("selfimplementer"));
            Assert.AreEqual(PaymentPlanType.SelfImplementer_Monthly_March2016, PaymentAccessor.GetPlanType("SELFimplementer"));

            Assert.AreEqual(PaymentPlanType.Enterprise_Monthly_March2016, PaymentAccessor.GetPlanType("ENTERPRISE"));
            Assert.AreEqual(PaymentPlanType.Professional_Monthly_March2016, PaymentAccessor.GetPlanType("professional"));

#pragma warning disable CS0618 // Type or member is obsolete
			var plan = PaymentAccessor.GeneratePlan(PaymentPlanType.SelfImplementer_Monthly_March2016);
#pragma warning restore CS0618 // Type or member is obsolete

			Assert.AreEqual(0, plan.Id);
            Assert.AreEqual(0, plan.OrgId);
            Assert.AreEqual(199, plan.BaselinePrice);
            Assert.AreEqual(10, plan.FirstN_Users_Free);
            Assert.AreEqual(12, plan.L10PricePerPerson);
            Assert.AreEqual(3, plan.ReviewPricePerPerson);

#pragma warning disable CS0618 // Type or member is obsolete
			plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Enterprise_Monthly_March2016);
#pragma warning restore CS0618 // Type or member is obsolete

			Assert.AreEqual(0, plan.Id);
            Assert.AreEqual(0, plan.OrgId);
            Assert.AreEqual(500, plan.BaselinePrice);
            Assert.AreEqual(45, plan.FirstN_Users_Free);
            Assert.AreEqual(2, plan.L10PricePerPerson);
            Assert.AreEqual(0, plan.ReviewPricePerPerson);


#pragma warning disable CS0618 // Type or member is obsolete
			plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Professional_Monthly_March2016);
#pragma warning restore CS0618 // Type or member is obsolete

			Assert.AreEqual(0, plan.Id);
            Assert.AreEqual(0, plan.OrgId);
            Assert.AreEqual(149, plan.BaselinePrice);
            Assert.AreEqual(10, plan.FirstN_Users_Free);
            Assert.AreEqual(10, plan.L10PricePerPerson);
            Assert.AreEqual(2, plan.ReviewPricePerPerson);
        }

        private async Task TestPlan(UserModel userModel,PaymentPlanType plan,int baseCharge,
            int chargeAnd19Users_L10, int chargeAnd19Users_L10_Review, int chargeAnd17Users_L10, int chargeAnd17Users_L10_Review,
            int chargeAnd107Users_L10_Review)
        {
            MockHttpContext();
			UserOrganizationModel user;
			AccountabilityNode userNode;
			var now = new DateTime(2016, 3, 9);

            var org = new OrganizationAccessor().CreateOrganization(userModel,"PaymentPlanTest " + plan + " Org",plan,
                now, out user,out userNode, true, true);
			DbCommit(s => {
				var o = s.Get<PaymentPlan_Monthly>(org.PaymentPlan.Id);
				o.NoChargeForUnregisteredUsers = false;
				s.Update(o);
			});


				var token = await PaymentAccessor.GenerateFakeCard(plan+" " + DateTime.UtcNow.ToJavascriptMilliseconds());

            await PaymentAccessor.SetCard(user, org.Id, token.id, token.@class, token.card_type,
                token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year,
                "", "", "", "", "", "", "", "", "", true);

            Assert.IsNotNull(org.PaymentPlan);
            Assert.IsNotNull(org.PaymentPlan.Task);
            Assert.IsNotNull(org.PaymentPlan.Description);


			//var result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now);
			var result =await TaskAccessor.ExecuteTask_Test(org.PaymentPlan.Task, now);
			//var log = (await PaymentSpringUtil.GetAllLogs(true, 1,10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(0, result.Response.amount_settled);

			//result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(29));
			var nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(29));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(0, result.Response.amount_settled);

            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(31));
			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(31));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
            Assert.AreEqual(baseCharge, result.Response.amount_settled);
						
            var ids = new List<long>();
            DbCommit(s => {
                for (var i = 0; i < 9; i++) {
                    var u = new UserOrganizationModel() { Organization = org, CreateTime = now.AddDays(32 + i) };
                    s.Save(u);
                    ids.Add(u.Id);
                }
            });

			//result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(62));
			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(62));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(baseCharge, result.Response.amount_settled);
			
            DbCommit(s => {
                for (var i = 0; i < 9; i++) {
                    s.Save(new UserOrganizationModel() {
						Organization = org, CreateTime = now.AddDays(62 + i)
					});
                }
            });

			// result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(73));
			// Assert.AreEqual(chargeAnd19Users_L10, result.amount_settled);
			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(74));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(chargeAnd19Users_L10, result.Response.amount_settled);


			//result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(93));
			//Assert.AreEqual(chargeAnd19Users_L10_Review, result.amount_settled);
			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(93));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(chargeAnd19Users_L10_Review, result.Response.amount_settled);


			DbCommit(s => {
                for (var i = 0; i < 2; i++) {
                    var u = s.Get<UserOrganizationModel>(ids[i]);
                    u.DeleteTime = now.AddDays(63);
                    s.Update(u);
                }
            });

			//result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(73));
			//Assert.AreEqual(chargeAnd19Users_L10, result.amount_settled);
			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(73));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(chargeAnd19Users_L10, result.Response.amount_settled);

			//result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(94));
			//Assert.AreEqual(chargeAnd17Users_L10_Review, result.amount_settled);
			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(94));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(chargeAnd17Users_L10_Review, result.Response.amount_settled);

			DbCommit(s => {
                for (var i = 0; i < 90; i++) {
                    s.Save(new UserOrganizationModel() { Organization = org, CreateTime = now.AddDays(62) });
                }
            });

			//result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendReceipt: false, executeTime: now.AddDays(94));
			//Assert.AreEqual(chargeAnd107Users_L10_Review, result.amount_settled);
			nextTask = result.NewTasks.Single();
			result = await TaskAccessor.ExecuteTask_Test(nextTask, now.AddDays(94));
			//log = (await PaymentSpringUtil.GetAllLogs(true, 1, 10)).Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.AreEqual(chargeAnd107Users_L10_Review, result.Response.amount_settled);
		}

        [TestMethod]
        public async Task ChargeOrgs()
        {
            var types = Enum.GetNames(typeof(PaymentPlanType));
            Assert.AreEqual(5, types.Length, "Make sure to update PaymentAccessor switch-cases and add test cases below");
            MockApplication();
           // OrganizationModel org = null;
            UserModel userModel = null;
           // UserOrganizationModel manager = null;
           // L10Recurrence recur = null;
           // PeriodModel period = null;
            DbCommit(s => {
                userModel = new UserModel();
                s.Save(userModel);
            });

			var slf_reviewPrice = 3 * 100;
			var pro_reviewPrice = 2 * 100;
			var ent_reviewPrice = 0 * 100;

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
                baseprice + Math.Max(0,19 -  numFreeUsers) * l10Price,
                baseprice + Math.Max(0,19 -  numFreeUsers) * l10Price + 19  * reviewPrice,
                baseprice + Math.Max(0,17 -  numFreeUsers) * l10Price,
                baseprice + Math.Max(0,17 -  numFreeUsers) * l10Price + 17	 * reviewPrice,
				baseprice + Math.Max(0,107 - numFreeUsers) * l10Price + 107 * reviewPrice
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

			///await TestPlan(userModel, PaymentPlanType.Enterprise_Monthly_March2016,
			//ent_baseprice,
			//ent_baseprice,
			//ent_baseprice + 19 * ent_reviewPrice,
			//ent_baseprice,
			//ent_baseprice + 17 * ent_reviewPrice,
			//ent_baseprice + 7 * 200 + 107 * ent_reviewPrice
			//); 



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

			//await TestPlan(userModel, PaymentPlanType.SelfImplementer_Monthly_March2016,
			//    19900,
			//    19900 + 9  * 1200,
			//    19900 + 9  * 1200 + 19  * 500,
			//    19900 + 7  * 1200,        
			//    19900 + 7  * 1200 + 17  * 500,
			//    19900 + 97 * 1200 + 107 * 500
			//);

            //var now = new DateTime(2016, 3, 9);
            //org = new OrganizationAccessor().CreateOrganization(
            //    userModel,
            //    "PaymentPlanTest Professional Org",
            //    PaymentPlanType.Professional_Monthly_March2016,
            //    now, out user, true, true);

            //var token = await PaymentAccessor.GenerateFakeCard("Professional " + DateTime.UtcNow.ToJavascriptMilliseconds());

            //await PaymentAccessor.SetCard(user, org.Id,
            //    token.id, token.@class, token.card_type, token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year,
            //    "", "", "", "", "", "", "", "", "", true);



            //Assert.IsNotNull(org.PaymentPlan);
            //Assert.IsNotNull(org.PaymentPlan.Task);
            //Assert.IsNotNull(org.PaymentPlan.Description);

            //var result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now);
            //Assert.AreEqual(0, result.amount_settled);

            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(29));
            //Assert.AreEqual(0, result.amount_settled);

            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(31));
            //Assert.AreEqual(14900, result.amount_settled);
            //var ids = new List<long>();
            //DbCommit(s => {
            //    for (var i = 0; i < 9; i++) {
            //        var u = new UserOrganizationModel() { Organization = org, CreateTime = now.AddDays(32 + i) };
            //        s.Save(u);
            //        ids.Add(u.Id);
            //    }
            //});

            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(62));
            //Assert.AreEqual(14900, result.amount_settled);

            //DbCommit(s => {
            //    for (var i = 0; i < 9; i++) {
            //        s.Save(new UserOrganizationModel() { Organization = org, CreateTime = now.AddDays(62 + i) });
            //    }
            //});

            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(73));
            //Assert.AreEqual(14900 + 9000, result.amount_settled);


            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(93));
            //Assert.AreEqual(14900 + (1000 + 400) * 9, result.amount_settled);


            //DbCommit(s => {
            //    for (var i = 0; i < 2; i++) {
            //        var u = s.Get<UserOrganizationModel>(ids[i]);
            //        u.DeleteTime = now.AddDays(63);
            //        s.Update(u);
            //    }
            //});

            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(73));
            //Assert.AreEqual(14900 + 9000, result.amount_settled);

            //result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(94));
            //Assert.AreEqual(14900 + (1000 + 400) * 7, result.amount_settled);


            //org = new OrganizationAccessor().CreateOrganization(
            //   userModel,
            //   "PaymentPlanTest Enterprise Org",
            //   PaymentPlanType.Enterprise_Monthly_March2016,
            //   now, out user, true, true);

            //token = await PaymentAccessor.GenerateFakeCard("Enterprise " + DateTime.UtcNow.ToJavascriptMilliseconds());

            //await PaymentAccessor.SetCard(user, org.Id,
            //    token.id, token.@class, token.card_type, token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year,
            //    "", "", "", "", "", "", "", "", "", true);

        }
    }
}
