﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CamundaCSharpClient.Model;
using CamundaCSharpClient.Helper;
using RestSharp;
using Newtonsoft.Json;
using CamundaCSharpClient.Model.ProcessDefinition;
using CamundaCSharpClient.Model.ProcessInstance;
using System.Threading.Tasks;

namespace CamundaCSharpClient.Query
{
    public class ProcessDefinitionQuery : QueryBase
    {
        private ProcessDefinitionQueryModel model = new ProcessDefinitionQueryModel();

        public ProcessDefinitionQuery(CamundaRestClient client)
            : base(client)
        {
        }        

        public ProcessDefinitionQuery Id(string id)
        {
            this.model.id = id; 
            return this;
        }

        public ProcessDefinitionQuery Name(string name)
        {
            this.model.name = name;
            return this;
        }

        public ProcessDefinitionQuery BusinessKey(string businessKey)
        {
            this.model.businessKey = businessKey;
            return this;
        }

        public ProcessDefinitionQuery CaseInstanceId(string caseInstanceId)
        {
            this.model.caseInstanceId = caseInstanceId;
            return this;
        }

        public ProcessDefinitionQuery NameLike(string nameLike)
        {
            this.model.nameLike = nameLike;
            return this;
        }

        public ProcessDefinitionQuery DeploymentId(string deploymentId)
        {
            this.model.deploymentId = deploymentId;
            return this;
        }

        public ProcessDefinitionQuery Key(string key)
        {
            this.model.key = key;
            return this;
        }

        public ProcessDefinitionQuery KeyLike(string keyLike)
        {
            this.model.keyLike = keyLike;
            return this;
        }

        public ProcessDefinitionQuery Category(string category)
        {
            this.model.category = category;
            return this;
        }

        public ProcessDefinitionQuery CategoryLike(string categoryLike)
        {
            this.model.categoryLike = categoryLike;
            return this;
        }

        public ProcessDefinitionQuery Version(int version)
        {
            this.model.version = version;
            return this;
        }

        public ProcessDefinitionQuery LatestVersion(bool latestVersion)
        {
            this.model.latestVersion = latestVersion.ToString().ToLower();
            return this;
        }

        public ProcessDefinitionQuery ResourceName(string resourceName)
        {
            this.model.resourceName = resourceName;
            return this;
        }

        public ProcessDefinitionQuery ResourceNameLike(string resourceNameLike)
        {
            this.model.resourceNameLike = resourceNameLike;
            return this;
        }

        public ProcessDefinitionQuery StartableBy(string startableBy)
        {
            this.model.startableBy = startableBy;
            return this;
        }

        public ProcessDefinitionQuery Active(bool active)
        {
            this.model.active = active.ToString().ToLower();
            return this;
        }

        public ProcessDefinitionQuery Suspended(bool suspended)
        {
            this.model.suspended = suspended.ToString().ToLower();
            return this;
        }

        public ProcessDefinitionQuery IncidentId(string incidentId)
        {
            this.model.incidentId = incidentId;
            return this;
        }

        public ProcessDefinitionQuery IncidentType(string incidentType)
        {
            this.model.incidentType = incidentType;
            return this;
        }

        public ProcessDefinitionQuery IncidentMessage(string incidentMessage)
        {
            this.model.incidentMessage = incidentMessage;
            return this;
        }

        public ProcessDefinitionQuery IncidentMessageLike(string incidentMessageLike)
        {
            this.model.incidentMessageLike = incidentMessageLike;
            return this;
        }

        public ProcessDefinitionQuery FirstResult(int firstResult)
        {
            this.model.firstResult = firstResult;
            return this;
        }

        public ProcessDefinitionQuery MaxResults(int maxResults)
        {
            this.model.maxResults = maxResults;
            return this;
        }

        public ProcessDefinitionQuery SortByNSortOrder(ProcessDefinitionQueryModel.SortByValue sortBy, string sortOrder)
        {
            this.model.sortBy = Enum.GetName(sortBy.GetType(), sortBy);
            this.model.sortOrder = sortOrder;
            return this;
        }        

        /// <summary> Query for process definitions that fulfill given parameters.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pd1 = camundaCl.ProcessDefinition().Suspended(false).list();
        /// </code>
        /// </example>
        public async Task<List<ProcessDefinitionModel>> list()
        {
            var request = new RestRequest();
            request.Resource = "/process-definition";
            return await this.List<ProcessDefinitionModel>(QueryHelper.BuildQuery<ProcessDefinitionQueryModel>(this.model, request));
        }

