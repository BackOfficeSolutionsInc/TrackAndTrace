
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.CoreProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmazonSDK {

	public class AmazonSQS {
		private string queueURL = Config.GetAppSetting("SQS_QueueURL");
		AmazonSQSClient amazonSQSClient;
		public AmazonSQS() {
			amazonSQSClient = new AmazonSQSClient(Config.GetAppSetting("SQS_AccessKey"), Config.GetAppSetting("SQS_SecretKey"), RegionEndpoint.USWest2);
		}

		public async Task<bool> SendMessage(MessageQueueModel model) {
			bool result = false;
			try {
				//string msg = "This is test message new.";
				string message = Newtonsoft.Json.JsonConvert.SerializeObject(model);
				SendMessageRequest messageRequest = new SendMessageRequest(queueURL, message);
				AmazonSQSClient amazonSQSClient = new AmazonSQSClient(Config.GetAppSetting("SQS_AccessKey"), Config.GetAppSetting("SQS_SecretKey"), RegionEndpoint.USWest2);
				SendMessageResponse sendMessageResponse = await amazonSQSClient.SendMessageAsync(messageRequest);
				if (sendMessageResponse.HttpStatusCode == System.Net.HttpStatusCode.OK) {
					result = true;
				}
			} catch (Exception ex) {
				throw ex;
			}
			return result;
		}

		public async Task<List<MessageModel>> ReceiveMessage() {
			string msg = string.Empty;
			try {
				ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
				receiveMessageRequest.QueueUrl = queueURL;
				receiveMessageRequest.MaxNumberOfMessages = 5;
				ReceiveMessageResponse result = await amazonSQSClient.ReceiveMessageAsync(receiveMessageRequest);
				return result.Messages.Select(s => new MessageModel() { Body = s.Body, MessageId = s.MessageId, ReceiptHandle = s.ReceiptHandle }).ToList();
			} catch (Exception ex) {
				throw ex;
			}
		}


		public async Task<bool> DeleteMessage(string receiptHandler) {
			bool result = false;
			try {
				DeleteMessageRequest deleteMessageRequest = new DeleteMessageRequest();

				deleteMessageRequest.QueueUrl = queueURL;
				deleteMessageRequest.ReceiptHandle = receiptHandler;

				DeleteMessageResponse response = await amazonSQSClient.DeleteMessageAsync(deleteMessageRequest);
				if (response.HttpStatusCode == System.Net.HttpStatusCode.OK) {
					result = true;
				}
			} catch (Exception ex) {
				throw ex;
			}
			return result;
		}
	}
}
