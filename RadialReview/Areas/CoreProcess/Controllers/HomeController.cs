
using CamundaCSharpClient.Model.Deployment;
using LambdaSerializer;
using NHibernate;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Controllers;
using RadialReview.Hooks;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess.Controllers {
    public class HomeController : BaseController {
        [Access(AccessLevel.Radial)]
        // GET: CoreProcess/Home
        public async Task<ActionResult> Index() {
            //TaskAccessor taskAccessor = new TaskAccessor();
            //var getTaskList = taskAccessor.GetAllTasks(new RadialReview.Models.UserOrganizationModel());

            try {
                ProcessDefAccessor processDef = new ProcessDefAccessor();

                ISession s = HibernateSession.GetCurrentSession();
                TodoModel todo = new TodoModel();
                //var task = HooksRegistry.Each<ITodoHook>(x => x.CreateTodo(s, todo));

                Expression<Func<User, bool>> lambda1 = x => x.Age > 20;

                var serializedLambda = JsonNetAdapter.Serialize(lambda1);
                var deserializedLambda = JsonNetAdapter.Deserialize<Expression<Func<User, bool>>>(serializedLambda);
                deserializedLambda.Compile();

                Expression<Func<ITodoHook, Task>> lambda = x => x.CreateTodo(null, todo);
                var serializedLambda1 = JsonNetAdapter.Serialize(lambda);
                var deserializedLambda1 = JsonNetAdapter.Deserialize<Expression<Func<IHook, Task>>>(serializedLambda1);

                deserializedLambda1.Compile();

                //await HooksRegistry.Each<ITodoHook>(deserializedLambda1);

                //await deserializedLambda1.Compile()(new TodoWebhook());


            } catch (Exception ex) {

                throw;
            }


            //string message = Newtonsoft.Json.JsonConvert.SerializeObject(task);

            //object taskObj = Newtonsoft.Json.JsonConvert.DeserializeObject(message);

            //Task t = (Task)taskObj;



            if (false) {
                TaskViewModel tskView = new TaskViewModel();
                tskView.Assignee = 1;
                tskView.description = "DescTest1";
                tskView.name = "NameTest1";
                tskView.Id = "Test1";

                //MessageQueueModel.CreateHookRegistryAction(tskView,new SerializableHook() { lambda= });

                MessageQueueModel t1 = new MessageQueueModel();
                t1.Identifier = Guid.NewGuid().ToString();
                t1.Model = tskView;
                t1.ModelType = "TaskViewModel";
                t1.UserName = GetUser().UserName;
                t1.UserOrgId = GetUser().Id;
                var result = await AmazonSQSUtility.SendMessage(t1);

                //AmazonSQS sqs = new AmazonSQS();
                //var msgRec = await sqs.ReceiveMessage();
                //string message = Newtonsoft.Json.JsonConvert.SerializeObject(t1);
                //string recieptHandler = "";
                //for (int i = 0; i < msgRec.Count; i++)
                //{
                //    if (msgRec[i].Body == message)
                //    {
                //        recieptHandler = msgRec[i].ReceiptHandle;
                //    }
                //}

                //var delResult = sqs.DeleteMessage(recieptHandler);

                //var claim = await processDef.GetCandidateGroupIdsForTask(GetUser(), "dd3114b2-6d28-11e7-9d1c-38d5471b275d");

                //CommClass commClass = new CommClass();
                //var getTask =await commClass.GetTaskByCandidateGroups("rgm_5", "9b59a3e7-6d25-11e7-9d1c-38d5471b275d");

                // var claim = await processDef.TaskClaim(GetUser(), "dd3114b2-6d28-11e7-9d1c-38d5471b275d", GetUser().Id.ToString());
                // var taskList =await processDef.GetTaskListByUserId(GetUser(), GetUser().Id.ToString());
                //
                // var unClaim = await processDef.TaskUnClaim(GetUser(), "dd3114b2-6d28-11e7-9d1c-38d5471b275d", GetUser().Id.ToString());
                // var setAssignee = await processDef.TaskAssignee(GetUser(), "dd3114b2-6d28-11e7-9d1c-38d5471b275d", GetUser().Id.ToString());
                // var complete = await processDef.TaskComplete(GetUser(), "dd3114b2-6d28-11e7-9d1c-38d5471b275d", GetUser().Id.ToString());


                //processDef.DetachNode();
            }

            //get ProcessDef By Key
            if (false) {
                // var getProcessDef = processDef.GetProcessDefByKey(new RadialReview.Models.UserOrganizationModel(), "calculate");
            }


            //deploy process
            if (true) {

                //string fileName = "calculation.bpmn";
                //var filePath = string.Format("~/Areas/CoreProcess/{0}", fileName);
                //var fullPath = HttpContext.Server.MapPath(filePath);
                //List<object> fileObject = new List<object>();
                //if (System.IO.File.Exists(fullPath))
                //{
                //    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                //    fileObject.Add(new FileParameter(bytes, fileName));


                //    CommClass commClass = new CommClass();
                //    var getDeploymentId = commClass.Deploy("testDeploy1", fileObject);
                //    //deploy file
                //    //processDefAccessor.Deploy(new RadialReview.Models.UserOrganizationModel(), "testDeploy", fileObject);
                //}
            }


            //processDefAccessor.Deploy(new RadialReview.Models.UserOrganizationModel(), "testDeploy", new List<object> {
            //    FileParameter.FromManifestResource(Assembly.GetExecutingAssembly(), "RadialReview.calculation.bpmn") });


            //Upload files to server
            if (true) {
                //get processDef list
                //var getProcessDefList = processDef.GetList(GetUser());


                //get processDef 
                //var getProcessDef = processDef.GetById(GetUser(), getProcessDefList.FirstOrDefault().Id);

                ////create processdef
                //var getresult = processDef.Create(GetUser(), "testporcess2");

                //string guid = Guid.NewGuid().ToString();
                //var path = "coreprocess/" + guid + ".bpmn";

                //create blank bmpn file

                //var getstream = processDef.CreateBpmnFile("testprocess2");

                ////upload to server
                ////processDef.UploadCamundaFile(getstream, path);
                //processDef.UploadCamundaFile(getstream, "CoreProcess/42fcf2e7-db75-4ea9-883f-349443cfa5df.bpmn");

                ////get file from server
                //processDef.GetCamundaFileFromServer("CoreProcess/42fcf2e7-db75-4ea9-883f-349443cfa5df.bpmn");

                //create task
                //var task = processDef.CreateTask(GetUser(), "8cc02155-c3c0-4cfd-92be-2a93aa71fe23", new Models.Process.TaskViewModel() { name = "test task" });

                ////Update task
                //var task = processDef.UpdateTask(GetUser(), "8cc02155-c3c0-4cfd-92be-2a93aa71fe23", new Models.Process.TaskViewModel() {
                //    Id = Guid.Parse("5f06e352-5c95-455d-b27a-253bb4472308"),
                //    name = "test task1",description="test" });


                //get element
                //var getTaskList = processDef.EditTask(GetUser(), "8cc02155-c3c0-4cfd-92be-2a93aa71fe23");

                ////edit process
                //var edit = processDef.Edit(GetUser(), "8cc02155-c3c0-4cfd-92be-2a93aa71fe23", "final test");

                //Modifiy
                //var res = processDef.ModifiyBpmnFile(GetUser(), "8cc02155-c3c0-4cfd-92be-2a93aa71fe23", 1, 0);

            }




            //upload files to server


            //Deploy if required--

            return View();
        }
    }
}
