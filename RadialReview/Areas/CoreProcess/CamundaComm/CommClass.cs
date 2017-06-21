using CamundaCSharpClient;
using CamundaCSharpClient.Model.Task;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.CamundaComm
{
    public class CommClass : ICommClass
    {
        // create new camunda rest client
        CamundaRestClient client = new CamundaRestClient("http://localhost:8080/engine-rest");
        public IProcessDef GetProcessDefByKey(string key)
        {
            // Call API and get JSON
            // Serialize JSON into IProcessDef

            return new ProcessDef();
        }

        public IEnumerable<ITask> GetTaskList()
        {
            client.Authenticator("demo", "demo");            
            return client.Task().Get().list().Select(s => new Task(s));
        }
    }

    public class ProcessDef : IProcessDef
    {
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
}
