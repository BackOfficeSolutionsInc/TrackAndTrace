using AmazonSDK.NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Controllers;
using RadialReview.Models;
using RadialReview.Utilities.Encrypt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmazonSDK
{
    class Program
    {
        static void Main(string[] args)
        {
            //var client1 = new HttpClient();
            //var nvc = new List<KeyValuePair<string, string>>();
            //nvc.Add(new KeyValuePair<string, string>("username", "kunal@mytractiontools.com"));
            //nvc.Add(new KeyValuePair<string, string>("password", "Test123"));
            //nvc.Add(new KeyValuePair<string, string>("grant_type", "password"));
            //nvc.Add(new KeyValuePair<string, string>("client_id", "self"));
            //var url = "http://localhost:44300/token";
            //var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };

            //HttpResponseMessage response1 = AsyncHelper.RunSync<HttpResponseMessage>(() => client1.SendAsync(req));
            //HttpContent responseContent1 = response1.Content;
            //using (var reader = new StreamReader(AsyncHelper.RunSync<Stream>(() => responseContent1.ReadAsStreamAsync())))
            //{
            //    var result1 = reader.ReadToEnd();
            //}


            //AccountController accountController = new AccountController();
            //var uId = "ecef4e68-81ab-4d8a-805d-a75600e8dfeb";
            // AsyncHelper.RunSync(() => accountController.AuthenticateUser(uId));

            //using (var s = HibernateSession.GetCurrentSession(true, "_RV"))
            //{
            //var s1 = HibernateSession.GetCurrentSession(true, "_RV");
            //var resultp = s1.QueryOver<UserModel>().Where(x => x.DeleteTime == null && x.CurrentRole == 2).SingleOrDefault();
            //AsyncHelper.RunSync(() => accountController.AuthenticateUser(resultp));
            //Console.WriteLine("");
            //Console.ReadLine();
            // }


            //TaskViewModel tskView = new TaskViewModel();
            //tskView.Assignee = "Test1";
            //tskView.description = "DescTest1";
            //tskView.name = "NameTest1";
            //tskView.Id = "Test1";

            //MessageQueueModel t1 = new MessageQueueModel();
            //t1.Identifier = Guid.NewGuid().ToString();
            //t1.Model = tskView;
            //t1.ModelType = "TaskViewModel";
            // t1.ApiUrl=Config.BaseUrl

            //var result = AsyncHelper.RunSync<bool>(() => AmazonSQSUtility.SendMessage(t1));
            if (true)
            {
                List<string> receiptHandleList = new List<string>();
                List<MessageQueueModel> getMessages = AsyncHelper.RunSync<List<MessageQueueModel>>(() => GetMessages());  //get List of Messages

                foreach (var item in getMessages)
                {
                    using (var s = HibernateSession.GetCurrentSession())
                    {
                        using (var tx = s.BeginTransaction(System.Data.IsolationLevel.Serializable))
                        {

                            var getMessage = s.QueryOver<MessageQueue>().Where(x => x.IdentifierId == item.Identifier).SingleOrDefault();

                            if (getMessage == null)
                            {
                                MessageQueue messageQueue = new MessageQueue();
                                messageQueue.IdentifierId = item.Identifier;
                                messageQueue.ReceiptHandle = item.ReceiptHandle;
                                messageQueue.Status = MessageQueueStatus.Start.ToString();
                                s.Save(messageQueue);

                                tx.Commit();
                                s.Flush();

                                // if true while Process Message
                                // run API methods in Radial

                                try
                                {

                                    var session = HibernateSession.GetCurrentSession(true, "_RV");
                                    var result = session.QueryOver<UserModel>().Where(x => x.DeleteTime == null && x.CurrentRole == item.UserId).SingleOrDefault();

                                    //AsyncHelper.RunSync(() => accountController.AuthenticateUser(result));

                                    session.Flush();
                                    session.Dispose();

                                    //get token
                                    string pwd = RadialReview.Utilities.Config.GetAppSetting("AMZ_secretkey").ToString() + result.UserName;
                                    string encrypt_key = Crypto.EncryptStringAES(pwd, RadialReview.Utilities.Config.GetAppSetting("AMZ_secretkey").ToString());

                                    var client = new HttpClient();
                                    var param = new List<KeyValuePair<string, string>>();
                                    param.Add(new KeyValuePair<string, string>("username", result.UserName));
                                    param.Add(new KeyValuePair<string, string>("password", encrypt_key));
                                    param.Add(new KeyValuePair<string, string>("grant_type", "password"));
                                    param.Add(new KeyValuePair<string, string>("client_id", "self"));
                                    var url = "http://localhost:44300/token";
                                    var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(param) };
                                    TokenModel tokenModel = new TokenModel();
                                    HttpResponseMessage response1 = AsyncHelper.RunSync<HttpResponseMessage>(() => client.SendAsync(req));
                                    HttpContent responseContent1 = response1.Content;
                                    using (var reader = new StreamReader(AsyncHelper.RunSync<Stream>(() => responseContent1.ReadAsStreamAsync())))
                                    {
                                        var result1 = reader.ReadToEnd();
                                        tokenModel = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenModel>(result1.ToString());
                                    }


                                    if (!string.IsNullOrEmpty(tokenModel.access_token))
                                    {
                                        var apiUrl = item.ApiUrl ?? "http://localhost:44300/api/v0/todo/mine";
                                        if (!string.IsNullOrEmpty(apiUrl))
                                        {
                                           // string getAccessToken = "Px_gH-grY_zNAzAVPDQ-8hYoKeNNsZ51EQPON-OE6v4rjz0ZpCaIgnY6WkcVMW85RXP9hkZsgy-R0yMoTQtfjuS9evkzAffcYuZZDuHyhwYkgJ6yqxo3bIfnk_2dsLME10BxW2WZDT-wyxb3Qs7PFELx0isnbzkkCJ-jvN5xEjYKCJpYfgVJO_ZBpm1x6vv8fH0SqMlUT9VNyFEJ8EbyRhWWvLWMSMH4yA18GyKW-qGUtpLc4we2IM09tnACjJUoPaAQ_0x_NPXGystBMvyPXHNRIsMAg6hx9WJgCjYYOdu7VfPXoEzWcDT9pC2KU9pZ-wE7UI6TjktCHPsWc4_P8fTKWOgrbQEWHZYJJTQX-ElkSU9vGifFWDElBF4NR2ULeZ1k1WWRLoIbTr_orHEu0JGGiMLaaMVn0uwvrLTBUp1fs2NCNdqEF__TcjaSORoyZCU2nUqUc9FYGdQTo8Ao7O6R-e4wuchiuXHjKomcTOoIJ0Y3HzFExGKS8OoUXNJI";
                                            client = new HttpClient();
                                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenModel.access_token);
                                            HttpResponseMessage response = AsyncHelper.RunSync<HttpResponseMessage>(() => client.GetAsync(apiUrl));
                                            HttpContent responseContent = response.Content;
                                            using (var reader = new StreamReader(AsyncHelper.RunSync<Stream>(() => responseContent.ReadAsStreamAsync())))
                                            {
                                                var result1 = reader.ReadToEnd();
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw;
                                }

                                //Update Status
                            }
                            else
                            {

                            }
                        }
                    }
                }

                // delete message

                //if (getMessage.Status == MessageQueueStatus.Complete.ToString())
                //{
                //    receiptHandleList.Add(item.ReceiptHandle);
                //}

                //AsyncHelper.RunSync(() => DeleteMessage(receiptHandleList));

                Console.Write("");
                Console.ReadLine();
            }
        }

        private static async Task<List<MessageQueueModel>> GetMessages()
        {
            List<MessageQueueModel> list = new List<MessageQueueModel>();
            AmazonSQS amazonSQS = new AmazonSQS();
            var getMessages = await amazonSQS.ReceiveMessage();
            foreach (var item in getMessages)
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageQueueModel>(item.Body);
                model.ReceiptHandle = item.ReceiptHandle;
                list.Add(model);
            }
            return list;
        }

        private static async Task DeleteMessage(List<string> ReceiptHandler)
        {
            AmazonSQS amazonSQS = new AmazonSQS();
            foreach (var item in ReceiptHandler)
            {
                await amazonSQS.DeleteMessage(item);
            }
        }

        internal static class AsyncHelper
        {
            private static readonly TaskFactory _myTaskFactory = new
              TaskFactory(CancellationToken.None,
                          TaskCreationOptions.None,
                          TaskContinuationOptions.None,
                          TaskScheduler.Default);

            public static TResult RunSync<TResult>(Func<Task<TResult>> func)
            {
                return AsyncHelper._myTaskFactory
                  .StartNew<Task<TResult>>(func)
                  .Unwrap<TResult>()
                  .GetAwaiter()
                  .GetResult();
            }

            public static void RunSync(Func<Task> func)
            {
                AsyncHelper._myTaskFactory
                  .StartNew<Task>(func)
                  .Unwrap()
                  .GetAwaiter()
                  .GetResult();
            }
        }
    }
}
