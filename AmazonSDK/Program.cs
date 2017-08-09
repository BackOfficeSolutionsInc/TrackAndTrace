using AmazonSDK.NHibernate;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Areas.CoreProcess.Models.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmazonSDK
{
    class Program
    {
        static void Main(string[] args)
        {

            //TaskViewModel tskView = new TaskViewModel();
            //tskView.Assignee = "Test1";
            //tskView.description = "DescTest1";
            //tskView.name = "NameTest1";
            //tskView.Id = "Test1";

            //MessageQueueModel t1 = new MessageQueueModel();
            //t1.Identifier = Guid.NewGuid();
            //t1.Model = tskView;
            //t1.ModelType = "TaskViewModel";
            //var result = AsyncHelper.RunSync<bool>(() => AmazonSQSUtility.SendMessage(t1));
            List<string> receiptHandleList = new List<string>();
            List<MessageQueueModel> getMessages = AsyncHelper.RunSync<List<MessageQueueModel>>(() => GetMessages());  //get List of Messages
            using (var s = HibernateSession.GetCurrentSession())
            {
                foreach (var item in getMessages)
                {
                    var getMessage = s.QueryOver<MessageQueue>().Where(x => x.Identifier == item.Identifier).SingleOrDefault();
                    if (getMessage == null)
                    {
                        MessageQueue messageQueue = new MessageQueue();
                        messageQueue.Identifier = item.Identifier;
                        messageQueue.ReceiptHandle = item.ReceiptHandle;
                        messageQueue.Status = MessageQueueStatus.Start.ToString();
                        s.Save(messageQueue);
                    }
                }


                //Process Message

                //Update Status

            }

            // delete message

            //if (getMessage.Status == MessageQueueStatus.Complete.ToString())
            //{
            //    receiptHandleList.Add(item.ReceiptHandle);
            //}

            AsyncHelper.RunSync(() => DeleteMessage(receiptHandleList));

            Console.Write("");
            Console.ReadLine();
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
