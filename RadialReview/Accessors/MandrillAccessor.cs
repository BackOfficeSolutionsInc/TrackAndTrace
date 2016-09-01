using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.ElastiCache.Model;
using Mandrill;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using NHibernate.Criterion;

namespace RadialReview.Accessors {
    public class MandrillAccessor {
        public static void ProcessWebhooks(IEnumerable<WebHookEvent> events)
        {
            //using (var rt = RealTimeUtility.Create(true)) {
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        foreach (var e in events) {
                            ProcessWebhook(s, e);
                        }
                        var batch = events.Batch(250);
                        var res = new List<Tuple<WebHookEvent, EmailModel>>();
                        foreach (var b in batch) {
                            res.AddRange(ProcessBatchWebhook(s, b.ToList()));
                        }
                        var batch2 = res.Batch(250);
                        foreach (var b in batch2) {
                            ProcessBatchUpdateUsers(s, b.ToList());
                        }

                        tx.Commit();
                        s.Flush();
                    }
                }
           // }
        }


        public static void UpdateJoinOrganization(ISession s, WebHookEvent e, EmailModel email)
        {
            var tu = s.QueryOver<TempUserModel>()
                .WhereRestrictionOn(x => x.Email).IsInsensitiveLike(email.ToAddress)
                .List().FirstOrDefault();
            if (tu != null) {
                tu.EmailStatus = e.Event;

                var lu = s.QueryOver<UserLookup>().Where(x => x.UserId == tu.UserOrganizationId).List().FirstOrDefault();
                switch (e.Event) {
                    case WebHookEventType.Send: tu.EmailStatusUnseen = false; break;
                    case WebHookEventType.Hard_bounce: tu.EmailStatusUnseen = true; break;
                    case WebHookEventType.Soft_bounce: ; break;
                    case WebHookEventType.Open: ; break;
                    case WebHookEventType.Click: ; break;
                    case WebHookEventType.Spam: tu.EmailStatusUnseen = true; break;
                    case WebHookEventType.Unsub: tu.EmailStatusUnseen = true; break;
                    case WebHookEventType.Reject: tu.EmailStatusUnseen = true; break;
                    case WebHookEventType.Deferral: ; break;
                    case WebHookEventType.Inbound: ; break;
                    default: break;
                }
                if (lu != null) {
                    lu.EmailStatus = e.Event;
                    s.Update(lu);
                }
                s.Update(tu);
                //var message = MessageAccessor.GenerateManageMembersMessage(tu, e.Event);
                //if (tu.OrganizationId.HasValue && message != null && tu.LastSentByUserId > 0) {
                //    var username = s.Get<UserOrganizationModel>(tu.LastSentByUserId).GetUsername();
                //    hub.UpdateUser(username).showAlert(ResultObject.CreateError(message.Message, message));
                //}

            }
        }

        public static void ProcessBatchUpdateUsers(ISession s, List<Tuple<WebHookEvent, EmailModel>> evts)
        {
            var emails = evts.Select(x=>x.Item2.ToAddress.ToUpper()).ToList();

            var temps =s.QueryOver<TempUserModel>().Where(Restrictions.In(
                Projections.SqlFunction("upper", NHibernateUtil.String, Projections.Property<TempUserModel>(x => x.Email)),
                emails)).List().ToList();
            var userIds = temps.Select(x=>x.UserOrganizationId).ToList();
            var lus = s.QueryOver<UserLookup>().WhereRestrictionOn(x => x.UserId).IsIn(userIds).List().ToList();

            foreach (var tu in temps) {
                var tuples = evts.Where(x => x.Item2.ToAddress.ToUpper() == tu.Email.ToUpper());
				if(tuples.Any())
				{
					var tuple = tuples.First();
					var e = tuple.Item1;
					tu.EmailStatus = e.Event;
					switch (e.Event) {
						case WebHookEventType.Send: tu.EmailStatusUnseen = false; break;
						case WebHookEventType.Hard_bounce: tu.EmailStatusUnseen = true; break;
						case WebHookEventType.Soft_bounce: ; break;
						case WebHookEventType.Open: ; break;
						case WebHookEventType.Click: ; break;
						case WebHookEventType.Spam: tu.EmailStatusUnseen = true; break;
						case WebHookEventType.Unsub: tu.EmailStatusUnseen = true; break;
						case WebHookEventType.Reject: tu.EmailStatusUnseen = true; break;
						case WebHookEventType.Deferral: ; break;
						case WebHookEventType.Inbound: ; break;
						default: break;
					}

					var lu = lus.FirstOrDefault(x => x.UserId == tu.UserOrganizationId);
					if (lu != null) {
						lu.EmailStatus = e.Event;
						s.Update(lu);
					}
					s.Update(tu);
				}
            }

        }
        public static List<Tuple<WebHookEvent,EmailModel>> ProcessBatchWebhook(ISession s, List<WebHookEvent> evts)
        {
           // var hub = GlobalHost.ConnectionManager.GetHubContext<MessageHub>();
            var found = s.QueryOver<EmailModel>().WhereRestrictionOn(x => x.MandrillId)
                .IsIn(evts.Select(x=>x.Msg.Id).ToList())
                .List().ToList();
            var o = new List<Tuple<WebHookEvent, EmailModel>>();
            foreach (var f in found) {
                switch (f.EmailType) {
                    case EmailType.JoinOrganization:
                        var e = evts.First(x => x.Msg.Id == f.MandrillId);
                        o.Add(Tuple.Create(e, f));
                        //UpdateJoinOrganization(s, e, f); 
                        break;
                    default: break;
                }
            }
            return o;
        }


        public static void ProcessWebhook(ISession s, WebHookEvent e)
        {
            var hook = new EmailWebhookModel() {
                Clicks = e.Msg.Clicks.NotNull(x => x.Count),
                Opens = e.Msg.Opens.NotNull(x => x.Count),
                MandrillId = e.Msg.Id,
                TimeStamp = e.TimeStamp,
                EventType = e.Event
            };

            s.Save(hook);



            switch (e.Event) {
                case WebHookEventType.Send: break;
                case WebHookEventType.Hard_bounce: break;
                case WebHookEventType.Soft_bounce: break;
                case WebHookEventType.Open: break;
                case WebHookEventType.Click: break;
                case WebHookEventType.Spam: break;
                case WebHookEventType.Unsub: break;
                case WebHookEventType.Reject: break;
                case WebHookEventType.Deferral: break;
                case WebHookEventType.Inbound: break;
                default: break;
            }

        }
    }
}