using System.EnterpriseServices;
using System.Text;
using log4net.Repository.Hierarchy;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Payments;
using RadialReview.Models.Periods;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Json;
using RadialReview.Models.Components;
using NHibernate;
using NHibernate.Cfg;
using RadialReview.Models.Synchronize;
using NHibernate.Criterion;
using System.Linq.Expressions;
using NHibernate.Impl;
using System.Threading;

namespace RadialReview.Controllers {

    /// <summary>
    /// DO NOT RENAME CONTROLLER. ChargeAccount is needed under Scheduler
    /// </summary>
    public class SchedulerController : BaseController {
        //
        // GET: /Scheduler/
        [Access(AccessLevel.Any)]
        public bool Index() {
            return true;
        }
		
		[Access(AccessLevel.Radial)]
		[AsyncTimeout(5000)]
		public async Task<ActionResult> Wait(CancellationToken ct, int seconds = 10, int timeout = 5) {
			await Task.Delay((int)(seconds * 1000));
			return Content("done " + DateTime.UtcNow.ToJsMs());
		}

		/// <summary>
		/// Do not change controller. 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="taskId"></param>
		/// <returns></returns>
		[Access(AccessLevel.Any)]
		[AsyncTimeout(20 * 60 * 1000)]
		public async Task<JsonResult> ChargeAccount(CancellationToken ct, long id, long taskId/*,long? executeTime=null*/) {
            PaymentException capturedPaymentException = null;
            Exception capturedException = null;
            //DateTime? time = null;
            //if (executeTime != null)
            //	time = executeTime.Value.ToDateTime();
            //decimal amt = 0;
            try {
                var result = await PaymentAccessor.ChargeOrganization(id, taskId, false);
                //amt = result.amount_settled;
            } catch (PaymentException e) {
                capturedPaymentException = e;
            } catch (FallthroughException e) {
                log.Error("FallthroughException", e);
                Response.StatusCode = 501;
                var type = PaymentExceptionType.Fallthrough;
                if (capturedPaymentException != null)
                    type = capturedPaymentException.Type;
                return Json(new {
                    charged = false,
                    payment_exception = true,
                    error = type,
                    message = e.NotNull(x => x.Message) ?? "Exception was null"
                }, JsonRequestBehavior.AllowGet);
            } catch (Exception e) {
                capturedException = e;
            }

            if (capturedPaymentException != null) {

                try {
                    using (var s = HibernateSession.GetCurrentSession()) {
                        using (var tx = s.BeginTransaction()) {
                            s.Save(PaymentErrorLog.Create(capturedPaymentException, taskId));
                            tx.Commit();
                            s.Flush();
                        }
                    }
                } catch (Exception e) {
                    log.Error("FatalPaymentException", e);
                }
                log.Error("PaymentException", capturedPaymentException);
                try {
                    var orgName = capturedPaymentException.OrganizationName + "(" + capturedPaymentException.OrganizationId + ")";
                    var trace = capturedPaymentException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
                    var email = Mail.To(EmailTypes.PaymentException, ProductStrings.PaymentExceptionEmail)
                        .Subject(EmailStrings.PaymentException_Subject, orgName)
                        .Body(EmailStrings.PaymentException_Body, capturedPaymentException.Message, "<b>" + capturedPaymentException.Type + "</b> for '" + orgName + "'  ($" + capturedPaymentException.ChargeAmount + ") at " + capturedPaymentException.OccurredAt + " [TaskId=" + taskId + "]", trace);

                    await Emailer.SendEmail(email, true);
                } catch (Exception e) {
                    log.Error("FatalPaymentException1", e);
                }
                Response.StatusCode = 501;
                return Json(new {
                    charged = false,
                    payment_exception = true,
                    error = capturedPaymentException.Type
                }, JsonRequestBehavior.AllowGet);
            }
            if (capturedException != null) {
                log.Error("Exception during Payment", capturedException);
                try {
                    var trace = capturedException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
                    var email = Mail.To(EmailTypes.PaymentException, ProductStrings.ErrorEmail)
                        .Subject(EmailStrings.PaymentException_Subject, "{Non-payment exception}")
                        .Body(EmailStrings.PaymentException_Body, capturedException.NotNull(x => x.Message), "{Non-payment}", trace, "[Id=" + id + "] --  [TaskId=" + taskId + "]");

                    await Emailer.SendEmail(email, true);
                } catch (Exception e) {
                    log.Error("FatalPaymentException2", e);
                }
                Response.StatusCode = 500;
                return Json(new {
                    charged = false,
                    payment_exception = false,
                    error = capturedException.NotNull(x => x.Message)
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new {
                charged = true,
                //amount = amt
            }, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Any)]
		[AsyncTimeout(60 * 60 * 1000)]
		public async Task<ActionResult> EmailTodos(int currentTime, CancellationToken ct, int divisor = 13, int remainder = 0, int sent = 0, string error = null,double duration=0) {
            if (remainder >= divisor) {
                return Content("Sent:" + sent+"<br/>Duration:"+duration+"s");
            }
            var start = DateTime.UtcNow;

            if (divisor <= 0)
                divisor = 1;

            var unsent = new List<Mail>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var nowUtc = DateTime.UtcNow;
                    if (nowUtc.DayOfWeek == DayOfWeek.Saturday || nowUtc.DayOfWeek == DayOfWeek.Sunday)
                        return Content("No fire on weekend.");

                    var started = s.QueryOver<ScheduledTask>().Where(x => x.TaskName == ApplicationAccessor.DAILY_EMAIL_TODO_TASK && x.Started != null).List().ToList();
                    if (!started.Any())
                        if (!Config.IsLocal())
                            throw new PermissionsException("Task not started");

                    await TodoEmailHelpers._ConstructTodoEmails(currentTime, unsent, s, nowUtc, divisor, remainder);

                    tx.Commit();
                    s.Flush();
                }
            }
            try {
                log.Info("EmailTodos sending " + unsent.Count + " emails.");
                if (Config.IsLocal()) {
                    await Task.Delay(6000);// 100 * unsent.Count);
                } else {
                    await Emailer.SendEmails(unsent);
                }
                sent += unsent.Count;

            } catch (Exception e) {
                log.Error("EmailTodos (" + divisor + "," + remainder + ") error.", e);
                error = error ?? "";
                error += " | " + e.Message;
            }
            duration += (DateTime.UtcNow - start).TotalSeconds;

			//Give some other requests a chance to go.
			await Task.Delay(1500);

            return RedirectToAction("EmailTodos", new {
                currentTime = currentTime,
                divisor = divisor,
                remainder = remainder + 1,
                sent = sent,
                duration = duration
            });
        }

