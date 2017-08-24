
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmazonSDK
{

    public class AmazonSQS
    {
        //private string profileName = "development";
        private string accessKey = "AKIAIT7AXO7YMDBNMNRA";
        private string secretKey = "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";
        //private string region = "us-west-2";
        private string queueURL = "https://sqs.us-west-2.amazonaws.com/812229332029/TractionToolsQueue";
        AmazonSQSClient amazonSQSClient;
        public AmazonSQS()
        {
            amazonSQSClient = new AmazonSQSClient(accessKey, secretKey, RegionEndpoint.USWest2);
        }

        public async Task<List<MessageModel>> ReceiveMessage()
        {
            string msg = string.Empty;
            try
            {
                ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
                receiveMessageRequest.QueueUrl = queueURL;
                receiveMessageRequest.MaxNumberOfMessages = 5;
                ReceiveMessageResponse result =await amazonSQSClient.ReceiveMessageAsync(receiveMessageRequest);
                return result.Messages.Select(s=>new MessageModel() {Body=s.Body, MessageId=s.MessageId,ReceiptHandle=s.ReceiptHandle }).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> DeleteMessage(string receiptHandler)
        {
            bool result = false;
            try
            {
                DeleteMessageRequest deleteMessageRequest = new DeleteMessageRequest();

                deleteMessageRequest.QueueUrl = queueURL;
                deleteMessageRequest.ReceiptHandle = receiptHandler;

                DeleteMessageResponse response =await amazonSQSClient.DeleteMessageAsync(deleteMessageRequest);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
    }
}
