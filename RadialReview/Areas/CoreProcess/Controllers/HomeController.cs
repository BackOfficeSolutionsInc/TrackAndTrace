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

namespace RadialReview.Areas.CoreProcess.Controllers {
	public class HomeController : BaseController {
		[Access(AccessLevel.Any)]
		// GET: CoreProcess/Home
		public async Task<ActionResult> Index() {
			//TaskAccessor taskAccessor = new TaskAccessor();
			//var getTaskList = taskAccessor.GetAllTasks(new RadialReview.Models.UserOrganizationModel());

			ProcessDefAccessor processDef = new ProcessDefAccessor();

			if (true) {
				processDef.DetachNode();
			}

			//get ProcessDef By Key
			if (false) {
				var getProcessDef = processDef.GetProcessDefByKey(new RadialReview.Models.UserOrganizationModel(), "calculate");
			}


			//deploy process
			if (false) {
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
			if (true) {
				//get processDef list
				var getProcessDefList = processDef.GetList(GetUser());


				//get processDef 
				var getProcessDef = processDef.GetById(GetUser(), getProcessDefList.FirstOrDefault().Id);

				//Create processDef
				//var getResult = processDef.Create(GetUser(), "TestPorcess1");

				//string guid = Guid.NewGuid().ToString();
				//var path = "CoreProcess/" + guid + ".bpmn";

				////create blank bmpn file

				//var getStream = processDef.CreateBmpnFile("testProcess");

				////upload to server
				//processDef.UploadCamundaFile(getStream, path);

				////get file from server
				//processDef.GetCamundaFileFromServer(path);

			}




			//upload files to server


			//Deploy if required--

			return View();
		}
	}
}