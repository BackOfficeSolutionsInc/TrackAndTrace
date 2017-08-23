﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using CamundaCSharpClient.Model;
using CamundaCSharpClient.Model.Task;
using CamundaCSharpClient.Helper;
using System.Threading.Tasks;

namespace CamundaCSharpClient.Query.Task
{
    public class TaskQuery : QueryBase
    {
        private TaskQueryModel model = new TaskQueryModel();

        public TaskQuery(CamundaRestClient client)
            : base(client)
        {
        }

        public TaskQuery Id(string id)
        {
            this.model.id = id;
            return this;
        }

        public TaskQuery UserId(string userId)
        {
            this.model.userId = userId;
            return this;
        }

        /// <summary>Claim a task for a specific user.
        /// Note: The difference with set a assignee is that here a check is performed to see if the task already has a user assigned to it.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk1 = camundaCl.Task().Id("37ccd7fe-78c5-11e5-beb3-40a8f0a54b22").UserId("salajlan").Claim();
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Claim()
        {
            EnsureHelper.NotNull("Id", this.model.id);
            EnsureHelper.NotNull("userId", this.model.userId);

            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/claim";
            request.Method = Method.POST;
            object obj = new { this.model.userId };
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp = await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary> Delegate a task to another user.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk7 = camundaCl.Task().Id("c4d3d6e8-78b1-11e5-a68d-40a8f0a54b22").UserId("salajlan").Delegate();
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Delegate()
        {
            EnsureHelper.NotNull("Id", this.model.id);
            EnsureHelper.NotNull("userId", this.model.userId);

            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/delegate";
            request.Method = Method.POST;
            object obj = new { this.model.userId };
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp = await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary> Resets a task's assignee. If successful, the task is not assigned to a user.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk5 = camundaCl.Task().Id("37ccd7fe-78c5-11e5-beb3-40a8f0a54b22").UnClaim();           
        /// </code>
        /// </example>
        public async Task<NoContentStatus> UnClaim()
        {
            EnsureHelper.NotNull("Id", this.model.id);

            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/unclaim";
            request.Method = Method.POST;
            var resp = await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary> Retrieves the form key for a task
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk5 = camundaCl.Task().Id("37ccd7fe-78c5-11e5-beb3-40a8f0a54b22").formKey();           
        /// </code>
        /// </example>
        public async Task<TaskFormKeyModel> formKey()
        {
            EnsureHelper.NotNull("Id", this.model.id);

            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/form";
            request.Method = Method.GET;
            var resp = this.client.Execute(request);
            return await this.client.Execute<TaskFormKeyModel>(request);
        }

        /// <summary> Complete a task and update process variables.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var obj = new { amount = new invoice.Amount() { value = "resolve" } };
        /// var tsk6 = camundaCl.Task().Id("ebb5bc85-789e-11e5-ac86-40a8f0a54b22").Complete(obj);
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Complete(object variables)
        {
            EnsureHelper.NotNull("Id", this.model.id);

            var request = new RestRequest();
            request.Method = Method.POST;
            request.Resource = "/task/" + this.model.id + "/complete";
            object obj = new { variables };
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp = await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary> Resolve a task and update execution variables.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var obj = new { amount = new invoice.Amount() { value = "resolve" } };
        /// var tsk9 = camundaCl.Task().Id("c4d3d6e8-78b1-11e5-a68d-40a8f0a54b22").Resolve(obj);
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Resolve(object variables)
        {
            EnsureHelper.NotNull("Id", this.model.id);

            var request = new RestRequest();
            request.Method = Method.POST;
            request.Resource = "/task/" + this.model.id + "/resolve";
            object obj = new { variables };
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp = await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary>Change the assignee of a task to a specific user.
        /// Note: The difference with claim a task is that this method does not check if the task already has a user assigned to it.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk2 = camundaCl.Task().Id("37ccd7fe-78c5-11e5-beb3-40a8f0a54b22").UserId("salajlan").Assignee();
        /// </code>
        /// </example>
        public async Task<NoContentStatus> Assignee()
        {
            EnsureHelper.NotNull("Id", this.model.id);
            EnsureHelper.NotNull("userId", this.model.userId);

            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/assignee";
            request.Method = Method.POST;
            object obj = new { this.model.userId };
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            var resp = await this.client.Execute(request);
            return ReturnHelper.NoContentReturn(resp.Content, resp.StatusCode);
        }

        /// <summary> Gets the comments for a task. or Retrieves a single task comment by task id and comment id.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk12 = camundaCl.Task().Id("a3d0eeb5-78c4-11e5-beb3-40a8f0a54b22").Comment();
        /// </code>
        /// </example>
        public async Task<List<TaskCommentModel>> Comment()
        {
            EnsureHelper.NotNull("Id", this.model.id);
            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/comment";
            return await this.client.Execute<List<TaskCommentModel>>(request);
        }

        /// <summary> Retrieves a single task comment by task id and comment id.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk10 = camundaCl.Task().Id("a3d0eeb5-78c4-11e5-beb3-40a8f0a54b22").Comment("d7a2ea89-7cae-11e5-beb3-40a8f0a54b22");
        /// </code>
        /// </example>
        public async Task<TaskCommentModel> Comment(string commentId)
        {
            EnsureHelper.NotNull("Id", this.model.id);
            EnsureHelper.NotNull("ComemntId", commentId);
            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/comment/" + commentId;
            return await this.client.Execute<TaskCommentModel>(request);
        }

        /// <summary> Create a comment for a task.
        /// </summary>
        /// <param name="comment"> comment to be create</param>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk11 = camundaCl.Task().Id("a3d0eeb5-78c4-11e5-beb3-40a8f0a54b22").Create("test Comment");
        /// </code>
        /// </example>
        public async Task<TaskCommentModel> Create(string comment)
        {
            EnsureHelper.NotNull("Id", this.model.id);
            EnsureHelper.NotNull("commentMessage", comment);

            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id + "/comment/create";
            request.Method = Method.POST;
            object obj = new { message = comment };
            string output = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", output, ParameterType.RequestBody);
            return await this.client.Execute<TaskCommentModel>(request);
        }

        /// <summary> Query for tasks that fulfill a given filter.
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk3 = camundaCl.Task().Get().ProcessInstanceId("37ccd7f9-78c5-11e5-beb3-40a8f0a54b22").list();
        /// </code>
        /// </example>
        public GetTaskQuery Get()
        {
            return new GetTaskQuery(this.client);
        }

        /// <summary> Retrieves a single task
        /// </summary>
        /// <example> 
        /// <code>
        /// var camundaCl = new camundaRestClient("http://localhost:8080/engine-rest");
        /// var tsk13 = camundaCl.Task().Id("37ccd7fe-78c5-11e5-beb3-40a8f0a54b22").singleResult();
        /// </code>
        /// </example>
        public async Task<TaskModel> SingleResult()
        {
            EnsureHelper.NotNull("Id", this.model.id);
            var request = new RestRequest();
            request.Resource = "/task/" + this.model.id;
            return await this.SingleResult<TaskModel>(request);
        }
    }
}
