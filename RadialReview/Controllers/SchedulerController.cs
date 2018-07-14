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
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using System.Configuration;
using RadialReview.Crosscutting.EventAnalyzers;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.ScheduledJobs;

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
		[AsyncTimeout(130000)]
		public async Task<ActionResult> Wait(CancellationToken ct, bool with = true) {
			//	HttpContext.Server.ScriptTimeout = 130;
			if (with) {
				//get the Report Generation Timeout
				//int reportTimeout = Int32.Parse(ConfigurationManager.AppSettings["reportTimeout"]);
				//

				//if (itemsToShow == null)
				//	itemsToShow = 5;
				////var results = _imageReportService.GetAllReports().OrderByDescending(x => x.Id);
				//var results = _imageReportService.GetLastXReports((int)itemsToShow).OrderByDescending(x => x.Id);

				//var model = _mapper.Map<IEnumerable<ImageReport>, IEnumerable<ImageReportViewModel>>(results);
				//ViewBag.NumberOfImagesToReport = _imageReportService.GetNumberOfImageItemsAwaitingReporting();
				//return View(model);

				//System.Web.HttpContext.Current.GetType().GetField("_timeoutState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(System.Web.HttpContext.Current, 1);
			}
			await Task.Delay((int)(120000));
			return Content("reached " + DateTime.UtcNow.ToJsMs());
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
		public async Task<ActionResult> EmailTodos(int currentTime, CancellationToken ct, int divisor = 13, int remainder = 0, int sent = 0, string error = null, double duration = 0) {
			if (remainder >= divisor) {
				return Content("Sent:" + sent + "<br/>Duration:" + duration + "s");
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

					await TodoEmailsScheduler.TodoEmailHelpers._ConstructTodoEmails(currentTime, unsent, s, nowUtc, divisor, remainder);

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

	


		/// <summary>
		/// 
		/// </summary>
		/// <param name="frequency"></param>
		/// <returns></returns>
		[Access(AccessLevel.Any)]
		//DO NOT RENAME
		[AsyncTimeout(60 * 60 * 1000)]
		public async Task<bool> ExecuteEvents(EventFrequency frequency, long taskId) {
			await EventAccessor.ExecuteAll(frequency, taskId, DateTime.UtcNow);
			return true;
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
							s.CreateSQLQuery("delete from " + syncTable + " where LastUpdateDb < \"" + DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd") + "\"")
							 .ExecuteUpdate();
						}
					} catch (Exception e) {
						log.Error(e);
					}
					#endregion

					await EventUtil.GenerateAllDailyEvents(s, DateTime.UtcNow);

					await CheckCardExpirations(s);

					tx.Commit();
					s.Flush();
				}
			}
			return any;
		}

		private async Task CheckCardExpirations(ISession s) {
			var date = DateTime.UtcNow.Date;
			if (date == new DateTime(date.Year, date.Month, 1) || date == new DateTime(date.Year, date.Month, 15) || date == new DateTime(date.Year, date.Month, 21)) {
				var expireMonth = date.AddMonths(1);
				var tokens = s.QueryOver<PaymentSpringsToken>()
						.Where(x => x.Active && x.DeleteTime == null && x.TokenType == PaymentSpringTokenType.CreditCard && x.MonthExpire == expireMonth.Month && x.YearExpire == expireMonth.Year)
						.List().ToList();

				var tt = tokens.GroupBy(x => x.OrganizationId).Select(x => x.OrderByDescending(y => y.CreateTime).First());
				foreach (var t in tt)
					await HooksRegistry.Each<IPaymentHook>((ses, x) => x.CardExpiresSoon(ses, t));

			}
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
