using Amazon.SQS.Model;
using AmazonSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Models
{
    public class AmazonSQSUtility
    {
        public async static Task SendMessage()
        {
            TestModel t1 = new TestModel();
            t1.Id = 2;
            t1.Name = "Test";
            string message = Newtonsoft.Json.JsonConvert.SerializeObject(t1);
            AmazonSQS amazonSQS = new AmazonSQS();
            var result = await amazonSQS.SendMessage(message);
        }

        public async static Task ReceiveMessage()
        {
            TestModel t1 = new TestModel();
            t1.Id = 2;
            t1.Name = "Test";
            string message = Newtonsoft.Json.JsonConvert.SerializeObject(t1);

            AmazonSQS amazonSQS = new AmazonSQS();
            var result = await amazonSQS.ReceiveMessage();
            if (result.Count != 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].Body == message)
                    {
                        var res = Newtonsoft.Json.JsonConvert.DeserializeObject<TestModel>(result[i].Body);
                    }
                }
            }
        }

        public async static Task DeleteMessage(string receiptHandle)
        {
            AmazonSQS amazonSQS = new AmazonSQS();
            var resp = await amazonSQS.DeleteMessage(receiptHandle);
        }
    }

    public class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}