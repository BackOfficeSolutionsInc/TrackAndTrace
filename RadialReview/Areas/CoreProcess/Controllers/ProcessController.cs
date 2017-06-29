﻿using CamundaCSharpClient.Model.Deployment;
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

namespace RadialReview.Areas.CoreProcess.Controllers
{
    public class ProcessController : BaseController
    {
        ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
        // GET: CoreProcess/Home
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            List<ProcessViewModel> Process = new List<ProcessViewModel>();
            var sstr = processDefAccessor.GetList(GetUser());

            foreach (var item in sstr)
            {
                ProcessViewModel process = new ProcessViewModel();
                process.Id = item.Id;
                process.Name = item.ProcessDefKey;
                if (item.CamundaId == null)
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
        public JsonResult Create(ProcessViewModel Modal)
        {
            var id = processDefAccessor.Create(GetUser(), Modal.Name);
            Modal.Id = id;

            return Json(ResultObject.SilentSuccess(Modal));
        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult Tasks(long id)
        {

            var sstr = processDefAccessor.GetById(GetUser(), id);
            var tasKList = processDefAccessor.GetAllTask(GetUser(), sstr.LocalId);

            ProcessViewModel process = new ProcessViewModel();
            process.taskList = tasKList;
            process.Name = sstr.ProcessDefKey;
            process.Id = sstr.Id;
            process.LocalID = sstr.LocalId;
            return View(process);
        }


        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult Create()
        {
            ProcessViewModel modal = new ProcessViewModel();
            return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateProcess.cshtml", modal);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult ReorderTask(string id, int oldOrder, int newOrder)
        {
            processDefAccessor.ModifiyBpmnFile(GetUser(), id, oldOrder, newOrder);
            //L10Accessor.ReorderPage(GetUser(),  oldOrder, newOrder);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        [Access(AccessLevel.UserOrganization)]
        public string Delete()
        {
            return "You are about to delete this meeting. Are you sure you want to continue?";
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult Delete(long id)
        {
            //L10Accessor.DeleteL10(GetUser(), id);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateTask(string id)
        {
            TaskViewModel task = new TaskViewModel();
            task.process = new ProcessViewModel();
            task.process.LocalID = id;
            return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateTask.cshtml", task);
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult EditTask(string id, string localid)
        {
            var list = processDefAccessor.GetAllTask(GetUser(), localid);
            var task = list.Where(m => m.Id == id).FirstOrDefault();
            task.process = new ProcessViewModel();
            task.process.LocalID = localid;
            task.Id = id;
            return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateTask.cshtml", task);
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public JsonResult EditTask(TaskViewModel model, string id, string localid)
        {
            var updatetask = processDefAccessor.UpdateTask(GetUser(), localid, model);
            return Json(ResultObject.SilentSuccess(updatetask));
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public JsonResult CreateTask(TaskViewModel model, string LocalID)
        {
            var create = processDefAccessor.CreateTask(GetUser(), LocalID, model);

            model.process = new ProcessViewModel();
            model.process.LocalID = LocalID;

            return Json(ResultObject.SilentSuccess(model));
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult Edit(long id)
        {
            ProcessViewModel process = new ProcessViewModel();
            if (id != 0)
            {
                var EditProcess = processDefAccessor.GetById(GetUser(), id);
                process.Name = EditProcess.ProcessDefKey;
                process.LocalID = EditProcess.LocalId;
            }

            return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateProcess.cshtml", process);
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public JsonResult Edit(ProcessViewModel Model)
        {
            var updateProcess = processDefAccessor.Edit(GetUser(), Model.LocalID, Model.Name);
            return Json(ResultObject.SilentSuccess(Model));
        }
    }
}