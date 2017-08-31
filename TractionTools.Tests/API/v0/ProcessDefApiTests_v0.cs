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
//using RadialReview.Areas.CoreProcess.Controllers.CoreProcess;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;

namespace TractionTools.Tests.Api {
    [TestClass]
    public class ProcessDefApiTests_v0 : BaseCoreProcessTest {

        #region start server
        private static Process Server;

        [ClassInitialize()]
        public static void Startup(TestContext ctx) {
            int exitCode;
            ProcessStartInfo processInfo;
            // Process process;

            var command = Config.GetAppSetting("CamundaServerBat");

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.WorkingDirectory = Directory.GetParent(command).FullName;
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;

            Server = Process.Start(processInfo);
            Thread.Sleep(10);
            
            ActOnProcessAndChildren(Server, x => MinimizeWindow(x.MainWindowHandle));
            Server.WaitForExit();
            ActOnProcessAndChildren(Server, x => MinimizeWindow(x.MainWindowHandle));

        }

        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void MinimizeWindow(IntPtr hwnd) {
            ShowWindow(hwnd, SW_MINIMIZE);
        }

        private static void ActOnProcessAndChildren(Process process,Action<Process> action) {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + process.Id);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc) {
                ActOnProcessAndChildren(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])),action);
            }
            try {
                action(process);
            } catch (ArgumentException) {
                // Process already exited.
            } catch (InvalidOperationException) {
                // Process already exited.
            }
        }

        [ClassCleanup]
        public static void Cleanup() {
            ActOnProcessAndChildren(Server,x=>x.Kill());
        }

        #endregion

        private ProcessDefAccessor processDefAccessor;


        [TestMethod]
        [TestCategory("CoreProcess")]
        public void TestEnsureApplicationExists() {
            ApplicationAccessor.EnsureApplicationExists();
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestCreateProcessDef() {
            var c = await Ctx.Build();
            var getResult = await CreateProcess(c);
            Assert.IsTrue(getResult > 0);
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestCreateTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestPublishProcess() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var getResult = await PublishProcess(c, getProcessDef);
            Assert.IsTrue(getResult);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestProcessStart() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            var getResult = await StartProcess(c, getProcessDef);
            Assert.IsNotNull(getResult);
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestTaskClaim() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await processDefAccessor.TaskClaim(c.E1, getTaskList[0].Id, c.E1.Id);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            Assert.AreEqual(getClaim, "u_" + c.E1.Id);
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestTaskUnClaim() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);
            await processDefAccessor.TaskClaim(c.E1, getTaskList[0].Id, c.E1.Id);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            await processDefAccessor.TaskUnClaim(c.E1, getTaskList[0].Id, c.E1.Id);
            var getTaskUnClaim = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getClaimUnclaim = getTaskUnClaim.Assignee;
            Assert.AreNotEqual(getClaim, getClaimUnclaim);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestAttachTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestDetachTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var deleteTask = await processDefAccessor.DeleteTask(c.E1, createTask.Id, getProcessDef);
            var getAllTask = await processDefAccessor.GetAllTask(c.E1, getProcessDef);
            Assert.IsTrue(getAllTask.Count == 0);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestGetListTaskForUser() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);
            await processDefAccessor.TaskClaim(c.E1, getTaskList[0].Id, c.E1.Id);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getListTaskForUser = await processDefAccessor.GetTaskListByUserId(c.E1, c.E1.Id.ToString());
            Assert.IsTrue(getListTaskForUser.Count > 0);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestGetTasksForCandidateGroup() {
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

            await processDefAccessor.TaskClaim(c.E1, getTaskList[0].Id, c.E1.Id);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            var getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, "", true);
            Assert.IsTrue(getTasksForCandidateGroup.Count > 0);

            getTasksForCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, "", false);

            Assert.IsTrue(getTasksForCandidateGroup.Count > 0);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestTaskComplete() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c);
            var createTask = await CreateTask(c, getProcessDef);
            var publishProcess = await PublishProcess(c, getProcessDef);
            await StartProcess(c, getProcessDef);
            processDefAccessor = new ProcessDefAccessor();
            var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await processDefAccessor.TaskClaim(c.E1, getTaskList[0].Id, c.E1.Id);
            await processDefAccessor.TaskComplete(c.E1, getTaskList[0].Id, c.E1.Id);
            var getTask = await processDefAccessor.GetTaskById(c.E1, getTaskList[0].Id);
            Assert.IsTrue(string.IsNullOrEmpty(getTask.Id));
        }

        private async Task<long> CreateProcess(Ctx ctx) {
            processDefAccessor = new ProcessDefAccessor();
            var getResult = await processDefAccessor.Create(ctx.E1, "Test Process Def");
            return getResult;
        }

        private async Task<TaskViewModel> CreateTask(Ctx ctx, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            TaskViewModel task = new TaskViewModel() { name = "Test Task", SelectedMemberId = new long[] { ctx.E1.Id } };
            var createTask = await processDefAccessor.CreateTask(ctx.E1, processDefId, task);
            return createTask;
        }

        private async Task<bool> PublishProcess(Ctx ctx, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            var publishProcess = await processDefAccessor.Deploy(ctx.E1, processDefId);
            return publishProcess;
        }

        private async Task<ProcessDef_Camunda> StartProcess(Ctx ctx, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            var startProcess = await processDefAccessor.ProcessStart(ctx.E1, processDefId);
            return startProcess;
        }

        private async Task<ProcessDef_Camunda> GetTaskList(Ctx ctx, long processDefId) {
            processDefAccessor = new ProcessDefAccessor();
            var startProcess = await processDefAccessor.ProcessStart(ctx.E1, processDefId);
            return startProcess;
        }

    }
}
