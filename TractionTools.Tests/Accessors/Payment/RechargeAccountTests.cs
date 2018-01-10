using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Hooks.CrossCutting;
using NHibernate;
using RadialReview.Utilities;
using RadialReview.Hooks;
using TractionTools.Tests.TestUtils;
using RadialReview.Models.Payments;
using TractionTools.Tests.Utilities;
using RadialReview.Accessors;
using System.Threading.Tasks;
using static RadialReview.Hooks.CrossCutting.ExecutePaymentCardUpdate;
using RadialReview.Exceptions;
using System.Linq;
using RadialReview.Controllers;
using System.Threading;
using TractionTools.Tests.Reflections;
using RadialReview.Models;

namespace TractionTools.Tests.Accessors.Payment {
    [TestClass]
    public class RechargeAccountTests : BaseTest{
        [TestMethod]
        public async Task EnsureOneCharge() {

            //HooksRegistry.RegisterHookForTests();

            var hook = new ExecutePaymentCardUpdate();
            var o = await OrgUtil.CreateOrganization();
            
            var token = (await PaymentAccessor.SetCard(o.Manager, o.Id, await PaymentAccessor.GenerateFakeCard())).GetToken();

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    o.Organization.PaymentPlan.FreeUntil = DateTime.UtcNow.AddDays(-10);
                    s.Update(o.Organization.PaymentPlan);

                    s.Save(new InvoiceModel() {
                        Organization = o.Organization,
                        AmountDue = 100,
                    });
                    s.Save(new InvoiceModel() {
                        Organization = o.Organization,
                        AmountDue = 101,
                    });
                    tx.Commit();
                    s.Flush();
                }
            }

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    hook.Test_ThrowExceptionOn = 1;
                    await hook.UpdateCard(s,token);
                    tx.Commit();
                    s.Flush();
                }
            }

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var a = s.QueryOver<InvoiceModel>().Where(x => x.Organization.Id == o.Id).List().ToList();
                    Assert.AreEqual(1, a.Count(x => x.PaidTime== null));
                    Assert.AreEqual(1, a.Count(x => x.PaidTime != null));
                }
            }
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    hook.Test_ThrowExceptionOn = 1;
                    await hook.UpdateCard(s, token);
                    tx.Commit();
                    s.Flush();
                }
            }
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var a = s.QueryOver<InvoiceModel>().Where(x => x.Organization.Id == o.Id).List().ToList();
                    Assert.AreEqual(2, a.Count(x => x.PaidTime != null));
                }
            }

        }


        [TestMethod]
        public async Task RechargeIntegration() {

            HooksRegistry.RegisterHookForTests(new ExecutePaymentCardUpdate());

            var org = await OrgUtil.CreateOrganization();
            DbCommit(s => {
                var pp = org.Organization.PaymentPlan;
                pp.FreeUntil = DateTime.UtcNow.AddDays(-1);
                s.Update(pp);
                pp.Task.Started = DateTime.UtcNow;
                s.Update(pp.Task);
            });

            var sc = new SchedulerController();
            MockHttpContext(sc,false);
            
            var result = await sc.ChargeAccount(new CancellationToken(),org.Id,org.Organization.PaymentPlan.Task.Id);
            var d = result.Data.ToDynamic();

            Assert.AreEqual(false, d.charged);
            Assert.AreEqual(true, d.payment_exception);
            Assert.AreEqual(PaymentExceptionType.MissingToken, d.error);

            DbQuery(s => {
                var errors = s.QueryOver<InvoiceModel>().Where(x => x.Organization.Id == org.Id).List().ToList();
                Assert.AreEqual(1, errors.Count);
                Assert.IsNull(errors[0].PaidTime);
            });

            //set the card, it should go through now...
            await PaymentAccessor.SetCard(org.Manager, org.Id, await PaymentAccessor.GenerateFakeCard());            
            DbQuery(s => {
            //    var errors = s.QueryOver<PaymentFailRecord>().Where(x => x.OrgId == org.Id).List().ToList();
            //    Assert.AreEqual(1, errors.Count);
            //    Assert.IsNotNull(errors[0].Resolved);

                var invoices =s.QueryOver<InvoiceModel>().Where(x => x.Organization.Id == org.Id).List().ToList();

                Assert.AreEqual(1, invoices.Count);
                Assert.IsNotNull(invoices[0].PaidTime);
                Assert.IsTrue(invoices[0].AmountDue>100);

            });

        }
    }
}
