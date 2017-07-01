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

namespace TractionTools.Tests.Accessors {
	[TestClass]
	public class PaymentAccessorTests : BaseTest {


#pragma warning disable CS0618 // Type or member is obsolete
		[TestMethod]
		public async Task ChargeOrganization() {

			var now = DateTime.UtcNow;
			var orgCreateTime = now;
			var executeTime = orgCreateTime.AddDays(60);

			var org = await OrgUtil.CreateOrganization(time: orgCreateTime);

			var caller = org.Manager;

			//for (var i = 0; i < 4; i++) {
			//	JoinOrganizationAccessor.AddUser(caller, org.ManagerNode, "a", "a", "a@a.com", null);
			//}			
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
				await PaymentAccessor.ChargeOrganizationAmount(org.Id, 1, true);
			}, e => {
				Assert.AreEqual(PaymentExceptionType.MissingToken, e.Type);
			});


			//Has token, payment failed
			var token = await PaymentAccessor.GenerateFakeCard("ChargeOrganization 1");
			await PaymentAccessor.SetCard(caller, org.Id, token);
			await ThrowsAsync<PaymentException>(async () => {
				await PaymentAccessor.ChargeOrganizationAmount(org.Id, 0.20m, true);
			}, e => {
				Assert.AreEqual(PaymentExceptionType.ResponseError, e.Type);
				Assert.AreEqual("Transaction declined (99820).", e.Message);
			});
			Assert.IsNull(task.Started);
			Assert.IsNull(task.Executed);
			Assert.AreEqual(0, task.ExceptionCount);

			await ThrowsAsync<PermissionsException>(async () => {
				await PaymentAccessor.ChargeOrganization(org.Id, task.Id, true, true, executeTime);
			}, e => {
				Assert.AreEqual("Task was not started.", e.Message);
			});

			//Run it 
			var result = await TaskAccessor.ExecuteTask_Test(task, executeTime);
			//var taskResult = results.Where(x => x.TaskId == task.Id).SingleOrDefault();

			Assert.IsNotNull(result);
			Assert.IsTrue(result.Executed);
			Assert.IsFalse(result.Error);
			DbQuery(s => task = s.Get<ScheduledTask>(task.Id));
			Assert.IsNull(task.Started);
			Assert.IsNotNull(task.Executed);
			Assert.AreEqual(0, task.ExceptionCount);


			//Confirm it's in PaymentSpring
			var t = PaymentSpringUtil.GetToken(org.Id);
			//var logs = await PaymentSpringUtil.GetAllLogs(true, 1);
			var log = result.Response;
		//	var log = logs.Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.IsNotNull(log);
			Assert.AreEqual(t.CustomerToken, log.customer_id);
			Assert.AreEqual("SETTLED",log.status);
			Assert.AreEqual("transaction", log.@class);
			Assert.AreEqual(0, log.amount_refunded);
			Assert.AreEqual(14900, log.amount_settled);

			//Try it again. Fails: already executed
			await ThrowsAsync<PermissionsException>(async () => {
				await PaymentAccessor.ChargeOrganization(org.Id, task.Id, true, true, orgCreateTime);
			}, e => {
				Assert.AreEqual("Task was already executed.", e.Message);
			});

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
			task = result.NewTasks.SingleOrDefault();
			Assert.IsNotNull(task);
			result = await TaskAccessor.ExecuteTask_Test(task, executeTime.AddDays(31));
			Assert.IsNotNull(result);
			Assert.IsTrue(result.Executed);
			Assert.IsFalse(result.Error);
			DbQuery(s => task = s.Get<ScheduledTask>(task.Id));
			Assert.IsNull(task.Started);
			Assert.IsNotNull(task.Executed);
			Assert.AreEqual(0, task.ExceptionCount);

			//Confirm it's in PaymentSpring
			t = PaymentSpringUtil.GetToken(org.Id);
			log = result.Response;
			//logs = await PaymentSpringUtil.GetAllLogs(true, 1);
			//log = logs.Where(x => x.action == "/api/v1/charge").OrderByDescending(x => x.date).FirstOrDefault();
			Assert.IsNotNull(log);
			Assert.AreEqual(t.CustomerToken, log.customer_id);
			Assert.AreEqual("SETTLED", log.status);
			Assert.AreEqual("transaction", log.@class);
			Assert.AreEqual(0, log.amount_refunded);
			Assert.AreEqual(15100, log.amount_settled);

			//Try it again. Fails: already executed
			await ThrowsAsync<PermissionsException>(async () => {
				await PaymentAccessor.ChargeOrganization(org.Id, task.Id, true, true, orgCreateTime);
			}, e => {
				Assert.AreEqual("Task was already executed.", e.Message);
			});


		}
	}
}
#pragma warning restore CS0618 // Type or member is obsolete