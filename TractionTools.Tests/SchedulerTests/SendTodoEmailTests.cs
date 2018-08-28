using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Crosscutting.ScheduledJobs;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Tasks;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using static RadialReview.Crosscutting.ScheduledJobs.TodoEmailsScheduler;
using static TractionTools.Tests.Permissions.BasePermissionsTest;

namespace TractionTools.Tests.SchedulerTests {
    [TestClass]
    public class SendTodoEmailTests : BaseTest {

        [TestMethod]
        [TestCategory("Scheduler")]
        public async Task TestRedirect() {
            var result = (string)await TaskAccessor.d_ExecuteTaskFunc(null,new ScheduledTask() { Url= "https://jigsaw.w3.org/HTTP/300/302.html" }, DateTime.MinValue);
            Assert.IsTrue(result.Contains("Redirect test page"));

		}


		[TestMethod]
		[TestCategory("Scheduler")]
		public async Task TestTodoQuery() {
			using (HibernateSession.SetDatabaseEnv_TestOnly(Env.local_mysql)) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var found = TodoEmailHelpers._QueryTodoModulo(s, 10, 13, 1, new DateTime(2017, 10, 27), new DateTime(2017, 11, 4), new DateTime(2017, 11, 4));

						int a = 0;

					}
				}
			}
		}


		[TestMethod]
        [TestCategory("Scheduler")]
        public async Task TestModulo() {
            var divisor = 13;
            var emailTime = 1;

            var nowUtc = new DateTime(2017, 10, 27);
            var yesterday = nowUtc.AddDays(-1);

            var tomorrow = nowUtc.Date.AddDays(2).AddTicks(-1);
            var rangeLow = nowUtc.Date.AddDays(-1);
            var rangeHigh = nowUtc.Date.AddDays(4).AddTicks(-1);
            var nextWeek = nowUtc.Date.AddDays(7);
            if (nowUtc.DayOfWeek == DayOfWeek.Friday)
                rangeHigh = rangeHigh.AddDays(1);

            var org = await OrgUtil.CreateOrganization();
            MockHttpContext();
            var dupIndex = 0L;
            var users = new List<UserOrganizationModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    for (var i = 0; i < divisor + 1; i++) {
                        var user = await OrgUtil.AddUserToOrg(org, "" + i);
                        await org.RegisterUser(user);
                        users.Add(user);

                        var auid = user.Id;

                        if (i == 0) {
                            dupIndex = auid % (long)divisor;
                        }
                        await TodoAccessor.CreateTodo(org.Manager, TodoCreation.CreatePersonalTodo("", accountableUserId: auid, dueDate: yesterday));

                    }
                    tx.Commit();
                    s.Flush();
                }
            }
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    foreach (var user in users) {
                        var u = s.Get<UserOrganizationModel>(user.Id);
                        u.User.SendTodoTime = emailTime;
                        s.Update(u);
                    }

                    tx.Commit();
                    s.Flush();
                }
            }

            var expectedCounts = new List<int>();
            for (var i = 0; i < divisor; i++)
                expectedCounts.Add(dupIndex == i ? 2 : 1);

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

					for (var remainder = 0; remainder < divisor; remainder += 1) {
						var todos = TodoEmailsScheduler.TodoEmailHelpers._QueryTodoModulo(s, emailTime, divisor, remainder, rangeLow, rangeHigh, nextWeek);
						Assert.AreEqual(expectedCounts[remainder], todos.Count);
					}
					for (var remainder = 0; remainder < divisor; remainder += 1) {
						var todos = TodoEmailsScheduler.TodoEmailHelpers._QueryTodoModulo(s, emailTime+1, divisor, remainder, rangeLow, rangeHigh, nextWeek);
						Assert.AreEqual(0, todos.Count);
					}

					//Noone listening at emailTime+1
					for (var remainder = 0; remainder < divisor; remainder += 1) {
                        var unsentMail = new List<Mail>();
                        await TodoEmailsScheduler.TodoEmailHelpers._ConstructTodoEmails(emailTime+1, unsentMail, s, nowUtc, divisor, remainder);
                        Assert.AreEqual(0, unsentMail.Count);
                    }

                    for (var remainder = 0; remainder < divisor; remainder += 1) {
                        var unsentMail = new List<Mail>();
                        await TodoEmailsScheduler.TodoEmailHelpers._ConstructTodoEmails(emailTime, unsentMail, s, nowUtc, divisor, remainder);
                        Assert.AreEqual(expectedCounts[remainder], unsentMail.Count);
                    }
                }
            }



        }
    }
}
