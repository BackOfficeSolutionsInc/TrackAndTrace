using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Todo;
using RadialReview.Utilities.DataTypes;
using System.Collections.Generic;
using RadialReview.Utilities;
using RadialReview.Models.Enums;
using TractionTools.Tests.TestUtils;
using RadialReview.Models.L10;
using RadialReview.Models;
using System.Linq;
using RadialReview.Api.V0;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Todos;
using RadialReview.Controllers;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Scorecard;
using static RadialReview.Controllers.L10Controller;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Areas.CoreProcess.Controllers;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.CoreProcess.Controllers.Api_V0;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Exceptions;
using NHibernate;
using System.Linq.Expressions;
using LambdaSerializer;
using RadialReview.Utilities.Hooks;
using RadialReview.Hooks;
using RadialReview.Utilities.Encrypt;

namespace TractionTools.Tests.Api {
    [TestClass]
    public class ProcessDefAccessorTest_v0 : BaseTest {
        private ProcessDefAccessor processDefAccessor;


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestEnsureApplicationExists() {
            ApplicationAccessor.EnsureApplicationExists();
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateProcessDef() {
            var c = await Ctx.Build();
            var getResult = await CreateProcess(c.E1);
            Assert.IsTrue(getResult > 0);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestPublishProcess() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var getResult = await PublishProcess(c.E1, getProcessDef);
            Assert.IsTrue(getResult);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestProcessStart() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            var getResult = await StartProcess(c.E1, getProcessDef);
            Assert.IsNotNull(getResult);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestTaskClaim() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
            var getTask = await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            Assert.AreEqual(getClaim, "u_" + c.E1.Id);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestTaskUnClaim() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);
            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
            var getTask = await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, false);
            var getTaskUnClaim = await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id);
            var getClaimUnclaim = getTaskUnClaim.Assignee;
            Assert.AreNotEqual(getClaim, getClaimUnclaim);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAttachTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestDetachTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            //confirmation of task create
            var getAllTaskList = await processDefAccessor.GetAllTaskForProcessDefinition(c.E1, getProcessDef);
            var deleteTask = await processDefAccessor.DeleteProcessDefTask(c.E1, createTask.Id, getProcessDef);
            var getAllTask = await processDefAccessor.GetAllTaskForProcessDefinition(c.E1, getProcessDef);
            Assert.IsTrue(getAllTask.Count == 0);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetListTaskForUser() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            {
                // claim task
                await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
                var getTask = await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id);
                var getListTaskForUser = await processDefAccessor.GetTaskListByUserId(c.E1, c.E1.Id);

