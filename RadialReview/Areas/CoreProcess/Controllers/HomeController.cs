using CamundaCSharpClient.Model.Deployment;
using RadialReview.Areas.CoreProcess.Accessors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess.Controllers
{
    public class HomeController : Controller
    {
        // GET: CoreProcess/Home
        public ActionResult Index()
        {
            //TaskAccessor taskAccessor = new TaskAccessor();
            //var getTaskList = taskAccessor.GetAllTasks(new RadialReview.Models.UserOrganizationModel());

            ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
            string fileName = "calculation.bpmn";
            var filePath = string.Format("~/Areas/CoreProcess/{0}", fileName);
            var fullPath = HttpContext.Server.MapPath(filePath);
            List<object> fileObject = new List<object>();
            if (System.IO.File.Exists(fullPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                fileObject.Add(new FileParameter(bytes, fileName));

                //deploy file
                processDefAccessor.Deploy(new RadialReview.Models.UserOrganizationModel(), "testDeploy", fileObject);
            }



            //processDefAccessor.Deploy(new RadialReview.Models.UserOrganizationModel(), "testDeploy", new List<object> {
            //    FileParameter.FromManifestResource(Assembly.GetExecutingAssembly(), "RadialReview.calculation.bpmn") });


            return View();
        }
    }
}