﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CamundaCSharpClient.Helper;
using CamundaCSharpClient.Model;
using RestSharp;
using Newtonsoft.Json;
using CamundaCSharpClient.Model.ProcessInstance;
using System.Threading.Tasks;

namespace CamundaCSharpClient.Query.ProcessInstance
{
    public class ProcessInstanceQuery : QueryBase
    {
        private ProcessInstanceQueryModel model = new ProcessInstanceQueryModel();

        public ProcessInstanceQuery(CamundaRestClient client)
            : base(client)
        {
        }                

        public ProcessInstanceQuery Id(string id)
        {
            this.model.id = id; 
            return this;
        }

        public ProcessInstanceQuery VarId(string varId)
        {
            this.model.varId = varId;
            return this;
        }

        public ProcessInstanceQuery DeserializeValues(bool deserializeValues)
        {
            this.model.deserializeValues = deserializeValues.ToString().ToLower();
            return this;
        }

        public ProcessInstanceQuery ProcessDefinitionId(string processDefinitionId)
        {
            this.model.processDefinitionId = processDefinitionId;
            return this;
        }

        public ProcessInstanceQuery ProcessDefinitionKey(string processDefinitionKey)
        {
            this.model.processDefinitionKey = processDefinitionKey;
            return this;
        }

        public ProcessInstanceQuery Suspended(bool suspended)
        {
            this.model.suspended = suspended.ToString().ToLower();
            return this;
        }        

        /// <summary>
        /// Query for process instances that fulfill given parameters. Parameters may be static as well as dynamic runtime properties of process instances.
        /// If there is no parameter it will retrieve all process instances available.
        /// ex : camundaCl.getProcessInstances(businessKey: "testBusinessKey",processInstanceIds:prcs,suspended:true);
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi1 = camundaCl.ProcessInstance().Get().BusinessKey("hi").list();
        /// </code>
        /// </example>
        public GetProcessInstanceQuery Get()
        {
            return new GetProcessInstanceQuery(client);
        }

        /// <summary>
        /// Retrieves a single process instance according to the ProcessInstance interface in the engine.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");   
        /// var pi = camundaCl.ProcessInstance().Id("a0efef43-7d51-11e5-beb3-40a8f0a54b22").SingleResult();
        /// </code>
        /// </example>
        public async Task<processInstanceModel> SingleResult()
        {
            EnsureHelper.NotNull("ProcessInstanceId", this.model.id);
            var request = new RestRequest();
            request.Resource = "/process-instance/" + this.model.id;
            return await base.SingleResult<processInstanceModel>(request);
        }

