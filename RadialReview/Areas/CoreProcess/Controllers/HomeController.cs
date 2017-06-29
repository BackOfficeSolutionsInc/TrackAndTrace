using CamundaCSharpClient.Model.Deployment;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess.Controllers
{
    public class HomeController : BaseController
    {
        [Access(AccessLevel.Any)]
        // GET: CoreProcess/Home
        public async Task<ActionResult> Index()
        {
            //TaskAccessor taskAccessor = new TaskAccessor();
            //var getTaskList = taskAccessor.GetAllTasks(new RadialReview.Models.UserOrganizationModel());

            ProcessDefAccessor processDef = new ProcessDefAccessor();

            if (true)
            {
                //processDef.DetachNode();
            }

            //get ProcessDef By Key
            if (false)
            {
                var getProcessDef = processDef.GetProcessDefByKey(new RadialReview.Models.UserOrganizationModel(), "calculate");
            }


            //deploy process
            if (false)
            {
                //ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
                //string fileName = "calculation.bpmn";
                //var filePath = string.Format("~/Areas/CoreProcess/{0}", fileName);
                //var fullPath = HttpContext.Server.MapPath(filePath);
                //List<object> fileObject = new List<object>();
                //if (System.IO.File.Exists(fullPath))
                //{
                //    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                //    fileObject.Add(new FileParameter(bytes, fileName));

                //    //deploy file
                //    processDefAccessor.Deploy(new RadialReview.Models.UserOrganizationModel(), "testDeploy", fileObject);
                //}
            }


            //processDefAccessor.Deploy(new RadialReview.Models.UserOrganizationModel(), "testDeploy", new List<object> {
            //    FileParameter.FromManifestResource(Assembly.GetExecutingAssembly(), "RadialReview.calculation.bpmn") });


            //Upload files to server
            if (true)
            {
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
                //var res = processDef.ModifiyBpmnFile(GetUser(), "8cc02155-c3c0-4cfd-92be-2a93aa71fe23", "ce41ad72-121f-4ee4-9f67-4afe310fde49", "5f06e352-5c95-455d-b27a-253bb4472308");

            }




            //upload files to server


            //Deploy if required--

            return View();
        }
    }
}