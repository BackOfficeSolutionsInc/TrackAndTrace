using CamundaCSharpClient;
using CamundaCSharpClient.Model;
using CamundaCSharpClient.Model.ProcessDefinition;
using CamundaCSharpClient.Model.ProcessInstance;
using CamundaCSharpClient.Model.Task;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static RadialReview.Utilities.Config;

namespace RadialReview.Areas.CoreProcess.CamundaComm
{
    public class CommClass : ICommClass
    {

        // create new camunda rest client
        //"http://localhost:8080/engine-rest"		
        CamundaRestClient client = new CamundaRestClient(Config.GetCamundaServer().Url);


        #region -----Process-----

        public async Task<IProcessDef> GetProcessDefByKey(string key)
        {
            // Call API and get JSON
            // Serialize JSON into IProcessDef
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            var getProcessDef = await client.ProcessDefinition().Key(key).singleResult();
            return new ProcessDef(getProcessDef);
        }


        public string Deploy(string key, List<object> files)
        {
            // Call API and get JSON
            // Serialize JSON into IProcessDef
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            var result = client.Deployment().Deploy(key, files);
            return result;
        }


        public async Task<processInstanceModel> ProcessStart(string id)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            var result = await client.ProcessDefinition().Id(id).Start<object>(new object());
            return result;
        }

        public async Task<NoContentStatus> ProcessSuspend(string id, bool isSuspend)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            var result = await client.ProcessInstance().Id(id).Suspended(isSuspend).Suspend();
            return result;
        }
        public async Task<int> GetProcessInstanceCount(string processDefId)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            var list = await client.ProcessInstance().Id(processDefId).Get().list();
            return list.Count();
        }

        public async Task<IEnumerable<IProcessInstance>> GetProcessInstanceList(string processDefId)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            var getList = await client.ProcessInstance().Id(processDefId).Get().list();
            var processInstances = getList.Select(s => new ProcessInstance(s));
            return processInstances;
        }
        #endregion

        #region ----Task------

        public async Task<IEnumerable<TaskModel>> GetTaskByCandidateGroup(string candidateGroup)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            var getList = await client.Task().Get().CandidateGroup(candidateGroup).list();
            return getList;
        }

        public async Task<IEnumerable<TaskModel>> GetTaskList(string processDefId)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            return await client.Task().Get().ProcessDefinitionId(processDefId).list();
        }

        public async Task<IEnumerable<TaskModel>> GetTaskList(List<string> processDefId)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            return await client.Task().Get().ProcessDefinitionKeyIn(processDefId).list();
        }

        public async Task<IEnumerable<TaskModel>> GetTaskListByInstanceId(string InstanceId)
        {
            client.Authenticator(Config.GetCamundaServer().Username, Config.GetCamundaServer().Password);
            return await client.Task().Get().ProcessInstanceId(InstanceId).list();
        }

        #endregion
    }

    public class ProcessDef : IProcessDef
    {

        public ProcessDef(ProcessDefinitionModel processDef)
        {
            Id = processDef.Id;
            description = processDef.Description != null ? processDef.Description.ToString() : "";
            key = processDef.Key;
            name = processDef.Name;
        }
        public string category
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string deploymentId
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool suspended
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Id { get; set; }
        public string description { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public string Getdescription()
        {
            return description;
        }
        public string GetId()
        {
            return Id;
        }
        public string Getkey()
        {
            return key;
        }
        public string Getname()
        {
            return name;
        }
    }

    public class Task : ITask
    {
        public Task(TaskModel task)
        {
            assignee = task.Assignee ?? "";
            processInstanceId = task.ProcessInstanceId ?? "";
            due = task.Due != null ? task.Due.ToString() : "";
            description = task.Description != null ? task.Description.ToString() : "";
            id = task.Id;
            name = task.Name;
            owner = task.Owner != null ? task.Owner.ToString() : "";
            processDefinitionId = task.ProcessDefinitionId ?? "";
        }
        public string id { get; set; }
        public string name { get; set; }
        public string assignee { get; set; }
        public string due { get; set; }
        public string description { get; set; }
        public string owner { get; set; }
        public string processDefinitionId { get; set; }
        public string processInstanceId { get; set; }

        public string GetAssignee()
        {
            return assignee;
        }

        public string GetDescription()
        {
            return description;
        }

        public string GetDue()
        {
            return due;
        }

        public string GetId()
        {
            return id;
        }

        public string GetName()
        {
            return name;
        }

        public string GetOwner()
        {
            return owner;
        }

        public string GetprocessDefinitionId()
        {
            return processDefinitionId;
        }

        public string GetProcessInstanceId()
        {
            return processInstanceId;
        }
    }

    public class ProcessInstance : IProcessInstance
    {

        public ProcessInstance(processInstanceModel processInstanceModel)
        {
            Id = processInstanceModel.Id;
            DefinitionId = processInstanceModel.DefinitionId;
            BusinessKey = processInstanceModel.BusinessKey ?? "";
            CaseInstanceId = (processInstanceModel.CaseInstanceId != null ? processInstanceModel.CaseInstanceId.ToString() : "");
            Ended = processInstanceModel.Ended;
            Suspended = processInstanceModel.Suspended;
        }
        public string Id { get; set; }

        public string DefinitionId { get; set; }

        public string BusinessKey { get; set; }

        public string CaseInstanceId { get; set; }

        public bool Ended { get; set; }

        public bool Suspended { get; set; }

        public string GetId()
        {
            return Id;
        }

        public string GetDefinitionId()
        {
            return DefinitionId;
        }

        public string GetBusinessKey()
        {
            return BusinessKey;
        }

        public string GetCaseInstanceId()
        {
            return CaseInstanceId;
        }

        public bool GetEnded()
        {
            return Ended;
        }

        public bool GetSuspended()
        {
            return Suspended;
        }
    }
}
