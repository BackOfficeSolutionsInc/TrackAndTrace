﻿using Newtonsoft.Json.Linq;
using NHibernate;
using PushSharp.Apple;
using PushSharp.Google;
using RadialReview.Models;
using RadialReview.Models.Notifications;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public class NotificationAccessor {


		public static async Task<UserDevice> TryRegisterPhone(string userName, string deviceId, string deviceType, string deviceVersion) {
			if (userName == null || deviceId == null || deviceType == null || deviceVersion == null) {
				return null;
			}

			userName = userName.ToLower();

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var found = s.QueryOver<UserDevice>()
						.Where(x => x.DeleteTime == null && x.DeviceId == deviceId && x.UserName == userName)
						.SingleOrDefault();

					if (found == null) {
						found = new UserDevice {
							DeviceId = deviceId,
							UserName = userName,
							DeviceType = deviceType,
							DeviceVersion = deviceVersion,
						};
						s.Save(found);
					} else {
						found.LastUsed = DateTime.UtcNow;
						found.DeviceVersion = deviceVersion;
						s.Update(found);
					}

					tx.Commit();
					s.Flush();

					return found;
				}
			}
		}

		
		public static async Task CreateNotification_Unsafe(NotifcationCreation notification,bool send) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					await notification.Save(s);
					if (send) {
						await notification.Send(s);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}

	}

	public class NotifcationCreation {

		private string Message { get; set; }
		private string Details { get; set; }
		private string ImageUrl { get; set; }
		private long UserId { get; set; }
		private NotificationType Type { get; set; }
		private NotificationPriority Priority { get; set; }

		public static NotifcationCreation Build(long userId, string message, string details = null, string imageUrl = null) {
			return new NotifcationCreation {
				Message = message,
				Details = details,
				ImageUrl = imageUrl,
				UserId = userId,
			};
		}

		public async Task<NotificationModel> Save(ISession s) {
			var nm = GenerateModel();
			s.Save(nm);
			return nm;
		}

		protected NotificationModel GenerateModel() {
			return new NotificationModel {
				Details = Details,
				ImageUrl = ImageUrl,
				Name = Message,
				Priority = Priority,
				Type = Type,
				Sent =false,
				UserId = UserId,
			};
		}

		public async Task Send(ISession s) {
			var user = s.Get<UserOrganizationModel>(UserId);
			var userName = user.NotNull(x => x.GetUsername().ToLower());

			if (userName != null) {
				var devices = s.QueryOver<UserDevice>()
					.Where(x => x.DeleteTime == null && x.Ignore == false && x.UserName == userName)
					.List().ToList();

				foreach (var d in devices) {
					NotifcationCreation.SendToDevice(d,this);
				}
			}
		}

		public static void SendToDevice(UserDevice d, NotifcationCreation c) {
			c.SendToDevice(d);
		}

		protected void SendToDevice(UserDevice d) {
			switch (d.DeviceType) {
				case "android":
					SendToAndroid(new[] { d });
					break;
				case "ios":
					SendToIOS(new[] { d });
					break;
				default:
					break;
			}
		}

		protected void SendToAndroid(IEnumerable<UserDevice> d) {
			var config = new GcmConfiguration("GCM-SENDER-ID", "AUTH-TOKEN", null);
			config.GcmUrl = "https://fcm.googleapis.com/fcm/send";

		}

		protected void SendToIOS(IEnumerable<UserDevice> devices) {
			var cert = new X509Certificate2(Resources.AppleAPS.ToBytes());
			var config = new ApnsConfiguration(ApnsConfiguration.ApnsServerEnvironment.Sandbox, cert);
			var apnsBroker = new ApnsServiceBroker(config);


			apnsBroker.OnNotificationFailed += (notification, aggregateEx) => {
				aggregateEx.Handle(ex => {
					//// See what kind of exception it was to further diagnose
					//if (ex is ApnsNotificationException) {
					//	var notificationException = (ApnsNotificationException)ex;

					//	// Deal with the failed notification
					//	var apnsNotification = notificationException.Notification;
					//	var statusCode = notificationException.ErrorStatusCode;

					//	Console.WriteLine($"Apple Notification Failed: ID={apnsNotification.Identifier}, Code={statusCode}");

					//} else {
					//	// Inner exception might hold more useful information like an ApnsConnectionException			
					//	Console.WriteLine($"Apple Notification Failed for some unknown reason : {ex.InnerException}");
					//}

					// Mark it as handled
					return true;
				});
			};

			apnsBroker.OnNotificationSucceeded += (notification) => {
				Console.WriteLine("Apple Notification Sent!");
			};
			apnsBroker.Start();

			foreach (var deviceToken in devices) {
				// Queue a notification to send
				apnsBroker.QueueNotification(new ApnsNotification {
					DeviceToken = deviceToken.DeviceId,
					Payload = JObject.Parse(@"{""aps"":{""title"":""" + Message.NotNull(x => x.EscapeJSONString()) + @""",""body"":""" + Details.NotNull(x => x.EscapeJSONString()) + @"""}}"),
					Expiration = DateTime.UtcNow.AddDays(14),
				});
			}
			apnsBroker.Stop();

		}
	}
}