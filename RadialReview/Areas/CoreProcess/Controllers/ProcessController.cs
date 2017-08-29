using CamundaCSharpClient.Model.Deployment;
using CamundaCSharpClient.Model.ProcessInstance;
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
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess.Controllers {
	public class ProcessController : BaseController {
		ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
		// GET: CoreProcess/Home
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			List<ProcessViewModel> Process = new List<ProcessViewModel>();

			var sstr = processDefAccessor.GetProcessDefinitionList(GetUser(), GetUser().Organization.Id);

			PermissionsAccessor obj = new PermissionsAccessor();
			ViewBag.CanCreate = obj.IsPermitted(GetUser(), x => x.CreateProcessDef());
			//ViewBag.CanEdit = obj.IsPermitted(GetUser(), x => x.EditProcessDef(id));
			//ViewBag.CanAdmin = obj.IsPermitted(GetUser(), x => x.CanAdminProcessDef(id));

			foreach (var item in sstr) {
				var List = processDefAccessor.GetProcessInstanceList(GetUser(), item.LocalId);
				ProcessViewModel process = new ProcessViewModel(item, List.Count);

				//process.Id = item.Id;
				//process.Name = item.ProcessDefKey;
				////process.IsStarted = status;
				//if (item.CamundaId == null) {
				//	process.Count = 0;
				//	process.status = "<div style='color:red'><i class='fa fa-2x fa-times-circle'></i></ div>";
				//} else {
				//	//process.Count = "1";
				//	var List = processDefAccessor.GetProcessInstanceList(item.LocalId);
				//	process.Count = List.Count();
				//	process.status = "<div style='color:green'><i class='fa fa-2x fa-check-circle'></i></ div>";
				//}
				// add logic for Process Start status
				//if (process.IsStarted) {
				//	process.Action = "Stop";
				//} else {
				//	process.Action = "Start";
				//}
				//process.LocalID = item.LocalId;

				Process.Add(process);
			}

			return View(Process);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> StartProcess(long id) {
			var StartProcess = await processDefAccessor.ProcessStart(GetUser(), id);
			var result = ResultObject.Create(new ProcessViewModel(StartProcess, processDefAccessor.GetProcessInstanceList(GetUser(), StartProcess.LocalId).Count), "Process started successfully.");
			return Json(result, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> Create(ProcessViewModel Modal) {
			var id = await processDefAccessor.Create(GetUser(), Modal.Name);
			Modal.Id = id;
			Modal.status = "<div style='color:red'><i class='fa fa-2x fa-times-circle'></i></ div>";

			return Json(ResultObject.SilentSuccess(Modal));
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Tasks(long id) {
			var sstr = processDefAccessor.GetProcessDefById(GetUser(), id);
			var tasKList = await processDefAccessor.GetAllTaskForProcessDefinition(GetUser(), sstr.LocalId);

			PermissionsAccessor obj = new PermissionsAccessor();
			ViewBag.CanEdit = obj.IsPermitted(GetUser(), x => x.EditProcessDef(id));
			ViewBag.CanAdmin = obj.IsPermitted(GetUser(), x => x.CanAdminProcessDef(id));

			ProcessViewModel process = new ProcessViewModel();
			process.taskList = tasKList;
			process.Name = sstr.ProcessDefKey;
			process.Id = sstr.Id;
			process.LocalID = sstr.LocalId;
			return View(process);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Create() {
			ProcessViewModel modal = new ProcessViewModel();
			return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateProcess.cshtml", modal);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> ReorderTask(long id, int oldOrder, int newOrder) {
			await processDefAccessor.ModifiyBpmnFile(GetUser(), id, oldOrder, newOrder);
			//L10Accessor.ReorderPage(GetUser(),  oldOrder, newOrder);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> DeleteTask(string id, long localId) // id is taskId
		{
			await processDefAccessor.DeleteProcessDefTask(GetUser(), id, localId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Delete(long id) // id is processid
		{
			var Process = processDefAccessor.GetProcessDefById(GetUser(), id);
			processDefAccessor.DeleteProcess(GetUser(), id);
			return Json(ResultObject.SilentSuccess(Process), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Publish(long id) {
			if (id > 0) {
				await processDefAccessor.Deploy(GetUser(), id);
				return RedirectToAction("Index");
			} else {
				var result = ResultObject.CreateError("Task Not Added", null);
				return Json(result, JsonRequestBehavior.AllowGet);
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateTask(long id) {
			TaskViewModel task = new TaskViewModel();
			task.process = new ProcessViewModel();
			task.process.LocalID = id;

			return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateTask.cshtml", task);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> EditTask(string id, long localId) {
			var list = await processDefAccessor.GetAllTaskForProcessDefinition(GetUser(), localId);
			var task = list.Where(m => m.Id == id).FirstOrDefault();
			task.process = new ProcessViewModel();
			task.process.LocalID = localId;
			task.Id = id;
			return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateTask.cshtml", task);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> EditTask(TaskViewModel model, string id, long localId) {
			var updatetask = await processDefAccessor.UpdateTask(GetUser(), localId, model);
			return Json(ResultObject.SilentSuccess(updatetask));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> CreateTask(TaskViewModel model, long localId) {
			var create = await processDefAccessor.CreateProcessDefTask(GetUser(), localId, model);

			model.process = new ProcessViewModel();
			model.process.LocalID = localId;

			return Json(ResultObject.SilentSuccess(model));
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Edit(long id) {
			ProcessViewModel process = new ProcessViewModel();
			if (id != 0) {
				var EditProcess = processDefAccessor.GetProcessDefById(GetUser(), id);
				process.Name = EditProcess.ProcessDefKey;
				process.LocalID = EditProcess.LocalId;
			}

			return PartialView("~/Areas/CoreProcess/Views/Shared/Partial/CreateProcess.cshtml", process);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> Edit(ProcessViewModel Model) {
			var updateProcess = await processDefAccessor.EditProcess(GetUser(), Model.LocalID, Model.Name);
			return Json(ResultObject.SilentSuccess(Model));
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult ProcessInstance(long id) // id is processid
		{
			ProcessInstanceViewModel processinstance = new ProcessInstanceViewModel();
			var List = processDefAccessor.GetProcessInstanceList(GetUser(), id);

			PermissionsAccessor obj = new PermissionsAccessor();
			ViewBag.CanEdit = obj.IsPermitted(GetUser(), x => x.EditProcessDef(id));

			return View(List);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Suspend(long id, string status) {
			ProcessInstanceViewModel processinstance = new ProcessInstanceViewModel();
			if (status == "true") {
				var Suspend = await processDefAccessor.SuspendProcess(GetUser(), id, false);
			} else {
				var Suspend = await processDefAccessor.SuspendProcess(GetUser(), id, true);
			}
			return RedirectToAction("ProcessInstance", "Process", new { @id = id });
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> UserTask() {
			List<TaskViewModel> model = new List<TaskViewModel>();
			var usertask = await processDefAccessor.GetTaskListByUserId(GetUser(), Convert.ToString(GetUser().Id));
			var getUserTaskByCandidateGroup = await processDefAccessor.GetTaskListByCandidateGroups(GetUser(), new long[] { GetUser().Id });

			if (usertask.Count == 0) {
				// create task for user
				string taskId = "5bb68dfa-7142-11e7-9964-54bef737c7d9";

				var result = await processDefAccessor.TaskAssignee(GetUser(), taskId, GetUser().Id);

				usertask = await processDefAccessor.GetTaskListByUserId(GetUser(), Convert.ToString(GetUser().Id));
			}

			var modelUserTask = new Tuple<List<TaskViewModel>, List<TaskViewModel>>(usertask, getUserTaskByCandidateGroup);
			return View(modelUserTask);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> AssignTask(string id) {
			var result = await processDefAccessor.TaskAssignee(GetUser(), id, GetUser().Id);
			return RedirectToAction("UserTask");
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> UnclaimTask(string id) {
			var result = await processDefAccessor.TaskClaimOrUnclaim(GetUser(), id, GetUser().Id, false);
			return RedirectToAction("UserTask");
		}

	}
}