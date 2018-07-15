using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using RadialReview.Models;
using System.Threading.Tasks;
using RadialReview.Models.Enums;
using RadialReview.Exceptions;
using RadialReview.Models.Tasks;
using System.Linq;
using RadialReview;
using RadialReview.Utilities;
using RadialReview.Models.ViewModels;
using RadialReview.Models.Payments;
using System.Collections.Generic;
using RadialReview.Crosscutting.Schedulers;

namespace TractionTools.Tests.Accessors {
	[TestClass]
	public class PaymentAccessorTests : BaseTest {

		[TestMethod]
		[TestCategory("Charge")]
		public async Task TestCredits() {


			//Not enough credits to cover
			{
				var org = await OrgUtil.CreateOrganization();
				//Save credits
				var credits = new List<PaymentCredit>();
				credits.Add(new PaymentCredit() {
					OriginalAmount = 10,
					AmountRemaining = 10,
					Message = "mesage",
					OrgId = org.Id,

				});
				credits.Add(new PaymentCredit() {
					OriginalAmount = 10,
					AmountRemaining = 0,
					Message = "mesage2",
					OrgId = org.Id,

				});
				DbCommit(s => {
					foreach (var o in credits)
						s.Save(o);
				});

				DbQuery(s => {
					var cs = s.QueryOver<PaymentCredit>().Where(x => x.DeleteTime == null && x.OrgId == org.Id).List().ToList();
					Assert.AreEqual(2, cs.Count);
					Assert.AreEqual(1, cs.Count(x => x.AmountRemaining > 0));
				});

				//Apply all the credits
				DbCommit(s => {
					var itemized = new List<Itemized>() {
					new Itemized() {
						Name = "Item1",
						Price = 3,
						Quantity = 1,
						Description = "desc1",
					}, new Itemized() {
						Name = "Item2",
						Price = 10,
						Quantity = 1,
						Description = "desc2",
					}
				};
					Assert.AreEqual(2, itemized.Count());
					Assert.AreEqual(13, itemized.Sum(x => x.Total()));
					PaymentAccessor._ApplyCreditsToInvoice(s, org.Organization, itemized, credits);
					Assert.AreEqual(3, itemized.Count());
					Assert.AreEqual(3, itemized.Sum(x => x.Total()));
				});

				//Try and apply them again... but it should fail
				DbCommit(s => {
					var itemized = new List<Itemized>() {
					new Itemized() {
						Name = "Item3",
						Price = 4,
						Quantity = 1,
						Description = "desc3",
					}, new Itemized() {
						Name = "Item4",
						Price = 11,
						Quantity = 1,
						Description = "desc4",
					}
				};

					Assert.AreEqual(2, itemized.Count());
					Assert.AreEqual(15, itemized.Sum(x => x.Total()));

					PaymentAccessor._ApplyCreditsToInvoice(s, org.Organization, itemized, credits);
					Assert.AreEqual(2, itemized.Count());
					Assert.AreEqual(15, itemized.Sum(x => x.Total()));

				});

				//Assert that there are none...
				DbQuery(s => {
					var cs = s.QueryOver<PaymentCredit>().Where(x => x.DeleteTime == null && x.OrgId == org.Id && x.AmountRemaining > 0).List().ToList();
					Assert.AreEqual(0, cs.Count);
				});
			}


			//Than enough credits to cover
			{
				var org = await OrgUtil.CreateOrganization();
				//Save credits
				var credits = new List<PaymentCredit>();
				credits.Add(new PaymentCredit() {
					OriginalAmount = 100,
					AmountRemaining = 100,
					Message = "mesage",
					OrgId = org.Id,

				});
				credits.Add(new PaymentCredit() {
					OriginalAmount = 10,
					AmountRemaining = 0,
					Message = "mesage2",
					OrgId = org.Id,

				});
				DbCommit(s => {
					foreach (var o in credits)
						s.Save(o);
				});

				DbQuery(s => {
					var cs = s.QueryOver<PaymentCredit>().Where(x => x.DeleteTime == null && x.OrgId == org.Id).List().ToList();
					Assert.AreEqual(2, cs.Count);
					Assert.AreEqual(1, cs.Count(x => x.AmountRemaining > 0));
				});

				//Apply all the credits
				DbCommit(s => {
					var itemized = new List<Itemized>() {
					new Itemized() {
						Name = "Item1",
						Price = 3,
						Quantity = 1,
						Description = "desc1",
					}, new Itemized() {
						Name = "Item2",
						Price = 10,
						Quantity = 1,
						Description = "desc2",
					}
				};
					Assert.AreEqual(2, itemized.Count());
					Assert.AreEqual(13, itemized.Sum(x => x.Total()));
					PaymentAccessor._ApplyCreditsToInvoice(s, org.Organization, itemized, credits);
					Assert.AreEqual(3, itemized.Count());
					Assert.AreEqual(0, itemized.Sum(x => x.Total()));
				});

				//Try and apply them again... but it should fail
				DbCommit(s => {
					var itemized = new List<Itemized>() {
					new Itemized() {
						Name = "Item3",
						Price = 4,
						Quantity = 1,
						Description = "desc3",
					}, new Itemized() {
						Name = "Item4",
						Price = 11,
						Quantity = 1,
						Description = "desc4",
					}
				};

					Assert.AreEqual(2, itemized.Count());
					Assert.AreEqual(15, itemized.Sum(x => x.Total()));

					PaymentAccessor._ApplyCreditsToInvoice(s, org.Organization, itemized, credits);
					Assert.AreEqual(3, itemized.Count());
					Assert.AreEqual(0, itemized.Sum(x => x.Total()));

				});

				//Assert that there are none...
				DbQuery(s => {
					var cs = s.QueryOver<PaymentCredit>().Where(x => x.DeleteTime == null && x.OrgId == org.Id && x.AmountRemaining > 0).List().ToList();
					Assert.AreEqual(1, cs.Count);
					Assert.AreEqual(72, cs.Single().AmountRemaining);
				});
			}
		}



