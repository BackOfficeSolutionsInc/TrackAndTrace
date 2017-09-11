using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentNHibernate.Mapping;
using LambdaSerializer;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Models {
    public class AmazonSQSUtility {
        //private string profileName = "development";
        private static string accessKey = "AKIAIT7AXO7YMDBNMNRA";
        private static string secretKey = "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";
        //private string region = "us-west-2";
        private static string queueURL = "https://sqs.us-west-2.amazonaws.com/812229332029/TractionToolsQueue";

        public static async Task<bool> SendMessage(MessageQueueModel model) {
            bool result = false;
            try {
                string message = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                SendMessageRequest messageRequest = new SendMessageRequest(queueURL, message);
                AmazonSQSClient amazonSQSClient = new AmazonSQSClient(accessKey, secretKey, RegionEndpoint.USWest2);
                SendMessageResponse sendMessageResponse = await amazonSQSClient.SendMessageAsync(messageRequest);
                if (sendMessageResponse.HttpStatusCode == System.Net.HttpStatusCode.OK) {
                    result = true;
                }
            } catch (Exception ex) {
                throw ex;
            }
            return result;
        }
    }

    public enum RequestTypeEnum {
        isHookRegistryAction,
        isHTTPRequest
    }

    public class MessageQueueModel {
        public string Identifier { get; set; }
        public object Model { get; set; }
        public string ModelType { get; set; } // name of model
        public string ReceiptHandle { get; set; }
        public string ApiUrl { get; set; }
        public long? UserOrgId { get; set; }
        public string UserName { get; set; }
        public Type type { get; set; }
        public RequestTypeEnum RequestType { get; set; }

        public string SerializedModel { get; set; }


        public static MessageQueueModel CreateHTTPRequest<T>(T model, UserOrganizationModel caller, Uri api) {

            return new MessageQueueModel() {
                Identifier = Guid.NewGuid().ToString(), UserName = caller.GetUsername(), UserOrgId = caller.Id,
                Model = model, ModelType = model.GetType().FullName, ApiUrl = "" + api, type = model.GetType(), RequestType = RequestTypeEnum.isHTTPRequest
            };
        }

        public static MessageQueueModel CreateHookRegistryAction<T>(T model, SerializableHook serializableHook) {
            return new MessageQueueModel() {
                Identifier = Guid.NewGuid().ToString(), Model = model,
                ModelType = model.GetType().FullName, ApiUrl = null, type = model.GetType(), RequestType = RequestTypeEnum.isHookRegistryAction,
                SerializedModel = JsonNetAdapter.Serialize(serializableHook)
            };
        }
    }

    public class SerializableHook {
        public object lambda { get; set; }
        public Type type { get; set; }
    }

    public class TokenIdentifierModel {
        public virtual long Id { get; set; }
        public virtual string key { get; set; }
    }

    public class TokenIdentifierModelMap : ClassMap<TokenIdentifierModel> {
        public TokenIdentifierModelMap() {
            Id(x => x.Id);
            Map(x => x.key);
        }
    }

}