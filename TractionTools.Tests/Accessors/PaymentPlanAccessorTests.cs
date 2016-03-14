using System;
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

            var plan = PaymentAccessor.GeneratePlan(PaymentPlanType.SelfImplementer_Monthly_March2016);

            Assert.AreEqual(0, plan.Id);
            Assert.AreEqual(0, plan.OrgId);
            Assert.AreEqual(199, plan.BaselinePrice);
            Assert.AreEqual(10, plan.FirstN_Users_Free);
            Assert.AreEqual(12, plan.L10PricePerPerson);
            Assert.AreEqual(5, plan.ReviewPricePerPerson);

            plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Enterprise_Monthly_March2016);

            Assert.AreEqual(0, plan.Id);
            Assert.AreEqual(0, plan.OrgId);
            Assert.AreEqual(999, plan.BaselinePrice);
            Assert.AreEqual(100, plan.FirstN_Users_Free);
            Assert.AreEqual(2, plan.L10PricePerPerson);
            Assert.AreEqual(3, plan.ReviewPricePerPerson);


            plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Professional_Monthly_March2016);

            Assert.AreEqual(0, plan.Id);
            Assert.AreEqual(0, plan.OrgId);
            Assert.AreEqual(149, plan.BaselinePrice);
            Assert.AreEqual(10, plan.FirstN_Users_Free);
            Assert.AreEqual(10, plan.L10PricePerPerson);
            Assert.AreEqual(4, plan.ReviewPricePerPerson);
        }

        private async Task TestPlan(UserModel userModel,PaymentPlanType plan,int baseCharge,
            int chargeAnd19Users_L10, int chargeAnd19Users_L10_Review, int chargeAnd17Users_L10, int chargeAnd17Users_L10_Review,
            int chargeAnd107Users_L10_Review)
        {
            MockHttpContext();
            UserOrganizationModel user;
            var now = new DateTime(2016, 3, 9);
            var org = new OrganizationAccessor().CreateOrganization(userModel,"PaymentPlanTest " + plan + " Org",plan,
                now, out user, true, true);

            var token = await PaymentAccessor.GenerateFakeCard(plan+" " + DateTime.UtcNow.ToJavascriptMilliseconds());

            await PaymentAccessor.SetCard(user, org.Id, token.id, token.@class, token.card_type,
                token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year,
                "", "", "", "", "", "", "", "", "", true);

            Assert.IsNotNull(org.PaymentPlan);
            Assert.IsNotNull(org.PaymentPlan.Task);
            Assert.IsNotNull(org.PaymentPlan.Description);

            var result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now);
            Assert.AreEqual(0, result.amount_settled);

            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(29));
            Assert.AreEqual(0, result.amount_settled);

            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(31));
            Assert.AreEqual(baseCharge, result.amount_settled);
            var ids = new List<long>();
            DbCommit(s => {
                for (var i = 0; i < 9; i++) {
                    var u = new UserOrganizationModel() { Organization = org, CreateTime = now.AddDays(32 + i) };
                    s.Save(u);
                    ids.Add(u.Id);
                }
            });

            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(62));
            Assert.AreEqual(baseCharge, result.amount_settled);

            DbCommit(s => {
                for (var i = 0; i < 9; i++) {
                    s.Save(new UserOrganizationModel() { Organization = org, CreateTime = now.AddDays(62 + i) });
                }
            });

            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(73));
            Assert.AreEqual(chargeAnd19Users_L10, result.amount_settled);


            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(93));
            Assert.AreEqual(chargeAnd19Users_L10_Review, result.amount_settled);


            DbCommit(s => {
                for (var i = 0; i < 2; i++) {
                    var u = s.Get<UserOrganizationModel>(ids[i]);
                    u.DeleteTime = now.AddDays(63);
                    s.Update(u);
                }
            });

            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(73));
            Assert.AreEqual(chargeAnd19Users_L10, result.amount_settled);

            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(94));
            Assert.AreEqual(chargeAnd17Users_L10_Review, result.amount_settled);


            DbCommit(s => {
                for (var i = 0; i < 90; i++) {
                    s.Save(new UserOrganizationModel() { Organization = org, CreateTime = now.AddDays(62) });
                }
            });

            result = await PaymentAccessor.ChargeOrganization(org.Id, org.PaymentPlan.Task.Id, sendInvoice: false, executeTime: now.AddDays(94));
            Assert.AreEqual(chargeAnd107Users_L10_Review, result.amount_settled);
        }

        [TestMethod]
        public async Task ChargeOrgs()
        {
            var types = Enum.GetNames(typeof(PaymentPlanType));
            Assert.AreEqual(5, types.Length, "Make sure to update PaymentAccessor switch-cases and add test cases below");
            MockApplication();
            OrganizationModel org = null;
            UserModel userModel = null;
            UserOrganizationModel manager = null;
            L10Recurrence recur = null;
            PeriodModel period = null;
            DbCommit(s => {
                userModel = new UserModel();
                s.Save(userModel);
            });

            await TestPlan(userModel, PaymentPlanType.Professional_Monthly_March2016,
                14900,
                14900 + 9 * 1000,
                14900 + 9 * 1000 + 19*400,
                14900 + 7 * 1000,
                14900 + 7 * 1000 + 17*400,
                14900 + 97 * 1000 + 107*400
            );


            await TestPlan(userModel, PaymentPlanType.Enterprise_Monthly_March2016,
                99900,
                99900,
                99900 + 19 * 300,
                99900,
                99900 + 17 * 300,
                99900 + 7 * 200 + 107 * 300
            ); 
            
            await TestPlan(userModel, PaymentPlanType.SelfImplementer_Monthly_March2016,
                19900,
                19900 + 9  * 1200,
                19900 + 9  * 1200 + 19  * 500,
                19900 + 7  * 1200,        
                19900 + 7  * 1200 + 17  * 500,
                19900 + 97 * 1200 + 107 * 500
            );

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