                Assert.IsTrue(getListTaskForUser.Count > 0);
            }

            {
                // create task and assign to E2
                // make sure task2 does not belong to E1
                await CreateAnotherUserTask(c);
                var getListTaskForUser = await processDefAccessor.GetTaskListByUserId(c.E1, c.E1.Id);
            }


            {
                // unclaim task or detach task
                await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, false);
                var getListTaskForUser = await processDefAccessor.GetTaskListByUserId(c.E1, c.E1.Id);

                Assert.IsTrue(getListTaskForUser.Count == 0);
            }

        }

        private async Task CreateAnotherUserTask(Ctx c) {
            var getProcessDef = await CreateProcess(c.E2);
            var createTask = await CreateTask(c, c.E2, getProcessDef);
            var publishProcess = await PublishProcess(c.E2, getProcessDef);
            await StartProcess(c.E2, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E2, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E2, getProcessInstance[0].Id);

            // claim task
            await processDefAccessor.TaskClaimOrUnclaim(c.E2, getTaskList[0].Id, c.E2.Id, true);
            var getTask = await processDefAccessor.GetTaskById_Unsafe(c.E2, getTaskList[0].Id);
            var getListTaskForUser = await processDefAccessor.GetTaskListByUserId(c.E2, c.E2.Id);
        }



        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetTasksForCandidateGroup() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);

            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            var getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, true);
            Assert.IsTrue(getTasksForCandidateGroup.Count > 0);

            // allowing some knowing exception 
            await ThrowsAsync<PermissionsException>(async () => await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E2.Id }, true));

            // check for second user
            getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E2, new long[] { c.E2.Id }, true);
            Assert.IsTrue(getTasksForCandidateGroup.Count == 0);

            getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, false);
            Assert.IsTrue(getTasksForCandidateGroup.Count > 0);

            getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.Manager.Id }, false);
            Assert.IsTrue(getTasksForCandidateGroup.Count > 0);

            getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.Org.InterreviewTeam.Id }, false);
            Assert.IsTrue(getTasksForCandidateGroup.Count > 0);

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestTaskComplete() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
            var getTaskforConfirmation = await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id);
            await processDefAccessor.TaskComplete(c.E1, getTaskList[0].Id, c.E1.Id);

            //var getTask = await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id);
            await ThrowsAsync<PermissionsException>(async () => await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id));
            //Assert.IsTrue(string.IsNullOrEmpty(getTask.Id));
        }

        private async Task<long> CreateProcess(UserOrganizationModel user) {
            processDefAccessor = new ProcessDefAccessor();
            var getResult = await processDefAccessor.Create(user, "Test Process Def");
            return getResult;
        }

        private async Task<TaskViewModel> CreateTask(Ctx ctx, UserOrganizationModel user, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            TaskViewModel task = new TaskViewModel() { name = "Test Task", SelectedMemberId = new long[] { ctx.E1.Id, ctx.E2.Id, ctx.Manager.Id, ctx.Org.InterreviewTeam.Id } };
            var createTask = await processDefAccessor.CreateProcessDefTask(user, processDefId, task);
            return createTask;
        }

        private async Task<bool> PublishProcess(UserOrganizationModel user, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            var publishProcess = await processDefAccessor.Deploy(user, processDefId);
            return publishProcess;
        }

        private async Task<ProcessDef_Camunda> StartProcess(UserOrganizationModel user, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            var startProcess = await processDefAccessor.ProcessStart(user, processDefId);
            return startProcess;
        }

        private async Task<ProcessDef_Camunda> GetTaskList(UserOrganizationModel user, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            var startProcess = await processDefAccessor.ProcessStart(user, processDefId);
            return startProcess;
        }

        public class SerializableHook {
            public object lambda { get; set; }
            public Type type { get; set; }
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestWebhook() {

            //var c = await Ctx.Build();

            HooksRegistry.RegisterHook(new TodoWebhook());

            try {
                ProcessDefAccessor processDef = new ProcessDefAccessor();

                ISession s = HibernateSession.GetCurrentSession();
                TodoModel todo = new TodoModel();
                //var task = HooksRegistry.Each<ITodoHook>(x => x.CreateTodo(s, todo));                

                Expression<Func<ITodoHook, Task>> lambda = x => x.CreateTodo(null, todo);
                SerializableHook obj = new SerializableHook();
                obj.lambda = lambda;
                obj.type = lambda.GetType();

                var serializedLambda1 = JsonNetAdapter.Serialize(obj);
                var deserializedLambda1 = JsonNetAdapter.Deserialize<SerializableHook>(serializedLambda1);

                dynamic func = JsonNetAdapter.Deserialize(deserializedLambda1.lambda.ToString(), deserializedLambda1.type);

                await HooksRegistry.Each<ITodoHook>(func);

            } catch (Exception ex) {

                throw;
            }

            //Assert.IsTrue(string.IsNullOrEmpty(getTask.Id));
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestEncryptedPassword() {
            string userName = "Test";
            string pwd = RadialReview.Utilities.Config.GetAppSetting("AMZ_secretkey").ToString() + userName;
            string encrypt_key = Crypto.EncryptStringAES(pwd, RadialReview.Utilities.Config.GetAppSetting("AMZ_secretkey").ToString());
            string _encrypt_key = Crypto.EncryptStringAES(pwd, RadialReview.Utilities.Config.GetAppSetting("AMZ_secretkey").ToString());

            Assert.AreNotEqual(encrypt_key, _encrypt_key);
        }
    }
}
