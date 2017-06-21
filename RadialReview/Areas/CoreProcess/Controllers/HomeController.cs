using RadialReview.Areas.CoreProcess.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess.Controllers
{
    public class HomeController : Controller
    {
        // GET: CoreProcess/Home
        public ActionResult Index()
        {
            TaskAccessor taskAccessor = new TaskAccessor();
            var getTaskList = taskAccessor.GetAllTasks(new RadialReview.Models.UserOrganizationModel());
            return View();
        }
    }
}