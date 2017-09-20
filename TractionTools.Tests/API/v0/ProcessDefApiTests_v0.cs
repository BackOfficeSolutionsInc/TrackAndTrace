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
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using RadialReview.Exceptions;
using NHibernate;
using System.Linq.Expressions;
using LambdaSerializer;
using RadialReview.Utilities.Hooks;
using RadialReview.Hooks;
using RadialReview.Utilities.Encrypt;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Areas.CoreProcess.CamundaComm;

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
            Thread.Sleep(2000);

            AsyncHelper.RunSync(() => {
                return new CommClass().DeleteAllProcess_Unsafe(TEST_PROCESS_DEF_NAME);
            });
        }

        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const string TEST_PROCESS_DEF_NAME = "Test Process Def 0312674BF3E3454B95916D394E780ED4";

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

        private ProcessDefAccessor pda;


        [TestMethod]
        [TestCategory("CoreProcess")]
        public void TestEnsureApplicationExists() {
            ApplicationAccessor.EnsureApplicationExists();
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestCreateProcessDef() {
            var c = await Ctx.Build();
            var getResult = await CreateProcess(c.E1);
            Assert.IsTrue(getResult > 0);
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestCreateTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestPublishProcess() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var getResult = await PublishProcess(c.E1, getProcessDef);
            Assert.IsTrue(getResult);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestProcessStart() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            var getResult = await StartProcess(c.E1, getProcessDef);
            Assert.IsNotNull(getResult);
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestTaskClaim() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            pda = new ProcessDefAccessor();
            var getProcessInstance = pda.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await pda.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await pda.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
            var getTask = await pda.GetTaskById_Unsafe( getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            Assert.AreEqual(getClaim, c.E1.Id);
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestTaskUnClaim() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            pda = new ProcessDefAccessor();
            var getProcessInstance = pda.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await pda.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);
            await pda.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
            var getTask = await pda.GetTaskById_Unsafe( getTaskList[0].Id);
            var getClaim = getTask.Assignee;
            await pda.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, false);
            var getTaskUnClaim = await pda.GetTaskById_Unsafe( getTaskList[0].Id);
            var getClaimUnclaim = getTaskUnClaim.Assignee;
            Assert.AreNotEqual(getClaim, getClaimUnclaim);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestAttachTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            Assert.IsTrue(!string.IsNullOrEmpty(createTask.Id));
        }

        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestDetachTask() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            pda = new ProcessDefAccessor();
            //confirmation of task create
            var getAllTaskList = await pda.GetAllTaskForProcessDefinition(c.E1, getProcessDef);
            var deleteTask = await pda.DeleteProcessDefTask(c.E1, createTask.Id, getProcessDef);
            var getAllTask = await pda.GetAllTaskForProcessDefinition(c.E1, getProcessDef);
            Assert.IsTrue(getAllTask.Count == 0);
        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestGetListTaskForUser() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            pda = new ProcessDefAccessor();
            var getProcessInstance = pda.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await pda.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            {
                // claim task
                await pda.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
                var getTask = await pda.GetTaskById_Unsafe(getTaskList[0].Id);
                var getListTaskForUser = await pda.GetTaskListByUserId(c.E1, c.E1.Id);

                Assert.AreEqual(1,getListTaskForUser.Count);
            }

            {
                // create task and assign to E2
                // make sure task2 does not belong to E1
                await CreateAnotherUserTask(c);
                var getListTaskForUser = await pda.GetTaskListByUserId(c.E1, c.E1.Id);
                Assert.AreEqual(1, getListTaskForUser.Count);
                Assert.AreEqual("Test Task", getListTaskForUser.First().name);

                var getListTaskForOtherUser = await pda.GetTaskListByUserId(c.E2, c.E2.Id);
                Assert.AreEqual(1, getListTaskForOtherUser.Count);
                Assert.AreEqual("Test other task", getListTaskForOtherUser.First().name);


            }


            {
                // unclaim task or detach task
                await pda.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, false);
                var getListTaskForUser = await pda.GetTaskListByUserId(c.E1, c.E1.Id);

                Assert.IsTrue(getListTaskForUser.Count == 0);
            }

        }

        private async Task CreateAnotherUserTask(Ctx c) {
            var getProcessDef = await CreateProcess(c.E2);
            var createTask = await CreateTask(c, c.E2, getProcessDef,name:"Test other task");
            var publishProcess = await PublishProcess(c.E2, getProcessDef);
            await StartProcess(c.E2, getProcessDef);
            pda = new ProcessDefAccessor();
            var getProcessInstance = pda.GetProcessInstanceList(c.E2, getProcessDef);
            var getTaskList = await pda.GetTaskListByProcessInstanceId(c.E2, getProcessInstance[0].Id);

            // claim task
            await pda.TaskClaimOrUnclaim(c.E2, getTaskList[0].Id, c.E2.Id, true);
            var getTask = await pda.GetTaskById_Unsafe( getTaskList[0].Id);
            var getListTaskForUser = await pda.GetTaskListByUserId(c.E2, c.E2.Id);
        }



        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestGetTasksForCandidateGroup() {
            var c = await Ctx.Build();
            var processE1 = await CreateProcess(c.E1);
            var processE5 = await CreateProcess(c.E1);
            var processTeam = await CreateProcess(c.E1);
            var createTaskE1 = await CreateTask(c, c.E1, processE1, name:"E1Task", groups: c.E1.Id);
            var createTaskE5 = await CreateTask(c, c.E1, processE5, name: "E5Task", groups: c.E5.Id);
            var createTaskTeam = await CreateTask(c, c.E1, processTeam, groups: c.Org.InterreviewTeam.Id);
            var publishProcessE1 = await PublishProcess(c.E1, processE1);
            var publishProcessE5 = await PublishProcess(c.E1, processE5);
            var publishProcessTeam = await PublishProcess(c.E1, processTeam);
            await StartProcess(c.E1, processE1);
            await StartProcess(c.E1, processE5);
            await StartProcess(c.E1, processTeam);

            pda = new ProcessDefAccessor();
            //var getProcessInstance = processDefAccessor.GetProcessInstanceList(c.E1, processE1);
            //var getTaskList = await processDefAccessor.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            var groupTasks = await pda.GetTaskListByCandidateGroups(c.E1, new long[] { c.E1.Id }, true);
            Assert.IsTrue(groupTasks.Count == 1);

            // allowing some knowing exception 
            await ThrowsAsync<PermissionsException>(async () => await pda.GetTaskListByCandidateGroups(c.E1, new long[] { c.E2.Id }, true));

            // All Unassigned
            groupTasks = await pda.GetTaskListByCandidateGroups(c.E2, new long[] { c.E2.Id }, true);
            Assert.IsTrue(groupTasks.Count == 0);

            groupTasks = await pda.GetTaskListByCandidateGroups(c.E5, new long[] { c.E5.Id }, true);
            Assert.IsTrue(groupTasks.Count == 2);

            groupTasks = await pda.GetTaskListByCandidateGroups(c.E6, new long[] { c.E6.Id }, true);
            Assert.IsTrue(groupTasks.Count == 1);

            groupTasks = await pda.GetTaskListByCandidateGroups(c.E5, new long[] { c.Org.InterreviewTeam.Id }, true);
            Assert.IsTrue(groupTasks.Count == 1);

            groupTasks = await pda.GetTaskListByCandidateGroups(c.E6, new long[] { c.Org.InterreviewTeam.Id }, true);
            Assert.IsTrue(groupTasks.Count == 1);

            await ThrowsAsync<PermissionsException>(async () => await pda.GetTaskListByCandidateGroups(c.E1, new long[] { c.Org.InterreviewTeam.Id }, true));

        }


        [TestMethod]
        [TestCategory("CoreProcess")]
        public async Task TestTaskComplete() {
            var c = await Ctx.Build();
            var getProcessDef = await CreateProcess(c.E1);
            var createTask = await CreateTask(c, c.E1, getProcessDef);
            var publishProcess = await PublishProcess(c.E1, getProcessDef);
            await StartProcess(c.E1, getProcessDef);
            pda = new ProcessDefAccessor();
            var getProcessInstance = pda.GetProcessInstanceList(c.E1, getProcessDef);
            var getTaskList = await pda.GetTaskListByProcessInstanceId(c.E1, getProcessInstance[0].Id);

            await pda.TaskClaimOrUnclaim(c.E1, getTaskList[0].Id, c.E1.Id, true);
            var getTaskforConfirmation = await pda.GetTaskById_Unsafe( getTaskList[0].Id);
            await pda.TaskComplete(c.E1, getTaskList[0].Id);

            //var getTask = await processDefAccessor.GetTaskById_Unsafe(c.E1, getTaskList[0].Id);
            await ThrowsAsync<PermissionsException>(async () => await pda.GetTaskById_Unsafe( getTaskList[0].Id));
            //Assert.IsTrue(string.IsNullOrEmpty(getTask.Id));
        }

        private async Task<long> CreateProcess(UserOrganizationModel user) {
            pda = new ProcessDefAccessor();
            var getResult = await pda.CreateProcessDef(user, TEST_PROCESS_DEF_NAME);
            return getResult;
        }

        private async Task<TaskViewModel> CreateTask(Ctx ctx, UserOrganizationModel user, long processDefId,string name = null, params long[] groups) {
            pda = new ProcessDefAccessor();

            if (groups.Length == 0)
                groups = new long[] { ctx.E1.Id, ctx.E2.Id, ctx.Manager.Id, ctx.Org.InterreviewTeam.Id };

            TaskViewModel task = new TaskViewModel() { name = name??"Test Task", SelectedMemberId = groups };
            var createTask = await pda.CreateProcessDefTask(user, processDefId, task);
            return createTask;
        }

        private async Task<bool> PublishProcess(UserOrganizationModel user, long processDefId) {
            pda = new ProcessDefAccessor();
            var publishProcess = await pda.Deploy(user, processDefId);
            return publishProcess;
        }

        private async Task<ProcessDef_Camunda> StartProcess(UserOrganizationModel user, long processDefId) {
            pda = new ProcessDefAccessor();
            var startProcess = await pda.ProcessStart(user, processDefId);
            return startProcess;
        }

        private async Task<ProcessDef_Camunda> GetTaskList(UserOrganizationModel user, long processDefId) {
            pda = new ProcessDefAccessor();
            var startProcess = await pda.ProcessStart(user, processDefId);
            return startProcess;
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
            string pwd = Config.SchedulerSecretKey() + userName;
            string encrypt_key = Crypto.EncryptStringAES(pwd, Config.SchedulerSecretKey());
            string _encrypt_key = Crypto.EncryptStringAES(pwd, Config.SchedulerSecretKey());

            Assert.AreNotEqual(encrypt_key, _encrypt_key);
        }
    }
}
