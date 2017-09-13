using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CamundaCSharpClient.Model;
using CamundaCSharpClient.Model.Deployment;
using log4net;
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
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Utilities;
using RadialReview.Utilities.CoreProcess;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using static RadialReview.Utilities.CoreProcess.BpmnUtility;

namespace RadialReview.Areas.CoreProcess.Accessors {
    public class ProcessDefAccessor : IProcessDefAccessor {

        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async Task<bool> Deploy(UserOrganizationModel caller, long localId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var deployed = await Deploy(s, localId, perms);
                    tx.Commit();
                    s.Flush();
                    return deployed;
                }
            }
        }
        public async Task<bool> Deploy(ISession s, long coreProcessId, PermissionsUtility perms) {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, coreProcessId);

            bool result = true;
            try {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == coreProcessId).SingleOrDefault();
                if (getProcessDefFileDetails != null) {
                    var getfileStream = await BpmnUtility.GetFileFromServer(getProcessDefFileDetails.FileKey);


                    List<object> fileObjects = new List<object>();
                    byte[] bytes = ((MemoryStream)getfileStream).ToArray();
                    fileObjects.Add(new FileParameter(bytes, getProcessDefFileDetails.FileKey.Split('/')[1].Replace("-", "")));

                    //getfileStream.Seek(0, SeekOrigin.Begin);
                    //XDocument x1 = XDocument.Load(getfileStream);

                    //var getProcessDefDetail = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == localId).SingleOrDefault();
                    var processDefDetail = s.Get<ProcessDef_Camunda>(coreProcessId);
                    if (processDefDetail.DeleteTime != null) {
                        throw new PermissionsException();
                    }

                    string deplyomentName = processDefDetail.ProcessDefKey;

                    // call Comm Layer
                    CommClass commClass = new CommClass();
                    var deploymentId = commClass.Deploy(deplyomentName, fileObjects);

                    getProcessDefFileDetails.DeploymentId = deploymentId;
                    s.Update(getProcessDefFileDetails);

                    //get process def
                    //var getProcessDef = await commClass.GetProcessDefByKey(key.Replace(" ", "") + localId.Replace("-", ""));
                    var processDef = await commClass.GetProcessDefByKey(deplyomentName.Replace(" ", "") + "bpmn_" + coreProcessId);

                    if (processDefDetail != null) {
                        processDefDetail.CamundaId = processDef.GetId();
                        s.Update(getProcessDefFileDetails);
                    }
                }
            } catch (Exception ex) {
                result = false;
                throw ex;
            }
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
                        CommClass commClass = new CommClass();
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
            } catch (Exception ex) {
                log.Error(ex);
                throw new PermissionsException("Cannot start process.");
            }
        }
        public async Task<bool> SuspendProcess(UserOrganizationModel caller, long localId, string processInstanceId, bool shouldSuspend) {
            bool result = false;
            try {
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        var perms = PermissionsUtility.Create(s, caller);
                        perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

                        //var getProcessDefDetails = s.Get<ProcessDef_Camunda>(localId);
                        var processInsDetail = s.QueryOver<ProcessInstance_Camunda>().Where(x => x.DeleteTime == null &&
                      x.LocalProcessInstanceId == localId && x.CamundaProcessInstanceId == processInstanceId).SingleOrDefault();
                        if (processInsDetail == null) {
                            throw new PermissionsException("Process doesn't exists.");
                        }

                        // call Comm Layer
                        CommClass commClass = new CommClass();
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
            } catch (Exception ex) {
                throw ex;
            }
            return result;
        }
        public List<ProcessInstanceViewModel> GetProcessInstanceList(UserOrganizationModel caller, long localId) {
            using (var s = HibernateSession.GetCurrentSession()) {

                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanView(PermItem.ResourceType.CoreProcess, localId);

                    var processInstance = s.QueryOver<ProcessInstance_Camunda>().Where(x => x.DeleteTime == null
                     && x.LocalProcessInstanceId == localId
                     && x.CompleteTime == null
                    ).List()
                        .Select(x => new ProcessInstanceViewModel() {
                            Id = x.CamundaProcessInstanceId,
                            DefinitionId = x.LocalProcessInstanceId,
                            Suspended = x.Suspended,
                            CreateTime = x.CreateTime,
                            CompleteTime = x.CompleteTime
                        }).ToList();

                    tx.Commit();
                    s.Flush();

                    return processInstance;
                }
            }
        }
        public async Task<long> Create(UserOrganizationModel caller, string processName) {
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

            ProcessDef_Camunda processDef = new ProcessDef_Camunda();
            processDef.OrgId = perms.GetCaller().Organization.Id;
            processDef.Creator = ForModel.Create(perms.GetCaller());  // issue with Unit test
            processDef.ProcessDefKey = processName;
            //processDef.LocalId = localProcessDefId;

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
        public async Task<bool> EditProcess(UserOrganizationModel caller, long localId, string processName) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);
                    var updated = await EditProcess(s, perms, localId, processName);
                    tx.Commit();
                    s.Flush();
                    return updated;
                }
            }
        }
        public async Task<bool> EditProcess(ISession s, PermissionsUtility perms, long localId, string processName) {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            bool result = true;
            try {
                var getProcessDefDetails = s.Get<ProcessDef_Camunda>(localId);
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
                    var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();

                    MemoryStream fileStream = new MemoryStream();
                    XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                    var getElement = xmlDocument.Root.Element(BpmnUtility.BPMN_NAMESPACE + "process");
                    getElement.SetAttributeValue("name", processName);

                    xmlDocument.Save(fileStream);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.Position = 0;

                    await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);
                }
            } catch (Exception ex) {
                result = false;
                throw ex;
            }

            return result;
        }
        public bool DeleteProcess(UserOrganizationModel caller, long processId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var deleted = DeleteProcess(s, perms, processId);
                    tx.Commit();
                    s.Flush();
                    return deleted;
                }
            }
        }
        public bool DeleteProcess(ISession s, PermissionsUtility perms, long processId) {
            perms.CanAdmin(PermItem.ResourceType.CoreProcess, processId);
            try {
                var processDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == processId).SingleOrDefault();
                if (processDefDetails == null || processDefDetails.DeleteTime != null) {
                    throw new PermissionsException("Process does not exists");
                }
                processDefDetails.DeleteTime = DateTime.UtcNow;
                s.Update(processDefDetails);
                //var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == getProcessDefDetails.LocalId).SingleOrDefault();
                //if (getProcessDefFileDetails != null) {
                //	await BpmnUtility.DeleteFileFromServer(getProcessDefFileDetails.FileKey);
                //}
            } catch (Exception ex) {
                throw ex;
            }
            return true;
        }
        public async Task<TaskViewModel> CreateProcessDefTask(UserOrganizationModel caller, long localId, TaskViewModel model) {
            using (var s = HibernateSession.GetCurrentSession()) {
                var perm = PermissionsUtility.Create(s, caller);
                var created = await CreateProcessDefTask(s, perm, localId, model);
                return created;
            }
        }
               
        public async Task<TaskViewModel> CreateProcessDefTask(ISession s, PermissionsUtility perm, long localId, TaskViewModel model) {
            if (model.SelectedMemberId == null || !model.SelectedMemberId.Any()) {
                throw new PermissionsException("You must select a group.");
            }

            // check permissions
            perm.CanEdit(PermItem.ResourceType.CoreProcess, localId);


            foreach (var item in model.SelectedMemberId) {
                perm.ViewRGM(item);
            }

            TaskViewModel modelObj = new TaskViewModel();
            modelObj = model;
            try {

                //var processDef

                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>()
                                                .Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId)
                                                .SingleOrDefault();

                if (getProcessDefFileDetails == null) {
                    throw new PermissionsException("file does not exists");
                }

                MemoryStream fileStream = new MemoryStream();
                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
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
                //XDocument x1 = XDocument.Load(fileStream);
                fileStream.Position = 0;

                await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

                modelObj.Id = taskId;
                modelObj.SelectedMemberName = BpmnUtility.GetMemberNames(s, perm, model.SelectedMemberId);

            } catch (Exception) {

            }
            return modelObj;
        }

       

        public async Task<List<TaskViewModel>> GetAllTaskForProcessDefinition(UserOrganizationModel caller, long localId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller).CanView(PermItem.ResourceType.CoreProcess, localId);

                    var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                    if (getProcessDefFileDetails != null) {
                        XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                        var getAllElement = BpmnUtility.GetAllElementByAttr(xmlDocument, "userTask");

                        foreach (var item in getAllElement) {
                            //var getCandidateGroup = (item.Attribute(camunda + "candidateGroups") != null ? (item.Attribute(camunda + "candidateGroups").Value) : "");
                            //var getCandidateGroup = BpmnUtility.GetAttributeValue(item, "candidateGroups", BpmnUtility.CAMUNDA_NAMESPACE);
                            taskList.Add(TaskViewModel.Create(s, perms, item));
                            /*{
                                description = BpmnUtility.GetAttributeValue(item, "description"),
                                name = BpmnUtility.GetAttributeValue(item, "name"),
                                Id = BpmnUtility.GetAttributeValue(item, "id"),
                                SelectedMemberId = BpmnUtility.ParseCandidateGroupIds(getCandidateGroup),
                                SelectedMemberName = BpmnUtility.GetMemberNamesFromString(s,perms, getCandidateGroup),
                                CandidateList = GetCandidateGroupsMembersFromString(s,perms, getCandidateGroup)
                            });*/
                        }
                    }
                }
            }
            return taskList;
        }

        // get all tasks of all processes under specific User Organization
        public async Task<List<TaskViewModel>> GetAllTaskByRGM(UserOrganizationModel caller, long rgmId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perm = PermissionsUtility.Create(s, caller);

                    List<Task<List<TaskViewModel>>> threadList = new List<Task<List<TaskViewModel>>>();
                    var getProcessList = s.QueryOver<ProcessDef_Camunda>().Where(t => t.DeleteTime == null && t.OrgId == caller.Organization.Id).List().ToList();
                    foreach (var processDef in getProcessList) {
                        threadList.Add(GetAllTaskByRgmId(s, perm, caller, processDef.Id, rgmId));
                    }

                    return (await Task.WhenAll(threadList)).SelectMany(x => x).ToList();
                }
            }
        }

        public async Task<List<TaskViewModel>> GetAllTaskByRgmId(ISession s, PermissionsUtility perm, UserOrganizationModel caller, long localId, long rgmId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            perm.CanView(PermItem.ResourceType.CoreProcess, localId);
            perm.ViewRGM(rgmId);

            var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
            if (getProcessDefFileDetails != null) {
                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
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
            List<TaskViewModel> taskList = new List<TaskViewModel>();

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);

                    foreach (var item in candidateGroupIds) {
                        perms.CanViewTasksForCandidateGroup(item);
                    }

                    var updatedCandiateGroupIdsList = new List<long>();
                    updatedCandiateGroupIdsList.AddRange(candidateGroupIds);

                    foreach(var cgid in candidateGroupIds) {
                        updatedCandiateGroupIdsList.AddRange(ResponsibilitiesAccessor.GetResponsibilityGroupsForRgm(s, perms, cgid).Select(x=>x.Id));
                    }


                    //var candidateGroups = String.Join(",", candidateGroupIds.Select(x => "rgm_" + x));
                    CommClass comClass = new CommClass();
                    var getUsertaskList = await comClass.GetTaskByCandidateGroups(updatedCandiateGroupIdsList.ToArray(), unassigned: unassigned);

                    foreach (var item in getUsertaskList) {
                        taskList.Add(TaskViewModel.Create(item));
                    }

                    tx.Commit();
                    s.Flush();
                }
            }

            return taskList;
        }

        public async Task<long[]> GetCandidateGroupIdsForTask_UnSafe(ISession s, string taskId) {
            List<long> candidateGroupId = new List<long>();
            //var perms = PermissionsUtility.Create(s, caller);
            CommClass comClass = new CommClass();
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
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    // Need to discuss this permission required or not?
                    PermissionsUtility.Create(s, caller).CanViewTasksForCandidateGroup(userId);

                    string _userId = "u_" + userId;
                    CommClass comClass = new CommClass();
                    var getUsertaskList = await comClass.GetTaskListByAssignee(_userId);

                    foreach (var item in getUsertaskList) {
                        taskList.Add(TaskViewModel.Create(item));
                    }

                    tx.Commit();
                    s.Flush();
                }
            }
            return taskList;
        }

        public async Task<List<TaskViewModel>> GetTaskListByProcessInstanceId(UserOrganizationModel caller, string processInstanceId) {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                var perms = PermissionsUtility.Create(s, caller);

                CommClass comClass = new CommClass();
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

        public async Task<TaskViewModel> GetTaskById_Unsafe(UserOrganizationModel caller, string taskId) {
            TaskViewModel output = null;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller);
                    CommClass comClass = new CommClass();
                    var task = await comClass.GetTaskById(taskId);
                    if (task != null) {
                        output = TaskViewModel.Create(task);
                    }
                }
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
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    //perms.InValidPermission();

                    string _userId = "u_" + userId;
                    CommClass commClass = new CommClass();
                    var setAssignee = await commClass.SetAssignee(taskId, _userId);
                    if (setAssignee.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
                        return true;
                    }

                    tx.Commit();
                    s.Flush();
                }
            }
            return false;
        }

        /// <summary>
        /// Note: The difference with set a assignee is that here a check is performed to see if the task already has a user assigned to it.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="taskId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>        
        public async Task<bool> TaskClaimOrUnclaim(UserOrganizationModel caller, string taskId, long userId, bool claim) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    string _userId = "u_" + userId;
                    CommClass commClass = new CommClass();
                    if (claim) {
                        var taskClaim = await commClass.TaskClaim(taskId, _userId);
                        if (taskClaim.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
                            return true;
                        }
                    } else {
                        var taskUnClaim = await commClass.TaskUnClaim(taskId, _userId);
                        if (taskUnClaim.TNoContentStatus.ToString() == TextContentStatus.Success.ToString()) {
                            return true;
                        }
                    }

                    tx.Commit();
                    s.Flush();
                }
            }
            return false;
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

        public async Task<bool> TaskComplete(UserOrganizationModel caller, string taskId, long userId) {
            try {
                using (var s = HibernateSession.GetCurrentSession()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    CommClass commClass = new CommClass();
                    string _userId = "u_" + userId;
                    var taskComplete = await commClass.TaskComplete(taskId, _userId);
                    if (taskComplete.TNoContentStatus.ToString() == "Success") {
                        return true;
                    }
                }
            } catch (Exception ex) {
                throw ex;
            }
            return false;
        }



        public async Task<TaskViewModel> UpdateTask(UserOrganizationModel caller, long localId, TaskViewModel model) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var updated = await UpdateTask(s, perms, localId, model, caller);
                    tx.Commit();
                    s.Flush();
                    return updated;
                }
            }
        }

        public async Task<TaskViewModel> UpdateTask(ISession s, PermissionsUtility perms, long localId, TaskViewModel model, UserOrganizationModel caller) {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            TaskViewModel modelObj = new TaskViewModel();
            modelObj = model;
            var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
            if (getProcessDefFileDetails != null) {
                MemoryStream fileStream = new MemoryStream();
                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                var getAllElement = BpmnUtility.GetAllElement(xmlDocument);
                string candidateGroups = BpmnUtility.ConcatedCandidateString(model.SelectedMemberId);
                //if (model.SelectedMemberId != null) {
                //	if (model.SelectedMemberId.Any()) {
                //		foreach (var item in model.SelectedMemberId) {
                //			if (string.IsNullOrEmpty(candidateGroups))
                //				candidateGroups = "rgm_" + item;
                //			else
                //				candidateGroups += ", rgm_" + item;
                //		}
                //	}
                //}

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

                    tx.Rollback();
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

        public IEnumerable<ProcessDef_Camunda> GetProcessDefinitionList(UserOrganizationModel caller, long orgId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewOrganization(orgId);
                    //PermissionsAccessor.GetExplicitPermItemsForUser(s, perms, caller.Id, PermItem.ResourceType.CoreProcess);

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
            }
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


        public async Task<bool> ReOrderBPMNFile(UserOrganizationModel caller, long localId, int oldOrder, int newOrder) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);
                    var result = await ReOrderBPMNFile_Unsafe(s, localId, oldOrder, newOrder);
                    return result;
                }
            }
        }

        public async Task<bool> ReOrderBPMNFile_Unsafe(ISession s, long localId, int oldOrder, int newOrder) {
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
    }
}