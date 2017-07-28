using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
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
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace RadialReview.Areas.CoreProcess.Accessors
{
    public class ProcessDefAccessor : IProcessDefAccessor
    {
        private XNamespace camunda = "http://camunda.org/schema/1.0/bpmn";
        private XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async Task<bool> Deploy(UserOrganizationModel caller, long localId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var deployed = await Deploy(s, localId, perms);
                    tx.Commit();
                    s.Flush();
                    return deployed;
                }
            }
        }
        public async Task<bool> Deploy(ISession s, long localId, PermissionsUtility perms)
        {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            bool result = true;
            try
            {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    var getfileStream = await BpmnUtility.GetFileFromServer(getProcessDefFileDetails.FileKey);


                    List<object> fileObject = new List<object>();
                    byte[] bytes = ((MemoryStream)getfileStream).ToArray();
                    fileObject.Add(new FileParameter(bytes, getProcessDefFileDetails.FileKey.Split('/')[1].Replace("-", "")));

                    getfileStream.Seek(0, SeekOrigin.Begin);
                    XDocument x1 = XDocument.Load(getfileStream);

                    //var getProcessDefDetail = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == localId).SingleOrDefault();
                    var getProcessDefDetail = s.Get<ProcessDef_Camunda>(localId);
                    if (getProcessDefDetail.DeleteTime != null)
                    {
                        throw new PermissionsException();
                    }

                    string key = getProcessDefDetail.ProcessDefKey;

                    // call Comm Layer
                    CommClass commClass = new CommClass();
                    var getDeploymentId = commClass.Deploy(key, fileObject);

                    getProcessDefFileDetails.DeploymentId = getDeploymentId;
                    s.Update(getProcessDefFileDetails);

                    //get process def
                    //var getProcessDef = await commClass.GetProcessDefByKey(key.Replace(" ", "") + localId.Replace("-", ""));
                    var getProcessDef = await commClass.GetProcessDefByKey(key.Replace(" ", "") + "bpmn_" + localId);

                    if (getProcessDefDetail != null)
                    {
                        getProcessDefDetail.CamundaId = getProcessDef.GetId();
                        s.Update(getProcessDefFileDetails);
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }
            return result;
        }
        public async Task<ProcessDef_Camunda> ProcessStart(UserOrganizationModel caller, long processDefId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
                        // permissions
                        perms.CanEdit(PermItem.ResourceType.CoreProcess, processDefId);

                        //var getProcessDefDetail = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == processDefId).SingleOrDefault();
                        var getProcessDefDetail = s.Get<ProcessDef_Camunda>(processDefId);
                        if (getProcessDefDetail.DeleteTime != null)
                        {
                            throw new PermissionsException();
                        }

                        if (getProcessDefDetail != null)
                        {
                            // call Comm Layer
                            CommClass commClass = new CommClass();
                            var startProcess = await commClass.ProcessStart(getProcessDefDetail.CamundaId);

                            ProcessInstance_Camunda processIns = new ProcessInstance_Camunda();
                            processIns.LocalProcessInstanceId = getProcessDefDetail.Id;
                            //processIns.ProcessDefId = startProcess.DefinitionId;
                            processIns.Suspended = startProcess.Suspended;
                            processIns.CamundaProcessInstanceId = startProcess.Id;
                            s.Save(processIns);

                            tx.Commit();
                            s.Flush();

                            return getProcessDefDetail;

                        }
                        else
                        {
                            throw new PermissionsException("Cannot start process.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw new PermissionsException("Cannot start process.");
            }

            return new ProcessDef_Camunda();
        }
        public async Task<bool> ProcessSuspend(UserOrganizationModel caller, long localId, bool isSuspend)
        {
            bool result = false;
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
                        perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

                        //var getProcessDefDetails = s.Get<ProcessDef_Camunda>(localId);
                        var getProcessInsDetail = s.QueryOver<ProcessInstance_Camunda>().Where(x => x.DeleteTime == null && x.LocalProcessInstanceId == localId).SingleOrDefault();
                        if (getProcessInsDetail != null)
                        {
                            // call Comm Layer
                            CommClass commClass = new CommClass();
                            var startProcess = await commClass.ProcessSuspend(getProcessInsDetail.CamundaProcessInstanceId, isSuspend);
                            if (startProcess.TNoContentStatus.ToString() == "Success")
                            {
                                getProcessInsDetail.Suspended = isSuspend;
                                s.Update(getProcessInsDetail);

                                tx.Commit();
                                s.Flush();
                                result = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public List<ProcessInstanceViewModel> GetProcessInstanceList(UserOrganizationModel caller, long localId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {

                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanView(PermItem.ResourceType.CoreProcess, localId);

                    return s.QueryOver<ProcessInstance_Camunda>().Where(x => x.DeleteTime == null
                    && x.LocalProcessInstanceId == localId
                    && x.CompleteTime == null
                    ).List()
                        .Select(x => new ProcessInstanceViewModel()
                        {
                            Id = x.CamundaProcessInstanceId,
                            DefinitionId = x.LocalProcessInstanceId,
                            Suspended = x.Suspended,
                            CreateTime = x.CreateTime,
                            CompleteTime = x.CompleteTime
                        }).ToList();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task<long> Create(UserOrganizationModel caller, string processName)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var created = await CreateProcessDef(s, perms, processName);
                    tx.Commit();
                    s.Flush();
                    return created;
                }
            }
        }
        public async Task<long> CreateProcessDef(ISession s, PermissionsUtility perms, string processName)
        {
            try
            {
                // permissions
                perms.CreateProcessDef();

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
                var getStream = CreateBpmnFile(processName, bpmnId);
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
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<bool> Edit(UserOrganizationModel caller, long localId, string processName)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);
                    var updated = await Edit(s, perms, localId, processName);
                    tx.Commit();
                    s.Flush();
                    return updated;
                }
            }
        }
        public async Task<bool> Edit(ISession s, PermissionsUtility perms, long localId, string processName)
        {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            bool result = true;
            try
            {
                var getProcessDefDetails = s.Get<ProcessDef_Camunda>(localId);
                if (getProcessDefDetails.DeleteTime != null)
                {
                    throw new PermissionsException();
                }

                if (!String.IsNullOrEmpty(processName))
                {
                    getProcessDefDetails.ProcessDefKey = processName;
                    s.Update(getProcessDefDetails);
                }

                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();

                MemoryStream fileStream = new MemoryStream();
                XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                var getElement = xmlDocument.Root.Element(bpmn + "process");
                getElement.SetAttributeValue("name", processName);

                xmlDocument.Save(fileStream);
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Position = 0;

                await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }
        public async Task<bool> Delete(UserOrganizationModel caller, long processId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var deleted = await Delete(s, processId, perms);
                    tx.Commit();
                    s.Flush();
                    return deleted;
                }
            }
        }
        public async Task<bool> Delete(ISession s, long processId, PermissionsUtility perms)
        {
            perms.CanAdmin(PermItem.ResourceType.CoreProcess, processId);

            bool result = true;
            try
            {
                var getProcessDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == processId).SingleOrDefault();
                getProcessDefDetails.DeleteTime = DateTime.UtcNow;

                s.Update(getProcessDefDetails);

                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == getProcessDefDetails.LocalId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    await BpmnUtility.DeleteFileFromServer(getProcessDefFileDetails.FileKey);
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }
        public async Task<TaskViewModel> CreateTask(UserOrganizationModel caller, long localId, TaskViewModel model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                var perm = PermissionsUtility.Create(s, caller);
                var created = await CreateTask(s, perm, localId, model, caller);
                return created;
            }
        }
        public async Task<TaskViewModel> CreateTask(ISession s, PermissionsUtility perm, long localId, TaskViewModel model, UserOrganizationModel caller)
        {

            // check permissions
            perm.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            if (model.SelectedMemberId == null || !model.SelectedMemberId.Any())
            {
                throw new PermissionsException("You must select a group.");
            }

            foreach (var item in model.SelectedMemberId)
            {
                perm.ViewRGM(item);
            }

            TaskViewModel modelObj = new TaskViewModel();
            modelObj = model;
            try
            {

                //var processDef

                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();

                if (getProcessDefFileDetails != null)
                {
                    MemoryStream fileStream = new MemoryStream();
                    XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                    var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();
                    var getEndProcessElement = getAllElement.Where(t => (t.Attribute("id") != null ? t.Attribute("id").Value : "") == "EndEvent").FirstOrDefault();
                    var getStartProcessElement = getAllElement.Where(t => (t.Attribute("id") != null ? t.Attribute("id").Value : "") == "StartEvent").FirstOrDefault();

                    //getAllElement
                    int targetCounter = 0;
                    int sourceCounter = 0;
                    foreach (var item in getAllElement.ToList())
                    {
                        if (item.Attribute("targetRef") != null)
                        {
                            if (item.Attribute("targetRef").Value == "EndEvent")
                            {
                                targetCounter++;
                            }
                        }

                        if (item.Attribute("sourceRef") != null)
                        {
                            if (item.Attribute("sourceRef").Value == "StartEvent")
                            {
                                sourceCounter++;
                            }
                        }
                    }

                    string userTaskId = "Task" + Guid.NewGuid().ToString().Replace("-", "");
                    var candidateGroups = String.Join(",", model.SelectedMemberId.Select(x => "rgm_" + x));
                    //if (model.SelectedMemberId != null) {
                    //	if (model.SelectedMemberId.Any()) {
                    //		foreach (var item in model.SelectedMemberId) {
                    //			if (string.IsNullOrEmpty(candidateGroups))
                    //				candidateGroups = "rgm_" + item;
                    //			else
                    //				candidateGroups += ",rgm_" + item;
                    //		}
                    //	}
                    //}

                    if (sourceCounter == 0)
                    {
                        getAllElement.Where(m => m.Attribute("id").Value == getStartProcessElement.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                  new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
                                  new XAttribute("sourceRef", getStartProcessElement.Attribute("id").Value), new XAttribute("targetRef", userTaskId))
                                  );

                        if (targetCounter == 0)
                        {
                            getAllElement.Where(m => m.Attribute("id").Value == getEndProcessElement.Attribute("id").Value).FirstOrDefault().AddBeforeSelf(
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name), new XAttribute(camunda + "candidateGroups", candidateGroups)),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
                                        new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );
                        }
                        else
                        {
                            var getEndEventSrc = getAllElement.Where(x => (x.Attribute("sourceRef") != null ? x.Attribute("sourceRef").Value : "") == getEndProcessElement.Attribute("id").Value).FirstOrDefault();
                            getAllElement.Where(m => m.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name), new XAttribute(camunda + "candidateGroups", candidateGroups)),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
                                        new XAttribute("sourceRef", getEndEventSrc.Attribute("id").Value), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );
                        }

                    }
                    else
                    {
                        if (targetCounter == 0)
                        {
                            getAllElement.Where(m => m.Attribute("id").Value == getStartProcessElement.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                      new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
                                      new XAttribute("sourceRef", getStartProcessElement.Attribute("id").Value), new XAttribute("targetRef", userTaskId)),
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name), new XAttribute(camunda + "candidateGroups", candidateGroups)),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
                                        new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );
                        }
                        else
                        {
                            var getEndEventSrc = getAllElement.Where(x => (x.Attribute("targetRef") != null ? x.Attribute("targetRef").Value : "") == getEndProcessElement.Attribute("id").Value).FirstOrDefault();
                            getAllElement.Where(m => m.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name), new XAttribute(camunda + "candidateGroups", candidateGroups)),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
                                        new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );

                            getAllElement.Where(x => x.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", userTaskId);
                        }
                    }

                    xmlDocument.Save(fileStream);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    //XDocument x1 = XDocument.Load(fileStream);
                    fileStream.Position = 0;

                    await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

                    modelObj.Id = userTaskId;
                    modelObj.SelectedMemberName = BpmnUtility.GetMemberName(caller, "", model.SelectedMemberId);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return modelObj;
        }
        public async Task<List<TaskViewModel>> GetAllTask(UserOrganizationModel caller, long localId)
        {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    PermissionsUtility.Create(s, caller).CanView(PermItem.ResourceType.CoreProcess, localId);

                    var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                    if (getProcessDefFileDetails != null)
                    {
                        XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                        var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask");

                        foreach (var item in getAllElement)
                        {
                            var getCandidateGroup = (item.Attribute(camunda + "candidateGroups") != null ? (item.Attribute(camunda + "candidateGroups").Value) : "");
                            taskList.Add(new TaskViewModel()
                            {
                                description = (item.Attribute("description") != null ? item.Attribute("description").Value : ""),
                                name = item.Attribute("name").Value,
                                Id = item.Attribute("id").Value,
                                SelectedMemberId = BpmnUtility.GetMemberIds(getCandidateGroup),
                                SelectedMemberName = BpmnUtility.GetMemberName(caller, getCandidateGroup, null),
                                CandidateList = GetCandidateGroupList(caller, getCandidateGroup)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return taskList;
        }

        public async Task<List<TaskViewModel>> GetAllTaskByTeamId(UserOrganizationModel caller, long teamId)
        {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    var getProcessList = s.QueryOver<ProcessDef_Camunda>().Where(t => t.DeleteTime == null && t.OrgId == caller.Organization.Id).List().ToList();
                    foreach (var item in getProcessList)
                    {
                        var getTaskList = await GetAllTaskByTeamId(s, caller, item.Id, teamId);
                        if (getTaskList.Any())
                            taskList.AddRange(getTaskList);
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return taskList;
        }

        public async Task<List<TaskViewModel>> GetAllTaskByTeamId(ISession s, UserOrganizationModel caller, long localId, long teamId)
        {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            try
            {
                PermissionsUtility.Create(s, caller).CanView(PermItem.ResourceType.CoreProcess, localId);
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                    var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask");

                    foreach (var item in getAllElement)
                    {
                        var getCandidateGroup = (item.Attribute(camunda + "candidateGroups") != null ? (item.Attribute(camunda + "candidateGroups").Value) : "");
                        var memberIds = BpmnUtility.GetMemberIds(getCandidateGroup).ToList();
                        if (memberIds.Contains(teamId))
                        {
                            taskList.Add(new TaskViewModel()
                            {
                                description = (item.Attribute("description") != null ? item.Attribute("description").Value : ""),
                                name = item.Attribute("name").Value,
                                Id = item.Attribute("id").Value,
                                // SelectedMemberId = GetMemberIds(getCandidateGroup),
                                // SelectedMemberName = GetMemberName(caller, getCandidateGroup, null),
                                // CandidateList = GetCandidateGroupList(caller, getCandidateGroup)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return taskList;
        }

        public async Task<List<TaskViewModel>> GetTaskListByCandidateGroups(UserOrganizationModel caller, long[] candidateGroupIds, string processInstanceId = "", bool unassigned = false)
        {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    PermissionsUtility.Create(s, caller);
                    //var candidateGroups = String.Join(",", candidateGroupIds.Select(x => "rgm_" + x));
                    CommClass comClass = new CommClass();
                    var getUsertaskList = await comClass.GetTaskByCandidateGroups(candidateGroupIds, processInstanceId, unassigned);

                    foreach (var item in getUsertaskList)
                    {
                        taskList.Add(new TaskViewModel()
                        {
                            name = item.Name,
                            Id = item.Id,
                            Assignee = item.Assignee,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return taskList;
        }

        public async Task<List<TaskViewModel>> GetTaskListByUserId(UserOrganizationModel caller, string userId)
        {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    PermissionsUtility.Create(s, caller);
                    string _userId = "u_" + userId;
                    CommClass comClass = new CommClass();
                    var getUsertaskList = await comClass.GetTaskListByAssignee(_userId);

                    foreach (var item in getUsertaskList)
                    {
                        taskList.Add(new TaskViewModel()
                        {
                            name = item.Name,
                            Assignee = ((!string.IsNullOrEmpty(item.Assignee)) ? item.Assignee : ""),
                            Id = item.Id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return taskList;
        }


        public async Task<List<TaskViewModel>> GetTaskListByProcessInstanceId(UserOrganizationModel caller, string processInstanceId)
        {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    PermissionsUtility.Create(s, caller);
                    CommClass comClass = new CommClass();
                    var getUsertaskList = await comClass.GetTaskListByInstanceId(processInstanceId);

                    foreach (var item in getUsertaskList)
                    {
                        taskList.Add(new TaskViewModel()
                        {
                            name = item.Name,
                            Id = item.Id,
                            Assignee = ((!string.IsNullOrEmpty(item.Assignee)) ? item.Assignee : ""),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return taskList;
        }


        public async Task<TaskViewModel> GetTaskById(UserOrganizationModel caller, string taskId)
        {
            TaskViewModel task = new TaskViewModel();
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    PermissionsUtility.Create(s, caller);
                    CommClass comClass = new CommClass();
                    var getUsertask = await comClass.GetTaskById(taskId);

                    if (getUsertask != null)
                    {
                        task.name = getUsertask.Name;
                        task.Id = getUsertask.Id;
                        task.Assignee = ((!string.IsNullOrEmpty(getUsertask.Assignee)) ? getUsertask.Assignee : "");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return task;
        }


        /// <summary>
        /// Note: The difference with claim a task is that this method does not check if the task already has a user assigned to it.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="taskId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<bool> TaskAssignee(UserOrganizationModel caller, string taskId, long userId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    //perms.InValidPermission();

                    string _userId = "u_" + userId;
                    CommClass commClass = new CommClass();
                    var setAssignee = await commClass.SetAssignee(taskId, _userId);
                    if (setAssignee.TNoContentStatus.ToString() == "Success")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
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
        public async Task<bool> TaskClaim(UserOrganizationModel caller, string taskId, long userId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    string _userId = "u_" + userId;
                    CommClass commClass = new CommClass();
                    var taskClaim = await commClass.TaskClaim(taskId, _userId);
                    if (taskClaim.TNoContentStatus.ToString() == "Success")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return false;
        }

        public async Task<bool> TaskUnClaim(UserOrganizationModel caller, string taskId, long userId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    string _userId = "u_" + userId;
                    CommClass commClass = new CommClass();
                    var taskUnClaim = await commClass.TaskUnClaim(taskId, _userId);
                    if (taskUnClaim.TNoContentStatus.ToString() == "Success")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return false;
        }

        public async Task<bool> TaskComplete(UserOrganizationModel caller, string taskId, long userId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);
                    // check if user is member of candidategroup in task
                    perms.CanEditTask(taskId);
                    CommClass commClass = new CommClass();
                    string _userId = "u_" + userId;
                    var taskComplete = await commClass.TaskComplete(taskId, _userId);
                    if (taskComplete.TNoContentStatus.ToString() == "Success")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return false;
        }

        public async Task<long[]> GetCandidateGroupIdsForTask(UserOrganizationModel caller, string taskId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    CommClass comClass = new CommClass();
                    var getTask = await comClass.GetTaskById(taskId);

                    if (!string.IsNullOrEmpty(getTask.ProcessDefinitionId))
                    {
                        var getProcessDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.CamundaId == getTask.ProcessDefinitionId).SingleOrDefault();
                        if (getProcessDefDetails == null)
                        {
                            throw new PermissionsException("process definition not found");
                        }
                        var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == getProcessDefDetails.Id).SingleOrDefault();
                        if (getProcessDefFileDetails != null)
                        {
                            XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                            var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask");

                            var getTaskDetail = getAllElement.Where(t => t.Attribute("name").Value == getTask.Name).FirstOrDefault();
                            var getCandidateGroup = (getTaskDetail.Attribute(camunda + "candidateGroups") != null ? (getTaskDetail.Attribute(camunda + "candidateGroups").Value) : "");

                            return BpmnUtility.GetMemberIds(getCandidateGroup);
                            //return GetMemberName(caller, getCandidateGroup, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return null;
        }


        public List<CandidateGroupViewModel> GetCandidateGroupList(UserOrganizationModel caller, string candidateGroupName)
        {
            List<CandidateGroupViewModel> list = new List<CandidateGroupViewModel>();
            var getMemberIds = BpmnUtility.GetMemberIds(candidateGroupName);
            ResponsibilitiesAccessor respAccessor = new ResponsibilitiesAccessor();
            string memberName = string.Empty;
            if (getMemberIds != null)
            {
                if (getMemberIds.Any())
                {
                    foreach (var item in getMemberIds)
                    {
                        var getMemberName = respAccessor.GetResponsibilityGroup(caller, item).GetName();
                        if (!string.IsNullOrEmpty(getMemberName))
                        {
                            list.Add(new CandidateGroupViewModel()
                            {
                                Id = item,
                                Name = getMemberName
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<long> GetCandidateGroupIds_UnSafe(ISession s, long localId)
        {
            List<long> candidateGroupIdList = new List<long>();
            try
            {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();

                if (getProcessDefFileDetails != null)
                {
                    XDocument xmlDocument = AsyncHelper.RunSync<XDocument>(() => BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey));
                    var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask");

                    foreach (var item in getAllElement)
                    {
                        var getCandidateGroup = (item.Attribute(camunda + "candidateGroups") != null ? (item.Attribute(camunda + "candidateGroups").Value) : "");
                        var getIds = BpmnUtility.GetMemberIds(getCandidateGroup);
                        foreach (var item1 in getIds)
                        {
                            candidateGroupIdList.Add(item1);
                        }
                    }
                }
                else
                {
                    throw new PermissionsException();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return candidateGroupIdList.ToList();
        }

        public async Task<TaskViewModel> UpdateTask(UserOrganizationModel caller, long localId, TaskViewModel model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                var perms = PermissionsUtility.Create(s, caller);
                var updated = await UpdateTask(s, localId, model, caller, perms);
                return updated;
            }
        }

        public async Task<TaskViewModel> UpdateTask(ISession s, long localId, TaskViewModel model, UserOrganizationModel caller, PermissionsUtility perms)
        {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            TaskViewModel modelObj = new TaskViewModel();
            modelObj = model;
            try
            {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    MemoryStream fileStream = new MemoryStream();
                    XDocument xmlDocument = await BpmnUtility.GetBpmnFileXmlDoc(getProcessDefFileDetails.FileKey);
                    var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();
                    string candidateGroups = string.Empty;
                    if (model.SelectedMemberId != null)
                    {
                        if (model.SelectedMemberId.Any())
                        {
                            foreach (var item in model.SelectedMemberId)
                            {
                                if (string.IsNullOrEmpty(candidateGroups))
                                    candidateGroups = "rgm_" + item;
                                else
                                    candidateGroups += ", rgm_" + item;
                            }
                        }
                    }

                    //update name element
                    getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue("name", model.name);

                    //update description element
                    getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue(camunda + "candidateGroups", candidateGroups);


                    xmlDocument.Save(fileStream);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.Position = 0;

                    await BpmnUtility.UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

                    modelObj.SelectedMemberName = BpmnUtility.GetMemberName(caller, "", model.SelectedMemberId);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return modelObj;
        }

        public async Task<bool> DeleteTask(UserOrganizationModel caller, string taskId, long localId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                var perms = PermissionsUtility.Create(s, caller);
                var result = await DeleteTask(s, localId, taskId, perms);
                return result;
            }
        }

        public async Task<bool> DeleteTask(ISession s, long localId, string taskId, PermissionsUtility perms)
        {
            perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);

            bool result = true;
            try
            {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
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
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;

            }
            return result;
        }

        public IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller, long orgId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewOrganization(orgId);
                    //PermissionsAccessor.GetExplicitPermItemsForUser(s, perms, caller.Id, PermItem.ResourceType.CoreProcess);

                    IEnumerable<ProcessDef_Camunda> processDefList = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == orgId).List();
                    List<ProcessDef_Camunda> finalList = new List<ProcessDef_Camunda>();
                    foreach (var item in processDefList.ToList())
                    {
                        try
                        {
                            perms.CanView(PermItem.ResourceType.CoreProcess, item.LocalId);
                            finalList.Add(item);
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    return finalList;
                }
            }
        }

        public ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, processId);
                    ProcessDef_Camunda processDef = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id && x.Id == processId).SingleOrDefault();
                    return processDef;
                }
            }
        }

        public Stream CreateBpmnFile(string processName, string bpmnId)
        {
            Stream fileStm = new MemoryStream();
            try
            {
                string id = bpmnId.Replace("-", "");
                XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
                XDocument xmldocument = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement(bpmn + "definitions", new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(XNamespace.Xmlns + "bpmn", "http://www.omg.org/spec/BPMN/20100524/MODEL"), new XAttribute(XNamespace.Xmlns + "bpmndi", "http://www.omg.org/spec/BPMN/20100524/DI"), new XAttribute(XNamespace.Xmlns + "dc", "http://www.omg.org/spec/DD/20100524/DC"), new XAttribute(XNamespace.Xmlns + "camunda", "http://camunda.org/schema/1.0/bpmn"), new XAttribute(XNamespace.Xmlns + "di", "http://www.omg.org/spec/DD/20100524/DI"), new XAttribute("id", "Definitions_1"), new XAttribute("targetNamespace", "http://bpmn.io/schema/bpmn"),
                    new XElement(bpmn + "process", new XAttribute("id", processName.Replace(" ", "") + id), new XAttribute("name", processName), new XAttribute("isExecutable", "true"),
                    new XElement(bpmn + "startEvent", new XAttribute("id", "StartEvent"), new XAttribute("name", processName + "&#10;requested")),
                    new XElement(bpmn + "endEvent", new XAttribute("id", "EndEvent"), new XAttribute("name", processName + "&#10;finished")))));

                // Save XDocument into the stream
                xmldocument.Save(fileStm);
                fileStm.Seek(0, SeekOrigin.Begin);
                fileStm.Position = 0;
            }
            catch (Exception)
            {
                throw;
            }
            return fileStm;
        }

        public async Task<bool> ModifiyBpmnFile(UserOrganizationModel caller, long localId, int oldOrder, int newOrder)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanEdit(PermItem.ResourceType.CoreProcess, localId);
                    var result = await ModifyBpmnFile(s, localId, oldOrder, newOrder);
                    return result;
                }
            }
        }

        public async Task<bool> ModifyBpmnFile(ISession s, long localId, int oldOrder, int newOrder)
        {
            bool result = true;
            try
            {
                string oldOrderId = string.Empty;
                string newOrderId = string.Empty;
                string name = string.Empty;
                string candidateGroups = string.Empty;
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    var getStream = await BpmnUtility.GetFileFromServer(getProcessDefFileDetails.FileKey);

                    BpmnUtility.GetNodeDetail(getStream, oldOrder, newOrder, out oldOrderId, out newOrderId, out name, out candidateGroups);

                    //Remove all elements under root node
                    var de_stream = BpmnUtility.DetachNode(getStream, oldOrderId);

                    //Insert element

                    var ins_stream = BpmnUtility.InsertNode(de_stream, oldOrder, newOrder, oldOrderId, newOrderId, name, candidateGroups);

                    //stream upload
                    await BpmnUtility.UploadFileToServer(ins_stream, getProcessDefFileDetails.FileKey);
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }


        public async Task<List<TaskViewModel>> GetTaskListByProcessDefId(UserOrganizationModel caller, List<string> processDefId)
        {
            var selectedList = new List<TaskViewModel>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                var perms = PermissionsUtility.Create(s, caller);
                var getProcessDefList = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null
                && x.OrgId == perms.GetCaller().Organization.Id).List();


                CommClass commClass = new CommClass();
                var getTaskList = await commClass.GetTaskListByProcessDefId(processDefId[0]);
                foreach (var item in getProcessDefList.ToList())
                {
                    var getProcessInstanceList = getTaskList.Where(t => (t.ProcessDefinitionId ?? "").Contains(item.CamundaId)).ToList().Select(x => new TaskViewModel()
                    {
                        Id = x.Id,
                        name = x.Name ?? ""
                    }).ToList();

                    if (getProcessInstanceList.Any())
                        selectedList.AddRange(getProcessInstanceList);
                }
            }
            return selectedList;
        }
    }




}