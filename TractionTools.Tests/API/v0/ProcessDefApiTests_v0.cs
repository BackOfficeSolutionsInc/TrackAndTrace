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

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class ProcessDefAccessorTest_v0 : BaseTest
    {
        private ProcessDefAccessor processDefAccessor;


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestEnsureApplicationExists()
        {
            ApplicationAccessor.EnsureApplicationExists();
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateProcessDef()
        {
            var c = await Ctx.Build();
            var getResult = await CreateProcess(c);
            Assert.IsTrue(getResult > 0);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateTask()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestPublishProcess()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var getResult = await PublishProcess(c, getProcessDef);
            Assert.IsTrue(getResult);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestProcessStart()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            var getResult = await StartProcess(c, getProcessDef);
            Assert.IsNotNull(getResult);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestTaskClaim()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id,true);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            Assert.AreEqual(getClaim, "u_" + c.E1.Id);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestTaskUnClaim()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);
            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id,true);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id,false);
            var getTaskUnClaim = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getClaimUnclaim = getTaskUnClaim.Assignee;
            Assert.AreNotEqual(getClaim, getClaimUnclaim);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAttachTask()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestDetachTask()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var deleteTask = await processDefAccessor.DeleteProcessDefTask(c.E1, createTask.Id, getProcessDef);
            var getAllTask = await processDefAccessor.GetAllTaskForProcessDefinition(c.E1, getProcessDef);
            Assert.IsTrue(getAllTask.Count == 0);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetListTaskForUser()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);
            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id,true);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getListTaskForUser = await processDefAccessor.GetTaskListByUserId(c.E1, c.E1.Id.ToString());
            Assert.IsTrue(getListTaskForUser.Count > 0);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetTasksForCandidateGroup()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            //var createTask1 = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);

            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);
            //getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            //var getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, "", true);
            //getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, "", false);

            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id,true);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, true);
           // getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, "", false);

            Assert.IsTrue(getTasksForCandidateGroup.Count > 0);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestTaskComplete()
        {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await processDefAccessor.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id,true);
			var getTaskforConfirmation = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
			await processDefAccessor.TaskComplete(c.E1, getTaskList[0].Id, c.E1.Id);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            Assert.IsTrue(string.IsNullOrEmpty(getTask.Id));
        }

        private async Task<long> CreateProcess(Ctx ctx)
        {
            processDefAccessor = new ProcessDefAccessor();
            var getResult = await processDefAccessor.Create(ctx.E1, "Test Process Def");
            return getResult;
        }

        private async Task<TaskViewModel> CreateTask(Ctx ctx, long processDefId)
        {
            processDefAccessor = new ProcessDefAccessor();
            TaskViewModel task = new TaskViewModel() { name = "Test Task", SelectedMemberId = new long[] { ctx.E1.Id } };
            var createTask = await processDefAccessor.CreateProcessDefTask(ctx.E1, processDefId, task);
            return createTask;
        }

        private async Task<bool> PublishProcess(Ctx ctx, long processDefId)
        {
            processDefAccessor = new ProcessDefAccessor();
            var publishProcess = await processDefAccessor.Deploy(ctx.E1, processDefId);
            return publishProcess;
        }

        private async Task<ProcessDef_Camunda> StartProcess(Ctx ctx, long processDefId)
        {
            processDefAccessor = new ProcessDefAccessor();
            var startProcess = await processDefAccessor.ProcessStart(ctx.E1, processDefId);
            return startProcess;
        }

        private async Task<ProcessDef_Camunda> GetTaskList(Ctx ctx, long processDefId)
        {
            processDefAccessor = new ProcessDefAccessor();
            var startProcess = await processDefAccessor.ProcessStart(ctx.E1, processDefId);
            return startProcess;
        }

    }
}
