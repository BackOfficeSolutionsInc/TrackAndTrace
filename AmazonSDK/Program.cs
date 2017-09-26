﻿using AmazonSDK.NHibernate;
using LambdaSerializer;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Controllers;
using RadialReview.Hooks;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.CoreProcess;
using RadialReview.Utilities.Encrypt;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmazonSDK {
    class Program {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args) {
            while (true) {
                Scheduler();
                AsyncHelper.RunSync<HttpStatusCode>(() => UpdateTaskRequest());
                //Thread t = new Thread(Scheduler);
                //t.Start();
                Thread.Sleep(500);
            }
            //Scheduler();
        }

        private static void Scheduler() {
            log.Info("Start");
            List<string> receiptHandleList = new List<string>();
            List<MessageQueueModel> getMessages = AsyncHelper.RunSync<List<MessageQueueModel>>(() => GetMessages());  //get List of Messages
            log.Info("Get list of messages count " + getMessages.Count().ToString());

            try {
                foreach (var item in getMessages) {
                    MarkStarted(item);
                    try {
                        //Delete Message from SQS
                        AsyncHelper.RunSync(() => DeleteMessage(item.ReceiptHandle));
                        log.Info("Delete Message from SQS --> Complete ");

                        if (item.RequestType == RequestTypeEnum.isHookRegistryAction) { // this is hook registry process
                                                                                        // exceute action
                                                                                        //var deserializedLambda1 = JsonNetAdapter.Deserialize<SerializableHook>(item.SerializedModel);

                            //dynamic func = JsonNetAdapter.Deserialize(deserializedLambda1.lambda.ToString(), deserializedLambda1.type);

                            //dynamic func = JsonNetAdapter.Deserialize(item.Model.ToString(), item.type);

                            //if (item.type.FullName == "ITodoHook TodoHookModel") {
                            //    HooksRegistry.RegisterHook(new TodoWebhook());
                            //    HooksRegistry.GetHooks<ITodoHook>().ForEach(x => {
                            //        try {
                            //            func.Compile()(x);
                            //        } catch (Exception e) {
                            //            throw;
                            //        }
                            //    });

                            //    //func.Compile();
                            //    //HooksRegistry.Each<ITodoHook>(func);
                            //}
                            //if (item.type.FullName == "IIssueHook IssueHookModel") {
                            //    HooksRegistry.Each<IIssueHook>(func);
                            //}

                        } else if (item.RequestType == RequestTypeEnum.isHTTPRequest) {
                            //Process API
                            //var status = AsyncHelper.RunSync<HttpStatusCode>(() => ApiRequest(new MessageQueueModel() { UserName = "kunal@mytractiontools.com", ApiUrl = "http://app-tractiontools-dev.us-west-2.elasticbeanstalk.com/api/v0/todo/users/mine" }));
                            var status = AsyncHelper.RunSync<HttpStatusCode>(() => ApiRequest(item));

                            // Mark Complete
                            if (status != HttpStatusCode.OK) {
                                throw new Exception("An error ocurred during HTTP Request." + " Status Code:" + status);
                            }
                        }
                        // Mark Complete
                        MarkComplete(item);
                    } catch (Exception ex) {
                        AsyncHelper.RunSync(() => SendMessage(item));
                        log.Error(ex.Message);
                    }
                }
            } catch (Exception ex) {
                log.Error(ex.Message);
            }

        }

        private static void MarkStarted(MessageQueueModel model) {
            using (var s = NHibernate.HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction(System.Data.IsolationLevel.Serializable)) {
                    try {
                        var getMessage = s.QueryOver<MessageQueue>().Where(x => x.IdentifierId == model.Identifier
                        && x.UserName == model.UserName
                        && x.Status == MessageQueueStatus.Start.ToString()
                        ).SingleOrDefault();
                        log.Info("Retreive data [MessageQueue] from DB");

                        if (getMessage == null) {
                            MessageQueue messageQueue = new MessageQueue();
                            messageQueue.IdentifierId = model.Identifier;
                            messageQueue.ReceiptHandle = model.ReceiptHandle;
                            messageQueue.UserOrgId = model.UserOrgId;
                            messageQueue.UserName = model.UserName;
                            messageQueue.Status = MessageQueueStatus.Start.ToString();
                            s.Save(messageQueue);

                            log.Info("Save data [MessageQueue] to DB");
                            tx.Commit();
                        }
                    } catch (Exception ex) {
                        tx.Rollback();
                        log.Error(ex.Message);
                        throw ex;
                    }
                    s.Flush();
                }
            }
        }

        private static async Task SendMessage(MessageQueueModel model) {
            AmazonSQS amazonSQS = new AmazonSQS();
            model.Identifier = Guid.NewGuid().ToString();
            await amazonSQS.SendMessage(model);
        }

        private static async Task<HttpStatusCode> ApiRequest(MessageQueueModel model) {
            try {
                using (var s = NHibernate.HibernateSession.GetCurrentSession(true, "_RV")) {
                    using (var tx = s.BeginTransaction()) {
                        log.Info("Generate token");
                        //get token
                        string pwd = Config.SchedulerSecretKey() + "_" + model.UserName;
                        string encrypt_key = Crypto.EncryptStringAES(pwd, Config.SchedulerSecretKey());

                        //strore key to db
                        TokenIdentifier tokenIdentifierModel = new TokenIdentifier();
                        tokenIdentifierModel.TokenKey = encrypt_key;
                        s.Save(tokenIdentifierModel);
                        tx.Commit();
                        s.Flush();

                        var client = new HttpClient();
                        var param = new List<KeyValuePair<string, string>>();
                        param.Add(new KeyValuePair<string, string>("username", model.UserName)); // hash it 
                        param.Add(new KeyValuePair<string, string>("password", encrypt_key));
                        param.Add(new KeyValuePair<string, string>("grant_type", "password"));
                        param.Add(new KeyValuePair<string, string>("client_id", "self"));
                        var url = Config.GetScedulerHostUrl(); //  System.Configuration.ConfigurationManager.AppSettings["HostName"];
                        var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(param) };
                        TokenModel tokenModel = new TokenModel();
                        HttpResponseMessage response1 = await client.SendAsync(req);

                        log.Info("Token process complete");
                        HttpContent responseContent1 = response1.Content;
                        using (var reader = new StreamReader(await responseContent1.ReadAsStreamAsync())) {
                            var result1 = reader.ReadToEnd();
                            tokenModel = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenModel>(result1.ToString());
                        }

                        if (!string.IsNullOrEmpty(tokenModel.access_token)) {
                            var apiUrl = model.ApiUrl ?? "";
                            if (!string.IsNullOrEmpty(apiUrl)) {
                                log.Info("Calling Api");
                                client = new HttpClient();
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenModel.access_token);
                                HttpResponseMessage response = await client.GetAsync(apiUrl);
                                log.Info("Calling Api complete");
                                return response.StatusCode;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                throw ex;
            }
            return HttpStatusCode.NotFound;
        }


        private static async Task<HttpStatusCode> UpdateTaskRequest() {
            try {
                using (var s = NHibernate.HibernateSession.GetCurrentSession(true, "_RV")) {
                    using (var tx = s.BeginTransaction()) {
                        log.Info("Generate token");
                        //get token
                        string pwd = Config.SchedulerSecretKey() + "_" + Config.UpdateTaskUserName();
                        string encrypt_key = Crypto.EncryptStringAES(pwd, Config.SchedulerSecretKey());

                        //strore key to db
                        TokenIdentifier tokenIdentifierModel = new TokenIdentifier();
                        tokenIdentifierModel.TokenKey = encrypt_key;
                        s.Save(tokenIdentifierModel);
                        tx.Commit();
                        s.Flush();

                        var client = new HttpClient();
                        var param = new List<KeyValuePair<string, string>>();
                        param.Add(new KeyValuePair<string, string>("username", Config.UpdateTaskUserName())); // hash it 
                        param.Add(new KeyValuePair<string, string>("password", encrypt_key));
                        param.Add(new KeyValuePair<string, string>("grant_type", "password"));
                        param.Add(new KeyValuePair<string, string>("client_id", "self"));
                        var url = Config.GetScedulerHostUrl(); // System.Configuration.ConfigurationManager.AppSettings["HostName"];
                        var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(param) };
                        TokenModel tokenModel = new TokenModel();
                        HttpResponseMessage response1 = await client.SendAsync(req);

                        log.Info("Token process complete");
                        HttpContent responseContent1 = response1.Content;
                        using (var reader = new StreamReader(await responseContent1.ReadAsStreamAsync())) {
                            var result1 = reader.ReadToEnd();
                            tokenModel = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenModel>(result1.ToString());
                        }

                        if (!string.IsNullOrEmpty(tokenModel.access_token)) {
                            var apiUrl = Config.GetUpdateTaskUrl();
                            log.Info("Update Task Calling Api");
                            client = new HttpClient();
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenModel.access_token);
                            HttpResponseMessage response = await client.GetAsync(apiUrl);
                            //HttpContent responseContent2 = response.Content;
                            //using (var reader = new StreamReader(await responseContent2.ReadAsStreamAsync())) {
                            //    var result1 = reader.ReadToEnd();
                            //}
                            log.Info("Update Task Calling Api complete");
                            return response.StatusCode;
                        }
                    }
                }
            } catch (Exception ex) {
                log.Error(ex.Message);
            }
            return HttpStatusCode.NotFound;
        }


        private static void MarkComplete(MessageQueueModel model) {
            using (var s = NHibernate.HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    try {
                        log.Info("Update data [MessageQueue] to DB--start");
                        var getMessage = s.QueryOver<MessageQueue>().Where(x => x.IdentifierId == model.Identifier
                       && x.UserName == model.UserName
                       && x.Status == MessageQueueStatus.Start.ToString()
                       ).SingleOrDefault();
                        if (getMessage != null) {
                            getMessage.Status = MessageQueueStatus.Complete.ToString();
                            s.Update(getMessage);
                            tx.Commit();
                            log.Info("Update data [MessageQueue] to DB--start");
                        }
                    } catch (Exception ex) {
                        tx.Rollback();
                        log.Error(ex.Message);
                        throw ex;
                    }
                    s.Flush();
                }
            }

        }

        private static void LogDetails(string message, string type) {
            try {
                Console.WriteLine(message + " " + type);
                string errorLogPath = @"c:\\TestFile\\AmzonSDK_err_log.txt";
                File.AppendAllText(errorLogPath, Environment.NewLine + type + "==>:" + message + "_" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss"));
            } catch (Exception) {
            }
        }

        private static async Task<List<MessageQueueModel>> GetMessages() {
            List<MessageQueueModel> list = new List<MessageQueueModel>();
            AmazonSQS amazonSQS = new AmazonSQS();
            var getMessages = await amazonSQS.ReceiveMessage();
            foreach (var item in getMessages) {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageQueueModel>(item.Body);
                model.ReceiptHandle = item.ReceiptHandle;
                list.Add(model);
            }
            return list;
        }

        private static async Task DeleteMessage(string ReceiptHandler) {
            AmazonSQS amazonSQS = new AmazonSQS();
            await amazonSQS.DeleteMessage(ReceiptHandler);
        }

        internal static class AsyncHelper {
            private static readonly TaskFactory _myTaskFactory = new
              TaskFactory(CancellationToken.None,
                          TaskCreationOptions.None,
                          TaskContinuationOptions.None,
                          TaskScheduler.Default);

            public static TResult RunSync<TResult>(Func<Task<TResult>> func) {
                return AsyncHelper._myTaskFactory
                  .StartNew<Task<TResult>>(func)
                  .Unwrap<TResult>()
                  .GetAwaiter()
                  .GetResult();
            }

            public static void RunSync(Func<Task> func) {
                AsyncHelper._myTaskFactory
                  .StartNew<Task>(func)
                  .Unwrap()
                  .GetAwaiter()
                  .GetResult();
            }
        }
    }
}