        /// <summary> Retrieves a single process definition according to the ProcessDefinition interface in the engine.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pd7 = camundaCl.ProcessDefinition().Key("invoice").singleResult();
        /// </code>
        /// </example>
        public async Task<ProcessDefinitionModel> singleResult()
        {
            var request = new RestRequest();
            if (this.model.id != null) 
            {
                request.Resource = "/process-definition/" + this.model.id; 
            }
            else
            {
                EnsureHelper.NotNull("processDefiniftionKey", this.model.key);
                request.Resource = "/process-definition/key/" + this.model.key;
            }

            return await this.SingleResult<ProcessDefinitionModel>(request);
        }

        /// <summary> Request the number of process definitions that fulfill the query criteria
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pd2 = camundaCl.ProcessDefinition().Suspended(false).count();
        /// </code>
        /// </example>
        public async Task<Count> count()
        {
            var request = new RestRequest();
            request.Resource = "/process-definition/count";
            return await this.Count(QueryHelper.BuildQuery<ProcessDefinitionQueryModel>(this.model, request));
        }

        /// <summary> Retrieves the BPMN 2.0 XML of this process definition.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pd3 = camundaCl.ProcessDefinition().Key("invoice").Xml();
        /// var pd4 = camundaCl.ProcessDefinition().Id("invoice:1:54302a7a-7736-11e5-bc04-40a8f0a54b22").Xml();
        /// </code>
        /// </example>
        public async Task<ProcessDefinitionXMLModel> Xml()
        {
            var request = new RestRequest();
            if (this.model.id != null) 
            {
                request.Resource = "/process-definition/" + this.model.id + "/xml"; 
            }
            else
            {
                EnsureHelper.NotNull("processDefiniftionKey", this.model.key);
                request.Resource = "/process-definition/key/" + this.model.key + "/xml";
            }

            return await client.Execute<ProcessDefinitionXMLModel>(request);
        }

        /// <summary> Instantiates a given process definition.
        /// </summary>
        /// <param name="variables">A JSON object containing the variables the process is to be initialized with.</param>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// invoice.CommunicationRootObject d = new invoice.CommunicationRootObject() { comment = new invoice.Comment() { value = "test" }, DeptHead = new invoice.DeptHead() { value = "salajlan" }, approver = new invoice.Approver() { value = "basim"} };
        /// var pd5 = camundaCl.ProcessDefinition().Key("invoice").BusinessKey("hi").start<invoice.CommunicationRootObject>(d);
        /// </code>
        /// </example>
        public async Task<processInstanceModel> Start<T>(T variables)
        {
            EnsureHelper.NotNull("processDefinitionVariables", variables);
            var request = new RestRequest();
            if (this.model.id != null)
            {
                request.Resource = "/process-definition/" + this.model.id + "/start"; 
            }
            else
            {
                EnsureHelper.NotNull("processDefiniftionKey", this.model.key);
                request.Resource = "/process-definition/key/" + this.model.key + "/start";
            }

            request.Method = Method.POST;
            object obj = new { variables, this.model.businessKey, this.model.caseInstanceId };
            string output = JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            return await client.Execute<processInstanceModel>(request);
        }

        /// <summary> Activate or suspend a given process definition
        /// </summary>
        /// <param name="data"> processDefinitionSuspend object</param>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pr = new processDefinitionSuspend(){ suspended = false, includeProcessInstances = false, executionDate = DateTime.Now };
        /// var pd6 = camundaCl.ProcessDefinition().Key("invoice").Suspend(pr);
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Suspend(ProcessDefinitionSuspendModel data)
        {
            EnsureHelper.NotNull("processDefinitionSuspend data", data);
            var request = new RestRequest();
            if (this.model.id != null)
            {
                request.Resource = "/process-definition/" + this.model.id + "/suspended"; 
            }
            else
            {
                EnsureHelper.NotNull("processDefiniftionKey", this.model.key);
                request.Resource = "/process-definition/key/" + this.model.key + "/suspended";
            }

            request.Method = Method.PUT;
            string output = JsonConvert.SerializeObject(data);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp =await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }
    }
}
