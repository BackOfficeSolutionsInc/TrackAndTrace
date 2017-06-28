using CamundaCSharpClient.Model.Deployment;
using RadialReview.Accessors;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Controllers;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess.Controllers {
	public class ProcessController : BaseController {
		ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
		// GET: CoreProcess/Home
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			List<ProcessViewModel> Process = new List<ProcessViewModel>();
			var sstr = processDefAccessor.GetList(GetUser());

			foreach (var item in sstr) {
				ProcessViewModel process = new ProcessViewModel();
				process.Id = item.Id;
				process.Name = item.ProcessDefKey;
                if(item.CamundaId == null)
                {
                    process.status = "undeployed";
                }
                else
                {
                    process.status = "deployed";
                }
                Process.Add(process);
			}


			return View(Process);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult Create(ProcessViewModel Modal) {
            var createprocess = processDefAccessor.Create(GetUser(), Modal.Name);
            return Json(ResultObject.SilentSuccess(Modal));
		}

        [Access(AccessLevel.UserOrganization)]
        public ActionResult GetProcess(long id)
        {

            List<TaskViewModel> str = new List<TaskViewModel>();

            str.Add(new TaskViewModel { Id = Guid.NewGuid(), name = "Task1", description = "Test Description1" });
            str.Add(new TaskViewModel { Id = Guid.NewGuid(), name = "Task2", description = "Test Description2" });
            str.Add(new TaskViewModel { Id = Guid.NewGuid(), name = "Task3", description = "Test Description3" });

            var sstr = processDefAccessor.GetById(GetUser(),id);


            ProcessViewModel process = new ProcessViewModel();
            process.taskList = str;
            process.Name = sstr.ProcessDefKey;
            process.Id = sstr.Id;
            return View("~/Areas/CoreProcess/Views/Process/Create.cshtml", process);
        }


        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult Create()
        {
            ProcessViewModel modal = new ProcessViewModel();
            return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateProcess.cshtml", modal);
        }

        [Access(AccessLevel.UserOrganization)]
		public JsonResult ReorderTask(int oldOrder, int newOrder,long Id) {
			//L10Accessor.ReorderPage(GetUser(),  oldOrder, newOrder);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public string Delete() {
			return "You are about to delete this meeting. Are you sure you want to continue?";
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Delete(long id) {
			//L10Accessor.DeleteL10(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateTask() {
			TaskViewModel task = new TaskViewModel();
			return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateTask.cshtml", task);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult EditTask(string id) {
			return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateTask.cshtml");
		}
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult CreateTask(TaskViewModel model) {			
			return Json(ResultObject.SilentSuccess(model));
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Edit(long id) {
            ProcessViewModel process = new ProcessViewModel();
            if (id != 0)
            {
                var EditProcess = processDefAccessor.GetById(GetUser(), id);
                process.Name = EditProcess.ProcessDefKey;
            }
            else
            {
                //var UpdateProcess = processDefAccessor.u(GetUser(), id);
                process.Name = "";
            }
			return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateProcess.cshtml", process);
        }
    }
}