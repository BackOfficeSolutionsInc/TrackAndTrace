using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CamundaCSharpClient.Model;
using CamundaCSharpClient.Model.Deployment;
using CamundaCSharpClient.Model.Task;
using log4net;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Areas.CoreProcess.Models.ViewModel;
using RadialReview.Exceptions;
using RadialReview.Hooks;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CoreProcess;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Application;
using RadialReview.Models.Components;
using RadialReview.Utilities;
using RadialReview.Utilities.CoreProcess;
using RadialReview.Utilities.Hooks;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using static CamundaCSharpClient.Query.Task.TaskQuery;
using static RadialReview.Utilities.CoreProcess.BpmnUtility;

namespace RadialReview.Areas.CoreProcess.Accessors {
    public class ProcessDefAccessor : IProcessDefAccessor {

        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async Task<bool> Deploy(UserOrganizationModel caller, long processDefId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var deployed = await Deploy(s, perms, processDefId);
                    tx.Commit();
                    s.Flush();
                    return deployed;
                }
            }
        }
        public async Task<bool> Deploy(ISession s, PermissionsUtility perms, long processDefId) {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, processDefId);

            bool result = false;
            var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == processDefId).SingleOrDefault();
            if (getProcessDefFileDetails == null) {
                throw new PermissionsException("Process does not exist.");
            }
            var getfileStream = await BpmnUtility.GetFileFromServer(getProcessDefFileDetails.FileKey);


            List<object> fileObjects = new List<object>();
            byte[] bytes = ((MemoryStream)getfileStream).ToArray();
            fileObjects.Add(new FileParameter(bytes, getProcessDefFileDetails.FileKey.Split('/')[1].Replace("-", "")));

            var processDefDetail = s.Get<ProcessDef_Camunda>(processDefId);
            if (processDefDetail == null || processDefDetail.DeleteTime != null) {
                throw new PermissionsException("Process doesn't exist.");
            }

            string deplyomentName = processDefDetail.ProcessDefKey;

            // call Comm Layer
            ICommClass commClass = CommFactory.Get();
            var deploymentId = await commClass.Deploy(deplyomentName, fileObjects);

            getProcessDefFileDetails.DeploymentId = deploymentId;
            s.Update(getProcessDefFileDetails);

            //get process def
            var xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
            var getElement = xmlDocument.Root.Element(BpmnUtility.BPMN_NAMESPACE + "process");
            var processDefKey = getElement.GetId();
            var processDef = await commClass.GetProcessDefByKey(processDefKey);

            if (processDefDetail != null) {
                processDefDetail.CamundaId = processDef.GetId();
                s.Update(getProcessDefFileDetails);
            }
            result = true;


            return result;
        }
        public async Task<ProcessDef_Camunda> ProcessStart(UserOrganizationModel caller, long processDefId) {
            try {
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        var perms = PermissionsUtility.Create(s, caller);
                        // permissions
                        perms.CanEdit(PermItem.ResourceType.CoreProcess, processDefId);

                        //var getProcessDefDetail = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == processDefId).SingleOrDefault();
                        var processDefDetail = s.Get<ProcessDef_Camunda>(processDefId);
                        if (processDefDetail.DeleteTime != null || processDefDetail == null) {
                            throw new PermissionsException();
                        }

                        // call Comm Layer
                        ICommClass commClass = CommFactory.Get();
                        log.Info("User (" + caller.Id + ") started process: " + processDefDetail.CamundaId);
                        var startProcess = await commClass.ProcessStart(processDefDetail.CamundaId);

                        ProcessInstance_Camunda processIns = new ProcessInstance_Camunda() {
                            LocalProcessInstanceId = processDefDetail.Id,
                            Suspended = startProcess.Suspended,
                            CamundaProcessInstanceId = startProcess.Id
                        };

                        s.Save(processIns);

                        tx.Commit();
                        s.Flush();

                        return processDefDetail;
                    }
                }
            } catch (ArgumentNullException ex) {
                if (ex.ParamName == "processDefiniftionKey")
                    throw new PermissionsException("Cannot start process. It must be published first.");
                log.Error(ex);
                throw new PermissionsException("Cannot start process.");
            } catch (Exception ex) {
                log.Error(ex);
                throw new PermissionsException("Cannot start process.");
            }
        }
        public async Task<bool> SuspendProcess(UserOrganizationModel caller, long processDefId, string processInstanceId, bool shouldSuspend) {
            bool result = false;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, processDefId);

                    //var getProcessDefDetails = s.Get<ProcessDef_Camunda>(localId);
                    var processInsDetail = s.QueryOver<ProcessInstance_Camunda>()
                                            .Where(x => x.DeleteTime == null && x.LocalProcessInstanceId == processDefId && x.CamundaProcessInstanceId == processInstanceId).SingleOrDefault();
                    if (processInsDetail == null) {
                        throw new PermissionsException("Process doesn't exists.");
                    }

                    // call Comm Layer
                    ICommClass commClass = CommFactory.Get();
                    var startProcess = await commClass.ProcessSuspend(processInsDetail.CamundaProcessInstanceId, shouldSuspend);
                    if (startProcess.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
                        processInsDetail.Suspended = shouldSuspend;
                        s.Update(processInsDetail);
                        tx.Commit();
                        s.Flush();
                        result = true;
                    }
                }
            }
            return result;
        }
        public List<ProcessInstanceViewModel> GetProcessInstanceList(UserOrganizationModel caller, long processDefId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanView(PermItem.ResourceType.CoreProcess, processDefId);

                    return s.QueryOver<ProcessInstance_Camunda>()
                        .Where(x => x.DeleteTime == null && x.LocalProcessInstanceId == processDefId && x.CompleteTime == null).List()
                        .Select(x => ProcessInstanceViewModel.Create(x)).ToList();
                }
            }
        }
        public async Task<long> CreateProcessDef(UserOrganizationModel caller, string processName) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var created = await CreateProcessDef(s, perms, processName);
                    tx.Commit();
                    s.Flush();
                    return created;
                }
            }
        }
        public async Task<long> CreateProcessDef(ISession s, PermissionsUtility perms, string processName) {
            // permissions
            perms.CanCreateProcessDef();

            var processDef = new ProcessDef_Camunda() {
                OrgId = perms.GetCaller().Organization.Id,
                Creator = ForModel.Create(perms.GetCaller()),  // issue with Unit test
                ProcessDefKey = processName,
            };

            s.Save(processDef);

            // create permItem
            PermissionsAccessor.CreatePermItems(s, perms.GetCaller(), PermItem.ResourceType.CoreProcess, processDef.Id, PermTiny.Creator(), PermTiny.Admins(), PermTiny.Members(admin: false));

            //create empty bpmn file
            var bpmnId = "bpmn_" + processDef.Id;
            var getStream = BpmnUtility.CreateBlankFile(processName, bpmnId);
            string guid = Guid.NewGuid().ToString();
            var path = "CoreProcess/" + guid + ".bpmn";

            //upload to server
            await BpmnUtility.UploadFileToServer(getStream, path);

            ProcessDef_CamundaFile processDef_File = new ProcessDef_CamundaFile();
            processDef_File.FileKey = path;
            processDef_File.LocalProcessDefId = processDef.Id;

            s.Save(processDef_File);

            return processDef.Id;

        }
        public async Task<bool> EditProcess(UserOrganizationModel caller, long processDefId, string processName) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, processDefId);
                    var updated = await EditProcess(s, perms, processDefId, processName);
                    tx.Commit();
                    s.Flush();
                    return updated;
                }
            }
        }
        public async Task<bool> EditProcess(ISession s, PermissionsUtility perms, long processDefId, string processName) {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, processDefId);

            bool result = true;
            var getProcessDefDetails = s.Get<ProcessDef_Camunda>(processDefId);
            if (getProcessDefDetails.DeleteTime != null) {
                throw new PermissionsException();
            }

            bool shouldUpload = false;
            if (!String.IsNullOrEmpty(processName) && getProcessDefDetails.ProcessDefKey != processName) // check if process name changed or not
            {
                getProcessDefDetails.ProcessDefKey = processName;
                s.Update(getProcessDefDetails);
                shouldUpload = true;
            }

            if (shouldUpload) {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == processDefId).SingleOrDefault();

                MemoryStream fileStream = new MemoryStream();
                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                var getElement = xmlDocument.Root.Element(BpmnUtility.BPMN_NAMESPACE + "process");
                getElement.SetAttributeValue("name", processName);

                xmlDocument.Save(fileStream);
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Position = 0;

                await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);
            }

            return result;
        }
        public async Task<bool> DeleteProcess(UserOrganizationModel caller, long processId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var deleted = await DeleteProcessDef(s, perms, processId);
                    tx.Commit();
                    s.Flush();
                    return deleted;
                }
            }
        }
        public async Task<bool> DeleteProcessDef(ISession s, PermissionsUtility perms, long processDefId) {
            perms.CanAdmin(PermItem.ResourceType.CoreProcess, processDefId);

            var processDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == processDefId).SingleOrDefault();
            if (processDefDetails == null || processDefDetails.DeleteTime != null) {
                throw new PermissionsException("Process does not exists");
            }
            processDefDetails.DeleteTime = DateTime.UtcNow;
            if (processDefDetails.CamundaId != null)
                await CommFactory.Get().ProcessSuspend(processDefDetails.CamundaId, true);

            s.Update(processDefDetails);

            return true;
        }

        public async Task<TaskViewModel> CreateProcessDefTask(UserOrganizationModel caller, long processDefId, TaskViewModel model) {
            using (var s = HibernateSession.GetCurrentSession()) {
                var perm = PermissionsUtility.Create(s, caller);
                var created = await CreateProcessDefTask(s, perm, processDefId, model);
                return created;
            }
        }
        public async Task<TaskViewModel> CreateProcessDefTask(ISession s, PermissionsUtility perm, long processDefId, TaskViewModel model) {
            if (model.SelectedMemberId == null || !model.SelectedMemberId.Any()) {
                throw new PermissionsException("You must select a group.");
            }

            // check permissions
            perm.CanEdit(PermItem.ResourceType.CoreProcess, processDefId);

            foreach (var item in model.SelectedMemberId) {
                perm.ViewRGM(item);
            }

            var modelObj = new TaskViewModel();
            modelObj = model;

            var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>()
                                            .Where(x => x.DeleteTime == null && x.LocalProcessDefId == processDefId)
                                            .SingleOrDefault();

            if (getProcessDefFileDetails == null) {
                throw new PermissionsException("file does not exists");
            }

            var fileStream = new MemoryStream();
            var xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
            var allElements = BpmnUtility.GetAllElement(xmlDocument);
            var endElement = BpmnUtility.FindElementByAttribute(allElements, "id", "EndEvent");
            var startElement = BpmnUtility.FindElementByAttribute(allElements, "id", "StartEvent");

            var startId = startElement.GetId();
            var endId = endElement.GetId();

            //getAllElement
            int sourceCounter = allElements.Count(e => e.GetSourceRef() == "StartEvent");
            int targetCounter = allElements.Count(e => e.GetTargetRef() == "EndEvent");

            string taskId = "Task" + GenGuid();
            var candidateGroups = BpmnUtility.ConcatedCandidateString(model.SelectedMemberId);



            if (sourceCounter == 0) {
                allElements.FirstOrDefault(e => e.GetId() == startId)
                    .AddAfterSelf(GenSequenceFlow(startId, taskId));                //new XElement(BpmnUtility.BPMN_NAMESPACE + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + GenGuid()), new XAttribute("sourceRef", startId), new XAttribute("targetRef", taskId)));

                if (targetCounter == 0) {
                    allElements.FirstOrDefault(e => e.GetId() == endId).AddBeforeSelf(
                                GenUserTask(model, taskId, candidateGroups),
                                GenSequenceFlow(taskId, endElement)
                              );
                } else {
                    var getEndEventSrc = allElements.FirstOrDefault(e => e.GetSourceRef() == endId);
                    allElements.FirstOrDefault(e => e.GetId() == getEndEventSrc.GetId()).AddAfterSelf(
                                GenUserTask(model, taskId, candidateGroups),
                                GenSequenceFlow(getEndEventSrc, endElement)
                              );
                }
            } else {
                if (targetCounter == 0) {
                    allElements.FirstOrDefault(e => e.GetId() == startId).AddAfterSelf(
                                GenSequenceFlow(startId, taskId),                    //new XElement(BpmnUtility.BPMN_NAMESPACE + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + GenGuid()),new XAttribute("sourceRef", startId), new XAttribute("targetRef", taskId)),
                                GenUserTask(model, taskId, candidateGroups),         //new XElement(BpmnUtility.BPMN_NAMESPACE + "userTask", new XAttribute("id", taskId), new XAttribute("name", model.name), new XAttribute(BpmnUtility.CAMUNDA_NAMESPACE + "candidateGroups", candidateGroups)),
                                GenSequenceFlow(taskId, endId)                       //new XElement(BpmnUtility.BPMN_NAMESPACE + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + GenGuid()), new XAttribute("sourceRef", taskId), new XAttribute("targetRef", endId))

                              );
                } else {
                    var getEndEventSrc = allElements.FirstOrDefault(e => e.GetTargetRef() == endId);
                    allElements.FirstOrDefault(e => e.GetId() == getEndEventSrc.GetId()).AddAfterSelf(
                                GenUserTask(model, taskId, candidateGroups),          //new XElement(BpmnUtility.BPMN_NAMESPACE + "userTask", new XAttribute("id", taskId), new XAttribute("name", model.name), new XAttribute(BpmnUtility.CAMUNDA_NAMESPACE + "candidateGroups", candidateGroups)),
                                GenSequenceFlow(taskId, endId)                       //new XElement(BpmnUtility.BPMN_NAMESPACE + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + GenGuid()),new XAttribute("sourceRef", taskId), new XAttribute("targetRef", endId))
                              );

                    allElements.FirstOrDefault(e => e.GetId() == getEndEventSrc.GetId()).SetAttributeValue("targetRef", taskId);
                }
            }

            xmlDocument.Save(fileStream);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Position = 0;

            await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

            modelObj.Id = taskId;
            modelObj.SelectedMemberName = BpmnUtility.GetMemberNames(s, perm, model.SelectedMemberId);


            return modelObj;
        }

        public async Task<List<TaskViewModel>> GetAllTaskForProcessDefinition(UserOrganizationModel caller, long processDefId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller).CanView(PermItem.ResourceType.CoreProcess, processDefId);

                    var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == processDefId).SingleOrDefault();
                    if (getProcessDefFileDetails != null) {
                        XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                        var getAllElement = BpmnUtility.GetAllElementByAttr(xmlDocument, "userTask");

                        foreach (var item in getAllElement) {
                            taskList.Add(TaskViewModel.Create(s, perms, item));
                        }
                    }
                }
            }
            return taskList;
        }
        /// <summary>
        /// get all tasks of all processes under specific User Organization
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="rgmId"></param>
        /// <returns></returns>
        public async Task<List<TaskViewModel>> GetAllTaskByRGM(UserOrganizationModel caller, long rgmId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perm = PermissionsUtility.Create(s, caller);

                    var threadList = new List<Task<List<TaskViewModel>>>();
                    var getProcessList = s.QueryOver<ProcessDef_Camunda>()
                                          .Where(t => t.DeleteTime == null && t.OrgId == caller.Organization.Id)
                                          .List().ToList();

                    foreach (var processDef in getProcessList) {
                        threadList.Add(GetAllTaskByRgmId(s, perm, processDef.Id, rgmId));
                    }

                    return (await Task.WhenAll(threadList)).SelectMany(x => x).ToList();
                }
            }
        }

        public async Task<List<TaskViewModel>> GetAllTaskByRgmId(ISession s, PermissionsUtility perm, long processDefId, long rgmId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            perm.CanView(PermItem.ResourceType.CoreProcess, processDefId);
            perm.ViewRGM(rgmId);

            var processDefDetails = s.QueryOver<ProcessDef_CamundaFile>()
                                            .Where(x => x.DeleteTime == null && x.LocalProcessDefId == processDefId)
                                            .SingleOrDefault();
            if (processDefDetails != null) {
                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(processDefDetails.FileKey);
                var getAllElement = BpmnUtility.GetAllElementByAttr(xmlDocument, "userTask");

                if (getAllElement == null) {
                    throw new PermissionsException("file does not exists");
                }

                foreach (var item in getAllElement) {
                    var getCandidateGroup = BpmnUtility.GetAttributeValue(item, "candidateGroups", BpmnUtility.CAMUNDA_NAMESPACE);
                    var memberIds = BpmnUtility.ParseCandidateGroupIds(getCandidateGroup).ToList();
                    if (memberIds.Contains(rgmId)) {
                        taskList.Add(TaskViewModel.CreateMinimal(item));
                    }
                }
            }
            return taskList;
        }

        public async Task<List<TaskViewModel>> GetTaskListByCandidateGroups(UserOrganizationModel caller, long[] candidateGroupIds, bool unassigned = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return await GetTaskListByCandidateGroups(s, perms, candidateGroupIds, unassigned);
                }
            }
        }

        private static async Task<List<TaskViewModel>> GetTaskListByCandidateGroups(ISession s, PermissionsUtility perms, long[] candidateGroupIds, bool unassigned) {
            var taskList = new List<TaskViewModel>();

            foreach (var item in candidateGroupIds) {
                perms.CanViewTasksForCandidateGroup(item);
            }

            var updatedCandiateGroupIdsList = new List<long>();
            updatedCandiateGroupIdsList.AddRange(candidateGroupIds);

            foreach (var cgid in candidateGroupIds) {
                updatedCandiateGroupIdsList.AddRange(ResponsibilitiesAccessor.GetResponsibilityGroupsForRgm(s, perms, cgid).Select(x => x.Id));
            }

            ICommClass comClass = CommFactory.Get();
            var getUsertaskList = await comClass.GetTaskByCandidateGroups(updatedCandiateGroupIdsList.ToArray(), unassigned: unassigned);

            foreach (var item in getUsertaskList) {
                taskList.Add(TaskViewModel.Create(item));
            }
            return taskList;
        }

        public async Task<List<TaskViewModel>> GetVisibleTasksForUser(ISession s, PermissionsUtility perms, long userId) {
            var userTaskDelay = GetTaskListByUserId(perms, userId);
            var candidateGroupDelay = GetTaskListByCandidateGroups(s, perms, new long[] { userId }, false);

            var results = await Task.WhenAll<List<TaskViewModel>>(userTaskDelay, candidateGroupDelay);

            return results.SelectMany(x => x).ToList();
        }

        public async Task<long[]> GetCandidateGroupIdsForTask_UnSafe(ISession s, string taskId) {
            List<long> candidateGroupId = new List<long>();
            //var perms = PermissionsUtility.Create(s, caller);
            ICommClass comClass = CommFactory.Get();
            var getTask = await comClass.GetTaskById(taskId);

            if (!string.IsNullOrEmpty(getTask.ProcessDefinitionId)) {
                var getProcessDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.CamundaId == getTask.ProcessDefinitionId).SingleOrDefault();
                if (getProcessDefDetails == null) {
                    throw new PermissionsException("process definition not found");
                }
                var processDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == getProcessDefDetails.Id).SingleOrDefault();
                if (processDefFileDetails == null) {
                    throw new PermissionsException("file doesn't exists");
                }

                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(processDefFileDetails.FileKey);
                var getAllElement = BpmnUtility.GetAllElementByAttr(xmlDocument, "userTask");

                var getTaskDetail = BpmnUtility.FindElementByAttribute(getAllElement, "name", getTask.Name);
                //getAllElement.Where(t => t.Attribute("name").Value == getTask.Name).FirstOrDefault();
                var getCandidateGroup = BpmnUtility.GetAttributeValue(getTaskDetail, "candidateGroups", BpmnUtility.CAMUNDA_NAMESPACE);
                // (getTaskDetail.Attribute(camunda + "candidateGroups") != null ? (getTaskDetail.Attribute(camunda + "candidateGroups").Value) : "");

                candidateGroupId = BpmnUtility.ParseCandidateGroupIds(getCandidateGroup).ToList();
                //return GetMemberName(caller, getCandidateGroup, null);
            }
            return candidateGroupId.ToArray();
        }


        public async Task<List<long>> GetCandidateGroupIds_Unsafe(ISession s, long processDefId) {
            List<long> candidateGroupIdList = new List<long>();
            var processDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == processDefId).SingleOrDefault();

            if (processDefFileDetails == null) {
                throw new PermissionsException("File doesn't exists");
            }

            XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(processDefFileDetails.FileKey);
            var getAllElement = BpmnUtility.GetAllElementByAttr(xmlDocument, "userTask");

            foreach (var item in getAllElement) {
                var getCandidateGroup = BpmnUtility.GetAttributeValue(item, "candidateGroups", BpmnUtility.CAMUNDA_NAMESPACE);
                var getIds = BpmnUtility.ParseCandidateGroupIds(getCandidateGroup);
                foreach (var item1 in getIds) {
                    candidateGroupIdList.Add(item1);
                }
            }
            return candidateGroupIdList.ToList();
        }

        public async Task<List<TaskViewModel>> GetTaskListByUserId(UserOrganizationModel caller, long userId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    // Need to discuss this permission required or not?
                    var perms = PermissionsUtility.Create(s, caller);
                    return await GetTaskListByUserId(perms, userId);
                }
            }
        }

        private static async Task<List<TaskViewModel>> GetTaskListByUserId(PermissionsUtility perms, long userId) {
            perms.CanViewTasksForCandidateGroup(userId);
            List<TaskViewModel> taskList = new List<TaskViewModel>();

            string _userId = "u_" + userId;
            ICommClass comClass = CommFactory.Get();
            var getUsertaskList = await comClass.GetTaskListByAssignee(_userId);

            foreach (var item in getUsertaskList) {
                taskList.Add(TaskViewModel.Create(item));
            }
            return taskList;
        }

        public async Task<List<TaskViewModel>> GetTaskListByProcessInstanceId(UserOrganizationModel caller, string processInstanceId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                var perms = PermissionsUtility.Create(s, caller);

                ICommClass comClass = CommFactory.Get();
                var getUsertaskList = await comClass.GetTaskListByInstanceId(processInstanceId);

                // permission check for InstanceId using associated ProcessDefId
                foreach (var item in getUsertaskList) {
                    var processDef = GetProcessDefByCamundaId_Unsafe(s, item.ProcessDefinitionId);
                    perms.CanViewProcessDef(processDef.Id);
                }

                foreach (var item in getUsertaskList) {
                    taskList.Add(TaskViewModel.Create(item));
                }


            }
            return taskList;
        }

        public async Task<TaskViewModel> GetTaskById_Unsafe(string taskId) {
            TaskViewModel output = null;
            ICommClass comClass = CommFactory.Get();
            var task = await comClass.GetTaskById(taskId);
            if (task != null) {
                output = TaskViewModel.Create(task);
            }

            return output;
        }

        /// <summary>
        /// Note: The difference with claim a task is that this method does not check if the task already has a user assigned to it.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="taskId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<bool> TaskAssignee(UserOrganizationModel caller, string taskId, long userId) {
            var result = false;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    //perms.InValidPermission();

                    string _userId = "u_" + userId;
                    ICommClass commClass = CommFactory.Get();
                    var setAssignee = await commClass.SetAssignee(taskId, _userId);
                    if (setAssignee.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
                        result = true;
                    }
                }
            }

            await HooksRegistry.Each<ITaskHook>((s, x) => x.ClaimTask(s, taskId, userId));

            return result;
        }

        /// <summary>
        /// Note: The difference with set a assignee is that here a check is performed to see if the task already has a user assigned to it.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="taskId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>        
        public async Task<bool> TaskClaimOrUnclaim(UserOrganizationModel caller, string taskId, long userId, bool claim) {
            var result = false;

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    string _userId = "u_" + userId;
                    ICommClass commClass = CommFactory.Get();
                    if (claim) {
                        var taskClaim = await commClass.TaskClaim(taskId, _userId);
                        if (taskClaim.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
                            result = true;
                        }
                    } else {
                        var taskUnClaim = await commClass.TaskUnClaim(taskId, _userId);
                        if (taskUnClaim.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
                            result = true;
                        }
                    }
                }
            }

            if (claim)
                await HooksRegistry.Each<ITaskHook>((s, x) => x.ClaimTask(s, taskId, userId));
            else
                await HooksRegistry.Each<ITaskHook>((s, x) => x.UnclaimTask(s, taskId));

            return result;
        }

        //public async Task<bool> TaskUnClaim(UserOrganizationModel caller, string taskId, long userId) {
        //	try {
        //		using (var s = HibernateSession.GetCurrentSession()) {
        //			var perms = PermissionsUtility.Create(s, caller);
        //			perms.Self(userId);
        //			// check if user is member of candidategroup in task
        //			perms.CanEditTask(taskId);
        //			string _userId = "u_" + userId;
        //			CommClass commClass = new CommClass();
        //			var taskUnClaim = await commClass.TaskUnClaim(taskId, _userId);
        //			if (taskUnClaim.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
        //				return true;
        //			}
        //		}
        //	} catch (Exception ex) {
        //		throw ex;
        //	}
        //	return false;
        //}

        public async Task<bool> TaskComplete(UserOrganizationModel caller, string taskId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                }
            }
            //If you extend the Session, you must also rework the Hook to be outside the session.
            ICommClass commClass = CommFactory.Get();
            var task = await commClass.GetTaskById(taskId);
            var taskComplete = await commClass.TaskComplete(taskId);
            if (taskComplete.TNoContentStatus.ToString() == "Success") {
                var userId = task.Assignee.SubstringAfter("u_").ToLong();
                await HooksRegistry.Each<ITaskHook>((s, x) => x.CompleteTask(s, taskId, userId));
                return true;
            }
            if (taskComplete.RestException.Message.EndsWith(" is suspended."))
                throw new PermissionsException("Cannot complete task. Process is suspended.");
            throw new PermissionsException("Could not complete task.");
        }

        public async Task<TaskViewModel> UpdateTask(UserOrganizationModel caller, long localId, TaskViewModel model) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var updated = await UpdateTask(s, perms, localId, model);
                    tx.Commit();
                    s.Flush();
                    return updated;
                }
            }
        }
        public async Task<TaskViewModel> UpdateTask(ISession s, PermissionsUtility perms, long localId, TaskViewModel model) {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            TaskViewModel modelObj = new TaskViewModel();
            modelObj = model;
            var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
            if (getProcessDefFileDetails != null) {
                MemoryStream fileStream = new MemoryStream();
                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                var getAllElement = BpmnUtility.GetAllElement(xmlDocument);
                string candidateGroups = BpmnUtility.ConcatedCandidateString(model.SelectedMemberId);

                //update name element
                getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue("name", model.name);

                //update description element
                getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue(BpmnUtility.CAMUNDA_NAMESPACE + "candidateGroups", candidateGroups);


                xmlDocument.Save(fileStream);
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Position = 0;

                await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

                modelObj.SelectedMemberName = BpmnUtility.GetMemberNames(s, perms, model.SelectedMemberId);
            }
            return modelObj;
        }

        public async Task<bool> DeleteProcessDefTask(UserOrganizationModel caller, string taskId, long localId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var result = await DeleteProcessDefTask(s, perms, localId, taskId);

                    tx.Commit();
                    s.Flush();
                    return result;
                }
            }
        }
        public async Task<bool> DeleteProcessDefTask(ISession s, PermissionsUtility perms, long localId, string taskId) {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);
            var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
            if (getProcessDefFileDetails == null) {
                throw new PermissionsException("File doesn't exists");
            }

            var getfileStream = await BpmnUtility.GetFileFromServer(getProcessDefFileDetails.FileKey);
            MemoryStream fileStream = new MemoryStream();

            //Detach Node
            var getModifiedStream = BpmnUtility.DetachNode(getfileStream, taskId);

            getModifiedStream.Seek(0, SeekOrigin.Begin);
            XDocument xmlDocument = XDocument.Load(getModifiedStream);


            xmlDocument.Save(fileStream);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Position = 0;

            await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

            return true;
        }

        public IEnumerable<ProcessDef_Camunda> GetVisibleProcessDefinitionList(UserOrganizationModel caller, long orgId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    return GetVisibleProcessDefinitionList(s, perms, orgId);
                }
            }
        }
        public IEnumerable<ProcessDef_Camunda> GetVisibleProcessDefinitionList(ISession s, PermissionsUtility perms, long orgId) {
            perms.ViewOrganization(orgId);

            IEnumerable<ProcessDef_Camunda> processDefList = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == orgId).List();
            List<ProcessDef_Camunda> finalList = new List<ProcessDef_Camunda>();
            foreach (var item in processDefList.ToList()) {
                try {
                    perms.CanView(PermItem.ResourceType.CoreProcess, item.LocalId);
                    finalList.Add(item);
                } catch (Exception) {
                }
            }

            return finalList;
        }

        public ProcessDef_Camunda GetProcessDefById(UserOrganizationModel caller, long processDefId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanView(PermItem.ResourceType.CoreProcess, processDefId);
                    ProcessDef_Camunda processDef = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id && x.Id == processDefId).SingleOrDefault();
                    return processDef;
                }
            }
        }
        // Unsafe
        private ProcessDef_Camunda GetProcessDefByCamundaId_Unsafe(ISession s, string camundaId) {
            ProcessDef_Camunda processDef = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.CamundaId == camundaId).SingleOrDefault();
            return processDef;
        }


        public async Task<bool> ReorderBPMNFile(UserOrganizationModel caller, long localId, int oldOrder, int newOrder) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);
                    var result = await ReorderBPMNFile_Unsafe(s, localId, oldOrder, newOrder);
                    return result;
                }
            }
        }
        public async Task<bool> ReorderBPMNFile_Unsafe(ISession s, long localId, int oldOrder, int newOrder) {
            string oldOrderId = string.Empty;
            string newOrderId = string.Empty;
            string name = string.Empty;
            string candidateGroups = string.Empty;
            var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
            if (getProcessDefFileDetails != null) {
                var getStream = await BpmnUtility.GetFileFromServer(getProcessDefFileDetails.FileKey);

                BpmnUtility.GetNodeDetail(getStream, oldOrder, newOrder, out oldOrderId, out newOrderId, out name, out candidateGroups);

                //Remove all elements under root node
                var de_stream = BpmnUtility.DetachNode(getStream, oldOrderId);

                //Insert element
                var ins_stream = BpmnUtility.InsertNode(de_stream, oldOrder, newOrder, oldOrderId, newOrderId, name, candidateGroups);

                //stream upload
                await BpmnUtility.UploadFileToServer(ins_stream, getProcessDefFileDetails.FileKey);
            }
            return true;
        }

        public async Task<IEnumerable<TaskViewModel>> GetAllNewTasks_Unsafe(DateTime after) {
            ICommClass comClass = CommFactory.Get();
            var allTasks = await comClass.GetAllTasksAfter(after);
            return allTasks.Select(x => TaskViewModel.Create(x));
        }
        private async Task<Tuple<string, IEnumerable<IdentityLink>>> _GetLinks(ICommClass comClass, string id) {
            var links = await comClass.GetIdentityLinks(id);
            return Tuple.Create(id, links);
        }
        private async Task<Dictionary<string, IEnumerable<IdentityLink>>> _GetTaskIdentityLinkLookup(IEnumerable<TaskModel> tasks) {
            ICommClass comClass = CommFactory.Get();
            var allIdentityLinksTasks = new List<Task<Tuple<string, IEnumerable<IdentityLink>>>>();
            foreach (var t in tasks.Distinct(x => x.Id)) {
                allIdentityLinksTasks.Add(_GetLinks(comClass, t.Id));
            }
            var result = await Task.WhenAll(allIdentityLinksTasks);

            return result.ToDictionary(x => x.Item1, x => x.Item2);
        }

        public class UpdateAllTaskData {
            public TimeSpan ExecutionTime { get; set; }
            public DateTime LastUpdate { get; set; }
            public DateTime MaxTime { get; set; }
            public int TaskCount { get; set; }
            public List<TaskModel> Tasks { get; set; }
        }

        public async Task<UpdateAllTaskData> UpdateAllTasks_Unsafe() {
            IEnumerable<TaskModel> allTasks = null;
            ICommClass comClass = CommFactory.Get();
            var o = new UpdateAllTaskData { };
            var start = DateTime.UtcNow;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var lastUpdate = s.GetSettingOrDefault<DateTime>(Variable.Names.LAST_CAMUNDA_UPDATE_TIME, DateTime.UtcNow.AddDays(-1));
                    var maxTime = lastUpdate;
                    string lastId = null;
                    o.LastUpdate = lastUpdate;
                    allTasks = await comClass.GetAllTasksAfter(lastUpdate);
                    foreach (var t in allTasks) {
                        var a = DateTime.Parse(t.Created);
                        if (a > maxTime) {
                            maxTime = a;
                            lastId = t.Id;
                        }
                        maxTime = Math2.Max(maxTime, a.AddSeconds(1));
                    }
                    o.MaxTime = maxTime;
                    s.UpdateSetting(Variable.Names.LAST_CAMUNDA_UPDATE_TIME, maxTime);
                    tx.Commit();
                    s.Flush();
                }
            }
            o.TaskCount = allTasks.Count();
            o.Tasks = allTasks.ToList();

            var lookup = await _GetTaskIdentityLinkLookup(allTasks);

            var hub = GlobalHost.ConnectionManager.GetHubContext<CoreProcessHub>();
            foreach (var t in allTasks) {
                var rgms = lookup.GetOrDefault(t.Id, new List<IdentityLink>());
                var assignee = rgms.Where(x => x.type == "assignee").Select(x => CoreProcessHub.GenerateRgm(x.userId.SubstringAfter("u_").ToLong())).ToList();
                if (assignee.Any()) {
                    var group = hub.Clients.Groups(assignee);
                    group.update(new AngularUpdate() {
                        new AngularCoreProcessData() {
                            Tasks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer,AngularTask.Create(TaskViewModel.Create(t)))
                        }
                    });
                    group.showMessage("assignee" + t.Id);
                } else {
                    var candidateGroups = rgms.Where(x => x.type == "candidate").Select(x => CoreProcessHub.GenerateRgm(x.groupId)).ToList();
                    var group = hub.Clients.Groups(candidateGroups);
                    group.update(new AngularUpdate() {
                        new AngularCoreProcessData() {
                            Tasks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer,AngularTask.Create(TaskViewModel.Create(t)))
                        }
                    });
                    group.showMessage("candidate:" + t.Id);
                }
            }
            o.ExecutionTime = DateTime.UtcNow - start;
            return o;


        }

        public static void JoinCoreProcessHub(UserOrganizationModel caller, string connectionId) {
            var hub = GlobalHost.ConnectionManager.GetHubContext<CoreProcessHub>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var groups = ResponsibilitiesAccessor.GetResponsibilityGroupsForRgm(s, perms, caller.Id);

                    foreach (var r in groups) {
                        hub.Groups.Add(connectionId, CoreProcessHub.GenerateRgm(r));
                    }
                }
            }
        }
    }
}