        /// <summary>Deletes a running process instance. or Deletes a variable of a given process instance.
        /// </summary>
        /// <example> Deletes a running process instance
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi3 = camundaCl.ProcessInstance().Id("182112c8-78c1-11e5-beb3-40a8f0a54b22").Delete();
        /// </code>
        /// </example>
        /// <example> Deletes a variable of a given process instance.
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi3 = camundaCl.ProcessInstance().Id("182112c8-78c1-11e5-beb3-40a8f0a54b22").VarId("test varriable").Delete();
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Delete()
        {
            EnsureHelper.NotNull("ProcessInstanceId", this.model.id);
            var request = new RestRequest();
            if (this.model.varId != null)
            {
                request.Resource = "/process-instance/" + this.model.id + "/variables/" + this.model.varId;
            }
            else
            {
                request.Resource = "/process-instance/" + this.model.id; 
            }

            request.Method = Method.DELETE;
            var resp =await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary>
        /// Retrieves a variables of a given process instance or given Variable Id
        /// </summary>
        /// <example> retrive by variables by process instance Id
        /// <code>
        /// 
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi5 = camundaCl.ProcessInstance().Id("84b04b82-7cbc-11e5-beb3-40a8f0a54b22").Variables<invoice.InvoiceRootObject>();
        ///
        /// </code>
        /// </example>
        /// <example> retrive by variable by process instance Id and variable Id
        /// <code>
        /// 
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi4 = camundaCl.ProcessInstance().Id("84b04b82-7cbc-11e5-beb3-40a8f0a54b22").VarId("amount").Variables<invoice.amount>();
        ///
        /// </code>
        /// </example>
        public async Task<T> Variables<T>() where T : new()
        {
            EnsureHelper.NotNull("ProcessInstanceId", this.model.id);
            var request = new RestRequest();
            if (this.model.varId != null)
            {
                request.Resource = "/process-instance/" + this.model.id + "/variables/" + this.model.varId;
            }
            else
            {
                request.Resource = "/process-instance/" + this.model.id + "/variables";
            }

            string output = JsonConvert.SerializeObject(this.model.deserializeValues);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            return await client.Execute<T>(request);
        }

        /// <summary>
        /// Updates or deletes the variables of a process instance. Updates precede deletions. So, if a variable is updated AND deleted, the deletion overrides the update.
        /// </summary>        
        /// <param name="modifications">A JSON object containing variable key-value pairs. Each key is a variable name and each value a JSON variable value object</param>   
        /// <param name="deletions">An array of String keys of variables to be deleted.</param>
        /// <example>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var obj = new { amount = new invoice.Amount() { value = "modification" } };
        /// var pi6 = camundaCl.ProcessInstance().Id("84b04b82-7cbc-11e5-beb3-40a8f0a54b22").Variables<object>(obj, new string[] { "amount" });
        /// <code>
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Variables<T>(T modifications, string[] deletions)
        {
            EnsureHelper.NotNull("ProcessInstanceId", this.model.id);
            var request = new RestRequest();
            request.Resource = "/process-instance/" + this.model.id + "/variables";
            request.Method = Method.POST;
            object obj = new { modifications, deletions };
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp =await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary>
        /// Sets a variable of a given process instance
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi7 = camundaCl.ProcessInstance().Id("84b04b82-7cbc-11e5-beb3-40a8f0a54b22").VarId("DeptHead").Variables<invoice.DeptHead>(new invoice.DeptHead() { value = "salajlan" });
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Variables<T>(T variable)
        {
            EnsureHelper.NotNull("ProcessInstanceId", this.model.id);
            EnsureHelper.NotNull("variableData", variable);
            EnsureHelper.NotNull("varId", this.model.varId);
            var request = new RestRequest();
            request.Resource = "/process-instance/" + this.model.id + "/variables/" + this.model.varId;
            request.Method = Method.PUT;
            string output = JsonConvert.SerializeObject(variable);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp =await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary>
        /// Activate or suspend a given process instance.
        /// Activate or suspend process instances with the given process definition id.
        /// Activate or suspend process instances with the given process definition key.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi9 = camundaCl.ProcessInstance().Id("a8a4755f-78b2-11e5-878f-40a8f0a54b22").Suspended(false).Suspend();
        /// var pi10 = camundaCl.ProcessInstance().ProcessDefinitionKey("invoice").Suspended(false).Suspend();
        /// var pi11 = camundaCl.ProcessInstance().ProcessDefinitionId("invoice:1:54302a7a-7736-11e5-bc04-40a8f0a54b22").Suspended(true).Suspend();
        /// </code>
        /// </example>
        /// <returns>noContentStatus</returns>
        public async Task<NoContentStatus> Suspend()
        {
            EnsureHelper.NotNull("Suspended", this.model.suspended);
            object obj;
            var request = new RestRequest();
            request.Resource = "/process-instance/suspended";
            if (this.model.id != null)
            {
                request.Resource = "/process-instance/" + this.model.id + "/suspended";
                obj = new { this.model.suspended };
            }
            else if (this.model.processDefinitionId == null)
            {
                EnsureHelper.NotNull("processDefinitionKey", this.model.processDefinitionKey);
                obj = new { this.model.processDefinitionKey, this.model.suspended };
            }
            else
            {
                EnsureHelper.NotNull("processDefinitionId", this.model.processDefinitionId);
                obj = new { this.model.processDefinitionId, this.model.suspended };
            }

            request.Method = Method.PUT;
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp =await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary>
        /// Retrieves an Activity Instance (Tree) for a given process instance.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var pi12 = camundaCl.ProcessInstance().Id("14cc53f0-8067-11e5-ac78-40a8f0a54b22").ActivityInstance();
        /// </code>
        /// </example>
        /// <returns>activityInstance</returns>
        public async Task<ActivityInstanceModel> ActivityInstance()
        {
            EnsureHelper.NotNull("processInstanceId", this.model.id);
            var request = new RestRequest();
            request.Resource = "/process-instance/" + this.model.id + "/activity-instances";
            return await client.Execute<ActivityInstanceModel>(request);
        }
    }
}
