using FirebaseNet.Messaging;
using Newtonsoft.Json.Linq;
using NHibernate;
using PushSharp.Apple;
using PushSharp.Google;
using RadialReview.Models;
using RadialReview.Models.Notifications;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using RadialReview.Api.V1;
using RadialReview.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Exceptions;

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
        public static async Task SetNotificationStatus(UserOrganizationModel caller, long notificationId, NotificationStatus status) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var n = s.Get<NotificationModel>(notificationId);
                    perms.Self(n.UserId);
                    var oldStatus = n.GetStatus();
                    var updates = new INotificationHookUpdates();

                    if (oldStatus != status) {
                        updates.StatusChanged = true;
                        switch (status) {
                            case NotificationStatus.Unread:
                                n.DeleteTime = null;
                                n.Seen = null;
                                break;
                            case NotificationStatus.Read:
                                n.Seen = DateTime.UtcNow;
                                break;
                            case NotificationStatus.Delete:
                                n.DeleteTime = DateTime.UtcNow;
                                n.Seen = n.Seen ?? n.DeleteTime;
                                break;
                            default:
                                break;
                        }
                        s.Update(n);
                    }

                    await HooksRegistry.Each<INotificationHook>((ses, x) => x.UpdateNotification(ses, n, updates));

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static NotificationModel GetNotification(UserOrganizationModel caller, long notificationId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var notification = s.Get<NotificationModel>(notificationId);
                    perms.Self(notification.UserId);
                    return notification;
                }
            }
        }
        public static List<NotificationModel> GetNotificationsForUser(UserOrganizationModel caller, long userId, bool includeSeen = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);// must be self.. 
                    var uids = s.Get<UserOrganizationModel>(userId).User.UserOrganizationIds;
                    var notificationsQ = s.QueryOver<NotificationModel>()
                        .Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.UserId).IsIn(uids);
                    if (!includeSeen)
                        notificationsQ = notificationsQ.Where(x => x.Seen == null);
                    return notificationsQ.List().ToList();
                }
            }
        }

        public static async Task<NotificationModel> CreateNotification_Unsafe(NotifcationCreation notification, bool send) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var n = await notification.Save(s);
                    if (send) {
                        await notification.Send(s);
                    }
                    tx.Commit();
                    s.Flush();

                    return n;
                }
            }
        }

    }

    public class NotifcationCreation {

        private long NotificationId { get; set; }
        private string Message { get; set; }
        private string Details { get; set; }
        private string ImageUrl { get; set; }
        private long UserId { get; set; }
        private NotificationType Type { get; set; }
        private NotificationPriority Priority { get; set; }
        public bool Sensitive { get; private set; }

        public static NotifcationCreation Build(long userId, string message, string details = null, bool sensitive = true, string imageUrl = null) {
            return new NotifcationCreation {
                Message = message,
                Details = details,
                ImageUrl = imageUrl,
                UserId = userId,
                Sensitive = sensitive,
            };
        }

        public async Task<NotificationModel> Save(ISession s) {
            var nm = GenerateModel();
            s.Save(nm);
            NotificationId = nm.Id;
            return nm;
        }

        protected NotificationModel GenerateModel() {
            return new NotificationModel {
                Details = Details,
                ImageUrl = ImageUrl,
                Name = Message,
                Priority = Priority,
                Type = Type,
                Sent = null,
                UserId = UserId,
            };
        }

        public async Task Send(ISession s) {
            var user = s.Get<UserOrganizationModel>(UserId);
            var userName = user.NotNull(x => x.GetUsername().ToLower());

            if (userName != null) {
                var devices = s.QueryOver<UserDevice>()
                    .Where(x => x.DeleteTime == null && x.IgnoreDevice == false && x.UserName == userName)
                    .List().ToList();

                foreach (var d in devices) {
                    await NotifcationCreation.SendToDevice(d, this);
                }
            }
        }

        public static async Task SendToDevice(UserDevice d, NotifcationCreation c) {
            await c.SendToDevice(d);
        }

        protected async Task SendToDevice(UserDevice d) {
            await SendFCM(d);
            //switch (d.DeviceType) {
            //	case "android":
            //		SendToAndroid(new[] { d });
            //		break;
            //	case "ios":
            //		SendToIOS(new[] { d });
            //		break;
            //	default:
            //		break;
            //}
        }

        protected async Task SendFCM(UserDevice d) {
            try {
                var ServerApiKey = "AAAAzO3GfJk:APA91bEC5L0yOH5KWgeyXb1AYiFBOVRbyd_g-dZHvWj0PeDNNYtfN01B4bgTWgMSjBJ0x727U56GNr9rS64YMoZxZj7mPBO7fYr6u8h2xh98PxMzjT3bJb219bfvTIVMQJ0EO6Hc-CLR";
                string applicationID = "AIzaSyDVKKEEsYW_WdluXoJIqSICXfY-kXm0D40";
                string senderId = "880162536601";
                string deviceId = d.DeviceId;

                FCMClient client = new FCMClient(ServerApiKey);

                if (this.Sensitive == true && NotificationId == 0)
                    throw new PermissionsException("You must save the notification before sending");

                var message = new Message() {
                    To = deviceId,//"bk3RNwTe3H0:CI2k_HHwgIpoDKCIZvvDMExUdFQ3P1...",
                };

                if (Sensitive) {
                    message.Data = new Dictionary<string, string>{
                                { "MessageId", ""+this.NotificationId},
                                { "Type", ""+this.Type },
                                { "Priority", ""+this.Priority }
                            };
                } else {
                    message.Notification = new AndroidNotification() {
                        Body = this.Details,
                        Title = this.Message,
                        Icon = this.ImageUrl
                    };
                }
                var result = await client.SendMessageAsync(message);
                int i = 0;
            } catch (Exception ex) {
                string str = ex.Message;
                Console.WriteLine("error");
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