        public class TodoEmailHelpers {
            /// <summary>
            /// remainder < divisor
            /// </summary>
            /// <param name="currentTime"></param>
            /// <param name="unsent"></param>
            /// <param name="s"></param>
            /// <param name="nowUtc"></param>
            /// <param name="divisor"></param>
            /// <param name="remainder"></param>
            /// <returns></returns>
            public static async Task _ConstructTodoEmails(int currentTime, List<Mail> unsent, ISession s, DateTime nowUtc, int divisor, int remainder) {
                var tomorrow = nowUtc.Date.AddDays(2).AddTicks(-1);
                var rangeLow = nowUtc.Date.AddDays(-1);
                var rangeHigh = nowUtc.Date.AddDays(4).AddTicks(-1);
                var nextWeek = nowUtc.Date.AddDays(7);
                if (nowUtc.DayOfWeek == DayOfWeek.Friday)
                    rangeHigh = rangeHigh.AddDays(1);

                List<TodoModel> todos = _QueryTodoModulo(s, divisor, remainder, rangeLow, rangeHigh, nextWeek);

                var dictionary = new Dictionary<string, List<TodoModel>>();
                foreach (var t in todos.GroupBy(x => x.AccountableUser.NotNull(y => y.User.NotNull(z => z.Email)))) {
                    if (t.Key != null) {
                        dictionary.GetOrAddDefault(t.Key, x => new List<TodoModel>()).AddRange(t);
                    }
                }

                foreach (var userTodos in dictionary) {
                    await _ConstructTodoEmail(currentTime, unsent, nowUtc, userTodos.Value);
                }
            }
            
            public static List<TodoModel> _QueryTodoModulo(ISession s, long divisor, long remainder, DateTime rangeLow, DateTime rangeHigh, DateTime nextWeek) {
                return s.QueryOver<TodoModel>()
                                .Where(x => ((rangeLow <= x.DueDate && x.DueDate <= rangeHigh) || (x.CompleteTime == null && x.DueDate <= nextWeek)) && x.DeleteTime == null)
                                .Where(Restrictions.Eq(Projections.SqlFunction("mod", NHibernateUtil.Int64, Projections.Property<TodoModel>(x=>x.AccountableUserId), Projections.Constant(divisor)),remainder))
                                .List().ToList();
            }

