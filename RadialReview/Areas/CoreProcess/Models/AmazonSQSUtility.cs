﻿using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Models
{
    public class AmazonSQSUtility
    {
        //private string profileName = "development";
        private static string accessKey = "AKIAIT7AXO7YMDBNMNRA";
        private static string secretKey = "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";
        //private string region = "us-west-2";
        private static string queueURL = "https://sqs.us-west-2.amazonaws.com/812229332029/TractionToolsQueue";

        public static async Task<bool> SendMessage(MessageQueueModel model)
        {
            bool result = false;
            try
            {
                //string msg = "This is test message new.";
                string message = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                SendMessageRequest messageRequest = new SendMessageRequest(queueURL, message);
                AmazonSQSClient amazonSQSClient= new AmazonSQSClient(accessKey, secretKey, RegionEndpoint.USWest2);
                SendMessageResponse sendMessageResponse = await amazonSQSClient.SendMessageAsync(messageRequest);
                if (sendMessageResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
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

    public class MessageQueueModel
    {
        public Guid Identifier { get; set; }
        public object Model { get; set; }
        public string ModelType { get; set; } // name of model
        public string ReceiptHandle { get; set; }
    }
}