		[TestMethod]
		[TestCategory("Charge")]
		public async Task TestChargeOrganization() {
			var now = DateTime.UtcNow;
			var orgCreateTime = now;
			var executeTime = orgCreateTime.AddDays(60);

			var org = await OrgUtil.CreateOrganization(time: orgCreateTime);

			var caller = org.Manager;

			ScheduledTask task = null;

			DbCommit(s => {
				var o = s.Get<OrganizationModel>(org.Id);
				o.AccountType = AccountType.Demo;
				task = o.PaymentPlan.Task;
				task.Fire = executeTime;
				s.Update(o);
			});

			//Has token, payment failed
			var token = await PaymentAccessor.Test.GenerateFakeCard("ChargeOrganization 1");
			await PaymentAccessor.SetCard(caller, org.Id, token);

			//Run with standard number of users.
			{
				//Run it 
				var result = await new PaymentAccessor.Unsafe().ChargeViaHangfire(org.Id, task.Id, true, true, executeTime);

				//var taskResult = results.Where(x => x.TaskId == task.Id).SingleOrDefault();
				Assert.IsNotNull(result);
				Assert.IsTrue(result.WasCharged);
				Assert.IsFalse(result.HasError);
				Assert.IsFalse(result.WasFallthrough);
				Assert.IsFalse(result.WasPaymentException);

				//Confirm token is in PaymentSpring
				var t = PaymentSpringUtil.GetToken(org.Id);

				var log = result.PaymentResult;
				Assert.IsNotNull(log);
				Assert.AreEqual(t.CustomerToken, log.customer_id);
				Assert.AreEqual("SETTLED", log.status);
				Assert.AreEqual("transaction", log.@class);
				Assert.AreEqual(0, log.amount_refunded);
				Assert.AreEqual(14900, log.amount_settled);

			}

			//Run with additional user
			{
				MockHttpContext();
				var settings = new CreateUserOrganizationViewModel() {
					Email = "a@a.com",
					FirstName = "a",
					LastName = "a",
					OrgPositionId = null,
					ManagerNodeId = org.ManagerNode.Id
				};

				await JoinOrganizationAccessor.AddUser(caller, settings);


				var result = await new PaymentAccessor.Unsafe().ChargeViaHangfire(org.Id, task.Id, true, true, executeTime.AddDays(30));


				//Confirm it's in PaymentSpring
				var t = PaymentSpringUtil.GetToken(org.Id);

				var log = result.PaymentResult;
				//logs = await PaymentSpringUtil.GetAllLogs(true, 1);
				//log = logs.Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
				Assert.IsNotNull(log);
				Assert.AreEqual(t.CustomerToken, log.customer_id);
				Assert.AreEqual("SETTLED", log.status);
				Assert.AreEqual("transaction", log.@class);
				Assert.AreEqual(0, log.amount_refunded);
				Assert.AreEqual(15100, log.amount_settled);


			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		[TestMethod]
		[TestCategory("Charge")]
		public async Task EnqueueChargeOrganization() {

			using (var mock = Scheduler.Mock()) {

				var now = DateTime.UtcNow;
				var orgCreateTime = now;
				var executeTime = orgCreateTime.AddDays(60);

				var org = await OrgUtil.CreateOrganization(time: orgCreateTime);

				var caller = org.Manager;
				
				ScheduledTask task = null;
				DbCommit(s => {
					var o = s.Get<OrganizationModel>(org.Id);
					o.AccountType = AccountType.Demo;
					task = o.PaymentPlan.Task;
					task.Fire = executeTime;
					s.Update(o);
				});

				//No Token, Demo
				await ThrowsAsync<PaymentException>(async () => {
					await PaymentAccessor.Unsafe.ChargeOrganizationAmount(org.Id, 1, true);
				}, e => {
					Assert.AreEqual(PaymentExceptionType.MissingToken, e.Type);
				});


				//Has token, payment failed
				var token = await PaymentAccessor.Test.GenerateFakeCard("ChargeOrganization 1");
				await PaymentAccessor.SetCard(caller, org.Id, token);
				await ThrowsAsync<PaymentException>(async () => {
					await PaymentAccessor.Unsafe.ChargeOrganizationAmount(org.Id, 0.20m, true);
				}, e => {
					Assert.AreEqual(PaymentExceptionType.ResponseError, e.Type);
					Assert.AreEqual("Transaction declined (99820).", e.Message);
				});
				Assert.IsNull(task.Started);
				Assert.IsNull(task.Executed);
				Assert.AreEqual(0, task.ExceptionCount);

				await ThrowsAsync<PermissionsException>(async () => {
					await PaymentAccessor.EnqueueChargeOrganizationFromTask(org.Id, task.Id, true, true, executeTime);
				}, e => {
					Assert.AreEqual("Task was not started.", e.Message);
				});

				//Run it 
				var resultObj = await TaskAccessor.ExecuteTask_Test(task, executeTime);
				{
					var result = resultObj;
					Assert.IsNotNull(result);
					Assert.IsTrue(result.Executed);
					Assert.IsFalse(result.Error);
					DbQuery(s => task = s.Get<ScheduledTask>(task.Id));
					Assert.IsNull(task.Started);
					Assert.IsNotNull(task.Executed);
					Assert.AreEqual(0, task.ExceptionCount);


					//Confirm it's in PaymentSpring
					var t = PaymentSpringUtil.GetToken(org.Id);

					string jobId = result.Response;
					//Assert.AreEqual("1", jobId);
					var job = mock.GetJob(jobId);
					var expectedMethod = typeof(PaymentAccessor.Unsafe).GetMethod(nameof(PaymentAccessor.Unsafe.ChargeViaHangfire));
					Assert.AreEqual(expectedMethod, job.Method);

					//ChargeViaHangfire(long organizationId, long unchecked_taskId, bool forceUseTest, bool sendReceipt, DateTime executeTime);


					Assert.AreEqual(org.Id, job.Args[0]);
					Assert.AreEqual(task.Id, job.Args[1]);
					Assert.AreEqual(true, job.Args[2]);
					Assert.AreEqual(true, job.Args[3]);
					Assert.AreEqual(executeTime, job.Args[4]);

					//Try it again. Fails: already executed
					await ThrowsAsync<PermissionsException>(async () => {
						await PaymentAccessor.EnqueueChargeOrganizationFromTask(org.Id, task.Id, true, true, orgCreateTime);
					}, e => {
						Assert.AreEqual("Task was already executed.", e.Message);
					});
				}
				{
					MockHttpContext();
					var settings = new CreateUserOrganizationViewModel() {
						Email = "a@a.com",
						FirstName = "a",
						LastName = "a",
						OrgPositionId = null,
						ManagerNodeId = org.ManagerNode.Id
					};

					await JoinOrganizationAccessor.AddUser(caller, settings);



					//Try with newly created task
					task = resultObj.NewTasks.SingleOrDefault();
					Assert.IsNotNull(task);
					var result = await TaskAccessor.ExecuteTask_Test(task, executeTime.AddDays(31));
					string jobId = result.Response;
					var job = mock.GetJob(jobId);

					Assert.IsNotNull(result);
					Assert.IsTrue(result.Executed);
					Assert.IsFalse(result.Error);
					DbQuery(s => task = s.Get<ScheduledTask>(task.Id));
					Assert.IsNull(task.Started);
					Assert.IsNotNull(task.Executed);
					Assert.AreEqual(0, task.ExceptionCount);

					//Confirm it's in PaymentSpring
					var t = PaymentSpringUtil.GetToken(org.Id);


					Assert.AreEqual(org.Id, job.Args[0]);
					Assert.AreEqual(task.Id, job.Args[1]);
					Assert.AreEqual(true, job.Args[2]);
					Assert.AreEqual(true, job.Args[3]);
					Assert.AreEqual(executeTime.AddDays(31), job.Args[4]);

				}

				//Try it again. Fails: already executed
				await ThrowsAsync<PermissionsException>(async () => {
					await PaymentAccessor.EnqueueChargeOrganizationFromTask(org.Id, task.Id, true, true, orgCreateTime);
				}, e => {
					Assert.AreEqual("Task was already executed.", e.Message);
				});

			}
		}



	}
}
#pragma warning restore CS0618 // Type or member is obsolete