            public static async Task _ConstructTodoEmail(int currentTime, List<Mail> unsent, DateTime nowUtc, List<TodoModel> userTodos) {
                string subject = null;
                var nowLocal = userTodos.First().Organization.ConvertFromUTC(nowUtc).Date;

                var overDue = userTodos.Count(x => x.DueDate.Date <= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);
                if (overDue == 1)
                    subject = "You have an overdue to-do";
                else if (overDue > 1)
                    subject = "You have " + overDue + " overdue to-dos";
                else {
                    var dueToday = userTodos.Count(x => x.DueDate.Date == nowLocal.Date && x.CompleteTime == null);

                    if (dueToday == 1)
                        subject = "You have a to-do due today";
                    else if (dueToday > 1)
                        subject = "You have " + dueToday + " to-dos due today";
                    else {
                        var dueTomorrow = userTodos.Count(x => x.DueDate.Date == nowLocal.AddDays(1).Date && x.CompleteTime == null);
                        if (dueTomorrow == 1)
                            subject = "You have a to-do due tomorrow";
                        else if (dueTomorrow > 1)
                            subject = "You have " + dueTomorrow + " to-dos due tomorrow";
                        else {
                            var dueSoon = userTodos.Count(x => x.DueDate.Date > nowLocal.AddDays(1).Date && x.CompleteTime == null);
                            if (dueSoon == 1)
                                subject = "You have a to-do due soon";
                            else if (dueSoon > 1)
                                subject = "You have " + dueSoon + " to-dos due soon";
                        }
                    }
                }

                var shouldSend = userTodos.Count(x => x.DueDate.Date >= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);

                if (subject != null && shouldSend > 0) {

                    try {
                        var user = userTodos.First().AccountableUser;

                        if ((user.User.NotNull(x => x.SendTodoTime)) == currentTime) {
                            var email = user.GetEmail();

                            var builder = new StringBuilder();
                            foreach (var t in userTodos.Where(x => x.CompleteTime == null || x.DueDate.Date > nowUtc.Date).GroupBy(x => x.ForRecurrenceId)) {
                                var table = await TodoAccessor.BuildTodoTable(t.ToList(), t.First().ForRecurrence.NotNull(x => x.Name + " To-do"));
                                builder.Append(table);
                                builder.Append("<br/>");
                            }

                            var mail = Mail.To(EmailTypes.DailyTodo, email)
                                .Subject(EmailStrings.TodoReminder_Subject, subject)
                                .Body(EmailStrings.TodoReminder_Body,
                                    user.GetName(),
                                    builder.ToString(),
                                    Config.ProductName(user.Organization),
                                    Config.BaseUrl(user.Organization) + "Todo/List"
                                );

                            unsent.Add(mail);
                        }
                    } catch (Exception) {

                    }
                }
            }
        }

        [Access(AccessLevel.Any)]
        public async Task<bool> Daily() {
            var any = false;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var orgs = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).List().ToList();

                    var tomorrow = DateTime.UtcNow.Date.AddDays(7);
                    foreach (var o in orgs) {
                        var o1 = o;
                        var period = s.QueryOver<PeriodModel>().Where(x => x.OrganizationId == o1.Id && x.DeleteTime == null && x.StartTime <= tomorrow && tomorrow < x.EndTime).List().ToList();

                        if (!period.Any()) {

                            var startOfYear = (int)o.Settings.StartOfYearMonth;

                            if (startOfYear == 0)
                                startOfYear = 1;

                            var start = new DateTime(tomorrow.Year - 2, startOfYear, 1);

                            //var curM = (int)o.Settings.StartOfYearMonth;
                            //var curY = tomorrow.Year;
                            //var last = 
                            var quarter = 0;
                            var prev = start;
                            while (true) {
                                start = start.AddMonths(3);
                                quarter += 1;
                                var tick = start.AddDateOffset(o.Settings.StartOfYearOffset);
                                if (tick > tomorrow) {
                                    break;
                                }
                                prev = start;
                            }

                            var p = new PeriodModel() {
                                StartTime = prev.AddDateOffset(o.Settings.StartOfYearOffset),
                                EndTime = start.AddDateOffset(o.Settings.StartOfYearOffset).AddDays(-1),
                                Name = prev.AddDateOffset(o.Settings.StartOfYearOffset).Year + " Q" + (((quarter + 3) % 4) + 1),// +3 same as -1
                                Organization = o,
                                OrganizationId = o.Id,
                            };

                            s.Save(p);
                            any = true;
                        }
                    }


                    #region Cleanup Sync model
                    try {
						{
							var syncTable = "Sync";
							s.CreateSQLQuery("delete from " + syncTable + " where CreateTime < \"" + DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd") + "\"")
							 .ExecuteUpdate();
						}
						{
							var syncTable = "SyncLock";
							s.CreateSQLQuery("delete from " + syncTable + " where LastUpdate < \"" + DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd") + "\"")
							 .ExecuteUpdate();
						}
					} catch (Exception e) {
                        log.Error(e);
                    }
                    #endregion

                    await EventUtil.GenerateAllDailyEvents(s, DateTime.UtcNow);

                    tx.Commit();
                    s.Flush();
                }
            }
            return any;
        }


        [Access(AccessLevel.Any)]
        [AsyncTimeout(60000 * 30)]//30 minutes..
        public async Task<JsonResult> Reschedule(CancellationToken ct) {
			//HttpContext.Server.ScriptTimeout = 20*60; // Twenty minutes..
			var res = await TaskAccessor.ExecuteTasks();
            return Json(res, JsonRequestBehavior.AllowGet);
        }


        [Access(AccessLevel.Any)]
        public async Task<JsonResult> Trigger(long id, EventType @event, decimal? arg1 = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    if (arg1 == null && @event == EventType.AccountAge_monthly) {
                        arg1 = (decimal)((DateTime.UtcNow - s.Get<OrganizationModel>(id).CreationTime).TotalDays / 30.4375);
                    }
                }
            }
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    await EventUtil.Trigger(x => x.Create(s, @event, null, id, ForModel.Create<OrganizationModel>(id), arg1: arg1));
                    tx.Commit();
                    s.Flush();
                }
            }
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
    }
   
}
