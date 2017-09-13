using AmazonSDK.NHibernate;
using LambdaSerializer;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Controllers;
using RadialReview.Hooks;
using RadialReview.Models;
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
		static void Main(string[] args) {
            while (true) {
                Console.WriteLine("Start: " + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss"));
                Thread t = new Thread(Scheduler);
                t.Start();
                Thread.Sleep(500);
            }
            //Scheduler();
        }

        private static void Scheduler() {
			LogDetails("Start", "INFO");
			List<string> receiptHandleList = new List<string>();
			List<MessageQueueModel> getMessages = AsyncHelper.RunSync<List<MessageQueueModel>>(() => GetMessages());  //get List of Messages
			LogDetails("Get list of messages", "INFO");

			foreach (var item in getMessages) {
				LogDetails("Loop start", "INFO");

				//Mark Started
				LogDetails("MarkStarted --> Start ", "INFO");
				MarkStarted(item);
				LogDetails("MarkStarted --> Complete ", "INFO");

				try {
					//Delete Message from SQS
					LogDetails("Delete Message from SQS --> Start ", "INFO");
					AsyncHelper.RunSync(() => DeleteMessage(item.ReceiptHandle));
					LogDetails("Delete Message from SQS --> Complete ", "INFO");

					//Process API
					LogDetails("ApiRequest --> Start ", "INFO");
					var status = AsyncHelper.RunSync<HttpStatusCode>(() => ApiRequest(item));
					LogDetails("ApiRequest --> Complete ", "INFO");

					// Mark Complete
                        if (status != HttpStatusCode.OK) {
                            throw new Exception("An error ocurred during HTTP Request." + " Status Code:" + status);
                        }
                    

                    // Mark Complete
						LogDetails("MarkComplete --> Start ", "INFO");
						MarkComplete(item);
						LogDetails("MarkComplete --> Complete ", "INFO");
				} catch (Exception ex) {
					AsyncHelper.RunSync(() => SendMessage(item));
					LogDetails(ex.Message, "ERROR");

				}
			}
		}

		private static void MarkStarted(MessageQueueModel model) {

			using (var s = HibernateSession.GetCurrentSession()) {
				LogDetails("session open", "INFO");
				using (var tx = s.BeginTransaction(System.Data.IsolationLevel.Serializable)) {
					LogDetails("transaction lock", "INFO");
					try {
						var getMessage = s.QueryOver<MessageQueue>().Where(x => x.IdentifierId == model.Identifier
						&& x.UserName == model.UserName
						&& x.Status == MessageQueueStatus.Start.ToString()
						).SingleOrDefault();
						LogDetails("Retreive data [MessageQueue] from DB", "INFO");

						if (getMessage == null) {
							MessageQueue messageQueue = new MessageQueue();
							messageQueue.IdentifierId = model.Identifier;
							messageQueue.ReceiptHandle = model.ReceiptHandle;
							messageQueue.UserOrgId = model.UserOrgId;
							messageQueue.UserName = model.UserName;
							messageQueue.Status = MessageQueueStatus.Start.ToString();
							s.Save(messageQueue);

							LogDetails("Save data [MessageQueue] to DB", "INFO");
							tx.Commit();
							s.Flush();
						}
					} catch (Exception ex) {
						tx.Rollback();
						s.Flush();
						LogDetails(ex.Message, "ERROR");
					}

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
                using (var s = HibernateSession.GetCurrentSession(true, "_RV")) {
                    LogDetails("session open", "INFO");
                    using (var tx = s.BeginTransaction()) {
				LogDetails("Generate token", "INFO");
				//get token
				string pwd = RadialReview.Utilities.Config.GetAppSetting("AMZ_secretkey").ToString() + model.UserName;
				string encrypt_key = Crypto.EncryptStringAES(pwd, RadialReview.Utilities.Config.GetAppSetting("AMZ_secretkey").ToString());

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
				var url = System.Configuration.ConfigurationManager.AppSettings["HostName"];
				var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(param) };
				TokenModel tokenModel = new TokenModel();
				HttpResponseMessage response1 = await client.SendAsync(req);

				LogDetails("Token process complete", "INFO");
				HttpContent responseContent1 = response1.Content;
				using (var reader = new StreamReader(await responseContent1.ReadAsStreamAsync())) {
					var result1 = reader.ReadToEnd();
					tokenModel = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenModel>(result1.ToString());
				}

				if (!string.IsNullOrEmpty(tokenModel.access_token)) {
					var apiUrl = model.ApiUrl ?? "";
					if (!string.IsNullOrEmpty(apiUrl)) {
						LogDetails("Calling Api", "INFO");
						client = new HttpClient();
						client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenModel.access_token);
						HttpResponseMessage response = await client.GetAsync(apiUrl);
						LogDetails("Calling Api complete", "INFO");
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
		private static void MarkComplete(MessageQueueModel model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				LogDetails("session open", "INFO");
				using (var tx = s.BeginTransaction(System.Data.IsolationLevel.Serializable)) {
					try {
						LogDetails("Update data [MessageQueue] to DB--start", "INFO");
						var getMessage = s.QueryOver<MessageQueue>().Where(x => x.IdentifierId == model.Identifier
					   && x.UserName == model.UserName
					   && x.Status == MessageQueueStatus.Start.ToString()
					   ).SingleOrDefault();
						if (getMessage != null) {
							getMessage.Status = MessageQueueStatus.Complete.ToString();
							s.Update(getMessage);
							tx.Commit();
							s.Flush();
							LogDetails("Update data [MessageQueue] to DB--start", "INFO");
						}
					} catch (Exception ex) {
						tx.Rollback();
						LogDetails(ex.Message, "ERROR");
						s.Flush();
						throw ex;
					}
				}
			}

		}

		private static void LogDetails(string message, string type) {
            try {
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
